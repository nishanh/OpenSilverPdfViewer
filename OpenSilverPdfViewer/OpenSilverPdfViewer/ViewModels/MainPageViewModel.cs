
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.ViewModels
{
    public sealed class MainPageViewModel : INotifyPropertyChanged
    {
        #region Properties

        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;

        private string _filename = string.Empty;
        public string Filename
        {
            get { return _filename; }
            set 
            { 
                _filename = value;
                OnPropertyChanged();
            }
        }

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

        private int _currentPage = 0;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    ShowPageSize();
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

        private bool _canLoadPdf = false;
        public bool CanLoadPdf
        {
            get => _canLoadPdf;
            set
            {
                if (_canLoadPdf != value)
                {
                    _canLoadPdf = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _pageZoomLevel = 0;
        public int PageZoomLevel
        {
            get => _pageZoomLevel;
            set
            {
                if (_pageZoomLevel != value)
                {
                    _pageZoomLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _pageZoomValue = 100d;
        public double PageZoomValue
        {
            get => _pageZoomValue;
            set
            {
                if (_pageZoomValue != value)
                {
                    _pageZoomValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private RenderModeType _renderMode;
        public RenderModeType RenderMode
        {
            get => _renderMode;
            set
            {
                if (_renderMode != value)
                {
                    _renderMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAnimateThumbnails));
                }
            }
        }

        private UnitMeasure _rulerUnits = UnitMeasure.Imperial;
        public UnitMeasure RulerUnits
        {
            get => _rulerUnits;
            set
            {
                if (_rulerUnits != value)
                {
                    _rulerUnits = value;
                    OnPropertyChanged();
                    ShowPageSize();
                }
            }
        }

        private ThumbnailUpdateType _thumbnailUpdate = ThumbnailUpdateType.Random;
        public ThumbnailUpdateType ThumbnailUpdate
        {
            get => _thumbnailUpdate;
            set
            {
                if (_thumbnailUpdate != value)
                {
                    _thumbnailUpdate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAnimateThumbnails));
                }
            }
        }

        private bool _animateThumbnails = true;
        public bool AnimateThumbnails
        {
            get => _animateThumbnails;
            set
            {
                if (_animateThumbnails != value)
                {
                    _animateThumbnails = value;
                    OnPropertyChanged();
                }
            }
        }

        private ThumbnailSize _thumbnailSize = ThumbnailSize.Medium;
        public ThumbnailSize ThumbnailSize
        {
            get => _thumbnailSize;
            set
            {
                if (_thumbnailSize != value)
                {
                    _thumbnailSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanAnimateThumbnails => ThumbnailUpdate != ThumbnailUpdateType.WhenRendered && RenderMode == RenderModeType.OpenSilver;
        
        #endregion Properties
        #region Commands

        private ICommand _loadPdfCommand;
        public ICommand LoadPdfCommand 
        { 
            get
            {
                return _loadPdfCommand ?? (_loadPdfCommand = new DelegateCommand(param => LoadPdf(param), (param) => true));
            }
        }

        private ICommand _setRenderModeCommand;
        public ICommand SetRenderModeCommand
        {
            get
            {
                return _setRenderModeCommand ?? (_setRenderModeCommand = new DelegateCommand(param => { RenderMode = (RenderModeType)param; }, (param) => true));
            }
        }

        private ICommand _setRulerUnitsCommand;
        public ICommand SetRulerUnitsCommand
        {
            get
            {
                return _setRulerUnitsCommand ?? (_setRulerUnitsCommand = new DelegateCommand(param => { RulerUnits = (UnitMeasure)param; }, (param) => true));
            }
        }

        private ICommand _setThumbnailUpdateTypeCommand;
        public ICommand SetThumbnailUpdateTypeCommand
        {
            get
            {
                return _setThumbnailUpdateTypeCommand ?? (_setThumbnailUpdateTypeCommand = new DelegateCommand(param => { ThumbnailUpdate = (ThumbnailUpdateType)param; }, (param) => true));
            }
        }

        private ICommand _setAnimateThumbnailCommand;
        public ICommand SetAnimateThumbnailCommand
        {
            get
            {
                return _setAnimateThumbnailCommand ?? (_setAnimateThumbnailCommand = new DelegateCommand(param => { AnimateThumbnails = !AnimateThumbnails; }, (param) => true));
            }
        }

        private ICommand _setThumbnailSizeCommand;
        public ICommand SetThumbnailSizeCommand
        {
            get
            {
                return _setThumbnailSizeCommand ?? (_setThumbnailSizeCommand = new DelegateCommand(param => { ThumbnailSize = (ThumbnailSize)param; }, (param) => true));
            }
        }

        #endregion Commands
        #region Methods

        public async void LoadPdf(object param)
        {
            var sourceOption = param as string;
            if (string.IsNullOrEmpty(sourceOption))
                return;

            int pageCount;
            
            if (sourceOption == "file")
            {
                (string filename, int pages) = await LoadPdfFileStream();
                Filename = filename;
                pageCount = pages;
            }
            else
            {
                var filename = sourceOption == "sample1" ? "POH_Calidus_4.0_EN.pdf" : "compressed.tracemonkey-pldi-09.pdf";
                pageCount = await PdfJs.LoadPdfFileAsync($@"Data\{filename}");
                Filename = filename; // setting this property triggers the pageviewer, so set it *after* the document is loaded
            }
            StatusText = $"PDF - {Filename} loaded with {pageCount} pages";
            PageCount = pageCount;
        }
        public async Task<(string, int)> LoadPdfFileStream()
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
                    pageCount = await PdfJs.LoadPdfFileStreamAsync(base64Stream);
                }
            }
            return (filename, pageCount);
        }
        private async void ShowPageSize()
        {
            if (CurrentPage < 1 || CurrentPage > PageCount)
                return;

            var unitConvert = RulerUnits == UnitMeasure.Metric ? 25.4 : 1d;
            var unitSuffix = RulerUnits == UnitMeasure.Metric ? "mm" : "in";
            var size = await PdfJs.GetPdfPageSizeAsync(CurrentPage);
            var logicalWidth = Math.Round((size.Width / 72d) * unitConvert, 2);
            var logicalHeight = Math.Round((size.Height / 72d) * unitConvert, 2);

            PageSizeText = $"Page size: {logicalWidth} x {logicalHeight} {unitSuffix}";
        }

        #endregion Methods
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Events
    }
}
