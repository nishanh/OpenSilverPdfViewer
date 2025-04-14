using OpenSilverPdfViewer.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageViewer : UserControl
    {
        public PageViewer()
        {
            this.InitializeComponent();
        }
        public void Ctrl_Loaded(object sender, RoutedEventArgs e)
        {
            var dc = this.DataContext;
        }
    }
}
