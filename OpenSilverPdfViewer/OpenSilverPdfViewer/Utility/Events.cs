
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.Utility
{
    public delegate void RenderCompleteEventHandler(object sender, RenderCompleteEventArgs e);
    public class RenderCompleteEventArgs : EventArgs
    {
        public List<Grid> Thumbnails { get; set; }

        public RenderCompleteEventArgs(List<Grid> thumbnails)
        {
            Thumbnails = thumbnails;
        }
    }
}