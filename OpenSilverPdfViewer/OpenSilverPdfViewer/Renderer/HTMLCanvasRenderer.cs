
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

using OpenSilverPdfViewer.Utility;
using CSHTML5.Native.Html.Controls;

namespace OpenSilverPdfViewer.Renderer
{
    internal sealed class HTMLCanvasRenderer : RenderStrategyBase
    {
        #region Fields / Properties

        private Color _thumbnailFillColor;
        private Color _thumbnailStrokeColor;
        private Color _thumbnailFontColor;

        private readonly Dictionary<int, BlobElement> _pageImageCache = new Dictionary<int, BlobElement>();
        private readonly HtmlCanvas renderCanvas;
        private readonly Debouncer _thumbnailTimer = new Debouncer(50);

        private ThreadedRenderQueue<BlobElement> RenderQueue { get; set; }
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
                    .Where(child => child is ContainerElement)
                    .Select(elem => int.Parse(elem.Name))
                    .Sum();

                // Things need to be added and/or removed from the viewport if the checksums differ
                return renderedChecksum != intersectChecksum;
            }
        }

        #endregion Fields / Properties
        #region Initialization

        public HTMLCanvasRenderer(HtmlCanvas canvas) 
        {
            renderCanvas = canvas;

            RenderQueue = new ThreadedRenderQueue<BlobElement>(RenderWorkerCallback);
            if (ThumbnailUpdate != ThumbnailUpdateType.WhenRendered)
                RenderQueue.QueueCompletedCallback = RenderQueueCompleted;

            _thumbnailFillColor = (Color)renderCanvas.FindResource("CMSCtrlNormalStartColor");
            _thumbnailStrokeColor = (Color)renderCanvas.FindResource("CMSForegroundColor");
            _thumbnailFontColor = (Color)renderCanvas.FindResource("CMSForegroundColor");
        }

        #endregion Initialization
        #region Methods

        protected override async Task<int> RenderCurrentPage()
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
            {
                var renderScale = _renderDPI / _nativePdfDpi;
                image = await PdfJs.GetPdfPageBlobElementAsync(RenderPageNumber, renderScale);

                if (!_pageImageCache.ContainsKey(RenderPageNumber))
                    _pageImageCache.Add(RenderPageNumber, image);
            }
            var pagePosition = GetPagePosition();
            image.X = pagePosition.X;
            image.Y = pagePosition.Y;
            image.Scale = GetDisplayScale();
            renderCanvas.Children.Clear();
            renderCanvas.Children.Add(image);
            renderCanvas.Draw();

            return RenderPageNumber;
        }
        protected override void RenderThumbnails()
        {
            RenderThumbnails(false);
        }
        private void RenderThumbnails(bool deferredDraw = false)
        {
            var scrollRect = ViewportScrollRect;
            var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect)).ToList();

            // Re-position any existing items in the viewport if the layout has changed due to panel resizing
            intersectList.ForEach(rect =>
            {
                var thumbnail = renderCanvas.Children.FirstOrDefault(child => int.Parse(child.Name) == rect.Id);
                if (thumbnail != null)
                {
                    thumbnail.X = rect.X;
                    thumbnail.Y = rect.Y;
                }
            });

            // Get a list of page image ids that are currently rendered in the viewport
            var renderedIds = renderCanvas.Children
                .Where(child => child is ContainerElement)
                .Select(pageElem => int.Parse(pageElem.Name));

            // Remove all page image elements that do not exist in the current intersection list from the viewport
            renderedIds
                .Except(intersectList.Select(rect => rect.Id))
                .ToList()
                .ForEach(id => {
                    RenderQueue.DequeueItem(id);
                    renderCanvas.Children.Remove(renderCanvas.Children.FirstOrDefault(child => int.Parse(child.Name) == id));
                });

            // Get a list of ids from the intersection list that are NOT currently rendered in the viewport
            var addIds = intersectList
                .Select(rect => rect.Id)
                .Except(renderedIds);

            if (addIds.Count() > 0)
            {
                // Add those page image elements that now need to be rendered
                var addList = addIds
                    .Select(id => intersectList.FirstOrDefault(item => item.Id == id))
                    .Where(rect => rect != null);

                // Render the new additions
                foreach (var rect in addList)
                    renderCanvas.Children.Add(CreateThumbnail(rect));
            }
            if (deferredDraw == false)
                renderCanvas.Draw();
        }
        private ContainerElement CreateThumbnail(LayoutRect rect)
        {
            var pageRect = new ContainerElement
            {
                Width = rect.Width,
                Height = rect.Height,
                FillColor = _thumbnailFillColor,
                StrokeColor = _thumbnailStrokeColor,
                StrokeThickness = 1d,
                Name = rect.Id.ToString()
            };
            
            // Render cached thumbnnail image
            if (_pageImageCache.TryGetValue(rect.Id, out var image))
                pageRect.Children.Add(image);
            
            else // Or draw a placeholder and queue the item for image rendering
            {
                var text = $"Page {rect.Id}";
                var metrics = PdfJs.GetTextMetrics(text, $"bold {_thumbnailFontSize}px {_thumbnailFont}");
                var pageNumberText = new TextElement
                {
                    FillColor = _thumbnailFontColor,
                    Font = _thumbnailFont,
                    FontWeight = FontWeights.Bold,
                    FontHeight = _thumbnailFontSize,
                    X = (rect.Width - metrics.BoundingBoxRight) / 2,
                    Y = (rect.Height - _thumbnailFontSize) / 2,
                    Text = text
                };
                pageRect.Children.Add(pageNumberText);
                RenderQueue.QueueItem(rect.Id, _thumbnailScale);
            }
            pageRect.X = rect.X;
            pageRect.Y = rect.Y;

            return pageRect;
        }

        // The ThreadedRenderQueue invokes this when an image thumbnail completes rendering
        private void RenderWorkerCallback(int pageNumber, BlobElement image, bool _)
        {
            if (image != null && renderCanvas.Children.FirstOrDefault(elem => elem is ContainerElement container && int.Parse(container.Name) == pageNumber) is ContainerElement pageThumbnail)
            {
                // This test should always pass since the cache shouldn't ever contain the image here
                // as CreateThumbnail shouldn't be queueing items if they've been previously cached
                if (_pageImageCache.ContainsKey(pageNumber) == false)
                    _pageImageCache.Add(pageNumber, image);

                // Replace the text placeholder with the rendered page image
                if (ThumbnailUpdate == ThumbnailUpdateType.WhenRendered && pageThumbnail.Children.FirstOrDefault() is TextElement textPlaceholder)
                {
                    pageThumbnail.Children.Remove(textPlaceholder);
                    pageThumbnail.Children.Add(image);
                    renderCanvas.Draw();
                }
            }
        }
        private void RenderQueueCompleted()
        {
            var placeHolders = renderCanvas.Children
                .Where(child => child is ContainerElement container && container.Children[0] is TextElement)
                .Cast<ContainerElement>()
                .ToList();

            _thumbnailTimer.OnSettled = () =>
            {
                var index = ThumbnailUpdate == ThumbnailUpdateType.Random ? new Random().Next(placeHolders.Count) : 0;
                var placeHolder = placeHolders[index];
                placeHolders.RemoveAt(index);

                if (_pageImageCache.TryGetValue(int.Parse(placeHolder.Name), out var image))
                {
                    placeHolder.Children.Clear();
                    placeHolder.Children.Add(image);
                    renderCanvas.Draw();
                }
                if (placeHolders.Count > 0)
                    _thumbnailTimer.Reset();
            };
            _thumbnailTimer.Reset();
        }

        #endregion Methods
        #region Interface Implementation

        public override void ScrollViewport(int scrollX, int scrollY)
        {
            if (_scrollPosition.X == scrollX && _scrollPosition.Y == scrollY) return;

            _scrollPosition.X = scrollX;
            _scrollPosition.Y = scrollY;

            if (ViewMode == ViewModeType.ThumbnailView)
            {
                if (ViewportItemsChanged)
                    RenderThumbnails(true);

                renderCanvas.Children
                    .Where(child => child is ContainerElement)
                    .ToList()
                    .ForEach(container =>
                    {
                        var layoutRect = LayoutRectList.FirstOrDefault(rect => rect.Id == int.Parse(container.Name));
                        if (layoutRect != null)
                        {
                            container.X = layoutRect.X - _scrollPosition.X;
                            container.Y = layoutRect.Y - _scrollPosition.Y;
                        }
                    });
            }
            else
            {
                if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
                    throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

                image.X = -_scrollPosition.X;
                image.Y = -_scrollPosition.Y;
            }
            var count = renderCanvas.Children.Count;
            renderCanvas.Draw();
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
            if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
                throw new Exception($"GetLayoutSize: No image found in cache for page {RenderPageNumber}");

            return new Size(image.Width, image.Height);
        }
        public override void ClearViewport()
        {
            renderCanvas.Children.Clear();
            renderCanvas.Draw();
        }
        public override void Reset()
        {
            ClearViewport();
            foreach (var pageImage in _pageImageCache.Values)
                pageImage.Invalidate();
            _pageImageCache.Clear();
            renderCanvas.Children.Clear();
            renderCanvas.Draw();
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
