
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
        public async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await PdfJsWrapper.Interop.Init();
            (DataContext as MainPageViewModel).StatusText = PdfJsWrapper.Interop.Version;
        }
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            /*
            var canvasCtrl = pageViewCanvas;
            if (canvasCtrl != null)
            {
                var id = canvasCtrl.GetDOMId();
                MessageBox.Show($"Field id: {id}");

                //var canvas = OpenSilver.Interop.ExecuteJavaScript("createCanvas($0,$1)", 100, 100);
                //var newId = OpenSilver.Interop.ExecuteJavaScript("$0.getAttribute('id')", canvas);
                //MessageBox.Show($"Canvas id: {newId}");
            }
            */
        }
    }
}

