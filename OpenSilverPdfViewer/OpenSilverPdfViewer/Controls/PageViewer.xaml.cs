
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : UserControl
    {
        #region Fields

        private const int renderDPI = 144;
        private const string viewCanvasId = "pageViewCanvas";
        private bool _rulersOn = false;
        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Interop;

      
        #endregion Fields
        #region Dependency Properties

        public static readonly DependencyProperty PreviewPageProperty = DependencyProperty.Register("PreviewPage", typeof(int), typeof(PageViewer),
            new PropertyMetadata(1, OnPreviewPageChanged));

        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(int), typeof(PageViewer),
            new PropertyMetadata(1, OnZoomLevelChanged));

        public static readonly DependencyProperty RenderModeProperty = DependencyProperty.Register("RenderMode", typeof(RenderModeType), typeof(PageViewer),
            new PropertyMetadata(RenderModeType.Dom, OnRenderModeChanged));

        public int PreviewPage
        {
            get => (int)GetValue(PreviewPageProperty);
            set => SetValue(PreviewPageProperty, value);
        }
        public int ZoomLevel
        {
            get => (int)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }
        public RenderModeType RenderMode
        {
            get => (RenderModeType)GetValue(RenderModeProperty);
            set => SetValue(RenderModeProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static async void OnPreviewPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.rulerToggleBtn.IsEnabled = (int)e.NewValue > 0;
            await ctrl.RenderCurrentPage();
        }
        private static async void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            await ctrl.RenderCurrentPage();
        }
        private static void OnRenderModeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
        }

        #endregion Dependency Property Event Handlers
        #region Implementation

        public PageViewer()
        {
            this.InitializeComponent();

            /*
            var image = await PdfJsWrapper.Interop.GetPdfPageImage(1, 0.2);
            var size = await PdfJsWrapper.Interop.GetPdfPageSize(1);
            thumbCanvas.Children.Add(image);
            */
        }

        private async Task RenderCurrentPage()
        {
            if (PreviewPage > 0)
            {
                await PdfJs.RenderPageToViewport(PreviewPage, renderDPI, ZoomLevel, viewCanvasId);
                SetScrollBars();
                DrawRulers();
            }
        }
        private void DrawRulers()
        {
            if (PreviewPage == 0 || _rulersOn == false) return;

            double logScale = GetLogicalViewportScale();

            // Font size and tick color
            const int fontSize = 10;
            var rulerFont = new FontFamily("Verdana");
            var tickBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xA9, 0xA9, 0xA9));

            // Tick mark length constants
            const int wholeTickLength = 12;
            const int sixteenthTick = wholeTickLength / 8 + 1;
            const int eighthTick = wholeTickLength / 4 + 1;
            const int quarterTick = wholeTickLength / 3 + 2;
            const int halfTick = wholeTickLength / 2 + 2;
            
            var resRuler = 0.125;
            var wholeUnitInterval = (int)(1d / resRuler);

            var margin = 10;
            var rulerSize = 30d;
            var offset = margin + rulerSize;

            var posX = offset - pageScrollBarHorz.Value;
            var posY = margin - pageScrollBarVert.Value;

            if (ZoomLevel == 0)
            {
                var displayScale = GetDisplayScale();
                var sourcePageSize = PdfJs.GetPageImageSize(PreviewPage);
                var viewportSize = PdfJs.GetViewportSize(viewCanvasId);
                var pxWidth = sourcePageSize.Width * displayScale;
                var pxHeight = sourcePageSize.Height * displayScale;
                posX = ((viewportSize.Width - pxWidth) / 2) + offset;
                posY = ((viewportSize.Height - pxHeight) / 2) + margin;
            }

            // Erase previous ruler ticks
            horzRuler.Children.Clear();
            vertRuler.Children.Clear();

            // Clip to ruler bounds
            horzRuler.Clip = new RectangleGeometry { Rect = new Rect(rulerSize, 0, horzRuler.ActualWidth, horzRuler.ActualHeight) };
            vertRuler.Clip = new RectangleGeometry { Rect = new Rect(0, 0, vertRuler.ActualWidth, vertRuler.ActualHeight) };

            var rulerBorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x48, 0x48, 0x48));
            horzRuler.Children.Add(new Line { X1 = vertRuler.ActualWidth, Y1 = horzRuler.ActualHeight - 1, X2 = horzRuler.ActualWidth, Y2 = horzRuler.ActualHeight - 1, Stroke = rulerBorderBrush });
            vertRuler.Children.Add(new Line { X1 = vertRuler.ActualWidth - 1, Y1 = 0, X2 = vertRuler.ActualWidth - 1, Y2 = vertRuler.ActualHeight, Stroke = rulerBorderBrush });

            // Find the first tick mark we can reasonably draw
            var originX = posX * logScale;
            var startX = originX - (int)originX;
            var i = -(int)(startX / resRuler);
            var devStartX = startX / logScale;

            var pos = 0d;
            while (pos < horzRuler.ActualWidth)
            {
                pos = Math.Round(devStartX + ((i * resRuler) / logScale), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (i % (wholeUnitInterval / 2) == 0) tickLength = halfTick;
                else if (i % (wholeUnitInterval / 4) == 0) tickLength = quarterTick;
                else if (i % (wholeUnitInterval / 8) == 0) tickLength = eighthTick;

                // Draw the ruler tick mark
                horzRuler.Children.Add(new Line
                {
                    X1 = pos,
                    X2 = pos,
                    Y1 = horzRuler.ActualHeight,
                    Y2 = horzRuler.ActualHeight - tickLength,
                    Stroke = tickBrush,
                    StrokeThickness = 1
                });
                if (i % wholeUnitInterval == 0)
                {
                    var unitVal = (i / wholeUnitInterval) - (int)originX;
                    var rulerVal = new TextBlock
                    {
                        Foreground = tickBrush,
                        FontFamily = rulerFont,
                        FontSize = fontSize,
                        Text = unitVal.ToString(CultureInfo.InvariantCulture)
                    };
                    rulerVal.SetValue(Canvas.TopProperty, 2d); // wholeTickLength - textSize.Height + 2);
                    rulerVal.SetValue(Canvas.LeftProperty, pos - (rulerVal.ActualWidth / 2));
                    horzRuler.Children.Add(rulerVal);
                }
                i++;
            }

            var originY = posY * logScale;
            var startY = originY - (int)originY;
            i = -(int)(startY / resRuler);
            var devStartY = startY / logScale;

            pos = 0d;
            while (pos < vertRuler.ActualHeight)
            {
                pos = Math.Round(devStartY + ((i * resRuler) / logScale), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (i % (wholeUnitInterval / 2) == 0) tickLength = halfTick;
                else if (i % (wholeUnitInterval / 4) == 0) tickLength = quarterTick;
                else if (i % (wholeUnitInterval / 8) == 0) tickLength = eighthTick;

                // Draw the ruler tick mark
                vertRuler.Children.Add(new Line
                {
                    Y1 = pos,
                    Y2 = pos,
                    X1 = vertRuler.ActualWidth,
                    X2 = vertRuler.ActualWidth - tickLength,
                    Stroke = tickBrush,
                    StrokeThickness = 1
                });

                if (i % wholeUnitInterval == 0)
                {
                    var unitVal = (i / wholeUnitInterval) - (int)originY;
                    var rulerVal = new TextBlock
                    {
                        Foreground = tickBrush,
                        FontFamily = rulerFont,
                        FontSize = fontSize,
                        Text = unitVal.ToString(CultureInfo.InvariantCulture)
                    };
                    rulerVal.SetValue(Canvas.LeftProperty, 2d);
                    rulerVal.SetValue(Canvas.TopProperty, pos - (rulerVal.ActualHeight / 2));
                    vertRuler.Children.Add(rulerVal);
                }
                i++;
            }
        }
        private void SetScrollBars()
        {
            var viewportSize = PdfJs.GetViewportSize(viewCanvasId);
            var pageSize = PdfJs.GetPageImageSize(PreviewPage);
            var displayScale = ZoomLevel == 0 ?
                Math.Min(viewportSize.Width / pageSize.Width, viewportSize.Height / pageSize.Height) :
                ZoomLevel / 100d;

            var deviceSize = new Size(pageSize.Width * displayScale, pageSize.Height * displayScale);

            var scrollViewWidth = Math.Max(deviceSize.Width, viewportSize.Width);
            var scrollViewHeight = Math.Max(deviceSize.Height, viewportSize.Height);
            var scrollExtentX = Math.Max(0, deviceSize.Width - viewportSize.Width);
            var scrollExtentY = Math.Max(0, deviceSize.Height - viewportSize.Height);

            pageScrollBarHorz.Value *= displayScale;
            pageScrollBarHorz.Maximum = scrollExtentX;
            pageScrollBarHorz.ViewportSize = scrollViewWidth;

            pageScrollBarVert.Value *= displayScale;
            pageScrollBarVert.Maximum = scrollExtentY;
            pageScrollBarVert.ViewportSize = scrollViewHeight;

            if (ZoomLevel != 0)
                PdfJs.ScrollViewportImage(PreviewPage, viewCanvasId, ZoomLevel,
                    (int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
        }
        private double GetDisplayScale()
        {
            var pageSize = PdfJs.GetPageImageSize(PreviewPage);
            var viewportSize = PdfJs.GetViewportSize(viewCanvasId);
            return ZoomLevel == 0 ?
                Math.Min(viewportSize.Width / pageSize.Width, viewportSize.Height / pageSize.Height) :
                ZoomLevel / 100d;
        }
        private double GetLogicalViewportScale()
        {
            var displayScale = GetDisplayScale();
            var sourcePageSize = PdfJs.GetPageImageSize(PreviewPage);

            var dpiScale = renderDPI / 72d;
            var ptWidth = sourcePageSize.Width / dpiScale;
            var pxWidth = sourcePageSize.Width * displayScale;
            var logScale = pxWidth / ptWidth * 72d;

            // Scale to convert from pixels to inches at the current zoom level
            return 1d / logScale;
        }
        private void PageScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            PdfJs.ScrollViewportImage(PreviewPage, viewCanvasId, ZoomLevel,
                (int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);

            DrawRulers(); 
        }

        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderCurrentPage();
        }
        private void RulerOnStoryboard_Completed(object sender, EventArgs e)
        {
            _rulersOn = true;
            DrawRulers();
        }
        private void RulerOffStoryboard_Completed(object sender, EventArgs e)
        {
            _rulersOn = false;
            horzRuler.Children.Clear();
            vertRuler.Children.Clear();
        }
        public void RulerToggle(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            var state = (bool)toggleButton.IsChecked ? "RulerOn" : "RulerOff";
            VisualStateManager.GoToState(this, state, false);
        }

        #endregion Implementation
    }
}
