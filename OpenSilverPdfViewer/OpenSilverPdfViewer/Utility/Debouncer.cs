
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Threading;

namespace OpenSilverPdfViewer.Utility
{
    public sealed class Debouncer
    {
        private Action OnTimeout { get; set; }
        private DispatcherTimer Timer { get; set; } 
        private readonly int _delayTime = 1000;

        public Debouncer(Action onTimeout, int delayTime = 1000)
        {
            OnTimeout = onTimeout;
            _delayTime = delayTime;
            Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayTime) };
            Timer.Tick += Timer_Tick;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            Timer.Stop();
            OnTimeout();
        }
        public void Reset()
        {
            Timer.Stop();
            Timer.Interval = TimeSpan.FromMilliseconds(_delayTime);
            Timer.Start();
        }
    }
}
