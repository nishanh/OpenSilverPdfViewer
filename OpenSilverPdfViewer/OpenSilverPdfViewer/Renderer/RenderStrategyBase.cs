
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

using CSHTML5.Native.Html.Controls;
using OpenSilverPdfViewer.JSInterop;
using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Renderer
{
    public interface IRenderStrategy
    {
        int RenderPageNumber { get; set; }
        int RenderZoomLevel { get; set; }
        Task Render(ViewModeType viewMode);
        void ScrollViewport(int scrollX, int scrollY);
        double GetDisplayScale();
        double GetPixelsToInchesConversion();
        Size GetViewportSize();
        Size GetLayoutSize();
        Point GetPagePosition();
        void ClearViewport();
        void InvalidatePageCache();
        Task SetPageSizeRunList();
    }
    public static class RenderStrategyFactory
    {
        public static IRenderStrategy Create(RenderModeType renderMode, FrameworkElement osCanvas)
        {
            if (renderMode == RenderModeType.Dom)
                return new DomCanvasRenderer();
            else if (renderMode == RenderModeType.OpenSilver)
                return new OSCanvasRenderer(osCanvas as Canvas);
            else
                return new HTMLCanvasRenderer(osCanvas as HtmlCanvas);
        }
        public static RenderModeType GetRenderModeType(IRenderStrategy strategy)
        {
            var modeType = RenderModeType.Dom;

            if (strategy is OSCanvasRenderer)
                modeType = RenderModeType.OpenSilver;
            else if (strategy is HTMLCanvasRenderer)
                modeType = RenderModeType.HTMLCanvas;
            
            return modeType;
        }
    }
    public abstract class RenderStrategyBase : IRenderStrategy
    {
        #region Fields / Properties

        protected const double _renderDPI = 144d;
        protected const double _nativePdfDpi = 72d;
        protected const double _thumbnailScale = 0.25;
        protected const int _scrollBufferZone = 100;

        public int RenderPageNumber { get; set; }
        public int RenderZoomLevel { get; set; }
        public Rect LayoutRect { get; private set; }
        public List<LayoutRect> LayoutRectList { get; private set; }
        protected PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        protected ViewModeType ViewMode { get; private set; }
        protected List<PageRun> PageSizeRunList { get; private set; }

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
        public abstract void ScrollViewport(int scrollX, int scrollY);
        public double GetDisplayScale()
        {
            var pageSize = GetLayoutSize();
            var viewportSize = GetViewportSize();

            if (ViewMode == ViewModeType.ThumbnailView)
            {
                // var pxToLog = 1 / (_nativePdfDpi * _thumbScale);
                return _thumbnailScale;
            }
            var zoomValue = RenderZoomLevel == 0 ?
                Math.Min(viewportSize.Width / pageSize.Width, viewportSize.Height / pageSize.Height) :
                RenderZoomLevel / 100d;

            return zoomValue;
        }
        public double GetPixelsToInchesConversion()
        {
            var displayScale = GetDisplayScale();
            var sourcePageSize = GetLayoutSize();

            var dpiScale = ViewMode == ViewModeType.PageView ? _renderDPI / _nativePdfDpi : 1d;
            var ptWidth = sourcePageSize.Width / dpiScale;
            var pxWidth = sourcePageSize.Width * displayScale;
            var logScale = pxWidth / ptWidth * _nativePdfDpi;

            // Scale to convert from pixels to inches at the current zoom level
            return 1d / logScale;
        }
        public abstract Size GetViewportSize();
        public abstract Size GetLayoutSize();
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
        public abstract void ClearViewport();
        public abstract void InvalidatePageCache();
        public async Task SetPageSizeRunList()
        {
            var json = await PdfJsWrapper.Instance.GetPdfPageSizeRunList();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            PageSizeRunList = JsonSerializer.Deserialize<List<PageRun>>(json, options);
        }

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
        
        #endregion Methods
    }
}
