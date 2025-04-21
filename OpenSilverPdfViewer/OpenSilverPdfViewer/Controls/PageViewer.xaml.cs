
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : INotifyPropertyChanged
    {
        #region Fields / Properties

        private bool _rulersOn = false;

        private readonly FontFamily _rulerFont = new FontFamily("Verdana");
        private readonly Brush _tickBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xA9, 0xA9, 0xA9));
        private readonly Brush _rulerBorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x48, 0x48, 0x48));

        private IRenderStrategy renderStrategy;

        private ViewModeType _viewMode = ViewModeType.Unknown;
        public ViewModeType ViewMode
        {
            get { return _viewMode; }
            set 
            { 
                _viewMode = value;
                OnPropertyChanged();
            }
        }

        #endregion Fields / Properties
        #region Dependency Properties

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register("Filename", typeof(string), typeof(PageViewer),
            new PropertyMetadata("", OnFilenameChanged));

        public static readonly DependencyProperty PreviewPageProperty = DependencyProperty.Register("PreviewPage", typeof(int), typeof(PageViewer),
            new PropertyMetadata(0, OnPreviewPageChanged));

        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(int), typeof(PageViewer),
            new PropertyMetadata(0, OnZoomLevelChanged));

        public static readonly DependencyProperty ZoomValueProperty = DependencyProperty.Register("ZoomValue", typeof(double), typeof(PageViewer),
            new PropertyMetadata(100d, OnZoomValueChanged));

        public static readonly DependencyProperty RenderModeProperty = DependencyProperty.Register("RenderMode", typeof(RenderModeType), typeof(PageViewer),
            new PropertyMetadata(RenderModeType.Dom, OnRenderModeChanged));

        public string Filename
        {
            get => (string)GetValue(FilenameProperty);
            set => SetValue(FilenameProperty, value);
        }
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
        public double ZoomValue
        {
            get => (double)GetValue(ZoomValueProperty);
            set => SetValue(ZoomValueProperty, value);
        }
        public RenderModeType RenderMode
        {
            get => (RenderModeType)GetValue(RenderModeProperty);
            set => SetValue(RenderModeProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static void OnFilenameChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.InvalidatePageCache();
        }
        private static async void OnPreviewPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;

            if ((int)e.NewValue > 0 && ctrl.ViewMode == ViewModeType.Unknown) 
                ctrl.ViewMode = ViewModeType.PageView;

            ctrl.renderStrategy.RenderPageNumber = (int)e.NewValue;
            await ctrl.RenderCurrentPage();
        }
        private static async void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.RenderZoomLevel = (int)e.NewValue;
            await ctrl.RenderCurrentPage();
        }
        private static void OnZoomValueChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
        }
        private static async void OnRenderModeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            var renderMode = (RenderModeType)e.NewValue;

            ctrl.renderStrategy.InvalidatePageCache();

            var canvasElement = renderMode == RenderModeType.OpenSilver ? 
                (FrameworkElement)ctrl.pageImageCanvas :
                ctrl.pageElementCanvas;

            ctrl.renderStrategy = RenderStrategyFactory.Create(renderMode, canvasElement);
            ctrl.renderStrategy.RenderPageNumber = ctrl.PreviewPage;
            ctrl.renderStrategy.RenderZoomLevel = ctrl.ZoomLevel;
            await ctrl.RenderCurrentPage();
        }

        #endregion Dependency Property Event Handlers
        #region Implementation

        public PageViewer()
        {
            InitializeComponent();
            renderStrategy = RenderStrategyFactory.Create(RenderMode, pageImageCanvas);
        }
        private async Task RenderCurrentPage()
        {
            if (PreviewPage > 0)
            {
                await renderStrategy.RenderCurrentPage();
                var displayScale = renderStrategy.GetDisplayScale() * 100d;
                ZoomValue = Math.Round(displayScale, 0);
                SetScrollBars();
                DrawRulers();
            }
        }
        private void DrawRulers()
        {
            if (PreviewPage == 0 || _rulersOn == false) return;

            const int fontSize = 10;
            const int margin = 10; // from xaml layout
            const int rulerSize = 30; // from xaml layout
            const int offsetX = margin + rulerSize; // account for the presence of the vertical ruler as well as the margin for horizontal placements

            // Tick mark length constants
            const int wholeTickLength = 12;
            const int sixteenthTick = wholeTickLength / 8 + 1;
            const int eighthTick = wholeTickLength / 4 + 1;
            const int quarterTick = wholeTickLength / 3 + 2;
            const int halfTick = wholeTickLength / 2 + 2;

            var displayScale = renderStrategy.GetDisplayScale();

            // Dynamically adjust the ruler resolution based on display scale.
            // This is to avoid tightly-packed tick marks at small scale values
            var resRuler = displayScale < 0.35 ? 0.25 : 0.125;
            resRuler = displayScale < 0.2 ? 0.5 : resRuler;
            resRuler = displayScale < 0.15 ? 1.0 : resRuler;
            resRuler = displayScale > 0.9 ? 0.0625 : resRuler;
            var wholeUnitInterval = (int)(1d / resRuler);

            // Erase previous ruler ticks and text
            horzRuler.Children.Clear();
            vertRuler.Children.Clear();

            // Clip to ruler bounds
            horzRuler.Clip = new RectangleGeometry { Rect = new Rect(rulerSize, 0, horzRuler.ActualWidth, horzRuler.ActualHeight) };
            vertRuler.Clip = new RectangleGeometry { Rect = new Rect(0, 0, vertRuler.ActualWidth, vertRuler.ActualHeight) };

            // Draw inner borders
            horzRuler.Children.Add(new Line { X1 = vertRuler.ActualWidth, Y1 = horzRuler.ActualHeight - 1, X2 = horzRuler.ActualWidth, Y2 = horzRuler.ActualHeight - 1, Stroke = _rulerBorderBrush });
            vertRuler.Children.Add(new Line { X1 = vertRuler.ActualWidth - 1, Y1 = 0, X2 = vertRuler.ActualWidth - 1, Y2 = vertRuler.ActualHeight, Stroke = _rulerBorderBrush });

            double pxToInches = renderStrategy.GetPixelsToInchesConversion();
            var pagePosition = renderStrategy.GetPagePosition();
            pagePosition.Offset(offsetX, margin);

            // Find the first tick mark we can reasonably draw.
            // Let's say the page image is centered horizontally in the viewport in logical units at 2.125"
            // Therefore, the first tick mark that can be drawn is the fractional value .125"
            // Convert that result back to device units (pixels) to get the first X or Y value to draw at 
            var originX = pagePosition.X * pxToInches;
            var startX = originX % 1; // get fractional part
            var i = -(int)(startX / resRuler);
            var devStartX = startX / pxToInches;

            // Draw horizontal ruler
            var pos = 0d;
            while (pos < horzRuler.ActualWidth)
            {
                // Compute tick mark X-pos in pixel units
                pos = Math.Round(devStartX + ((i * resRuler) / pxToInches), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (i % (wholeUnitInterval / 2) == 0) tickLength = halfTick;
                else if (i % (wholeUnitInterval / 4) == 0) tickLength = quarterTick;
                else if (i % (wholeUnitInterval / 8) == 0) tickLength = eighthTick;

                // Draw the ruler tick mark
                horzRuler.Children.Add(new Line
                {
                    X1 = pos, X2 = pos,
                    Y1 = horzRuler.ActualHeight,
                    Y2 = horzRuler.ActualHeight - tickLength,
                    Stroke = _tickBrush,
                    StrokeThickness = 1
                });

                // Draw the unit-value text
                if (i % wholeUnitInterval == 0)
                {
                    var unitVal = (i / wholeUnitInterval) - (int)originX;
                    var rulerVal = new TextBlock
                    {
                        Foreground = _tickBrush,
                        FontFamily = _rulerFont,
                        FontSize = fontSize,
                        Text = unitVal.ToString(CultureInfo.InvariantCulture)
                    };
                    rulerVal.SetValue(Canvas.TopProperty, 2d); 
                    rulerVal.SetValue(Canvas.LeftProperty, pos - (rulerVal.ActualWidth / 2));
                    horzRuler.Children.Add(rulerVal);
                }
                i++;
            }

            var originY = pagePosition.Y * pxToInches;
            var startY = originY % 1; // get fractional part
            i = -(int)(startY / resRuler);
            var devStartY = startY / pxToInches;

            // Draw vertical ruler
            pos = 0d;
            while (pos < vertRuler.ActualHeight)
            {
                // Compute tick mark Y-pos in pixel units
                pos = Math.Round(devStartY + ((i * resRuler) / pxToInches), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (i % (wholeUnitInterval / 2) == 0) tickLength = halfTick;
                else if (i % (wholeUnitInterval / 4) == 0) tickLength = quarterTick;
                else if (i % (wholeUnitInterval / 8) == 0) tickLength = eighthTick;

                // Draw the ruler tick mark
                vertRuler.Children.Add(new Line
                {
                    Y1 = pos, Y2 = pos,
                    X1 = vertRuler.ActualWidth,
                    X2 = vertRuler.ActualWidth - tickLength,
                    Stroke = _tickBrush,
                    StrokeThickness = 1
                });

                // Draw the unit-value text
                if (i % wholeUnitInterval == 0)
                {
                    var unitVal = (i / wholeUnitInterval) - (int)originY;
                    var rulerVal = new TextBlock
                    {
                        Foreground = _tickBrush,
                        FontFamily = _rulerFont,
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
            var displayScale = renderStrategy.GetDisplayScale();
            var viewportSize = renderStrategy.GetViewportSize();
            var pageSize = renderStrategy.GetPageImageSize();

            var scaledPageSize = new Size(pageSize.Width * displayScale, pageSize.Height * displayScale);
            var scrollViewWidth = Math.Max(scaledPageSize.Width, viewportSize.Width);
            var scrollViewHeight = Math.Max(scaledPageSize.Height, viewportSize.Height);
            var scrollExtentX = Math.Max(0, scaledPageSize.Width - viewportSize.Width);
            var scrollExtentY = Math.Max(0, scaledPageSize.Height - viewportSize.Height);

            pageScrollBarHorz.Value *= displayScale;
            pageScrollBarHorz.Maximum = scrollExtentX;
            pageScrollBarHorz.ViewportSize = scrollViewWidth;

            pageScrollBarVert.Value *= displayScale;
            pageScrollBarVert.Maximum = scrollExtentY;
            pageScrollBarVert.ViewportSize = scrollViewHeight;

            if (ZoomLevel != 0)
                renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
        }

        #endregion Implementation
        #region Event Handlers

        private void PageScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
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
        public void ViewModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            ViewMode = toggleButton.Name == "pageViewBtn" ? ViewModeType.PageView : ViewModeType.ThumbnailView;
        }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Event Handlers
    }
}
