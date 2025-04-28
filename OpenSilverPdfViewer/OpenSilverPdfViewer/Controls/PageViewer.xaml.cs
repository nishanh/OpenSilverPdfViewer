
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Globalization;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.Renderer;

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

        private ViewModeType _viewMode = ViewModeType.PageView;
        public ViewModeType ViewMode
        {
            get { return _viewMode; }
            set 
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanZoom));
                }
            }
        }

        public bool CanZoom
        {
            get { return PreviewPage > 0 && ViewMode == ViewModeType.PageView; }
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

        public static readonly DependencyProperty RulerUnitsProperty = DependencyProperty.Register("RulerUnits", typeof(RulerUnits), typeof(PageViewer),
            new PropertyMetadata(RulerUnits.Imperial, OnRulerUnitsChanged));

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
        public RulerUnits RulerUnits
        {
            get => (RulerUnits)GetValue(RulerUnitsProperty);
            set => SetValue(RulerUnitsProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static async void OnFilenameChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.InvalidatePageCache();
            await ctrl.renderStrategy.SetPageSizeRunList();

            // HACK: Find a better way to force this binding to update when a new document loads
            if (ctrl.PreviewPage == 1) ctrl.PreviewPage = 0;
            ctrl.PreviewPage = 1;
        }
        private static async void OnPreviewPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.RenderPageNumber = (int)e.NewValue;
            ctrl.OnPropertyChanged(nameof(CanZoom));
            await ctrl.RenderView();
        }
        private static async void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.RenderZoomLevel = (int)e.NewValue;
            await ctrl.RenderView();
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

            if (!string.IsNullOrEmpty(ctrl.Filename))
            {
                await ctrl.renderStrategy.SetPageSizeRunList();
                await ctrl.RenderView();
            }
        }
        private static void OnRulerUnitsChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.DrawRulers();
        }
        #endregion Dependency Property Event Handlers
        #region Implementation

        public PageViewer()
        {
            InitializeComponent();
            renderStrategy = RenderStrategyFactory.Create(RenderMode, pageImageCanvas);
            PropertyChanged += OnAsyncPropertyChanged;
        }
        private async Task RenderView()
        {
            if (PreviewPage <= 0) return;// && ViewMode != ViewModeType.ThumbnailView) return;

            await renderStrategy.Render(ViewMode);
            var displayScale = renderStrategy.GetDisplayScale() * 100d;
            ZoomValue = Math.Round(displayScale, 0);
            SetScrollBars();
            DrawRulers();
        }
        private void DrawRulers()
        {
            if (PreviewPage <= 0 || _rulersOn == false) return;

            const int fontSize = 10;
            const int margin = 10; // from xaml layout
            var metric = RulerUnits == RulerUnits.Metric;

            // Tick mark length constants
            const int wholeTickLength = 12;
            const int sixteenthTick = wholeTickLength / 8 + 1;
            const int eighthTick = wholeTickLength / 4 + 1;
            const int quarterTick = wholeTickLength / 3 + 2;
            const int halfTick = wholeTickLength / 2 + 2;
            const int millimeterTick = quarterTick;

            var pxToInches = renderStrategy.GetPixelsToInchesConversion();
            double resRuler, wholeUnitInterval, unitConvert;

            // Adjust the ruler resolution based on device-to-logical scale.
            // This is to avoid tightly-packed tick marks at high scale values
            if (metric)
            {
                const double metricScaleThreshold = 0.008;
                var metricRes = pxToInches > metricScaleThreshold ? 10d : 1d; // centimeters or millimeters
                resRuler = metricRes / 25.4;
                wholeUnitInterval = (int)(10 / metricRes);
                unitConvert = 2.54;
            }
            else
            {
                resRuler = pxToInches > .05 ? 1d : 0.5;
                resRuler = pxToInches < .025 ? 0.25 : resRuler;
                resRuler = pxToInches < .0125 ? 0.125 : resRuler;
                resRuler = pxToInches < .00625 ? 0.0625 : resRuler;
                wholeUnitInterval = (int)(1 / resRuler);
                unitConvert = 1.0;
            }

            // Erase previous ruler ticks and text
            horzRuler.Children.Clear();
            vertRuler.Children.Clear();

            // Clip to ruler bounds
            horzRuler.Clip = new RectangleGeometry { Rect = new Rect(0, 0, horzRuler.ActualWidth, horzRuler.ActualHeight) };
            vertRuler.Clip = new RectangleGeometry { Rect = new Rect(0, 0, vertRuler.ActualWidth, vertRuler.ActualHeight) };

            // Draw inner borders
            horzRuler.Children.Add(new Line
            {
                X1 = 0,
                Y1 = horzRuler.ActualHeight,
                X2 = horzRuler.ActualWidth,
                Y2 = horzRuler.ActualHeight,
                StrokeThickness = 2,
                Stroke = _rulerBorderBrush
            });
            vertRuler.Children.Add(new Line
            {
                X1 = vertRuler.ActualWidth,
                Y1 = 0,
                X2 = vertRuler.ActualWidth,
                Y2 = vertRuler.ActualHeight,
                StrokeThickness = 2,
                Stroke = _rulerBorderBrush
            });

            var pagePosition = renderStrategy.GetPagePosition();
            pagePosition.Offset(margin - pageScrollBarHorz.Value, margin - pageScrollBarVert.Value);

            // Find the first tick mark we can reasonably draw.
            // Let's say the page image is centered horizontally in the viewport in logical units such
            // that the left-edge of the page is at 2.125". Therefore, the first tick mark that can be
            // drawn is the fractional component of .125". Convert that result back to device units (pixels)
            // to get the first X or Y position to draw the initial tick mark at.
            var originX = pagePosition.X * (pxToInches * unitConvert);
            var startX = originX % 1; // get fractional part
            var i = -(int)(startX / resRuler);
            var devStartX = startX / (pxToInches * unitConvert);

            // Draw horizontal ruler
            var pos = 0d;
            while (pos < horzRuler.ActualWidth)
            {
                // Compute tick mark X-pos in device units
                pos = Math.Round(devStartX + ((i * resRuler) / pxToInches), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (metric) tickLength = millimeterTick;
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

            var originY = pagePosition.Y * (pxToInches * unitConvert);
            var startY = originY % 1; // get fractional part
            i = -(int)(startY / resRuler);
            var devStartY = startY / (pxToInches * unitConvert);

            // Draw vertical ruler
            pos = 0d;
            while (pos < vertRuler.ActualHeight)
            {
                // Compute tick mark Y-pos in device units
                pos = Math.Round(devStartY + ((i * resRuler) / pxToInches), 0);

                var tickLength = sixteenthTick;
                if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                else if (metric) tickLength = millimeterTick;
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
            var layoutSize = renderStrategy.GetLayoutSize();

            var scaledLayoutSize = new Size(layoutSize.Width * displayScale, layoutSize.Height * displayScale);
            var scrollExtentX = Math.Max(0, scaledLayoutSize.Width - viewportSize.Width);
            var scrollExtentY = Math.Max(0, scaledLayoutSize.Height - viewportSize.Height);

            pageScrollBarHorz.Value *= ViewMode == ViewModeType.ThumbnailView ? 1d : displayScale;
            pageScrollBarHorz.Maximum = scrollExtentX;
            pageScrollBarHorz.ViewportSize = viewportSize.Width;

            pageScrollBarVert.Value *= ViewMode == ViewModeType.ThumbnailView ? 1d : displayScale;
            pageScrollBarVert.Maximum = scrollExtentY;
            pageScrollBarVert.ViewportSize = viewportSize.Height;

            var reposition = (ZoomLevel != 0 || ViewMode == ViewModeType.ThumbnailView) && 
                (pageScrollBarVert.Value > 0 || pageScrollBarHorz.Value > 0);

            if (reposition)
                renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
        }

        #endregion Implementation
        #region Event Handlers

        private void PageScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
            DrawRulers(); 
        }
        private void PageView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ZoomLevel != 0 || ViewMode == ViewModeType.ThumbnailView)
            {
                var delta = e.Delta / 2;
                var scrollPos = Math.Min(Math.Max(0, pageScrollBarVert.Value - delta), pageScrollBarVert.Maximum);
                pageScrollBarVert.Value = scrollPos;
                PageScrollBars_Scroll(pageScrollBarVert, new ScrollEventArgs(scrollPos, delta < 0 ? ScrollEventType.SmallIncrement : ScrollEventType.SmallDecrement));
            }
        }
        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderView();
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
        private async void OnAsyncPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewMode))
            {
                renderStrategy.RenderPageNumber = PreviewPage;
                renderStrategy.Reset();
                await RenderView();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Event Handlers
    }
}
