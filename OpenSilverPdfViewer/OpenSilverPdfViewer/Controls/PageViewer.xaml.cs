
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : UserControl
    {
        private const string viewCanvasId = "pageViewCanvas";
        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Interop;

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

        private static async void OnPreviewPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
            await ctrl.RenderCurrentPage();
        }
        private static void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
        }
        private static void OnRenderModeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageViewer;
        }

        public PageViewer()
        {
            this.InitializeComponent();

            /*
            var image = await PdfJsWrapper.Interop.GetPdfPageImage(1, 0.2);
            var size = await PdfJsWrapper.Interop.GetPdfPageSize(1);
            thumbCanvas.Children.Add(image);
            */
        }

        public async Task RenderCurrentPage()
        {
            if (PreviewPage > 0)
                await PdfJs.RenderPageToViewport(PreviewPage, viewCanvasId);
        }

        private void PageScrollHorz_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void PageScrollVert_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private async void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RenderCurrentPage();
        }
    }
}
