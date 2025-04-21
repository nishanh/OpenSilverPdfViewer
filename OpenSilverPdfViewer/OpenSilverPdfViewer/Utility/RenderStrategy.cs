
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
    void ClearViewport();
    void InvalidatePageCache();
}
public static class RenderStrategyFactory
{
    public static IRenderStrategy Create(RenderModeType renderMode, Canvas osCanvas)
    {
        if (renderMode == RenderModeType.Dom)
            return new DOMRenderStrategy();
        else
            return new OSRenderStrategy(osCanvas);
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
        PdfJs.InvalidatePageCache();
    }
}
public sealed class OSRenderStrategy : RenderStrategyBase
{
    private Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
    private Canvas renderCanvas;

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
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform() { ScaleX = displayScale, ScaleY = displayScale });
        transform.Children.Add(new TranslateTransform() { X = posX, Y = posY });
        image.RenderTransform = transform;

        renderCanvas.Children.Clear();
        renderCanvas.Children.Add(image);

        return RenderPageNumber;
    }
    public override void ScrollViewport(int scrollX, int scrollY)
    {
        _pageImageCache.TryGetValue(RenderPageNumber, out var image);
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
        _pageImageCache.TryGetValue(RenderPageNumber, out var image);
        return new Size(image.Width, image.Height);
    }
    public override void ClearViewport()
    {
        renderCanvas.Children.Clear();
    }
    public override void InvalidatePageCache()
    {
        _pageImageCache.Clear();
    }
}
