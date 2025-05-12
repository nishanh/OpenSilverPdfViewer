
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.Renderer;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : INotifyPropertyChanged
    {
        #region Fields / Properties

        private bool _rulersOn = false;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        private IRenderStrategy _renderStrategy;
        private readonly Debouncer _wheelDebouncer = new Debouncer(100);
        private readonly Debouncer _doubleClickDetector = new Debouncer(200);
        private Storyboard _thumbnailStoryboard;

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

        public Grid AnimatingThumbnail { get; private set; }
        public bool CanZoom => PreviewPage > 0 && ViewMode == ViewModeType.PageView; 
        public bool IsFitToView => ZoomLevel == 0 && ViewMode == ViewModeType.PageView;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public static readonly DependencyProperty ThumbnailUpdateProperty = DependencyProperty.Register("ThumbnailUpdate", typeof(ThumbnailUpdateType), typeof(PageViewer),
            new PropertyMetadata(ThumbnailUpdateType.Random, OnThumbnailUpdateChanged));

        public static readonly DependencyProperty ThumbnailSizeProperty = DependencyProperty.Register("ThumbnailSize", typeof(ThumbnailSize), typeof(PageViewer),
            new PropertyMetadata(ThumbnailSize.Medium, OnThumbnailSizeChanged));

        public static readonly DependencyProperty ThumbnailAnimationProperty = DependencyProperty.Register("ThumbnailAnimation", typeof(bool), typeof(PageViewer),
            new PropertyMetadata(true, OnThumbnailAnimationChanged));

        public static readonly DependencyProperty ThumbnailAngleProperty = DependencyProperty.Register("ThumbnailAngle", typeof(double), typeof(PageViewer),
            new PropertyMetadata(0d, OnThumbnailAngleChanged));

        public double ThumbnailAngle
        {
            get => (double)GetValue(ThumbnailAngleProperty);
            set => SetValue(ThumbnailAngleProperty, value);
        }
        public bool ThumbnailAnimation
        {
            get => (bool)GetValue(ThumbnailAnimationProperty);
            set => SetValue(ThumbnailAnimationProperty, value);
        }
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
        public ThumbnailUpdateType ThumbnailUpdate
        {
            get => (ThumbnailUpdateType)GetValue(ThumbnailUpdateProperty);
            set => SetValue(ThumbnailUpdateProperty, value);
        }
        public ThumbnailSize ThumbnailSize
        {
            get => (ThumbnailSize)GetValue(ThumbnailSizeProperty);
            set => SetValue(ThumbnailSizeProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static async void OnFilenameChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl._renderStrategy.ThumbnailSize = ctrl.ThumbnailSize;
            ctrl._renderStrategy.Reset();
            await ctrl._renderStrategy.SetPageSizeRunList();

            // HACK: Find a better way to force this binding to update when a new document loads when on page 1
            if (ctrl.PreviewPage == 1) ctrl.PreviewPage = 0;
            ctrl.PreviewPage = 1;
        }
        private static async void OnPreviewPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl._renderStrategy.RenderPageNumber = (int)e.NewValue;
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
            ctrl._renderStrategy.RenderZoomLevel = (int)e.NewValue;
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

            // Reset and disconnect events from (soon to be) prior renderer
            ctrl._renderStrategy.Reset();
            ctrl._renderStrategy.RenderCompleteEvent -= ctrl.RenderStrategy_RenderCompleteEvent;

            // Create and initialize new renderer
            ctrl._renderStrategy = RenderStrategyFactory.Create(renderMode, ctrl.previewGrid);
            ctrl._renderStrategy.ThumbnailSize = ctrl.ThumbnailSize;
            ctrl._renderStrategy.RenderPageNumber = ctrl.PreviewPage;
            ctrl._renderStrategy.RenderZoomLevel = ctrl.ZoomLevel;
            ctrl._renderStrategy.AnimateThumbnails = ctrl.ThumbnailAnimation;
            ctrl._renderStrategy.SetThumbnailUpdateType(ctrl.ThumbnailUpdate);
            ctrl._renderStrategy.RenderCompleteEvent += ctrl.RenderStrategy_RenderCompleteEvent;

            if (!string.IsNullOrEmpty(ctrl.Filename))
            {
                await ctrl._renderStrategy.SetPageSizeRunList();
                await ctrl.RenderView();
            }
        }
        private static void OnRulerUnitsChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl.UpdateRulers();
        }
        private static void OnThumbnailUpdateChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl._renderStrategy.SetThumbnailUpdateType((ThumbnailUpdateType)e.NewValue);
        }
        private static async void OnThumbnailSizeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl._renderStrategy.ThumbnailSize = (ThumbnailSize)e.NewValue;
            ctrl._renderStrategy.Reset();
            await ctrl.RenderView();
        }
        private static void OnThumbnailAnimationChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            ctrl._renderStrategy.AnimateThumbnails = (bool)e.NewValue;
        }
        private static void OnThumbnailAngleChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;

            var oldValue = (int)Math.Truncate((double)e.OldValue);
            var newValue = (int)Math.Truncate((double)e.NewValue);

            if (newValue != oldValue)
            {
                var thumbBorder = ctrl.AnimatingThumbnail.Children[0] as Border;

                // Swap out the placeholder text with the page image at the ~90deg point in the animation
                if (newValue >= 90 && thumbBorder.Child is TextBlock && thumbBorder.Tag is Image image)
                {
                    // Need a mirror transform to "undo" the rotate effect on the image during the animation
                    image.RenderTransform = new ScaleTransform { CenterX = (thumbBorder.Width - 1) / 2, ScaleX = -1, ScaleY = 1 };
                    thumbBorder.Child = image;
                }
                thumbBorder.Rotate(newValue);
            }
        }

        #endregion Dependency Property Event Handlers
        #region Implementation

        public PageViewer()
        {
            InitializeComponent();
            _renderStrategy = RenderStrategyFactory.Create(RenderMode, previewGrid);
            PropertyChanged += OnAsyncPropertyChanged;
        }
        private async Task RenderView()
        {
            if (PreviewPage <= 0) return;

            await _renderStrategy.Render(ViewMode);
            ZoomValue = Math.Round(_renderStrategy.GetDisplayScale() * 100d, 0);
            UpdateScrollBars();
            UpdateRulers();
        }
        private void UpdateRulers()
        {
            if (PreviewPage <= 0 || _rulersOn == false) 
                return;

            PixelsToInches = _renderStrategy.GetPixelsToInchesConversion();

            var pagePosition = _renderStrategy.GetPagePosition();
            PagePositionX = pagePosition.X + previewGrid.Margin.Left;
            PagePositionY = pagePosition.Y + previewGrid.Margin.Top;

            ScrollPosX = pageScrollBarHorz.Value;
            ScrollPosY = pageScrollBarVert.Value;
        }
        private void UpdateScrollBars()
        {
            if (IsFitToView)
            {
                // Remove scrollbars and exit in 'fit to view' mode
                pageScrollBarHorz.Maximum = pageScrollBarVert.Maximum = 0;
                return; 
            }
            var displayScale = _renderStrategy.GetDisplayScale();
            var viewportSize = _renderStrategy.GetViewportSize();
            var layoutSize = _renderStrategy.GetLayoutSize();

            var scaledLayoutSize = new Size(layoutSize.Width * displayScale, layoutSize.Height * displayScale);
            var scrollExtentX = Math.Max(0, scaledLayoutSize.Width - viewportSize.Width);
            var scrollExtentY = Math.Max(0, scaledLayoutSize.Height - viewportSize.Height);
            var valueScale = ViewMode == ViewModeType.ThumbnailView ? 1d : displayScale;

            pageScrollBarHorz.ViewportSize = viewportSize.Width;
            pageScrollBarHorz.LargeChange = scrollExtentX / 10;
            pageScrollBarHorz.Maximum = scrollExtentX;
            pageScrollBarHorz.Value *= valueScale;

            pageScrollBarVert.ViewportSize = viewportSize.Height;
            pageScrollBarVert.LargeChange = scrollExtentY / 10;
            pageScrollBarVert.Maximum = scrollExtentY;
            pageScrollBarVert.Value *= valueScale;

            var reposition = (ZoomLevel != 0 || ViewMode == ViewModeType.ThumbnailView) && 
                (pageScrollBarVert.Value > 0 || pageScrollBarHorz.Value > 0);

            if (reposition)
                _renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
        }

        #endregion Implementation
        #region Event Handlers

        private void PageScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            _renderStrategy.ScrollViewport((int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
            UpdateRulers(); 
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

            // Use mouse-wheel to navigate pages in 'fit to view' mode
            else if (IsFitToView)
            {
                // Filter out rapid changes
                _wheelDebouncer.OnSettled = () =>
                {
                    if (e.Delta < 0)
                        PreviewPage = Math.Min(PreviewPage + 1, PageCount);
                    else
                        PreviewPage = Math.Max(PreviewPage - 1, 1);
                };
                _wheelDebouncer.Reset();
            }
        }
        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderView();
        }
        private void Preview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsFitToView) return;

            _isDragging = true;
            _dragStartPoint = e.GetPosition(previewGrid);
        }
        private void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var mousePos = e.GetPosition(previewGrid);
                var deltaX = mousePos.X - _dragStartPoint.X;
                var deltaY = mousePos.Y - _dragStartPoint.Y;
                _dragStartPoint = mousePos;
                pageScrollBarHorz.Value = Math.Max(0, Math.Min(pageScrollBarHorz.Maximum, pageScrollBarHorz.Value - deltaX));
                pageScrollBarVert.Value = Math.Max(0, Math.Min(pageScrollBarVert.Maximum, pageScrollBarVert.Value - deltaY));
                PageScrollBars_Scroll(this, null);
            }
        }
        private void Preview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;

            if (ViewMode != ViewModeType.ThumbnailView) return;

            if (!_doubleClickDetector.IsSettled)
            {
                var mousePos = e.GetPosition(previewGrid);
                var pageNumber = _renderStrategy.GetViewportPageAtPoint(mousePos);
                if (pageNumber > 0)
                {
                    PreviewPage = pageNumber;
                    ViewMode = ViewModeType.PageView;
                }
            }
            else 
                _doubleClickDetector.Reset();
        }
        private void Preview_MouseLeave(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }
        private void RulerToggle(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            _rulersOn = (bool)toggleButton.IsChecked;
            UpdateRulers();
        }
        private void ViewModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            ViewMode = toggleButton.Name == "pageViewBtn" ? ViewModeType.PageView : ViewModeType.ThumbnailView;
        }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void RenderStrategy_RenderCompleteEvent(object sender, RenderCompleteEventArgs e)
        {
            // Abort all animations if another request comes in while current animations are playing
            if (_thumbnailStoryboard != null && _thumbnailStoryboard.GetCurrentState() == ClockState.Active)
            {
                _thumbnailStoryboard.SkipToFill();
                _thumbnailStoryboard.Stop();
                _thumbnailStoryboard = null;
            }
            StartAnimationSequence(e.Thumbnails);
        }
        private void StartAnimationSequence(List<Grid> thumbnails)
        {
            var index = ThumbnailUpdate == ThumbnailUpdateType.Random ? new Random().Next(thumbnails.Count) : 0;
            AnimatingThumbnail = thumbnails[index];
            var isInDom = AnimatingThumbnail.SetupDOMAnimation();

            // Move to the next element if this one was removed from the DOM (by scrolling most likely)
            if (!isInDom)
            {
                thumbnails.RemoveAt(index);
                if (thumbnails.Count > 0)
                    StartAnimationSequence(thumbnails);
            }

            _thumbnailStoryboard = new Storyboard();
            var animationTime = new Duration(TimeSpan.FromMilliseconds(200));
            var thumbAnimation = new DoubleAnimation() { Duration = animationTime, To = 180d, By = 1d, FillBehavior = FillBehavior.Stop };
            Storyboard.SetTarget(thumbAnimation, this);
            Storyboard.SetTargetProperty(thumbAnimation, new PropertyPath(ThumbnailAngleProperty));
            _thumbnailStoryboard.Children.Add(thumbAnimation);

            _thumbnailStoryboard.Completed += (s, e) =>
            {
                var border = AnimatingThumbnail.Children[0] as Border;
                border.Child.RenderTransform = null;
                if (border.Child is Image)
                    border.BorderThickness = new Thickness(0);

                if (_thumbnailStoryboard != null)
                    thumbnails.Remove(border.Parent as Grid);
                else
                    thumbnails.Clear();

                if (thumbnails.Count > 0)
                    StartAnimationSequence(thumbnails);
            };
            _thumbnailStoryboard.Begin();
        }
        private async void OnAsyncPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewMode))
            {
                _renderStrategy.Reset();
                _renderStrategy.RenderPageNumber = PreviewPage;
                _renderStrategy.SetThumbnailUpdateType(ThumbnailUpdate);
                await RenderView();
                UpdateRulers();
            }
        }

        #endregion Event Handlers
    }
}
