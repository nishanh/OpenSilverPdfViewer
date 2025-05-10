
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Renderer
{
    public interface IRenderStrategy
    {
        int RenderPageNumber { get; set; }
        int RenderZoomLevel { get; set; }
        bool AnimateThumbnails { get; set; }
        ThumbnailSize ThumbnailSize { get; set; }
        Task Render(ViewModeType viewMode);
        void ScrollViewport(int scrollX, int scrollY);
        double GetDisplayScale();
        double GetPixelsToInchesConversion();
        Size GetViewportSize();
        Size GetLayoutSize();
        Point GetPagePosition();
        void ClearViewport();
        void Reset();
        Task SetPageSizeRunList();
        int GetViewportPageAtPoint(Point point);
        void SetThumbnailUpdateType(ThumbnailUpdateType thumbnailUpdateType);

        event RenderCompleteEventHandler RenderCompleteEvent;
    }
    public static class RenderStrategyFactory
    {
        public static IRenderStrategy Create(RenderModeType renderMode, Panel canvasContainer)
        {
            if (renderMode == RenderModeType.Dom)
                return new DomCanvasRenderer(canvasContainer);
            else if (renderMode == RenderModeType.OpenSilver)
                return new OSCanvasRenderer(canvasContainer);
            else
                return new HTMLCanvasRenderer(canvasContainer);
        }
    }
    public abstract class RenderStrategyBase : IRenderStrategy
    {
        #region Fields / Properties

        protected const string _thumbnailFont = "Verdana";
        protected const int _thumbnailFontSize = 12;
        protected const double _renderDPI = 144d; // Larger values produce better image quality at the cost of performance
        protected const double _nativePdfDpi = 72d; // Don't change this. The PDF spec expresses all unit-measured values in points
        protected const int _scrollBufferZone = 100; // Viewport top/bottom expansion in pixels applied when calculating thumbnail intersections
        protected Point _scrollPosition = new Point(0, 0);
        protected double _thumbnailScale;

        public event RenderCompleteEventHandler RenderCompleteEvent;

        public int RenderPageNumber { get; set; }
        public int RenderZoomLevel { get; set; }
        public bool AnimateThumbnails { get; set; }
        public Rect LayoutRect { get; private set; }
        public List<LayoutRect> LayoutRectList { get; private set; }
        protected PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        protected ViewModeType ViewMode { get; private set; }
        protected List<PageSizeRun> PageSizeRunList { get; private set; }
        protected ThumbnailUpdateType ThumbnailUpdate { get; set; }

        private ThumbnailSize _thumbnailSize;
        public ThumbnailSize ThumbnailSize 
        { 
            get => _thumbnailSize;
            set
            {
                _thumbnailSize = value;
                switch (_thumbnailSize)
                {
                    case ThumbnailSize.Small:
                        _thumbnailScale = 0.3; break;
                    case ThumbnailSize.Medium:
                        _thumbnailScale = 0.45; break;
                    case ThumbnailSize.Large:
                        _thumbnailScale = 0.6; break;
                }
            }
        }

        protected Rect ViewportScrollRect
        {
            get
            {
                // Inflate the viewport intersection a bit so that the page image pop-in isn't so obvious
                var viewportScrollRect = new Rect(_scrollPosition, GetViewportSize());
                viewportScrollRect.Inflate(0, _scrollBufferZone);
                return viewportScrollRect;
            }
        }

        #endregion Fields / Properties
        #region Interface Members

        public async Task Render(ViewModeType viewMode)
        {
            ViewMode = viewMode;
            if (viewMode == ViewModeType.PageView)
                await RenderCurrentPage();
            else
            {
                GetLayoutRects();
                RenderThumbnails();
            }
        }
        public double GetDisplayScale()
        {
            double zoomValue;

            if (ViewMode == ViewModeType.ThumbnailView)
                zoomValue = _thumbnailScale;

            // Compute "fit to view" scale value
            else if (RenderZoomLevel == 0)
            {
                var pageSize = GetLayoutSize();
                var viewportSize = GetViewportSize();
                zoomValue = Math.Min(viewportSize.Width / pageSize.Width, viewportSize.Height / pageSize.Height);
            }
            else
                zoomValue = RenderZoomLevel / 100d;

            return zoomValue;
        }
        public double GetPixelsToInchesConversion()
        {
            // Scale to convert from pixels to inches at the current zoom level
            var displayDpi = ViewMode == ViewModeType.PageView ? _renderDPI : _nativePdfDpi;
            return 1d / (displayDpi * GetDisplayScale());
        }
        public Point GetPagePosition()
        {
            var posX = 0d;
            var posY = 0d;

            if (RenderZoomLevel == 0 && ViewMode == ViewModeType.PageView)
            {
                var displayScale = GetDisplayScale();
                var pageImageSize = GetLayoutSize();
                var viewportSize = GetViewportSize();
                var scaledWidth = pageImageSize.Width * displayScale;
                var scaledHeight = pageImageSize.Height * displayScale;
                posX = (viewportSize.Width - scaledWidth) / 2;
                posY = (viewportSize.Height - scaledHeight) / 2;
            }
            return new Point(posX, posY);
        }
        public async Task SetPageSizeRunList()
        {
            var json = await PdfJs.GetPdfPageSizeRunList();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            PageSizeRunList = JsonSerializer.Deserialize<List<PageSizeRun>>(json, options);
        }
        public int GetViewportPageAtPoint(Point point)
        {
            var ptScrolled = new Point(point.X + _scrollPosition.X, point.Y + _scrollPosition.Y);
            var pageRect = LayoutRectList.FirstOrDefault(rect => rect.Contains(ptScrolled));
            return pageRect != null ? pageRect.Id : 0;
        }

        public abstract void ScrollViewport(int scrollX, int scrollY);
        public abstract Size GetViewportSize();
        public abstract Size GetLayoutSize();
        public abstract void ClearViewport();
        public abstract void Reset();
        public abstract void SetThumbnailUpdateType(ThumbnailUpdateType thumbnailUpdateType);

        #endregion Interface Members
        #region Methods

        protected abstract Task<int> RenderCurrentPage();
        protected abstract void RenderThumbnails();
        protected void GetLayoutRects()
        {
            const double toPts = 25400d / _nativePdfDpi;
            const int pageGap = 10;

            var nextX = 0d;
            var nextY = 0d;
            var rowHeight = 0d;
            var layoutWidth = GetViewportSize().Width;
            var rowRectList = new List<LayoutRect>();
            var rowList = new List<List<LayoutRect>>();

            foreach (var pageRun in PageSizeRunList)
            {
                var thumbWidth = pageRun.Width / toPts * _thumbnailScale;
                var thumbHeight = pageRun.Height / toPts * _thumbnailScale;

                for (var i = 0; i < pageRun.Count; i++)
                {
                    if (thumbWidth + nextX < layoutWidth)
                    {
                        var rect = new LayoutRect(nextX, nextY, thumbWidth, thumbHeight);
                        rowRectList.Add(rect);
                        rowHeight = Math.Max(rowHeight, thumbHeight);
                    }
                    else // start a new row
                    {
                        rowList.Add(rowRectList);
                        rowRectList = new List<LayoutRect>();
                        nextX = 0;
                        nextY += rowHeight + pageGap;

                        rowRectList.Add(new LayoutRect(nextX, nextY, thumbWidth, thumbHeight));
                        rowHeight = thumbHeight;
                    }
                    nextX += thumbWidth + pageGap;
                }
            }
            rowList.Add(rowRectList);

            // Run-list count of 1 means all the pages are the same size, so don't bother
            // with centering unless there's a diversity of page sizes
            if (PageSizeRunList.Count > 1)
            {
                // Center all rects within their respective rows            
                foreach (var row in rowList)
                {
                    rowHeight = row[0].Height.AsTMM();
                    if (row.Any(rect => rect.Height.AsTMM() != rowHeight))
                    {
                        rowHeight = row.Max(rect => rect.Height);
                        for (int i = 0; i < row.Count; i++)
                        {
                            var rect = row[i];
                            rect.Y += (rowHeight - rect.Height) / 2;
                            row[i] = rect;
                        }
                    }
                }
            }

            // Flatten the row list into a list of rects
            var id = 1;
            LayoutRectList = rowList.SelectMany(row => row).ToList();
            LayoutRectList.ForEach(rect => rect.Id = id++);
            LayoutRect = new Rect(new Size(LayoutRectList.Max(rect => rect.Right), LayoutRectList.Max(rect => rect.Bottom)));
        }
        protected void FireRenderCompleteEvent(List<Grid> thumbnails)
        {
            RenderCompleteEvent?.Invoke(this, new RenderCompleteEventArgs(thumbnails));
        }

        #endregion Methods
    }
}
