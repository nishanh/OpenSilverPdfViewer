
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using OpenSilverPdfViewer;
using OpenSilverPdfViewer.Layout;
using OpenSilverPdfViewer.Utility;
using CSHTML5.Native.Html.Controls;
using OpenSilverPdfViewer.JSInterop;

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
            return new DOMRenderStrategy();
        else if (renderMode == RenderModeType.OpenSilver)
            return new OSRenderStrategy(osCanvas as Canvas);
        else
            return new HTMLCanvasStrategy(osCanvas as HtmlCanvas);
    }
}
public abstract class RenderStrategyBase : IRenderStrategy
{
    protected const double _thumbScale = 0.2;
    public int RenderPageNumber { get; set; }
    public int RenderZoomLevel { get; set; }
    public Rect LayoutRect { get; set; }
    public List<LayoutRect> LayoutRectList { get; private set; }
    protected ViewModeType _viewMode;

    protected List<PageRun> PageSizeRunList { get; private set; } //= new List<PageRun> { new PageRun { Width = 215900, Height = 279400, Count = 1000 } };
    //protected List<PageRun> PageSizeRunList { get; private set; } = PageRun.CreateTestList();

    protected const double renderDPI = 144d;
    protected const double nativePdfDpi = 72d;
    protected PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;

    public async Task Render(ViewModeType viewMode)
    {
        _viewMode = viewMode;
        if (viewMode == ViewModeType.PageView)
            await RenderCurrentPage();
        else
        {
            LayoutRectList = GetLayoutRects();
            await RenderThumbnails();
        }
    }
    public abstract Task<int> RenderCurrentPage();
    public abstract Task RenderThumbnails();
    public abstract void ScrollViewport(int scrollX, int scrollY);
    public abstract Size GetViewportSize();
    public abstract Size GetLayoutSize();
    public abstract void ClearViewport();
    public abstract void InvalidatePageCache();
    public double GetDisplayScale()
    {
        var pageSize = GetLayoutSize();
        var viewportSize = GetViewportSize();

        if (_viewMode == ViewModeType.ThumbnailView)
        {
            // var pxToLog = 1 / (nativePdfDpi * _thumbScale);
            return _thumbScale;
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

        var dpiScale = _viewMode == ViewModeType.PageView ? renderDPI / nativePdfDpi : 1d;
        var ptWidth = sourcePageSize.Width / dpiScale;
        var pxWidth = sourcePageSize.Width * displayScale;
        var logScale = pxWidth / ptWidth * nativePdfDpi;

        // Scale to convert from pixels to inches at the current zoom level
        return 1d / logScale;
    }
    public async Task SetPageSizeRunList()
    {
        var json = await PdfJsWrapper.Instance.GetPdfPageSizeRunList();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        PageSizeRunList = JsonSerializer.Deserialize<List<PageRun>>(json, options);
    }
    protected List<LayoutRect> GetLayoutRects()
    {
        const double toPts = 25400d / nativePdfDpi;
        const int gap = 10;
        var layoutHeight = 0d;
        var nextX = 0d;
        var nextY = 0d;
        var rowHeight = 0d;
        var layoutWidth = GetViewportSize().Width;
        var layoutRectList = new List<LayoutRect>();
        var rectList = new List<LayoutRect>();
        var rowList = new List<List<LayoutRect>>();

        foreach (var pageRun in PageSizeRunList)
        {
            var thumbWidth = pageRun.Width / toPts * _thumbScale;
            var thumbHeight = pageRun.Height / toPts * _thumbScale;

            for (int i = 0; i < pageRun.Count; i++)
            {
                if (thumbWidth + nextX < layoutWidth)
                {
                    var rect = new LayoutRect(nextX, nextY, thumbWidth, thumbHeight);
                    rectList.Add(rect);
                    rowHeight = Math.Max(rowHeight, thumbHeight);
                }
                else // start a new row
                {
                    rowList.Add(rectList);
                    rectList = new List<LayoutRect>();
                    layoutHeight += rowHeight + gap;
                    nextX = 0; nextY = layoutHeight;

                    rectList.Add(new LayoutRect(nextX, nextY, thumbWidth, thumbHeight));
                    rowHeight = thumbHeight;
                }
                nextX += thumbWidth + gap;
            }
        }
        layoutHeight += rowHeight;
        rowList.Add(rectList);

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
        layoutRectList = rowList.SelectMany(row => row).ToList();
        layoutRectList.ForEach(rect => rect.Id = id++);
        LayoutRect = new Rect(new Size(layoutRectList.Max(rect => rect.Right), layoutRectList.Max(rect => rect.Bottom)));

        return layoutRectList;
    }
    public Point GetPagePosition()
    {
        var posX = 0d;
        var posY = 0d;

        if (RenderZoomLevel == 0 && _viewMode == ViewModeType.PageView)
        {
            var displayScale = GetDisplayScale();
            var imageSize = GetLayoutSize();
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
    public override Task RenderThumbnails()
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
public sealed class OSRenderStrategy : RenderStrategyBase
{
    private Dictionary<int, Image> _pageImageCache = new Dictionary<int, Image>();
    private readonly Canvas renderCanvas;
    private Point _scrollPoint = new Point(0, 0);

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
    public override async Task RenderThumbnails()
    {
        var pageFont = new FontFamily("Verdana");
        var fontBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        var fillBrush = renderCanvas.FindResource("CMSCtrlDisabledBodyBrush") as Brush;

        var viewportRect = new Rect(0, 0, renderCanvas.ActualWidth, renderCanvas.ActualHeight);
        viewportRect.Offset(_scrollPoint.X, _scrollPoint.Y);
        // Inflate the viewport intersection a bit so that the page image pop-in isn't so obvious
        viewportRect.Inflate(0, 100);

        var intersectList = LayoutRectList.Where(rect => rect.Intersects(viewportRect));

        // Get a list of page image ids that are currently rendered in the viewport
        var renderedIds = renderCanvas.Children
            .Where(child => child is Grid)
            .Select(pageElem => (int)((Grid)pageElem).Tag);

        // Remove all page image elements that do not exist in the current intersection list from the viewport
        renderedIds
            .Except(intersectList.Select(rect => rect.Id))
            .ToList()
            .ForEach(id => renderCanvas.Children.Remove(renderCanvas.Children.FirstOrDefault(child => child is Grid grid && (int)grid.Tag == id)));

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
        {
            var pageRect = new Grid
            {
                Width = rect.Width,
                Height = rect.Height
            };
            var canvasRect = new Rectangle
            {
                Width = rect.Width,
                Height = rect.Height,
                Fill = fillBrush,
                Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00))
            };
            pageRect.Children.Add(canvasRect);

            if (_pageImageCache.TryGetValue(rect.Id, out var image) == false)
            {
                image = await PdfJs.GetPdfPageImageAsync(rect.Id, _thumbScale);
                if (!_pageImageCache.Keys.Contains(rect.Id)) 
                    _pageImageCache.Add(rect.Id, image);
            }
            if (image.Parent != null)
                ((Grid)image.Parent).Children.Remove(image);

            pageRect.Children.Add(image);
            /*
            var pageNumber = new TextBlock
            {
                Foreground = fontBrush,
                FontFamily = pageFont,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = rect.Id.ToString(CultureInfo.InvariantCulture)
            };
            pageRect.Children.Add(pageNumber);
            */
            pageRect.SetValue(Canvas.LeftProperty, rect.X);
            pageRect.SetValue(Canvas.TopProperty, rect.Y);
            pageRect.Tag = rect.Id;
            renderCanvas.Children.Add(pageRect);
        }
    }
    public override async void ScrollViewport(int scrollX, int scrollY)
    {
        _scrollPoint.X = scrollX;
        _scrollPoint.Y = scrollY;
        
        TranslateTransform translate = new TranslateTransform { X = -_scrollPoint.X, Y = -_scrollPoint.Y };

        if (_viewMode == ViewModeType.ThumbnailView)
        {
            var viewportRect = new Rect(_scrollPoint, new Size(renderCanvas.ActualWidth, renderCanvas.ActualHeight));
            viewportRect.Offset(_scrollPoint.X, _scrollPoint.Y);
            // Inflate the viewport intersection a bit so that the page image pop-in isn't so obvious
            viewportRect.Inflate(0, 100);

            var intersectList = LayoutRectList.Where(rect => rect.Intersects(viewportRect));
            var intersectChecksum = intersectList
                .Select(rect => rect.Id)
                .Sum();

            var renderedChecksum = renderCanvas.Children
                .Where(child => child is Grid)
                .Select(elem => (int)((Grid)elem).Tag)
                .Sum();

            // Things need to be added and/or removed from the viewport if the checksums differ
            if (intersectChecksum != renderedChecksum)
                await RenderThumbnails();

            var gridList = renderCanvas.Children.ToList();
            gridList.ForEach(grid => grid.RenderTransform = translate);
        }
        else
        {
            if(_pageImageCache.TryGetValue(RenderPageNumber, out var image) == false)
                throw new Exception($"ScrollViewport: No image found in cache for page {RenderPageNumber}");

            translate = (image.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
            translate.X = -scrollX;
            translate.Y = -scrollY;
        }
    }
    public override Size GetViewportSize()
    {
        return new Size(renderCanvas.ActualWidth, renderCanvas.ActualHeight);
    }
    public override Size GetLayoutSize()
    {
        if (_viewMode == ViewModeType.ThumbnailView)
        {
            var unscaledWidth = LayoutRect.Width / _thumbScale;
            var unscaledHeight = LayoutRect.Height / _thumbScale;
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
    public override Task RenderThumbnails()
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
