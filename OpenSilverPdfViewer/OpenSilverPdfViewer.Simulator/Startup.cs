﻿using System;
using OpenSilver.Simulator;

namespace OpenSilverPdfViewer.Simulator
{
    internal static class Startup
    {
        [STAThread]
        static int Main(string[] args)
        {
            return SimulatorLauncher.Start(typeof(App));
        }
    }
}
