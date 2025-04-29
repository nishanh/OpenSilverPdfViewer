
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.JSInterop
{
    // Singleton interop wrapper for PdfJs 
    public sealed class PdfJsWrapper
    {
        #region Fields / Properties

        private const string scriptResourceName = "/OpenSilverPdfViewer;component/JSInterop/pdfJsInterop.js";
        private static PdfJsWrapper _instance;
        public static PdfJsWrapper Instance => _instance ?? (_instance = new PdfJsWrapper());
        public string Version { get; private set; }

        #endregion Fields / Properties
        #region Asynchronous Tasks

        public async Task InitAsync()
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
        public async Task<int> RenderThumbnailToCacheAsync(int pageNumber, double scale)
        {
            await InitAsync();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderThumbnailToCache", pageNumber, scale);
        }
        public async Task<Size> GetPdfPageSizeAsync(int pageNumber)
        {
            await InitAsync();
            var json = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getLogicalPageSizeAsync", pageNumber);
            return json.ParseJsonSize();
        }
        public async Task<Image> GetPdfPageImageAsync(int pageNumber, double scaleFactor)
        {
            var result = await Task.Run(async () =>
            {
                await InitAsync();

                var dataUrl = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("renderPageThumbnail", pageNumber, scaleFactor);
                var sizeHeader = dataUrl.Substring(0, dataUrl.IndexOf(";"));
                var width = double.Parse(sizeHeader.Substring(0, sizeHeader.IndexOf(":")));
                var height = double.Parse(sizeHeader.Substring(sizeHeader.IndexOf(":") + 1));

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
                    image.Width = width;
                    image.Height = height;
                }
                return image;
            });
            return result;
        }
        public async Task<BlobElement> GetPdfPageBlobElementAsync(int pageNumber, double scaleFactor)
        {
            await InitAsync();

            var blobUrl = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("renderPageToBlob", pageNumber, scaleFactor);
            var sizeHeader = blobUrl.Substring(0, blobUrl.IndexOf(";"));
            var width = double.Parse(sizeHeader.Substring(0, sizeHeader.IndexOf(":")));
            var height = double.Parse(sizeHeader.Substring(sizeHeader.IndexOf(":") + 1));
            blobUrl = blobUrl.Substring(blobUrl.IndexOf(";") + 1); // strip size header

            var element = new BlobElement
            {
                Source = blobUrl,
                Width = width,
                Height = height
            };
            await element.LoadBlob();

            return element;
        }
        public async Task<BlobElement> GetPdfPageBlobElementAsync1(int pageNumber, double scaleFactor)
        {
            var result = await Task.Run(async () =>
            {
                await InitAsync();

                var blobUrl = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("renderPageToBlob", pageNumber, scaleFactor);
                var sizeHeader = blobUrl.Substring(0, blobUrl.IndexOf(";"));
                var width = double.Parse(sizeHeader.Substring(0, sizeHeader.IndexOf(":")));
                var height = double.Parse(sizeHeader.Substring(sizeHeader.IndexOf(":") + 1));
                blobUrl = blobUrl.Substring(blobUrl.IndexOf(";") + 1); // strip size header

                var element = new BlobElement
                {
                    Source = blobUrl,
                    Width = width,
                    Height = height
                };
                await element.LoadBlob();

                return element;
            });
            return result;
        }
        public async Task<string> GetPdfPageSizeRunList()
        {
            await InitAsync();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getPageSizeRunList");
        }

        #endregion Asynchronous Tasks
        #region Synchronous Tasks

        public void ConsoleLog(string message)
        {
            OpenSilver.Interop.ExecuteJavaScript("logToConsole($0)", message);
        }
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
        public int RenderThumbnailToViewport(int pageNumber, double posX, double posY, double width, double height, string canvasId)
        {
            var result = OpenSilver.Interop.ExecuteJavaScript("renderThumbnailToViewport($0,$1,$2,$3,$4,$5)", pageNumber, posX, posY, width, height, canvasId);
            var page = (int)Convert.ChangeType(result, typeof(int));
            return page;
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
        public void InvalidateThumbnailCache()
        {
            OpenSilver.Interop.ExecuteJavaScript("invalidateThumbnailCache()");
        }
        public void ClearViewport(string canvasId)
        {
            OpenSilver.Interop.ExecuteJavaScript("clearViewport($0)", canvasId);
        }
        public TextMetrics GetTextMetrics(string text, string font)
        {
            var result = OpenSilver.Interop.ExecuteJavaScript("getTextMetrics($0,$1)", text, font);
            var json = (string)Convert.ChangeType(result, typeof(string));
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var textMetrics = JsonSerializer.Deserialize<TextMetrics>(json, options);
            return textMetrics;
        }

        #endregion Synchronous Tasks
    }
}
