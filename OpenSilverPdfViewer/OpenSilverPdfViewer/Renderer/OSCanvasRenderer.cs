
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Renderer
{
    internal class OSCanvasRenderer : RenderStrategyBase
    {
        #region Fields / Properties

        private readonly Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
        private readonly Canvas renderCanvas;
        private Point _scrollPoint = new Point(0, 0);
        private RenderQueue<Image> RenderQueue { get; set; }
        public bool ViewportItemsChanged
        {
            get
            {
                var scrollRect = ViewportScrollRect;
                var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect));
                var intersectChecksum = intersectList
                    .Select(rect => rect.Id)
                    .Sum();

                var renderedChecksum = renderCanvas.Children
                    .Where(child => child is Grid)
                    .Select(elem => (int)((Grid)elem).Tag)
                    .Sum();

                // Things need to be added and/or removed from the viewport if the checksums differ
                return renderedChecksum != intersectChecksum;
            }
        }
        public Rect ViewportScrollRect
        {
            get
            {
                // Inflate the viewport intersection a bit so that the page image pop-in isn't so obvious
                var viewportScrollRect = new Rect(_scrollPoint, GetViewportSize());
                viewportScrollRect.Inflate(0, _scrollBufferZone);
                return viewportScrollRect;
            }
        }

        #endregion Fields / Properties
        #region Initialization

        public OSCanvasRenderer(Canvas canvas)
        {
            renderCanvas = canvas;
            RenderQueue = new RenderQueue<Image>(RenderWorkerCallback);
        }

        #endregion Initialization
        #region Methods

        protected override async Task<int> RenderCurrentPage()
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
            {
                var renderScale = _renderDPI / _nativePdfDpi;
                image = await PdfJs.GetPdfPageImageAsync(RenderPageNumber, renderScale);
                _pageImageCache.Add(RenderPageNumber, image);
            }

            var displayScale = GetDisplayScale();
            var pagePosition = GetPagePosition();
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform() { ScaleX = displayScale, ScaleY = displayScale });
            transform.Children.Add(new TranslateTransform() { X = pagePosition.X, Y = pagePosition.Y });
            image.RenderTransform = transform;

            renderCanvas.Children.Clear();
            renderCanvas.Children.Add(image);

            return RenderPageNumber;
        }
        protected override void RenderThumbnails()
        {
            var pageFont = new FontFamily("Verdana");
            var fontBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            var fillBrush = renderCanvas.FindResource("CMSCtrlDisabledBodyBrush") as Brush;

            var scrollRect = ViewportScrollRect;
            var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect)).ToList();

            // Re-position any existing items in the viewport if the layout has changed
            intersectList.ForEach(rect => 
            {
                var thumbnail = renderCanvas.Children.FirstOrDefault(child => child is Grid grid && (int)grid.Tag == rect.Id);
                if (thumbnail != null)
                {
                    thumbnail.SetValue(Canvas.LeftProperty, rect.X);
                    thumbnail.SetValue(Canvas.TopProperty, rect.Y);
                }
            });
            
            // Get a list of page image ids that are currently rendered in the viewport
            var renderedIds = renderCanvas.Children
                .Where(child => child is Grid)
                .Cast<Grid>()
                .Select(pageElem => (int)pageElem.Tag);
            
            // Remove all page image elements that do not exist in the current intersection list from the viewport
            renderedIds
                .Except(intersectList.Select(rect => rect.Id))
                .ToList()
                .ForEach(id => {
                    RenderQueue.DequeueItem(id);
                    renderCanvas.Children.Remove(renderCanvas.Children.FirstOrDefault(child => child is Grid grid && (int)grid.Tag == id));
                });

            // Get a list of ids from the intersection list that are NOT currently rendered in the viewport
            var addIds = intersectList
                .Select(rect => rect.Id)
                .Except(renderedIds).ToList();

            // Return if there's nothing new to render
            if (addIds.Count == 0) return;

            // Add those page image elements that now need to be rendered
            var addList = addIds
                .Select(id => intersectList.FirstOrDefault(item => item.Id == id))
                .Where(rect => rect != null)
                .ToList();

            // Render the new additions
            foreach (var rect in addList)
                renderCanvas.Children.Add(CreateThumbnail(rect));
        }
        private Grid CreateThumbnail(LayoutRect rect)
        {
            var pageFont = new FontFamily("Verdana");
            var fontBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            var fillBrush = renderCanvas.FindResource("CMSCtrlDisabledBodyBrush") as Brush;

            var pageRect = new Grid
            {
                Width = rect.Width,
                Height = rect.Height,
                Tag = rect.Id
            };
            var borderRect = new Border
            {
                Width = rect.Width,
                Height = rect.Height,
                Background = fillBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00))
            };

            if (_pageImageCache.TryGetValue(rect.Id, out var image))
            {
                image.Disconnect();
                borderRect.Child = image;
            }
            else
            {
                var pageNumberText = new TextBlock
                {
                    Foreground = fontBrush,
                    FontFamily = pageFont,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Text = $"Rendering Page {rect.Id.ToString(CultureInfo.InvariantCulture)}"
                };
                borderRect.Child = pageNumberText;
                RenderQueue.QueueItem(rect.Id, _thumbnailScale);
            }

            pageRect.SetValue(Canvas.LeftProperty, rect.X);
            pageRect.SetValue(Canvas.TopProperty, rect.Y);
            pageRect.Children.Add(borderRect);
            
            return pageRect;
        }
        private void RenderWorkerCallback(int pageNumber, Image image, bool _)
        {
            if (image != null && renderCanvas.Children.FirstOrDefault(elem => elem is Grid grid && (int)grid.Tag == pageNumber) is Grid pageThumbnail)
            {
                // This *should* always be false here since CreateThumbnail shouldn't be queueing render
                // items if they've been previously cached
                if (!_pageImageCache.ContainsKey(pageNumber))
                    _pageImageCache.Add(pageNumber, image);

                if (pageThumbnail.Children.FirstOrDefault() is Border border)
                    border.Child = image;
            }
        }

        #endregion Methods
        #region Interface Implementation

        public override void ScrollViewport(int scrollX, int scrollY)
        {
            _scrollPoint.X = scrollX;
            _scrollPoint.Y = scrollY;

            TranslateTransform translate = new TranslateTransform { X = -_scrollPoint.X, Y = -_scrollPoint.Y };

            if (ViewMode == ViewModeType.ThumbnailView)
            {
                if (ViewportItemsChanged)
                    RenderThumbnails();

                var thumbnailElements = renderCanvas.Children.Where(child => child is Grid).ToList();
                thumbnailElements.ForEach(grid => grid.RenderTransform = translate);
            }
            else
            {
                if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
                    throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

                translate = (image.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
                translate.X = -scrollX;
                translate.Y = -scrollY;
            }
        }
        public override Size GetViewportSize()
        {
            return new Size(renderCanvas.ActualWidth, renderCanvas.ActualHeight);
        }
        public override Size GetLayoutSize()
        {
            if (ViewMode == ViewModeType.ThumbnailView)
            {
                var unscaledWidth = LayoutRect.Width / _thumbnailScale;
                var unscaledHeight = LayoutRect.Height / _thumbnailScale;
                return new Size(unscaledWidth, unscaledHeight);
            }
            if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
                throw new Exception($"GetPageImageSize: No image found in cache for page {RenderPageNumber}");

            return new Size(image.Width, image.Height);
        }
        public override void ClearViewport()
        {
            renderCanvas.Children.Clear();
        }
        public override void InvalidatePageCache()
        {
            ClearViewport();
            _pageImageCache.Clear();
        }
        
        #endregion Interface Implementation
    }
}
