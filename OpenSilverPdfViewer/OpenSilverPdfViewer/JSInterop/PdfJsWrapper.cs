
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
    // Singleton interop wrapper for PdfJs 
    public class PdfJsWrapper
    {
        private const string scriptResourceName = "/OpenSilverPdfViewer;component/JSInterop/pdfJsInterop.js";
        private static PdfJsWrapper _instance;
        public static PdfJsWrapper Instance => _instance ?? (_instance = new PdfJsWrapper());

        public string Version { get; private set; }

        private PdfJsWrapper() { }
        #region Asynchronous Tasks

        public async Task InitAsync()
        {
            if (string.IsNullOrEmpty(Version))
            {
                try
                {
                    await OpenSilver.Interop.LoadJavaScriptFile(scriptResourceName);
                    Version = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getLibraryVersion2");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"PdfJsWrapper.Init failed: {ex.Message}");
                }
            }
        }
        public async Task<int> LoadPdfFileAsync(string fileName)
        {
            await InitAsync();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("loadPdfFile", fileName);
        }
        public async Task<int> LoadPdfFileStreamAsync(string base64stream)
        {
            await InitAsync();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("loadPdfStream", base64stream);
        }
        public async Task<int> RenderPageToViewportAsync(int pageNumber, int dpi, int zoomLevel, string canvasId)
        {
            await InitAsync();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderPageToViewport", pageNumber, dpi, zoomLevel, canvasId);
        }
        public async Task<Size> GetPdfPageSizeAsync(int pageNumber)
        {
            await InitAsync();
            var json = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getLogicalPageSize", pageNumber);
            return json.ParseJsonSize();
        }
        public async Task<Image> GetPdfPageImageAsync(int pageNumber, double scaleFactor)
        {
            await InitAsync();

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

        #endregion Asynchronous Tasks
        #region Synchronous Tasks

        public Size GetPageImageSize(int pageNumber)
        {
            var result = OpenSilver.Interop.ExecuteJavaScript("getDevicePageSize($0)", pageNumber);
            var json = (string)Convert.ChangeType(result, typeof(string));
            return json.ParseJsonSize();
        }
        public Size GetViewportSize(string canvasId)
        {
            var result = OpenSilver.Interop.ExecuteJavaScript("getViewportSize($0)", canvasId);
            var json = (string)Convert.ChangeType(result, typeof(string));
            return json.ParseJsonSize();
        }
        public void ScrollViewportImage(int pageNumber, string canvasId, int zoomLevel, int scrollX, int scrollY)
        {
            OpenSilver.Interop.ExecuteJavaScript("scrollViewportImage($0,$1,$2,$3,$4)", 
                pageNumber, canvasId, zoomLevel, scrollX, scrollY);
        }
        public void InvalidatePageCache()
        {
            OpenSilver.Interop.ExecuteJavaScript("invalidatePageCache()");
        }

        #endregion Synchronous Tasks
    }
}
