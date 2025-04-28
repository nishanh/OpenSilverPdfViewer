
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.Utility
{
    internal static partial class ExtensionMethods
    {
        public static T BoundedValue<T>(T value, T lower, T upper) where T : IComparable<T>
        {
            var bounded = value;

            if (lower != null && value.CompareTo(lower) < 0)
                bounded = lower;

            else if (upper != null && value.CompareTo(upper) > 0)
                bounded = upper;

            return bounded;
        }
        public static int AsTMM(this double value)
        {
            return (int)Math.Truncate(value * 25400d);
        }
        public static void Disconnect(this Image image)
        {
            if (image.Parent != null)
            {
                if (image.Parent is Panel panel)
                    panel.Children.Remove(image);
                else if (image.Parent is Border border)
                    border.Child = null;
            }
        }
    }
}
