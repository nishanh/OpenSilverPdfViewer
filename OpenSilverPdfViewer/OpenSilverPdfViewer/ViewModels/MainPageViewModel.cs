
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;

using OpenSilverPdfViewer.JSInterop;

#pragma warning disable CS0067 // The event 'CanExecuteChanged' is never used

namespace OpenSilverPdfViewer.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Interop;

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPdfLoaded = false;
        public bool IsPdfLoaded
        {
            get => _isPdfLoaded;
            set
            {
                if (_isPdfLoaded != value)
                {
                    _isPdfLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        private ICommand _loadPdfCommand;
        public ICommand LoadPdfCommand 
        { 
            get
            {
                return _loadPdfCommand ?? (_loadPdfCommand = new DelegateCommand((param) => LoadPdf(param), (param) => !IsPdfLoaded));
            }
        }

        public MainPageViewModel() { }

        public async void LoadPdf(object param)
        {
            var baseFileName = "POH_Calidus_4.0_EN.pdf";
            // var baseFileName = "compressed.tracemonkey-pldi-09.pdf";
            var fileName = $"Data\\{baseFileName}";
            var pageCount = await PdfJs.LoadPdfFile(fileName);

            await PdfJs.RenderPageToViewport(1, "pageViewCanvas");

            //var size = OpenSilver.Interop.ExecuteJavaScript("getViewportSize($0)", domId).ToString();
            //var sizeObj = ViewportSize.Deserialize(size);
            //MessageBox.Show($"Canvas size: {sizeObj.width} x {sizeObj.height}");

            StatusText = $"PDF {baseFileName} loaded with {pageCount} pages";
            //IsPdfLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ViewportSize
    {
        public double width;
        public double height;

        public ViewportSize(double width, double height) 
        {
            this.width = width;
            this.height = height;
        }
        public static ViewportSize Deserialize(string json)
        {
            var sizes = json.Trim('{').Trim('}').Split(',');
            var widthStr = sizes[0].Substring(sizes[0].IndexOf(':') + 1);
            var width = double.Parse(widthStr);
            var heightStr = sizes[1].Substring(sizes[1].IndexOf(':') + 1);
            var height = double.Parse(heightStr);
            return new ViewportSize(width, height);
        }
    }

    public class DelegateCommand : ICommand
    {
        private Predicate<object> _canExecute;
        private Action<object> _method;

        public DelegateCommand(Action<object> method, Predicate<object> canExecute)
        {
            _method = method;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _method(parameter);
        }
        public event EventHandler CanExecuteChanged;
    }
}
