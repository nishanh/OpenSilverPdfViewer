using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using OpenSilverPdfViewer.JSInterop;
using OpenSilver;

namespace OpenSilverPdfViewer
{
    public partial class MainPage : Page
    {
        public PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Interop;

        public MainPage()
        {
            this.InitializeComponent();

            InitPdfJs();
        }
        private async void InitPdfJs()
        {
            await PdfJs.Init();
            MessageBox.Show($"Library version: {PdfJsWrapper.Interop.Version}");
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var fileName = "Data\\compressed.tracemonkey-pldi-09.pdf";
            var pageCount = await PdfJs.LoadPdfFile(fileName);
            MessageBox.Show($"PDF loaded with pages: {pageCount}");
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}

