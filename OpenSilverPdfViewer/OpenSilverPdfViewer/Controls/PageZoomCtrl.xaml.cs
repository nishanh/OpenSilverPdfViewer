
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageZoomCtrl : UserControl
    {
        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(int), typeof(PageZoomCtrl),
            new PropertyMetadata(1, OnZoomLevelChanged));

        public int ZoomLevel
        {
            get => (int)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }
        private static void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageZoomCtrl;
            var zoom = (int)e.NewValue;
            // ctrl?.UpdateControls();
        }

        public PageZoomCtrl()
        {
            this.InitializeComponent();
        }
        public void ZoomLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox))
                return;


            // If the selection has been simply invalidated, return.
            if (e.AddedItems.Count == 0) return;

            // Get the selected option
            var itemSelect = e.AddedItems[0] as ComboBoxItem;
            var selection = itemSelect.Content.ToString();
            var selectedIndex = (sender as ComboBox).SelectedIndex;

            // If 'Fit to View' selected, set the integer zoom value to zero, else parse the zoom level value
            ZoomLevel = selectedIndex == 0 ? 0 : int.Parse(selection.Trim('%'));
        }
    }
}
