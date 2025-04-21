
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Threading.Tasks;

using CSHTML5.Native.Html.Controls;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Utility
{
    public sealed class BlobElement : HtmlCanvasElement
    {
        private object _jsImage;
        public double Width;
        public double Height;
        public double Scale = 1.0;
        private string _sourceCache;
        private string _source;
        private bool _isValid;

        public string Source
        {
            get { return _source; }
            set
            {
                if (value.StartsWith("blob:") == false)
                    throw new Exception("BlobElement source can only be a blob Url");

                if (value != _sourceCache && !string.IsNullOrEmpty(_sourceCache))
                    InvalidateImage();

                _source = value;
                _sourceCache = value;
            }
        }

        public BlobElement() : base()
        {
            _jsImage = OpenSilver.Interop.ExecuteJavaScript("document.createElement('img')");
        }

        public override ElementStyle Draw(ElementStyle currentDrawingStyle, object jsContext2d, double xParent = 0, double yParent = 0)
        {
            if (this.Visibility == Visibility.Visible)
            {
                currentDrawingStyle = this.ApplyStyle(currentDrawingStyle, jsContext2d);

                if (!_isValid)
                    throw new Exception("Cannot draw image prior to load. Call LoadBlob() first");

                OpenSilver.Interop.ExecuteJavaScriptAsync("$0.drawImage($1, $2, $3, $4, $5)", 
                    jsContext2d, 
                    _jsImage, 
                    X + xParent, 
                    Y + yParent, 
                    Width * Scale, 
                    Height * Scale);
            }
            return currentDrawingStyle;
        }

        public async Task<bool> LoadBlob()
        {
            _isValid = await JSAsyncTaskRunner.RunJavaScriptAsync<bool>("loadBlobImageAsync", _jsImage, Source);
            return _isValid;
        }

        public void InvalidateImage()
        {
            OpenSilver.Interop.ExecuteJavaScript("URL.revokeObjectURL($0)", Source);
            _isValid = false;
        }

        public override bool IsPointed(double x, double y)
        {
            return x >= this.X && x < this.X + this.Width && y >= this.Y && y < this.Y + this.Height;
        }
    }
}
