
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using OpenSilverPdfViewer.JSInterop;

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
                return _loadPdfCommand ?? (_loadPdfCommand = new DelegateCommand((param) => LoadPdf(param), (param) => !IsPdfLoaded));
            }
        }

        #endregion Commands

        public async void LoadPdf(object param)
        {
            const string baseFileName = "POH_Calidus_4.0_EN.pdf";
            // const string baseFileName = "compressed.tracemonkey-pldi-09.pdf";

            var pageCount = await PdfJs.LoadPdfFile($@"Data\{baseFileName}");
            IsPdfLoaded = true;
            CurrentPage = 1;

            await RenderCurrentPage();
            StatusText = $"PDF - {baseFileName} loaded with {pageCount} pages";
        }

        public async Task RenderCurrentPage()
        {
            if (IsPdfLoaded)
                await PdfJs.RenderPageToViewport(CurrentPage, viewCanvasId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
