
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Shapes;
using System.Windows.Controls;

using OpenSilverPdfViewer.Utility;

namespace OpenSilverPdfViewer.Controls
{
    public partial class RulerCtrl
    {
        #region Dependency Properties

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(RulerCtrl),
            new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(double), typeof(RulerCtrl),
            new PropertyMetadata(30d, OnSizeChanged));

        public static readonly DependencyProperty UnitMeasureProperty = DependencyProperty.Register("UnitMeasure", typeof(UnitMeasure), typeof(RulerCtrl),
            new PropertyMetadata(UnitMeasure.Imperial, OnUnitMeasureChanged));

        public static readonly DependencyProperty LogicalScaleProperty = DependencyProperty.Register("LogicalScale", typeof(double), typeof(RulerCtrl),
            new PropertyMetadata(1d / 72d, OnLogicalScaleChanged));

        public static readonly DependencyProperty PageOffsetProperty = DependencyProperty.Register("PageOffset", typeof(double), typeof(RulerCtrl),
            new PropertyMetadata(0d, OnPageOffsetChanged));

        public static readonly DependencyProperty ScrollPositionProperty = DependencyProperty.Register("ScrollPosition", typeof(double), typeof(RulerCtrl),
            new PropertyMetadata(0d, OnScrollPositionChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        public UnitMeasure UnitMeasure
        {
            get => (UnitMeasure)GetValue(UnitMeasureProperty);
            set => SetValue(UnitMeasureProperty, value);
        }
        public double LogicalScale
        {
            get => (double)GetValue(LogicalScaleProperty);
            set => SetValue(LogicalScaleProperty, value);
        }
        public double PageOffset
        {
            get => (double)GetValue(PageOffsetProperty);
            set => SetValue(PageOffsetProperty, value);
        }
        public double ScrollPosition
        {
            get => (double)GetValue(ScrollPositionProperty);
            set => SetValue(ScrollPositionProperty, value);
        }

        #endregion Dependency Properties
        #region Dependency Property Event Handlers

        private static void OnOrientationChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            if ((Orientation)e.NewValue == Orientation.Horizontal)
            {
                ctrl.rulerBorder.Height = ctrl.Size;
                ctrl.rulerBorder.Width = double.NaN;
            }
            else
            {
                ctrl.rulerBorder.Height = double.NaN;
                ctrl.rulerBorder.Width = ctrl.Size;
            }
            ctrl.DrawRuler();
        }
        private static void OnSizeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            if (ctrl.Orientation == Orientation.Horizontal)
                ctrl.rulerBorder.Height = (double)e.NewValue;
            else
                ctrl.rulerBorder.Width = (double)e.NewValue;
            ctrl.DrawRuler();
        }
        private static void OnUnitMeasureChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            ctrl.DrawRuler();
        }
        private static void OnLogicalScaleChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            ctrl.DrawRuler();
        }
        private static void OnPageOffsetChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            ctrl.DrawRuler();
        }
        private static void OnScrollPositionChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = depObj as RulerCtrl;
            ctrl.DrawRuler();
        }

        #endregion Dependency Property Event Handlers
        #region Initialization

        public RulerCtrl()
        {
            this.InitializeComponent();

            if (Orientation == Orientation.Horizontal)
                rulerBorder.Height = Size;
            else
                rulerBorder.Width = Size;
        }

        #endregion Initialization
        #region Implementation

        private void DrawRuler()
        {
            if ((int)rulerCanvas.ActualHeight == 0 || (int)rulerCanvas.ActualWidth == 0) 
                return;

            var metric = UnitMeasure == UnitMeasure.Metric;
            const int baselineOffset = 2; // this should be computed for the font in use
            const double halfToneOffset = 0.5; // Supresses halftoning effects but makes tick-mark placement at certain intervals slightly inaccurate

            // Tick mark length constants
            const int wholeTickLength = 12;
            const int sixteenthTick = wholeTickLength / 8 + 1;
            const int eighthTick = wholeTickLength / 4 + 1;
            const int quarterTick = wholeTickLength / 3 + 2;
            const int halfTick = wholeTickLength / 2 + 2;
            const int millimeterTick = quarterTick;

            double resRuler, wholeUnitInterval, unitConvert;

            // Adjust the ruler resolution based on device-to-logical scale.
            // This is to avoid tightly-packed tick marks at high scale values
            if (metric)
            {
                const double metricScaleThreshold = 0.008;
                var metricRes = LogicalScale > metricScaleThreshold ? 10d : 1d; // centimeters or millimeters
                resRuler = metricRes / 25.4;
                wholeUnitInterval = (int)(10 / metricRes);
                unitConvert = 2.54;
            }
            else
            {
                resRuler = LogicalScale > .05 ? 1d : 0.5;
                resRuler = LogicalScale < .025 ? 0.25 : resRuler;
                resRuler = LogicalScale < .0125 ? 0.125 : resRuler;
                resRuler = LogicalScale < .00625 ? 0.0625 : resRuler;
                wholeUnitInterval = (int)(1 / resRuler);
                unitConvert = 1.0;
            }

            // Erase previous ruler ticks and text
            rulerCanvas.Children.Clear();

            // Draw inner borders
            rulerCanvas.Children.Add(new Line
            {
                X1 = Orientation == Orientation.Horizontal ? 0 : rulerCanvas.ActualWidth,
                Y1 = Orientation == Orientation.Horizontal ? rulerCanvas.ActualHeight : 0,
                X2 = rulerCanvas.ActualWidth,
                Y2 = rulerCanvas.ActualHeight,
                StrokeThickness = 2,
                Stroke = BorderBrush
            });

            var pagePosition = PageOffset - ScrollPosition;

            // Find the first tick mark we can reasonably draw.
            // Let's say the page image is centered horizontally in the viewport in logical units such
            // that the left-edge of the page is at 2.125". Therefore, the first tick mark that can be
            // drawn is the fractional component of .125". Convert that result back to device units (pixels)
            // to get the first X or Y position to draw the initial tick mark at.
            var origin = pagePosition * (LogicalScale * unitConvert);
            var start = origin % 1; // get fractional part
            var i = -(int)(start / resRuler / unitConvert);
            var devStart = start / (LogicalScale * unitConvert);

            var tickPos = 0d;
            var limit = Orientation == Orientation.Horizontal ? rulerCanvas.ActualWidth : rulerCanvas.ActualHeight;

            while (tickPos < limit)
            {
                // Compute tick mark position in device units
                tickPos = Math.Round(devStart + ((i * resRuler) / LogicalScale), 0) + halfToneOffset;
                if (tickPos >= 0)
                {
                    var tickLength = sixteenthTick;
                    if (i % wholeUnitInterval == 0) tickLength = wholeTickLength;
                    else if (metric) tickLength = millimeterTick;
                    else if (i % (wholeUnitInterval / 2) == 0) tickLength = halfTick;
                    else if (i % (wholeUnitInterval / 4) == 0) tickLength = quarterTick;
                    else if (i % (wholeUnitInterval / 8) == 0) tickLength = eighthTick;

                    // Draw the ruler tick mark
                    rulerCanvas.Children.Add(new Line
                    {
                        X1 = Orientation == Orientation.Horizontal ? tickPos : rulerCanvas.ActualWidth,
                        X2 = Orientation == Orientation.Horizontal ? tickPos : rulerCanvas.ActualWidth - tickLength,
                        Y1 = Orientation == Orientation.Horizontal ? rulerCanvas.ActualHeight : tickPos,
                        Y2 = Orientation == Orientation.Horizontal ? rulerCanvas.ActualHeight - tickLength : tickPos,
                        Stroke = Foreground,
                        StrokeThickness = 1
                    });

                    // Draw the unit-value text
                    if (i % wholeUnitInterval == 0)
                    {
                        var unitVal = (i / wholeUnitInterval) - (int)origin;
                        var rulerVal = new TextBlock
                        {
                            Foreground = Foreground,
                            FontFamily = FontFamily,
                            FontSize = FontSize,
                            Text = unitVal.ToString(CultureInfo.InvariantCulture)
                        };
                        var top = Orientation == Orientation.Horizontal ?
                            Size - rulerVal.ActualHeight - tickLength :
                            tickPos - (rulerVal.ActualHeight / 2) - baselineOffset;
                        var left = Orientation == Orientation.Horizontal ?
                            tickPos - (rulerVal.ActualWidth / 2) :
                            Size - rulerVal.ActualWidth - tickLength - 2; // fudge factor of 2px to provide a gap between tick and text

                        rulerVal.SetValue(Canvas.TopProperty, top);
                        rulerVal.SetValue(Canvas.LeftProperty, left);
                        rulerCanvas.Children.Add(rulerVal);
                    }
                }
                i++;
            }
        }
        private void Ruler_SizeChanged(object sender, RoutedEventArgs e)
        {
            DrawRuler();
        }

        #endregion Implementation
    }
}
