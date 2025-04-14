
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenSilverPdfViewer.JSInterop;
using OpenSilverPdfViewer.ViewModels;

namespace OpenSilverPdfViewer
{
    public partial class MainPage : Page
    {
        private MainPageViewModel ViewModel => (MainPageViewModel)DataContext;

        public MainPage()
        {
            InitializeComponent();
        }
        public async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await PdfJsWrapper.Interop.Init();
            ViewModel.StatusText = PdfJsWrapper.Interop.Version;
        }
        private async void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panelSize = $"View size: {e.NewSize.Width} x {e.NewSize.Height}";
            ViewModel.StatusText = panelSize;
            await ViewModel.RenderCurrentPage();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Image image = new Image();

            var imageBytes = await PdfJsWrapper.Interop.RenderPageThumbnail(1, 0.2);

            using (var stream = new MemoryStream(imageBytes))
            {
                stream.Write(imageBytes, 0, imageBytes.Length);
                stream.Position = 0; 

                var bitmap = new BitmapImage();
                bitmap.SetSource(stream);
                image.Source = bitmap;
            }
            thumbCanvas.Children.Add(image);
        }
    }
}

