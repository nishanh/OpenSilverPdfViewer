
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenSilverPdfViewer.Utility;
using System.Windows.Controls;
using CSHTML5.Native.Html.Controls;

namespace OpenSilverPdfViewer.Renderer
{
    internal sealed class DomCanvasRenderer : RenderStrategyBase
    {
        #region Fields / Properties

        private readonly string viewCanvasId;
        private readonly Dictionary<int, JSImageReference> _pageImageCache = new Dictionary<int, JSImageReference>();
        private List<int> _renderedIdList = new List<int>();
        private readonly Debouncer _thumbnailTimer = new Debouncer(50);

        private RenderQueue<JSImageReference> RenderQueue { get; set; }
        public bool ViewportItemsChanged
        {
            get
            {
                var scrollRect = ViewportScrollRect;
                var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect));
                var intersectChecksum = intersectList
                    .Select(rect => rect.Id)
                    .Sum();

                var renderedChecksum = _renderedIdList.Sum();

                // Things need to be added and/or removed from the viewport if the checksums differ
                return renderedChecksum != intersectChecksum;
            }
        }

        #endregion Fields / Properties
        #region Initialization

        public DomCanvasRenderer(Panel canvasContainer)
        {
            if (canvasContainer.Children.FirstOrDefault(child => child is HtmlPresenter) is HtmlPresenter presenter)
            {
                var domElement = presenter.Html;
                if (!domElement.StartsWith("<canvas"))
                    throw new Exception("DomCanvasRenderer ctor: the HTMLPresenter html element must be a canvas element");

                var match = Regex.Match(domElement, "id=\"([^\"]+)\"");
                viewCanvasId = match.Success ? match.Groups[1].Value : string.Empty;
                if (string.IsNullOrEmpty(viewCanvasId))
                    throw new Exception("DomCanvasRenderer ctor: the HTMLPresenter html element must have a valid id");

                RenderQueue = new RenderQueue<JSImageReference>(RenderWorkerCallback);
                if (ThumbnailUpdate != ThumbnailUpdateType.WhenRendered)
                    RenderQueue.QueueCompletedCallback = RenderQueueCompleted;
            }
            else
                throw new Exception("DomCanvasRenderer ctor: the canvas container must contain an HTMLPresenter");
        }

        #endregion Initialization
        #region Methods

        protected override async Task<int> RenderCurrentPage()
        {
            var pageNumber =  await PdfJs.RenderPageToViewportAsync(RenderPageNumber, (int)_renderDPI, RenderZoomLevel, viewCanvasId);

            if (!_pageImageCache.ContainsKey(pageNumber))
                _pageImageCache.Add(pageNumber, new JSImageReference(pageNumber, CacheStatus.Cached));

            return pageNumber;
        }
        protected override void RenderThumbnails()
        {
            var scrollRect = ViewportScrollRect;
            var intersectList = LayoutRectList.Where(rect => rect.Intersects(scrollRect)).ToList();

            // Remove all page image elements that do not exist in the current intersection list from the viewport
            _renderedIdList
                .Except(intersectList.Select(rect => rect.Id))
                .ToList()
                .ForEach(id => RenderQueue.DequeueItem(id));

            var updatedRenderIdList = _renderedIdList.Intersect(intersectList.Select(rect => rect.Id)).ToList();

            // Get a list of ids from the intersection list that are NOT currently rendered in the viewport
            var addIds = intersectList
                .Select(rect => rect.Id)
                .Except(_renderedIdList).ToList();

            updatedRenderIdList.AddRange(addIds);

            // If the Id sums are the same, then there's no change to what is being shown in the viewport
            if (updatedRenderIdList.Sum() == _renderedIdList.Sum())
                return;

            // Add those page image elements that now need to be rendered
            _renderedIdList = updatedRenderIdList;
            var renderRectList = _renderedIdList
                .Select(id => intersectList.FirstOrDefault(item => item.Id == id))
                .ToList();

            ClearViewport();

            foreach (var rect in renderRectList)
            {
                // This will render a placeholder or the actual page image thumbnail if cached
                PdfJs.RenderThumbnailToViewport(rect.Id, rect.X - _scrollPosition.X, rect.Y - _scrollPosition.Y, rect.Width, rect.Height, viewCanvasId);

                // Queue the item for rendering if not previously cached
                if (_pageImageCache.ContainsKey(rect.Id) == false)
                    RenderQueue.QueueItem(rect.Id, _thumbnailScale);
            }
        }

        // The RenderQueue invokes this when an image thumbnail completes rendering
        private void RenderWorkerCallback(int pageNumber, JSImageReference image, bool _)
        {
            if (image.Status != CacheStatus.None)
            {
                if (_renderedIdList.Contains(pageNumber))
                {
                    // This test should always pass since the cache shouldn't ever contain the image here
                    // as CreateThumbnail shouldn't be queueing items if they've been previously cached
                    if (_pageImageCache.ContainsKey(pageNumber) == false)
                        _pageImageCache.Add(pageNumber, image);

                    if (ThumbnailUpdate == ThumbnailUpdateType.WhenRendered && image.Status != CacheStatus.Exists)
                    {
                        // Replace the text placeholder with the rendered page image
                        var rect = LayoutRectList.Single(rc => rc.Id == pageNumber);
                        PdfJs.RenderThumbnailToViewport(rect.Id, rect.X - _scrollPosition.X, rect.Y - _scrollPosition.Y, rect.Width, rect.Height, viewCanvasId);
                    }
                }
            }
        }
        private void RenderQueueCompleted()
        {
            var placeHolderArray = new int[_renderedIdList.Count];
            _renderedIdList.CopyTo(placeHolderArray);
            List<int> placeHolders = placeHolderArray.ToList();

            _thumbnailTimer.OnSettled = () =>
            {
                var index = ThumbnailUpdate == ThumbnailUpdateType.Random ? new Random().Next(placeHolders.Count) : 0;
                var placeHolderId = placeHolders[index];
                placeHolders.RemoveAt(index);

                if (_pageImageCache.TryGetValue(placeHolderId, out var image))
                {
                    if (image.Status != CacheStatus.Exists)
                    {
                        var rect = LayoutRectList.Single(rc => rc.Id == placeHolderId);
                        PdfJs.RenderThumbnailToViewport(rect.Id, rect.X - _scrollPosition.X, rect.Y - _scrollPosition.Y, rect.Width, rect.Height, viewCanvasId);
                        image.Status = CacheStatus.Exists;
                    }
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
                    RenderThumbnails();

                else
                {
                    var renderRectList = LayoutRectList.Where(rect => _renderedIdList.Contains(rect.Id)).ToList();
                    ClearViewport();
                    foreach (var rect in renderRectList)
                        PdfJs.RenderThumbnailToViewport(rect.Id, rect.X - _scrollPosition.X, rect.Y - _scrollPosition.Y, rect.Width, rect.Height, viewCanvasId);
                }
            }
            else
            {
                PdfJs.ScrollViewportImage(RenderPageNumber, viewCanvasId, RenderZoomLevel, (int)_scrollPosition.X, (int)_scrollPosition.Y);
            }
        }
        public override Size GetViewportSize()
        {
            return PdfJs.GetViewportSize(viewCanvasId);
        }
        public override Size GetLayoutSize()
        {
            if (ViewMode == ViewModeType.ThumbnailView)
            {
                var unscaledWidth = LayoutRect.Width / _thumbnailScale;
                var unscaledHeight = LayoutRect.Height / _thumbnailScale;
                return new Size(unscaledWidth, unscaledHeight);
            }
            return PdfJs.GetPageImageSize(RenderPageNumber);
        }
        public override void ClearViewport()
        {
            PdfJs.ClearViewport(viewCanvasId);
        }
        public override void Reset()
        {
            ClearViewport();
            PdfJs.InvalidatePageCache();
            PdfJs.InvalidateThumbnailCache();
            _pageImageCache.Clear();
            _renderedIdList.Clear();
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
