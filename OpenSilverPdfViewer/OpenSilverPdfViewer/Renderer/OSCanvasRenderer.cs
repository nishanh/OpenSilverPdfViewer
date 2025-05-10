
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

        private readonly Brush _thumbnailFillBrush;
        private readonly Brush _thumbnailStrokeBrush;
        private readonly Brush _thumbnailFontBrush;
        private readonly FontFamily _thumbnailFontFamily = new FontFamily(_thumbnailFont);

        private readonly Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
        private readonly Canvas _renderCanvas;
        private readonly Debouncer _thumbnailTimer = new Debouncer(50);

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

                var renderedChecksum = _renderCanvas.Children
                    .Where(child => child is Grid)
                    .Select(elem => (int)((Grid)elem).Tag)
                    .Sum();

                // Things need to be added and/or removed from the viewport if the checksums differ
                return renderedChecksum != intersectChecksum;
            }
        }

        #endregion Fields / Properties
        #region Initialization

        public OSCanvasRenderer(Panel canvasContainer)
        {
            if (canvasContainer.Children.FirstOrDefault(child => child is Canvas) is Canvas canvas)
            {
                _renderCanvas = canvas;

                RenderQueue = new RenderQueue<Image>(RenderWorkerCallback);
                if (ThumbnailUpdate != ThumbnailUpdateType.WhenRendered)
                    RenderQueue.QueueCompletedCallback = RenderQueueCompleted;

                _thumbnailFontBrush = _renderCanvas.FindResource("CMSForegroundBrush") as Brush;
                _thumbnailFillBrush = _renderCanvas.FindResource("CMSPopupBorderBrush") as Brush;
                _thumbnailStrokeBrush = _renderCanvas.FindResource("CMSForegroundBrush") as Brush;
            }
            else
                throw new Exception("OSCanvasRenderer ctor: the canvas container must contain a Canvas element");
        }

        #endregion Initialization
        #region Methods

        protected override async Task<int> RenderCurrentPage()
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
            {
                var renderScale = _renderDPI / _nativePdfDpi;
                image = await PdfJs.GetPdfPageImageAsync(RenderPageNumber, renderScale);

                if (!_pageImageCache.ContainsKey(RenderPageNumber))
                    _pageImageCache.Add(RenderPageNumber, image);
            }

            var displayScale = GetDisplayScale();
            var pagePosition = GetPagePosition();
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform() { ScaleX = displayScale, ScaleY = displayScale });
            transform.Children.Add(new TranslateTransform() { X = pagePosition.X, Y = pagePosition.Y });
            image.RenderTransform = transform;

            _renderCanvas.Children.Clear();
            _renderCanvas.Children.Add(image);

            return RenderPageNumber;
        }
        protected override void RenderThumbnails()
        {
            var scrollRect = ViewportScrollRect;
            var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect));

            // Re-position any existing items in the viewport if the layout has changed
            intersectList.ForEach(rect => 
            {
                var thumbnail = _renderCanvas.Children.FirstOrDefault(child => child is Grid grid && (int)grid.Tag == rect.Id);
                if (thumbnail != null)
                {
                    thumbnail.SetValue(Canvas.LeftProperty, rect.X);
                    thumbnail.SetValue(Canvas.TopProperty, rect.Y);
                }
            });
            
            // Get a list of page image ids that are currently rendered in the viewport
            var renderedIds = _renderCanvas.Children
                .Where(child => child is Grid)
                .Cast<Grid>()
                .Select(pageElem => (int)pageElem.Tag);
            
            // Remove all page image elements that do not exist in the current intersection list from the viewport
            renderedIds
                .Except(intersectList.Select(rect => rect.Id))
                .ToList()
                .ForEach(id => {
                    RenderQueue.DequeueItem(id);
                    _renderCanvas.Children.Remove(_renderCanvas.Children.FirstOrDefault(child => child is Grid grid && (int)grid.Tag == id));
                });

            // Get a list of ids from the intersection list that are NOT currently rendered in the viewport
            var addIds = intersectList
                .Select(rect => rect.Id)
                .Except(renderedIds)
                .ToList();

            // Return if there's nothing new to render
            if (addIds.Count == 0) return;

            // Add those page image elements that now need to be rendered
            addIds
                .Select(id => intersectList.FirstOrDefault(item => item.Id == id))
                .Where(rect => rect != null)
                .ForEach(rect => _renderCanvas.Children.Add(CreateThumbnail(rect)));
        }
        private Grid CreateThumbnail(LayoutRect rect)
        {
            var pageRect = new Grid
            {
                Width = rect.Width,
                Height = rect.Height,
                Tag = rect.Id,
                Name = $"thumbnail-{rect.Id}"
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
            if (image != null && _renderCanvas.Children.FirstOrDefault(elem => elem is Grid grid && (int)grid.Tag == pageNumber) is Grid pageThumbnail)
            {
                // This test should always pass since the cache shouldn't ever contain the image here
                // as CreateThumbnail shouldn't be queueing items if they've been previously cached
                if (_pageImageCache.ContainsKey(pageNumber) == false)
                    _pageImageCache.Add(pageNumber, image);

                if (ThumbnailUpdate == ThumbnailUpdateType.WhenRendered)
                {
                    // Replace the placeholder with the actual page image thumbnail
                    pageThumbnail.Children.Clear();
                    pageThumbnail.Children.Add(image);
                }
            }
        }

        // The RenderQueue invokes this when all images in the queue have completed rendering
        private void RenderQueueCompleted()
        {
            var placeHolders = _renderCanvas.Children
                .Where(child => child is Grid grid && grid.Children[0] is Border border && border.Child is TextBlock)
                .Cast<Grid>()
                .ToList();

            if (AnimateThumbnails)
            {
                var borderList = placeHolders
                    .Select(child => child.Children[0])
                    .Cast<Border>()
                    .ToList();

                for (var i = 0; i < placeHolders.Count; i++)
                {
                    var thumb = placeHolders[i];
                    if (_pageImageCache.TryGetValue((int)thumb.Tag, out var image))
                        borderList[i].Tag = image;
                }
                FireRenderCompleteEvent(placeHolders);
            }
            else
            {
                _thumbnailTimer.OnSettled = () =>
                {
                    var index = ThumbnailUpdate == ThumbnailUpdateType.Random ? new Random().Next(placeHolders.Count) : 0;
                    var placeHolder = placeHolders[index];
                    placeHolders.RemoveAt(index);

                    if (_pageImageCache.TryGetValue((int)placeHolder.Tag, out var image))
                    {
                        var border = placeHolder.Children[0] as Border;
                        border.BorderThickness = new Thickness(0);
                        border.Child = image;
                    }
                    if (placeHolders.Count > 0)
                        _thumbnailTimer.Reset();
                };
                _thumbnailTimer.Reset();
            }
        }

        #endregion Methods
        #region Interface Implementation

        public override void ScrollViewport(int scrollX, int scrollY)
        {
            if (_scrollPosition.X == scrollX && _scrollPosition.Y == scrollY) return;

            _scrollPosition.X = scrollX;
            _scrollPosition.Y = scrollY;

            var translate = new TranslateTransform { X = -_scrollPosition.X, Y = -_scrollPosition.Y };

            if (ViewMode == ViewModeType.ThumbnailView)
            {
                if (ViewportItemsChanged)
                    RenderThumbnails();

                _renderCanvas.Children
                    .Where(child => child is Grid)
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
            return new Size(_renderCanvas.ActualWidth, _renderCanvas.ActualHeight);
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
            _renderCanvas.Children.Clear();
        }
        public override void Reset()
        {
            ClearViewport();
            _pageImageCache.Clear();
        }
        public override void SetThumbnailUpdateType(ThumbnailUpdateType thumbnailUpdateType)
        {
            ThumbnailUpdate = thumbnailUpdateType;
            if (ThumbnailUpdate == ThumbnailUpdateType.WhenRendered)
                RenderQueue.QueueCompletedCallback = null;
            else if (RenderQueue.QueueCompletedCallback == null)
                RenderQueue.QueueCompletedCallback = RenderQueueCompleted;
        }

        #endregion Interface Implementation
    }
}
