
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
    internal sealed class OSCanvasRenderer : RenderStrategyBase
    {
        #region Fields / Properties

        private Brush _thumbnailFillBrush;
        private Brush _thumbnailStrokeBrush;
        private Brush _thumbnailFontBrush;
        private FontFamily _thumbnailFontFamily = new FontFamily(_thumbnailFont);

        private readonly Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
        private readonly Canvas renderCanvas;
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

        #endregion Fields / Properties
        #region Initialization

        public OSCanvasRenderer(Canvas canvas)
        {
            renderCanvas = canvas;
            RenderQueue = new RenderQueue<Image>(RenderWorkerCallback);

            _thumbnailFontBrush = renderCanvas.FindResource("CMSForegroundBrush") as Brush;
            _thumbnailFillBrush = renderCanvas.FindResource("CMSPopupBorderBrush") as Brush;
            _thumbnailStrokeBrush = renderCanvas.FindResource("CMSForegroundBrush") as Brush;
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
            var pageRect = new Grid
            {
                Width = rect.Width,
                Height = rect.Height,
                Tag = rect.Id
            };

            // Use the actual cached thumbnail image
            if (_pageImageCache.TryGetValue(rect.Id, out var image))
            {
                image.Disconnect();
                pageRect.Children.Add(image);
            }

            else // Or create a placeholder and queue the item for rendering
            {
                var borderRect = new Border
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Background = _thumbnailFillBrush,
                    BorderBrush = _thumbnailStrokeBrush,
                    BorderThickness = new Thickness(1d)
                };
                var pageNumberText = new TextBlock
                {
                    Foreground = _thumbnailFontBrush,
                    FontFamily = _thumbnailFontFamily,
                    FontWeight = FontWeights.Bold,
                    FontSize = _thumbnailFontSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Text = $"Page {rect.Id.ToString(CultureInfo.InvariantCulture)}"
                };
                borderRect.Child = pageNumberText;
                pageRect.Children.Add(borderRect);
                RenderQueue.QueueItem(rect.Id, _thumbnailScale);
            }
            pageRect.SetValue(Canvas.LeftProperty, rect.X);
            pageRect.SetValue(Canvas.TopProperty, rect.Y);
            
            return pageRect;
        }

        // The RenderQueue invokes this when an image thumbnail completes rendering
        private void RenderWorkerCallback(int pageNumber, Image image, bool _)
        {
            if (image != null && renderCanvas.Children.FirstOrDefault(elem => elem is Grid grid && (int)grid.Tag == pageNumber) is Grid pageThumbnail)
            {
                // This test should always pass since the cache shouldn't ever contain the image here
                // as CreateThumbnail shouldn't be queueing items if they've been previously cached
                if (_pageImageCache.ContainsKey(pageNumber) == false)
                    _pageImageCache.Add(pageNumber, image);

                // Replace the placeholder with the actual page image thumbnail
                pageThumbnail.Children.Clear();
                pageThumbnail.Children.Add(image);
            }
        }

        #endregion Methods
        #region Interface Implementation

        public override void ScrollViewport(int scrollX, int scrollY)
        {
            _scrollPosition.X = scrollX;
            _scrollPosition.Y = scrollY;

            TranslateTransform translate = new TranslateTransform { X = -_scrollPosition.X, Y = -_scrollPosition.Y };

            if (ViewMode == ViewModeType.ThumbnailView)
            {
                if (ViewportItemsChanged)
                    RenderThumbnails();

                renderCanvas.Children
                    .Where(child => child is Grid)
                    .ToList()
                    .ForEach(grid => grid.RenderTransform = translate);
            }
            else
            {
                if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
                    throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

                translate = (image.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
                translate.X = -_scrollPosition.X;
                translate.Y = -_scrollPosition.Y;
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
        public override void Reset()
        {
            ClearViewport();
            _pageImageCache.Clear();
        }
        public override bool IsPageloaded(int pageNumber)
        {
            return _pageImageCache.ContainsKey(pageNumber);
        }

        #endregion Interface Implementation
    }
}
