
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : UserControl
    {
        #region Fields

        private const string viewCanvasId = "pageViewCanvas";
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
                await PdfJs.RenderPageToViewport(PreviewPage, ZoomLevel, viewCanvasId);
                SetScrollBars();
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
        private void PageScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            PdfJs.ScrollViewportImage(PreviewPage, viewCanvasId, ZoomLevel,
                (int)pageScrollBarHorz.Value, (int)pageScrollBarVert.Value);
        }

        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderCurrentPage();
        }
    }
}
