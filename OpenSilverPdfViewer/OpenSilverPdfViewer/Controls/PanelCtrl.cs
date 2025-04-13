using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.Controls
{
    internal sealed class PanelCtrl : ContentControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(PanelCtrl),
            new PropertyMetadata(""));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        static PanelCtrl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PanelCtrl), new FrameworkPropertyMetadata(typeof(PanelCtrl)));
        }

        public PanelCtrl()
        {
            // this.DefaultStyleKey = typeof(PanelCtrl);
            SizeChanged += PanelCtrl_SizeChanged;
        }
        /*
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
        */
        private void PanelCtrl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }
    }
}
