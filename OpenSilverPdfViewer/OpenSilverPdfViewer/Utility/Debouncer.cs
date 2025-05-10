
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Threading;

namespace OpenSilverPdfViewer.Utility
{
    public sealed class Debouncer
    {
        public Action OnSettled { get; set; }
        public bool IsSettled => _timer.IsEnabled == false;

        private readonly DispatcherTimer _timer;
        private readonly int _settleTime;

        public Debouncer(int settleTime = 1000)
        {
            _settleTime = settleTime;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(settleTime) };
            _timer.Tick += Timer_Tick;
        }
        public Debouncer(Action onSettle, int settleTime = 1000) : this(settleTime)
        {
            OnSettled = onSettle;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            OnSettled?.Invoke();
        }
        public void Reset()
        {
            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(_settleTime);
            _timer.Start();
        }
        public void Stop()
        {
            _timer.Stop();
        }
    }
}
