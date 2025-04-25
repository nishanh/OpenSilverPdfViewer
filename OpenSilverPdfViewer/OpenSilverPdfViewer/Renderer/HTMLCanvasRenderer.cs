
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;

using OpenSilverPdfViewer.Utility;
using CSHTML5.Native.Html.Controls;

namespace OpenSilverPdfViewer.Renderer
{
    internal class HTMLCanvasRenderer : RenderStrategyBase
    {
        private Dictionary<int, BlobElement> _pageImageCache = new Dictionary<int, BlobElement>();
        private readonly HtmlCanvas renderCanvas;

        public HTMLCanvasRenderer(HtmlCanvas canvas) 
        {
            renderCanvas = canvas;
        }
        protected override async Task<int> RenderCurrentPage()
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
            {
                var renderScale = _renderDPI / _nativePdfDpi;
                image = await PdfJs.GetPdfPageBlobElementAsync(RenderPageNumber, renderScale);
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
            throw new NotImplementedException();
        }
        public override void ScrollViewport(int scrollX, int scrollY)
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
                throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

            image.X = -scrollX;
            image.Y = -scrollY;
            renderCanvas.Draw();
        }
        public override Size GetLayoutSize()
        {
            if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
                throw new Exception($"GetPageImageSize: No image found in cache for page {RenderPageNumber}");

            return new Size(image.Width, image.Height);
        }
        public override Size GetViewportSize()
        {
            return new Size(renderCanvas.ActualWidth, renderCanvas.ActualHeight);
        }
        public override void ClearViewport()
        {
            renderCanvas.Children.Clear();
            renderCanvas.Draw();
        }
        public override void InvalidatePageCache()
        {
            ClearViewport();

            foreach (var page in _pageImageCache.Values)
                page.InvalidateImage();
            _pageImageCache.Clear();
        }
    }
}
