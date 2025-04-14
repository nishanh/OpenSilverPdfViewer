
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.


using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.JSInterop
{
    public class PdfJsWrapper
    {
        private const string scriptResourceName = "/OpenSilverPdfViewer;component/JSInterop/pdfJsInterop.js";
        private static PdfJsWrapper _instance;
        public static PdfJsWrapper Interop => _instance ?? (_instance = new PdfJsWrapper());

        public string Version { get; private set; }

        private PdfJsWrapper() { }
        public async Task Init()
        {
            if (string.IsNullOrEmpty(Version))
            {
                try
                {
                    await OpenSilver.Interop.LoadJavaScriptFile(scriptResourceName);
                    Version = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getLibraryVersion");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"PdfJsWrapper.Init failed: {ex.Message}");
                }
            }
        }
        public async Task<int> LoadPdfFile(string fileName)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("loadPdfFile", fileName);
        }
        public async Task<int> RenderPage(int pageNumber, string canvasId)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderPage", pageNumber, canvasId);
        }
        public async Task<int> RenderPageToViewport(int pageNumber, string canvasId)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderPageToViewport", pageNumber, canvasId);
        }
        public async Task<Size> GetPdfPageSize(int pageNumber)
        {
            await Init();
            var json = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getPageSize", pageNumber);

            var dimensions = json.Trim('{').Trim('}').Split(',');
            var widthStr = dimensions[0].Substring(dimensions[0].IndexOf(':') + 1);
            var width = double.Parse(widthStr);
            var heightStr = dimensions[1].Substring(dimensions[1].IndexOf(':') + 1);
            var height = double.Parse(heightStr);

            return new Size(width, height);
        }
        public async Task<Image> GetPdfPageImage(int pageNumber, double scaleFactor)
        {
            await Init();

            var dataUrl = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("renderPageThumbnail", pageNumber, scaleFactor);
            dataUrl = dataUrl.Substring(dataUrl.IndexOf(",") + 1); // strip header
            var imageBytes = Convert.FromBase64String(dataUrl);

            Image image = new Image();
            using (var stream = new MemoryStream(imageBytes))
            {
                stream.Write(imageBytes, 0, imageBytes.Length);
                stream.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.SetSource(stream);
                image.Source = bitmap;
            }
            return image;
        }
    }
}
