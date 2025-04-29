
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
        private DispatcherTimer Timer { get; set; } 
        public int SettleTime { get; set; }

        public Debouncer(int settleTime = 1000)
        {
            SettleTime = settleTime;
            Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(settleTime) };
            Timer.Tick += Timer_Tick;
        }
        public Debouncer(Action onSettle, int settleTime = 1000) : this(settleTime)
        {
            OnSettled = onSettle;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            Timer.Stop();
            OnSettled();
        }
        public void Reset()
        {
            Timer.Stop();
            Timer.Interval = TimeSpan.FromMilliseconds(SettleTime);
            Timer.Start();
        }
        public void Stop()
        {
            Timer.Stop();
        }
    }
}
