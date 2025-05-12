
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;

using OpenSilverPdfViewer.JSInterop;
using OpenSilverPdfViewer.ViewModels;

namespace OpenSilverPdfViewer
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await PdfJsWrapper.Instance.InitAsync();
            var viewModel = (MainPageViewModel)DataContext;
            viewModel.StatusText = PdfJsWrapper.Instance.Version;
        }
    }
}

