
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Renderer
{
    public delegate void WorkerCompleteDelegate<T>(int pageNumber, T result, bool cancelled);

    public sealed class RenderQueue<T>
    {
        #region Fields / Properties

        private readonly List<RenderWorker<T>> _workers = new List<RenderWorker<T>>();
        private WorkerCompleteDelegate<T> RenderCompleteCallback { get; set; }
        private DispatcherTimer DebounceRenderTimer { get; set; }

        #endregion Fields / Properties
        #region Initialization

        public RenderQueue(WorkerCompleteDelegate<T> workerCompleteDelegate)
        {
            if (!(typeof(T) == typeof(Image) || typeof(T) == typeof(BlobElement) || typeof(T) == typeof(JSImageReference)))
                throw new Exception($"Invalid type: {typeof(T).Name}. RenderQueue can only be used with Image, BlobElement or JSImageReference types");

            DebounceRenderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            DebounceRenderTimer.Tick += DebounceRenderTimer_Expired;
            RenderCompleteCallback = workerCompleteDelegate;
        }

        #endregion Initialization
        #region Implementation

        public void QueueItem(int pageNumber, double scaleFactor)
        {
            var renderWorker = _workers.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker == null)
            {
                var worker = new RenderWorker<T>(pageNumber, scaleFactor, WorkerCompleted);
                _workers.Add(worker);
                StartDebounceRenderTimer();
            }
        }
        public void DequeueItem(int pageNumber)
        {
            var renderWorker = _workers.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker != null)
            {
                renderWorker.Cancel();
                _workers.Remove(renderWorker);
                StartDebounceRenderTimer();
            }
        }
        private void StartDebounceRenderTimer()
        {
            DebounceRenderTimer.Stop();
            DebounceRenderTimer.Interval = TimeSpan.FromMilliseconds(500);
            DebounceRenderTimer.Start();
        }

        #endregion Implementation
        #region Event Handlers

        private void DebounceRenderTimer_Expired(object sender, EventArgs e)
        {
            // Defer rendering until the viewport scroll position has settled for a bit
            DebounceRenderTimer.Stop();
            _workers.ForEach(worker => worker.Start());
        }
        private void WorkerCompleted(int pageNumber, T result, bool cancelled)
        {
            var renderWorker = _workers.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker != null)
                _workers.Remove(renderWorker);

            if (!cancelled)
                RenderCompleteCallback(pageNumber, result, false);
        }

        #endregion Event Handlers
    }
    internal sealed class RenderWorker<T>
    {
        #region Fields / Properties

        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        public int PageNumber { get; private set; }
        private readonly double _scaleFactor;
        private WorkerCompleteDelegate<T> ItemComplete { get; set; }

        #endregion Fields / Properties
        #region Initialization

        public RenderWorker(int pageNumber, double scaleFactor, WorkerCompleteDelegate<T> callback) 
        {
            ItemComplete = callback;
            PageNumber = pageNumber;
            _scaleFactor = scaleFactor;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        #endregion Initialization
        #region Implementation

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            T result = e.Cancelled ? default : (T)e.Result;
            ItemComplete(PageNumber, result, e.Cancelled);
        }
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            if (!worker.CancellationPending)
            {
                if (typeof(T) == typeof(Image))
                {
                    var task = PdfJs.GetPdfPageImageAsync(PageNumber, _scaleFactor);
                    task?.Wait();
                    e.Result = task.Result;
                }
                else if (typeof(T) == typeof(BlobElement))
                {
                    var task = PdfJs.GetPdfPageBlobElementAsync(PageNumber, _scaleFactor);
                    task?.Wait();
                    e.Result = task.Result;
                }
                else if (typeof(T) == typeof(JSImageReference))
                {
                    var task = PdfJs.RenderThumbnailToCacheAsync(PageNumber, _scaleFactor);
                    task?.Wait();
                    e.Result = new JSImageReference(PageNumber, (CacheStatus)task.Result);
                }

                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    if (e.Result is BlobElement blob)
                        blob.InvalidateImage();
                }
            }
            else
                e.Cancel = true;
        }
        internal void Start() 
        {
            if (!_worker.IsBusy)
                _worker.RunWorkerAsync();
        }
        internal void Cancel()
        {
            if (_worker.IsBusy)
                _worker.CancelAsync();
        }

        #endregion Implementation
    }
}
