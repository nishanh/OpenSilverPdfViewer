
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
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

        private Debouncer WheelDebouncer { get; set; } = new Debouncer(100);

        private double _pixelsToInches = 1 / 72d;
        public double PixelsToInches
        {
            get => _pixelsToInches;
            set
            {
                if (_pixelsToInches != value)
                {
                    _pixelsToInches = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _scrollPosX = 0;
        public double ScrollPosX
        {
            get => _scrollPosX;
            set
            {
                if (_scrollPosX != value)
                {
                    _scrollPosX = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _scrollPosY = 0;
        public double ScrollPosY
        {
            get => _scrollPosY;
            set
            {
                if (_scrollPosY != value)
                {
                    _scrollPosY = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _pagePositionX = 0;
        public double PagePositionX
        {
            get => _pagePositionX;
            set
            {
                if (_pagePositionX != value)
                {
                    _pagePositionX = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _pagePositionY = 0;
        public double PagePositionY
        {
            get => _pagePositionY;
            set
            {
                if (_pagePositionY != value)
                {
                    _pagePositionY = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Fields / Properties
        #region Dependency Properties

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register("Filename", typeof(string), typeof(PageViewer),
            new PropertyMetadata("", OnFilenameChanged));

        public static readonly DependencyProperty PageCountProperty = DependencyProperty.Register("PageCount", typeof(int), typeof(PageViewer),
            new PropertyMetadata(0, OnPageCountChanged));

        public static readonly DependencyProperty PreviewPageProperty = DependencyProperty.Register("PreviewPage", typeof(int), typeof(PageViewer),
            new PropertyMetadata(0, OnPreviewPageChanged));

        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(int), typeof(PageViewer),
            new PropertyMetadata(0, OnZoomLevelChanged));

        public static readonly DependencyProperty ZoomValueProperty = DependencyProperty.Register("ZoomValue", typeof(double), typeof(PageViewer),
            new PropertyMetadata(100d, OnZoomValueChanged));

        public static readonly DependencyProperty RenderModeProperty = DependencyProperty.Register("RenderMode", typeof(RenderModeType), typeof(PageViewer),
            new PropertyMetadata(RenderModeType.Dom, OnRenderModeChanged));

        public static readonly DependencyProperty RulerUnitsProperty = DependencyProperty.Register("RulerUnits", typeof(UnitMeasure), typeof(PageViewer),
            new PropertyMetadata(UnitMeasure.Imperial, OnRulerUnitsChanged));

        public string Filename
        {
            get => (string)GetValue(FilenameProperty);
            set => SetValue(FilenameProperty, value);
        }
        public int PageCount
        {
            get => (int)GetValue(PageCountProperty);
            set => SetValue(PageCountProperty, value);
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
        public UnitMeasure RulerUnits
        {
            get => (UnitMeasure)GetValue(RulerUnitsProperty);
            set => SetValue(RulerUnitsProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static async void OnFilenameChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.renderStrategy.Reset();
            await ctrl.renderStrategy.SetPageSizeRunList();

            // HACK: Find a better way to force this binding to update when a new document loads when on page 1
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
        private static void OnPageCountChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
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

            ctrl.renderStrategy.Reset();

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
            ctrl.SetRulers();
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
            if (PreviewPage <= 0) return;

            await renderStrategy.Render(ViewMode);
            var displayScale = renderStrategy.GetDisplayScale() * 100d;
            ZoomValue = Math.Round(displayScale, 0);
            SetScrollBars();
            SetRulers();
        }
        private void SetRulers()
        {
            if (PreviewPage <= 0 || _rulersOn == false) 
                return;

            var pagePosition = renderStrategy.GetPagePosition();

            PixelsToInches = renderStrategy.GetPixelsToInchesConversion();
            PagePositionX = pagePosition.X + previewGrid.Margin.Left;
            PagePositionY = pagePosition.Y + previewGrid.Margin.Top;
            ScrollPosX = pageScrollBarHorz.Value;
            ScrollPosY = pageScrollBarVert.Value;
        }
        private void SetScrollBars()
        {
            var displayScale = renderStrategy.GetDisplayScale();
            var viewportSize = renderStrategy.GetViewportSize();
            var layoutSize = renderStrategy.GetLayoutSize();

            var scaledLayoutSize = new Size(layoutSize.Width * displayScale, layoutSize.Height * displayScale);
            var scrollExtentX = Math.Max(0, scaledLayoutSize.Width - viewportSize.Width);
            var scrollExtentY = Math.Max(0, scaledLayoutSize.Height - viewportSize.Height);
            var valueScale = ViewMode == ViewModeType.ThumbnailView ? 1d : displayScale;

            pageScrollBarHorz.Maximum = scrollExtentX;
            pageScrollBarHorz.ViewportSize = viewportSize.Width;
            pageScrollBarHorz.LargeChange = scrollExtentX / 10;
            pageScrollBarHorz.Value *= valueScale;

            pageScrollBarVert.Maximum = scrollExtentY;
            pageScrollBarVert.ViewportSize = viewportSize.Height;
            pageScrollBarVert.LargeChange = scrollExtentY / 10;
            pageScrollBarVert.Value *= valueScale;

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
            SetRulers(); 
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
            else if (ZoomLevel == 0)
            {
                // Filter out rapid changes
                WheelDebouncer.OnSettled = () =>
                {
                    if (e.Delta < 0)
                        PreviewPage = Math.Min(PreviewPage + 1, PageCount);
                    else
                        PreviewPage = Math.Max(PreviewPage - 1, 1);
                };
                WheelDebouncer.Reset();
            }
        }
        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderView();
        }
        public void RulerToggle(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            _rulersOn = (bool)toggleButton.IsChecked;
            SetRulers();
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
                renderStrategy.Reset();
                renderStrategy.RenderPageNumber = PreviewPage;
                await RenderView();
                SetRulers();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Event Handlers
    }
}
