
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
        private readonly List<RenderWorker<T>> _workers = new List<RenderWorker<T>>();
        private WorkerCompleteDelegate<T> RenderCompleteCallback { get; set; }
        private DispatcherTimer DebounceRenderTimer { get; set; }

        public RenderQueue(WorkerCompleteDelegate<T> workerCompleteDelegate)
        {
            if (!(typeof(T) == typeof(Image) || typeof(T) == typeof(BlobElement)))
                throw new Exception($"Invalid type: {typeof(T)}. RenderQueue can only be used with Image or BlobElement types");

            DebounceRenderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            DebounceRenderTimer.Tick += DebounceRenderTimer_Expired;
            RenderCompleteCallback = workerCompleteDelegate;
        }
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
    }
    internal sealed class RenderWorker<T>
    {
        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        public int PageNumber { get; private set; }
        private readonly double _scaleFactor;
        private WorkerCompleteDelegate<T> ItemComplete { get; set; }

        public RenderWorker(int pageNumber, double scaleFactor, WorkerCompleteDelegate<T> callback) 
        {
            ItemComplete = callback;
            PageNumber = pageNumber;
            _scaleFactor = scaleFactor;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }
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
            _worker.RunWorkerAsync();
        }
        internal void Cancel()
        {
            if (_worker.IsBusy)
                _worker.CancelAsync();
        }
    }
}
