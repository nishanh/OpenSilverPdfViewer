
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using OpenSilverPdfViewer;
using OpenSilverPdfViewer.Utility;
using CSHTML5.Native.Html.Controls;
using OpenSilverPdfViewer.JSInterop;

public interface IRenderStrategy
{
    int RenderPageNumber { get; set; }
    int RenderZoomLevel { get; set; }
    Task<int> RenderCurrentPage();
    void ScrollViewport(int scrollX, int scrollY);
    double GetDisplayScale();
    double GetPixelsToInchesConversion();
    Size GetViewportSize();
    Size GetPageImageSize();
    Point GetPagePosition();
    void ClearViewport();
    void InvalidatePageCache();
}
public static class RenderStrategyFactory
{
    public static IRenderStrategy Create(RenderModeType renderMode, FrameworkElement osCanvas)
    {
        if (renderMode == RenderModeType.Dom)
            return new DOMRenderStrategy();
        else if (renderMode == RenderModeType.OpenSilver)
            return new OSRenderStrategy(osCanvas as Canvas);
        else
            return new HTMLCanvasStrategy(osCanvas as HtmlCanvas);
    }
}
public abstract class RenderStrategyBase : IRenderStrategy
{
    public int RenderPageNumber { get; set; }
    public int RenderZoomLevel { get; set; }

    protected const double renderDPI = 144d;
    protected const double nativePdfDpi = 72d;
    protected PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;

    public abstract Task<int> RenderCurrentPage();
    public abstract void ScrollViewport(int scrollX, int scrollY);
    public abstract Size GetViewportSize();
    public abstract Size GetPageImageSize();
    public abstract void ClearViewport();
    public abstract void InvalidatePageCache();
    public double GetDisplayScale()
    {
        var pageSize = GetPageImageSize();
        var viewportSize = GetViewportSize();

        var zoomValue = RenderZoomLevel == 0 ?
            Math.Min(viewportSize.Width / pageSize.Width, viewportSize.Height / pageSize.Height) :
            RenderZoomLevel / 100d;

        return zoomValue;
    }
    public double GetPixelsToInchesConversion()
    {
        var displayScale = GetDisplayScale();
        var sourcePageSize = GetPageImageSize();

        var dpiScale = renderDPI / nativePdfDpi;
        var ptWidth = sourcePageSize.Width / dpiScale;
        var pxWidth = sourcePageSize.Width * displayScale;
        var logScale = pxWidth / ptWidth * nativePdfDpi;

        // Scale to convert from pixels to inches at the current zoom level
        return 1d / logScale;
    }
    public Point GetPagePosition()
    {
        var posX = 0d;
        var posY = 0d;
        var displayScale = GetDisplayScale();

        if (RenderZoomLevel == 0)
        {
            var imageSize = GetPageImageSize();
            var viewportSize = GetViewportSize();
            var scaledWidth = imageSize.Width * displayScale;
            var scaledHeight = imageSize.Height * displayScale;
            posX = (viewportSize.Width - scaledWidth) / 2;
            posY = (viewportSize.Height - scaledHeight) / 2;
        }
        return new Point(posX, posY);
    }
}
public sealed class DOMRenderStrategy : RenderStrategyBase
{
    private const string viewCanvasId = "pageViewCanvas";

    public override async Task<int> RenderCurrentPage()
    {
        return await PdfJs.RenderPageToViewportAsync(RenderPageNumber, (int)renderDPI, RenderZoomLevel, viewCanvasId);
    }
    public override void ScrollViewport(int scrollX, int scrollY)
    {
        PdfJs.ScrollViewportImage(RenderPageNumber, viewCanvasId, RenderZoomLevel, scrollX, scrollY);
    }
    public override Size GetViewportSize()
    {
        return PdfJs.GetViewportSize(viewCanvasId);
    }
    public override Size GetPageImageSize()
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
public sealed class OSRenderStrategy : RenderStrategyBase
{
    private Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
    private readonly Canvas renderCanvas;

    public OSRenderStrategy(Canvas canvas) 
    { 
        renderCanvas = canvas;
    }
    public override async Task<int> RenderCurrentPage()
    {
        if (_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
        {
            var renderScale = renderDPI / nativePdfDpi;
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
    public override void ScrollViewport(int scrollX, int scrollY)
    {
        if(_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
            throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

        var translate = (image.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
        translate.X = -scrollX;
        translate.Y = -scrollY;
    }
    public override Size GetViewportSize()
    {
        return new Size(renderCanvas.ActualWidth, renderCanvas.ActualHeight);
    }
    public override Size GetPageImageSize()
    {
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
}
public sealed class HTMLCanvasStrategy : RenderStrategyBase
{
    private Dictionary<int, BlobElement> _pageImageCache = new Dictionary<int, BlobElement>();
    private readonly HtmlCanvas renderCanvas;

    public HTMLCanvasStrategy(HtmlCanvas canvas)
    {
        renderCanvas = canvas;
    }
    public override async Task<int> RenderCurrentPage()
    {
        if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
        {
            var renderScale = renderDPI / nativePdfDpi;
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
    public override void ScrollViewport(int scrollX, int scrollY)
    {
        if (_pageImageCache.TryGetValue(RenderPageNumber, out BlobElement image) == false)
            throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

        image.X = -scrollX; 
        image.Y = -scrollY;
        renderCanvas.Draw();
    }
    public override Size GetPageImageSize()
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
