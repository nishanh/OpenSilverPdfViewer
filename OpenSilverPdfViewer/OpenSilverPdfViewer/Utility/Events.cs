using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.Utility
{
    public delegate void RenderCompleteEventHandler(object sender, RenderCompleteEventArgs e);
    public class RenderCompleteEventArgs : EventArgs
    {
        public RenderCompleteEventArgs(List<Grid> thumbnails)
        {
            Thumbnails = thumbnails;
        }

        public List<Grid> Thumbnails { get; set; }
    }
}