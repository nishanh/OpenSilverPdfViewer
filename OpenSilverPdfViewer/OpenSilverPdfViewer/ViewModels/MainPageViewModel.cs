
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using OpenSilverPdfViewer.JSInterop;
using OpenSilver.IO;
using System.IO;
using Microsoft.JSInterop;

#pragma warning disable CS0067 // The event 'CanExecuteChanged' is never used

namespace OpenSilverPdfViewer.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private const string viewCanvasId = "pageViewCanvas";
        #region Properties

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

        private string _pageSizeText;
        public string PageSizeText
        {
            get => _pageSizeText;
            set
            {
                if (_pageSizeText != value)
                {
                    _pageSizeText = value;
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

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    CurrentPageChanged.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set
            {
                if (_pageCount != value)
                {
                    _pageCount = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Properties
        #region Commands

        private ICommand _loadPdfCommand;
        public ICommand LoadPdfCommand 
        { 
            get
            {
                return _loadPdfCommand ?? (_loadPdfCommand = new DelegateCommand((param) => LoadPdf(param), (param) => true));
            }
        }

        #endregion Commands

        public MainPageViewModel()
        {
            CurrentPageChanged += MainPageViewModel_CurrentPageChanged;
        }

        private async void MainPageViewModel_CurrentPageChanged(object sender, EventArgs e)
        {
            await RenderCurrentPage();
        }

        public async void LoadPdf(object param)
        {
            var sourceOption = param as string;
            if (string.IsNullOrEmpty(sourceOption))
                return;

            int pageCount;
            string baseFileName;
            
            if (sourceOption == "file")
            {
                (string filename, int pages) = await LoadPdfFile();
                baseFileName = filename;
                pageCount = pages;
            }
            else
            {
                baseFileName = sourceOption == "sample1" ? "POH_Calidus_4.0_EN.pdf" : "compressed.tracemonkey-pldi-09.pdf";
                pageCount = await PdfJs.LoadPdfFile($@"Data\{baseFileName}");
            }

            IsPdfLoaded = true;
            CurrentPage = 1;

            await RenderCurrentPage();
            StatusText = $"PDF - {baseFileName} loaded with {pageCount} pages";
            PageCount = pageCount;
        }

        public async Task<(string, int)> LoadPdfFile()
        {
            string filename = string.Empty;
            int pageCount = 0;

            var dlg = new OpenSilver.Controls.OpenFileDialog
            {
                Multiselect = false,
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            
            var result = await dlg.ShowDialogAsync();

            if ((bool)result == true)
            {
                filename = dlg.File.Name;
                using (var pdfStream = dlg.File.OpenRead() as MemoryStream)
                {
                    var base64Stream = Convert.ToBase64String(pdfStream.ToArray());
                    pageCount = await PdfJs.LoadPdfFileStream(base64Stream);
                }
            }
            return (filename, pageCount);
        }

        public async Task RenderCurrentPage()
        {
            if (IsPdfLoaded)
            {
                await PdfJs.RenderPageToViewport(CurrentPage, viewCanvasId);
                var size = await PdfJs.GetPdfPageSize(CurrentPage);
                PageSizeText = $"Page size: { Math.Round(size.Width / 72d, 2) } x {Math.Round(size.Height / 72d, 2)} in";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private event EventHandler CurrentPageChanged;
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
