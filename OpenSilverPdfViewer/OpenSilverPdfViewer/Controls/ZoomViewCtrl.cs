
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Controls
{
    [TemplateVisualState(Name = "CommonStates", GroupName = "CommonStates")]
    [TemplatePart(Name = "PART_DecrementButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_IncrementButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_ComboBox", Type = typeof(ComboBox))]
    [TemplatePart(Name = "PART_ZoomValueText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_TextBlockHitTester", Type = typeof(Border))]
    [TemplatePart(Name = "PART_EditableTextBox", Type = typeof(TextBox))]
    public sealed class ZoomViewCtrl : Control
    {
        #region Constants

        private const string DecrementButtonPart = "PART_DecrementButton";
        private const string IncrementButtonPart = "PART_IncrementButton";
        private const string ComboBoxPart = "PART_ComboBox";
        private const string TextBlockPart = "PART_ZoomValueText";
        private const string TextBlockBorderPart = "PART_TextBlockHitTester";
        private const string TextBoxPart = "PART_EditableTextBox";

        #endregion Constants
        #region Dependency Properties

        /// <summary>
        /// This property reflects what is selected from the zoom control drop-down (as an integer). For "Fit to View," the value will be zero
        /// </summary>
        public static readonly DependencyProperty ZoomSelectionProperty = DependencyProperty.Register("ZoomSelection", typeof(int), typeof(ZoomViewCtrl),
            new PropertyMetadata(0, OnZoomSelectionChanged));

        /// <summary>
        /// This is the true zoom level as reported by the preview control. We use this value as the 'base' when using the increment and decrement 
        /// button or when clicking on the body of the combobox when 'Fit to View' is active.
        /// </summary>
        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(double), typeof(ZoomViewCtrl),
            new PropertyMetadata(0d, OnZoomLevelChanged));

        /// <summary>
        /// Invoked when the zoom selection changes. Sets the state of the increment / decrement buttons
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="e"></param>
        private static void OnZoomSelectionChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as ZoomViewCtrl;
            ctrl?.SetButtonEnableState((int)e.NewValue);
        }
        /// <summary>
        /// Invoked when the the 'true' zoom level changes. Stubbed out for now
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="e"></param>
        private static void OnZoomLevelChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (!(depObj is ZoomViewCtrl ctrl)) return;
        }
        /// <summary>
        /// Simplified accessor property for the ZoomSelectionProperty dependency property
        /// </summary>
        public int ZoomSelection
        {
            get => (int)GetValue(ZoomSelectionProperty);
            set => SetValue(ZoomSelectionProperty, value);
        }
        /// <summary>
        /// Simplified accessor property for the ZoomLevelProperty dependency property
        /// </summary>
        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }

        #endregion Dependency Properties
        #region Properties

        /// <summary>
        /// Reference to increment button element
        /// </summary>
        private RepeatButton IncrementButton { get; set; }

        /// <summary>
        /// Reference to decrement button element
        /// </summary>
        private RepeatButton DecrementButton { get; set; }

        /// <summary>
        /// Reference to combobox element
        /// </summary>
        private ComboBox ZoomComboBox { get; set; }

        /// <summary>
        /// Reference to the textbox element of the combobox element
        /// </summary>
        private TextBox ZoomTextBox { get; set; }
        private TextBlock ZoomValueTextBlock { get; set; }
        private Border TextBlockBorder { get; set; }

        /// <summary>
        /// Property that keeps track of the shift-button keyboard state while editable portion
        /// of the combobox has the focus
        /// </summary>
        private bool ShiftDown { get; set; }

        /// <summary>
        /// The delta by which the prevailing zoom level is incremented or decremented by the relevant
        /// buttons. Also serves as the minimum zoom level.
        /// </summary>
        private int ZoomChange { get; } = 10;

        /// <summary>
        /// Property that controls the maximum zoom level
        /// </summary>
        private int MaxZoom { get; } = 200;

        /// <summary>
        /// List of available zoom level options in the combobox drop-down list
        /// </summary>
        public ObservableCollection<string> ZoomOptions { get; set; }

        #endregion Properties
        #region Construction / Initialization

        static ZoomViewCtrl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomViewCtrl), new FrameworkPropertyMetadata(typeof(ZoomViewCtrl)));
        }

        /// <summary>
        /// Constructor. Sets up the zoom selection list and initial event-handlers
        /// </summary>
        public ZoomViewCtrl()
        {
            ZoomOptions = new ObservableCollection<string>
            {
                "Fit to View",
                "25%", "50%", "75%", "100%", "125%", "150%", "175%", "200%"
            };
            // MouseLeftButtonDown += ZoomViewCtrl_PreviewMouseLeftButtonDown;
        }

        /// <summary>
        /// This method obtains references to the constituent elements of the control and attaches the
        /// relevant event-handlers
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Remove prior event handler

            ZoomValueTextBlock = GetTemplateChild(TextBlockPart) as TextBlock;
            TextBlockBorder = GetTemplateChild(TextBlockBorderPart) as Border;

            if (DecrementButton != null)
                DecrementButton.Click -= DecrementButton_Click;

            // Obtain decrement button element
            DecrementButton = GetTemplateChild(DecrementButtonPart) as RepeatButton;

            // Assign button handler
            if (DecrementButton != null)
                DecrementButton.Click += DecrementButton_Click;

            // Remove prior event handler
            if (IncrementButton != null)
                IncrementButton.Click -= IncrementButton_Click;

            // Obtain increment button element
            IncrementButton = GetTemplateChild(IncrementButtonPart) as RepeatButton;

            // Assign button handler
            if (IncrementButton != null)
                IncrementButton.Click += IncrementButton_Click;

            if (ZoomTextBox != null)
            {
                ZoomTextBox.KeyDown -= TextBox_PreviewKeyDown;
                ZoomTextBox.KeyUp -= TextBox_PreviewKeyUp;
            }

            ZoomTextBox = GetTemplateChild(TextBoxPart) as TextBox;

            if (ZoomTextBox != null)
            {
                ZoomTextBox.KeyDown += TextBox_PreviewKeyDown;
                ZoomTextBox.KeyUp += TextBox_PreviewKeyUp;
            }

            // Remove prior event handlers
            if (ZoomComboBox != null)
            {
                ZoomComboBox.SelectionChanged -= ZoomComboBox_SelectionChanged;
                ZoomComboBox.DropDownOpened -= ZoomComboBox_DropDownOpened;
            }

            // Obtain combobox element
            ZoomComboBox = GetTemplateChild(ComboBoxPart) as ComboBox;

            // Attach event-handlers
            if (ZoomComboBox != null)
            {
                ZoomComboBox.ItemsSource = ZoomOptions;
                ZoomComboBox.SelectionChanged += ZoomComboBox_SelectionChanged;
                ZoomComboBox.DropDownOpened += ZoomComboBox_DropDownOpened;
                ZoomComboBox.SelectedIndex = 0;
            }
        }

        #endregion Construction / Initialization
        #region GUI Event Handlers

        /// <summary>
        /// Invoked when the combobox drop-list opens. Clear the edit-state of the combobox textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomComboBox_DropDownOpened(object sender, EventArgs e)
        {
            SetTextBoxEditState(false);
        }

        /// <summary>
        /// Invoked when the user makes a new selection from the combobox drop-down list.
        /// Set the selection as the active zoom level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox)) return;

            SetTextBoxEditState(false);

            // If the selection has been simply invalidated, return.
            if (e.AddedItems.Count == 0) return;

            // Get the selected option
            var selection = (string)e.AddedItems[0];

            // If 'Fit to View' selected, set the integer zoom value to zero, else parse the zoom level value
            ZoomSelection = selection == ZoomOptions[0] ? 0 : int.Parse(selection.Trim('%'));

            //ZoomValueTextBlock.Visibility = Visibility.Collapsed;
            ZoomValueTextBlock.Text = "";
        }

        /// <summary>
        /// Invoked when the increment button is clicked. If the ZoomSelection is zero (Fit to View), then
        /// use the 'true' zoom level as the base value to increment from
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            SetTextBoxEditState(false);

            // Use true zoom level if 'Fit to View' is active
            if (ZoomSelection == 0) ZoomSelection = (int)ZoomLevel;

            // Constrain the value to the maximum zoom level
            ZoomSelection = Math.Min(MaxZoom, ZoomSelection + ZoomChange);

            // Tack on a percent symbol to the displayed zoom value
            //ZoomValueTextBlock.Visibility = Visibility.Visible;
            ZoomValueTextBlock.Text = ZoomSelection + "%";

            // Invalidate the selected index. Without this, we can't re-select the
            // last valid selection from the combobox drop-down list
            ZoomComboBox.SelectedIndex = -1;
        }

        /// <summary>
        /// Invoked when the decrement button is clicked. If the ZoomSelection is zero (Fit to View), then
        /// use the 'true' zoom level as the base value to decrement from
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            SetTextBoxEditState(false);

            // Use true zoom level if 'Fit to View' is active
            if (ZoomSelection == 0) ZoomSelection = (int)ZoomLevel;

            // Constrain the value to the minimum zoom level
            ZoomSelection = Math.Max(ZoomChange, ZoomSelection - ZoomChange);

            // Tack on a percent symbol to the displayed zoom value
            //ZoomValueTextBlock.Visibility = Visibility.Visible;
            ZoomValueTextBlock.Text = ZoomSelection + "%";

            // Invalidate the selected index. Without this, we can't re-select the
            // last valid selection from the combobox drop-down list
            ZoomComboBox.SelectedIndex = -1;
        }

        /// <summary>
        /// Invoked when the user clicks the mouse on the textbox element of the zoom control.
        /// If 'Fit to View' is active, we want to display the 'true' zoom level in the textbox
        /// as the base value for the user to modify.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomViewCtrl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textRect = new Rect(0, 0, TextBlockBorder.ActualWidth, TextBlockBorder.ActualHeight);
            var text = "";

            if (textRect.Contains(e.GetPosition(ZoomTextBox)))
            {

                // If 'Fit to View' is the current selection...
                if (ZoomSelection == 0)
                {
                    // Invalidate the selection, so that it can be re-selected
                    ZoomComboBox.SelectedIndex = -1;

                    // Get the 'true' zoom level as an integer and display it in the textbox
                    var zoom = (int)ZoomLevel;
                    text = zoom.ToString();
                    ZoomSelection = zoom;
                }

                // If not 'Fit to View' remove the percent symbol from the displayed value 
                // prior to allowing the user to edit it
                else
                    text = ZoomTextBox.Text.Trim('%');

                // Display the modified value in the textbox
                ZoomTextBox.Text = text;
                SetTextBoxEditState(true);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Invoked as part of the shift-key tracking strategy. Clears the tracking flag
        /// if the shift-keys are released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox)) return;

            if (e.Key == Key.Shift)
            {
                ShiftDown = false;
                e.Handled = false;
            }
        }

        /// <summary>
        /// Invoked when the user presses a key on textbox element of the combobox, but before
        /// the character is actually displayed. This gives us the opportunity to filter out
        /// unwanted characters. As this is primarily a numeric control, we want to constrain 
        /// the values to numeric entries and some control-keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            // Setting handled to true prevents further processing by other chained event-handlers.
            // In this case, 'handled = true' means to reject the key-press. We'll assume that by default.
            var handled = true;

            // Accept the shift-keys, but keep track of their state. We don't want the symbols above the number keys
            if (e.Key == Key.Left)
            {
                ShiftDown = true;
                e.Handled = false;
            }

            // If the shift-key is not being held down...
            else if (!ShiftDown)
            {
                // Accept all numeric key presses
                if (e.Key == Key.D0 || e.Key == Key.NumPad0 ||
                    e.Key == Key.D1 || e.Key == Key.NumPad1 ||
                    e.Key == Key.D2 || e.Key == Key.NumPad2 ||
                    e.Key == Key.D3 || e.Key == Key.NumPad3 ||
                    e.Key == Key.D4 || e.Key == Key.NumPad4 ||
                    e.Key == Key.D5 || e.Key == Key.NumPad5 ||
                    e.Key == Key.D6 || e.Key == Key.NumPad6 ||
                    e.Key == Key.D7 || e.Key == Key.NumPad7 ||
                    e.Key == Key.D8 || e.Key == Key.NumPad8 ||
                    e.Key == Key.D9 || e.Key == Key.NumPad9)
                {

                    handled = false;
                }

                // Accept these control-key presses
                else if (e.Key == Key.Back || e.Key == Key.Delete ||
                         e.Key == Key.Home || e.Key == Key.End)
                {
                    handled = false;
                }

                // If the 'Enter' is pressed, we want to immediately make whatever value the user
                // entered the current zoom level
                else if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    // Get the entered text
                    var text = textBox.Text;

                    // If the string is empty, make 'Fit to View' the active zoom selection
                    if (string.IsNullOrEmpty(text))
                    {
                        ZoomComboBox.SelectedIndex = 0;
                        ZoomSelection = 0;
                    }
                    else
                    {
                        // Parse the entered text to an integer
                        var zoom = int.Parse(text);

                        // Constrain the value to the min/max zoom range
                        zoom = ExtensionMethods.BoundedValue(zoom, ZoomChange, MaxZoom);

                        // Display the zoom value with the percent symbol tacked on
                        textBox.Text = zoom + "%";

                        // Make that value the current zoom level
                        ZoomSelection = zoom;
                    }
                    // Push the focus to the enclosing combobox. This removes the textbox editing caret
                    ZoomComboBox.Focus();

                    SetTextBoxEditState(false);

                    // Accept the key press
                    e.Handled = false;
                }
            }
            e.Handled = handled;
        }

        #endregion GUI Event Handlers
        #region Utility Methods

        /// <summary>
        /// Set the visual editing state of the textbox control
        /// </summary>
        /// <param name="canEdit"></param>
        private void SetTextBoxEditState(bool canEdit)
        {
            if (ZoomTextBox == null) return;

            if (canEdit)
            {
                
                var bodyBrush = FindResource("CMSSolidBorderBrush") as Brush;
                var selectionBrush = FindResource("CMSSpinTextSelectionBrush") as Brush;
                ZoomTextBox.Visibility = Visibility.Visible;
                ZoomTextBox.Background = bodyBrush;
                ZoomTextBox.SelectionForeground = selectionBrush;
                ZoomTextBox.IsReadOnly = false;
                ZoomTextBox.Focus();
                ZoomTextBox.SelectAll();
            }
            else
            {
                var bodyBrush = FindResource("CtrlNormalBrush") as Brush;
                var selectionBrush = new SolidColorBrush(Colors.Transparent);
                ZoomTextBox.Visibility = Visibility.Collapsed;
                ZoomTextBox.Background = bodyBrush;
                ZoomTextBox.SelectionForeground = selectionBrush;
                ZoomTextBox.IsReadOnly = true;
                ZoomTextBox.Select(0, 0);
            }
        }

        /// <summary>
        /// Sets the enable state of the increment / decrement buttons
        /// </summary>
        /// <param name="zoom"></param>
        private void SetButtonEnableState(int zoom)
        {
            DecrementButton.IsEnabled = zoom == 0 || zoom > ZoomChange;
            IncrementButton.IsEnabled = zoom == 0 || zoom < MaxZoom;
        }

        #endregion Utility Methods
    }
}
