
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Threading.Tasks;

namespace OpenSilverPdfViewer.Renderer
{
    internal sealed class DomCanvasRenderer : RenderStrategyBase
    {
        private const string viewCanvasId = "pageViewCanvas";

        protected override async Task<int> RenderCurrentPage()
        {
            return await PdfJs.RenderPageToViewportAsync(RenderPageNumber, (int)_renderDPI, RenderZoomLevel, viewCanvasId);
        }
        protected override void RenderThumbnails()
        {
            throw new NotImplementedException();
        }
        public override void ScrollViewport(int scrollX, int scrollY)
        {
            PdfJs.ScrollViewportImage(RenderPageNumber, viewCanvasId, RenderZoomLevel, scrollX, scrollY);
        }
        public override Size GetViewportSize()
        {
            return PdfJs.GetViewportSize(viewCanvasId);
        }
        public override Size GetLayoutSize()
        {
            return PdfJs.GetPageImageSize(RenderPageNumber);
        }
        public override void ClearViewport()
        {
            PdfJs.ClearViewport(viewCanvasId);
        }
        public override void InvalidatePageCache()
        {
            ClearViewport();
            PdfJs.InvalidatePageCache();
        }
    }
}
