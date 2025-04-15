
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
    }
}

