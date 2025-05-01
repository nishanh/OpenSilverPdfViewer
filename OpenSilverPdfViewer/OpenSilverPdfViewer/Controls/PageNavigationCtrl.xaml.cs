
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.CompilerServices;

using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Controls
{
    public partial class PageNavigationCtrl : INotifyPropertyChanged
    {
        #region Fields / Properties

        private string _validNumericText = "";

        #endregion Fields / Properties
        #region Dependency Properties

        /// <summary>
        /// This property reflects the current page to be displayed in the preview
        /// </summary>
        public static readonly DependencyProperty NavigationPageProperty = DependencyProperty.Register("NavigationPage", typeof(int), typeof(PageNavigationCtrl),
            new PropertyMetadata(1, OnNavigationPageChanged));

        /// <summary>
        /// This property reflects the current navigation mode (page, side, sheet, etc)
        /// </summary>
        public static readonly DependencyProperty NavigationModeProperty = DependencyProperty.Register("NavigationMode", typeof(ViewModeType), typeof(PageNavigationCtrl),
            new PropertyMetadata(ViewModeType.PageView, OnNavigationModeChanged));

        /// <summary>
        /// This property reflects the total number of pages (aka "sheet sides") in the document
        /// </summary>
        public static readonly DependencyProperty PageCountProperty = DependencyProperty.Register("PageCount", typeof(int), typeof(PageNavigationCtrl),
            new PropertyMetadata(1, OnPageCountChanged));

        /// <summary>
        /// Simplified accessor property for the NavigationPageProperty dependency property
        /// </summary>
        public int NavigationPage
        {
            get => (int)GetValue(NavigationPageProperty);
            set => SetValue(NavigationPageProperty, value);
        }

        /// <summary>
        /// Simplified accessor property for the NavigationModeProperty dependency property
        /// </summary>
        public ViewModeType NavigationMode
        {
            get => (ViewModeType)GetValue(NavigationModeProperty);
            set => SetValue(NavigationModeProperty, value);
        }

        /// <summary>
        /// Simplified accessor property for the PageCountProperty dependency property
        /// </summary>
        public int PageCount
        {
            get => (int)GetValue(PageCountProperty);
            set => SetValue(PageCountProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Callbacks

        /// <summary>
        /// Invoked when the document page count changes. Updates the control to show the new page count
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="e"></param>
        private static void OnPageCountChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageNavigationCtrl;
            ctrl?.UpdateControls();
        }
        /// <summary>
        /// Invoked when the current navigation page changes. Updates the control to show the new navigation page
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="e"></param>
        private static void OnNavigationPageChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageNavigationCtrl;
            ctrl?.UpdateControls();
        }

        /// <summary>
        /// Invoked when the current navigation mode changes. Updates the control to show the new navigation mode
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="e"></param>
        private static void OnNavigationModeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as PageNavigationCtrl;
            ctrl?.UpdateControls();
        }

        #endregion Dependency Property Callbacks
        #region Accessor Properties

        /// <summary>
        /// This property formats the text that gets displayed in the body of the control
        /// </summary>
        public string PageNavText
        {
            get
            {
                if (PageCount == 0)
                    return "No Pages";
                
                if (NavigationMode == ViewModeType.ThumbnailView)
                    return "Multi Page";

                var format = "Page {0} of {1}";
                return string.Format(format, NavigationPage, PageCount);
            }
        }
        /// <summary>
        /// Property that control the enable-state of the first page / previous page navigation buttons
        /// </summary>
        public bool IsNotFirstPage => NavigationMode == ViewModeType.PageView && NavigationPage > 0 && NavigationPage > 1;

        /// <summary>
        /// Property that control the enable-state of the last page / next page navigation buttons
        /// </summary>
        public bool IsNotLastPage => NavigationMode == ViewModeType.PageView && NavigationPage > 0 && NavigationPage < PageCount;

        /// <summary>
        /// Property that keeps track of the shift-button keyboard state while editable portion
        /// of the combobox has the focus
        /// </summary>
        private bool ShiftDown { get; set; }

        #endregion Accessor Properties
        #region Construction / Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        public PageNavigationCtrl()
        {
            InitializeComponent();
        }

        #endregion Construction / Initialization
        #region Event Handlers

        /// <summary>
        /// Invoked when 'first page' navigation button is clicked. Set the current navigation page to the first page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationPage = 1;
            UpdateControls();
        }
        /// <summary>
        /// Invoked when 'previous page' navigation button is clicked. Set the current navigation page to the previous page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationPage > 1)
            {
                NavigationPage--;
                UpdateControls();
            }
        }
        /// <summary>
        /// Invoked when 'next page' navigation button is clicked. Set the current navigation page to the next page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationPage < PageCount)
            {
                NavigationPage++;
                UpdateControls();
            }
        }
        /// <summary>
        /// Invoked when 'last page' navigation button is clicked. Set the current navigation page to the last page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationPage = PageCount;
            UpdateControls();
        }
        /// <summary>
        /// This sends out the property notificationm messages that causes the GUI bindings to re-evaluate 
        /// these properties and set their state accordingly
        /// </summary>
        private void PageNavTextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigationMode == ViewModeType.PageView)
                SetTextBoxEditState(true);
        }
        /// <summary>
        /// This consumes the value entered into the page navigation textbox and then clears the editing state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageNavTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SetTextBoxEditState(false);
        }
        /// <summary>
        /// Invoked as part of the shift-key tracking strategy. Clears the tracking flag
        /// if the shift-keys are released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageNavTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox))
                return;

            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                // Get the entered text
                var text = textBox.Text;

                // Attempt to parse the entered text to an integer
                if (int.TryParse(text, out int pageNum))
                {
                    _validNumericText = text;
                    
                    // Constrain the value to the page count range
                    pageNum = ExtensionMethods.BoundedValue(pageNum, 1, PageCount);

                    // Make that value the current zoom level
                    NavigationPage = pageNum;

                    SetTextBoxEditState(false);

                    // Accept the key press
                    e.Handled = false;
                }
                else
                {
                    textBox.Text = _validNumericText;
                }
            }
        }
        /// <summary>
        /// This method sets or clears the page navigation textbox editing state
        /// </summary>
        /// <param name="canEdit"></param>
        private void SetTextBoxEditState(bool canEdit)
        {
            if (canEdit)
            {
                tbCurrentPage.Visibility = Visibility.Collapsed;
                bxCurrentPage.Foreground = FindResource("CMSForegroundBrush") as Brush;
                bxCurrentPage.SelectionBackground = FindResource("CMSSpinTextSelectionBrush") as Brush;
                bxCurrentPage.IsReadOnly = false;
                bxCurrentPage.Focus();
                bxCurrentPage.SelectAll();
            }
            else
            {
                bxCurrentPage.Text = NavigationPage.ToString();
                bxCurrentPage.SelectionBackground = new SolidColorBrush(Colors.Transparent);
                bxCurrentPage.Foreground = FindResource("CMSSolidBorderBrush") as Brush; 
                bxCurrentPage.IsReadOnly = true;
                tbCurrentPage.Visibility = Visibility.Visible;
                focusSink.Focus(); // Removes the caret from the textbox above this in the z-order
            }
            UpdateControls();
        }
        internal void UpdateControls()
        {
            OnPropertyChanged(nameof(PageNavText));
            OnPropertyChanged(nameof(IsNotFirstPage));
            OnPropertyChanged(nameof(IsNotLastPage));
        }

        #endregion Event Handlers
        #region Property Notification Support

        /// <summary>
        /// The property notification event and notifier method
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Property Notification Support
    }
}
