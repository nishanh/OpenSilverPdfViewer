
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using OpenSilverPdfViewer.Utility;
using OpenSilverPdfViewer.JSInterop;

namespace OpenSilverPdfViewer.Renderer
{
    public delegate void WorkerCompleteDelegate<T>(int pageNumber, T result, bool cancelled);
    public sealed class RenderQueue<T>
    {
        #region Fields / Properties

        private readonly List<RenderTask<T>> _taskList = new List<RenderTask<T>>();
        private WorkerCompleteDelegate<T> RenderCompleteCallback { get; set; }
        private Debouncer RenderRequestDebouncer { get; set; }
        private Debouncer RenderTaskAwaiter { get; set; }

        #endregion Fields / Properties
        #region Initialization

        public RenderQueue(WorkerCompleteDelegate<T> workerCompleteDelegate)
        {
            if (!(typeof(T) == typeof(Image) || typeof(T) == typeof(BlobElement) || typeof(T) == typeof(JSImageReference)))
                throw new Exception($"Invalid type: {typeof(T).Name}. ThreadedRenderQueue can only be used with Image, BlobElement or JSImageReference types");

            RenderCompleteCallback = workerCompleteDelegate;
            RenderRequestDebouncer = new Debouncer(() => StartWorkers(), 500);
            RenderTaskAwaiter = new Debouncer(() => UpdateWorkers(), 500);
        }

        #endregion Initialization
        #region Implementation

        public void QueueItem(int pageNumber, double scaleFactor)
        {
            var renderWorker = _taskList.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker == null)
            {
                var worker = new RenderTask<T>(pageNumber, scaleFactor);
                _taskList.Add(worker);
                RenderRequestDebouncer.Reset();
            }
        }
        public void DequeueItem(int pageNumber)
        {
            var renderWorker = _taskList.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker != null)
            {
                _taskList.Remove(renderWorker);
                RenderRequestDebouncer.Reset();
            }
        }
        public void UpdateWorkers()
        {
            var completedTasks = _taskList.Where(task => task.TaskWorker.IsCompleted).ToList();
            foreach (var worker in completedTasks)
            {
                var result = worker.TaskWorker.Result;
                RenderCompleteCallback(worker.PageNumber, result, false);
                _taskList.Remove(worker);
            }
            if (_taskList.Count > 0)
                RenderTaskAwaiter.Reset();
            else 
                RenderTaskAwaiter.Stop();
        }
        public void StartWorkers()
        {
            _taskList.ForEach(worker =>
            {
                if (worker.TaskWorker == null)
                    worker.Start();
            });
            RenderTaskAwaiter.Reset();
        }

        #endregion Implementation
    }
    public class RenderTask<T>
    {
        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        public int PageNumber { get; private set; }
        private readonly double _scaleFactor;
        public Task<T> TaskWorker { get; private set; }
        public RenderTask(int pageNumber, double scaleFactor)
        {
            PageNumber = pageNumber;
            _scaleFactor = scaleFactor;
        }
        internal void Start()
        {
            if (typeof(T) == typeof(Image))
            {
                PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to Image");
                TaskWorker = (Task<T>)(object)PdfJs.GetPdfPageImageAsync(PageNumber, _scaleFactor);
            }
            else if (typeof(T) == typeof(BlobElement))
            {
                PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to Blob");
                TaskWorker = (Task<T>)(object)PdfJs.GetPdfPageBlobElementAsync(PageNumber, _scaleFactor);
            }
            else
            {
                PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to internal cache");
                TaskWorker = (Task<T>)(object)PdfJs.RenderThumbnailToCacheAsync(PageNumber, _scaleFactor);
            }
        }
    }
    public sealed class ThreadedRenderQueue<T>
    {
        #region Fields / Properties

        private readonly List<ThreadedRenderWorker<T>> _workers = new List<ThreadedRenderWorker<T>>();
        private WorkerCompleteDelegate<T> RenderCompleteCallback { get; set; }
        private Debouncer RenderRequestDebouncer { get; set; }

        #endregion Fields / Properties
        #region Initialization

        public ThreadedRenderQueue(WorkerCompleteDelegate<T> workerCompleteDelegate)
        {
            if (!(typeof(T) == typeof(Image) || typeof(T) == typeof(BlobElement) || typeof(T) == typeof(JSImageReference)))
                throw new Exception($"Invalid type: {typeof(T).Name}. ThreadedRenderQueue can only be used with Image, BlobElement or JSImageReference types");

            RenderCompleteCallback = workerCompleteDelegate;
            RenderRequestDebouncer = new Debouncer(() => StartWorkers());
        }

        #endregion Initialization
        #region Implementation

        public void QueueItem(int pageNumber, double scaleFactor)
        {
            var renderWorker = _workers.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker == null)
            {
                var worker = new ThreadedRenderWorker<T>(pageNumber, scaleFactor, WorkerCompleted);
                _workers.Add(worker);
                RenderRequestDebouncer.Reset();
            }
        }
        public void DequeueItem(int pageNumber)
        {
            var renderWorker = _workers.FirstOrDefault(worker => worker.PageNumber == pageNumber);
            if (renderWorker != null)
            {
                renderWorker.Cancel();
                _workers.Remove(renderWorker);
                RenderRequestDebouncer.Reset();
            }
        }
        public void StartWorkers()
        {
            _workers.ForEach(worker => worker.Start());
        }
        #endregion Implementation
        #region Event Handlers

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
    internal sealed class ThreadedRenderWorker<T>
    {
        #region Fields / Properties

        private PdfJsWrapper PdfJs { get; } = PdfJsWrapper.Instance;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        public int PageNumber { get; private set; }
        private readonly double _scaleFactor;
        private WorkerCompleteDelegate<T> ItemComplete { get; set; }

        #endregion Fields / Properties
        #region Initialization

        public ThreadedRenderWorker(int pageNumber, double scaleFactor, WorkerCompleteDelegate<T> callback) 
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

        private async void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (typeof(T) == typeof(JSImageReference))
            {
                T result = default;
                if (!e.Cancelled)
                {
                    var cacheStatus = await (Task<int>)e.Result;
                    var jsImageRef = new JSImageReference(PageNumber, (CacheStatus)cacheStatus);
                    result = (T)Convert.ChangeType(jsImageRef, typeof(T));
                }
                ItemComplete(PageNumber, result, e.Cancelled);
            }
            else
            {
                T result = await (Task<T>)e.Result;

                if (e.Cancelled && result is BlobElement blob)
                    blob.InvalidateImage();

                ItemComplete(PageNumber, e.Cancelled ? default : result, e.Cancelled);
            }
        }

        // Marshall the task back to the main thread to await completion
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            if (!worker.CancellationPending)
            {
                if (typeof(T) == typeof(Image))
                {
                    PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to Image");
                    var task = PdfJs.GetPdfPageImageAsync(PageNumber, _scaleFactor);
                    e.Result = task;
                }
                else if (typeof(T) == typeof(BlobElement))
                {
                    PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to Blob");
                    var task = PdfJs.GetPdfPageBlobElementAsync(PageNumber, _scaleFactor);
                    e.Result = task;
                }
                else if (typeof(T) == typeof(JSImageReference))
                {
                    PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to internal cache");
                    var task = PdfJs.RenderThumbnailToCacheAsync(PageNumber, _scaleFactor);
                    e.Result = task;
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
        [Obsolete]
        private void Worker_RunWorkerCompletedOrg(object sender, RunWorkerCompletedEventArgs e)
        {
            T result = e.Cancelled ? default : (T)e.Result;
            ItemComplete(PageNumber, result, e.Cancelled);
        }
        // Sadly, this does not work in the browser environment since there is no thread synchronization context that
        // can make tasks awaitable in a background thread (in wasm, I guess). Works in the simulator though, so go figure...
        [Obsolete]
        private void Worker_DoWorkOrg(object sender, DoWorkEventArgs e)
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
                    PdfJs.ConsoleLog($"Worker {PageNumber} is rendering to Blob");
                    var task = PdfJs.GetPdfPageBlobElementAsync(PageNumber, _scaleFactor);
                    task?.Wait();
                    e.Result = task.Result;
                }
                else if (typeof(T) == typeof(JSImageReference))
                {
                    var task = PdfJs.RenderThumbnailToCacheAsync(PageNumber, _scaleFactor);
                    task?.Wait();
                    PdfJs.ConsoleLog($"Task complete: {task.Result}");
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

        #endregion Implementation
    }
}
