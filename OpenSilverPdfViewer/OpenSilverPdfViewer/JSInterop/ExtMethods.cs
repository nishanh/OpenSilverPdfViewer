
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Windows;
using System.Reflection;
using CSHTML5.Native.Html.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace OpenSilverPdfViewer.JSInterop
{
    internal static partial class ExtensionMethods
    {
        public static string GetDOMId(this HtmlCanvas canvas)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            var field = canvas.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("_jsCanvas"));

            if (field != null)
            {
                var value = field.GetValue(canvas);
                var id = OpenSilver.Interop.ExecuteJavaScript("$0.getAttribute('id')", field.GetValue(canvas));
                return id.ToString();
            }
            else
                 throw new InvalidOperationException("No field with the specified name was found");
        }
        public static Size ParseJsonSize(this string json)
        {
            try
            {
                var dimensions = json.Trim('{').Trim('}').Split(',');
                var widthStr = dimensions[0].Substring(dimensions[0].IndexOf(':') + 1);
                var width = double.Parse(widthStr);
                var heightStr = dimensions[1].Substring(dimensions[1].IndexOf(':') + 1);
                var height = double.Parse(heightStr);
                return new Size(width, height);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Invalid JSON size: {json}");
            }
        }
        public static void SetupDOMAnimation(this Grid thumbnail)
        {
            var domGrid = OpenSilver.Interop.GetDiv(thumbnail);
            OpenSilver.Interop.ExecuteJavaScript("$0.style.perspective='500px'", domGrid);
            var thumbBorder = thumbnail.Children[0] as Border;
            var domBorder = OpenSilver.Interop.GetDiv(thumbBorder);
            OpenSilver.Interop.ExecuteJavaScript("$0.style.transformOrigin='center'", domBorder);
        }
        public static void Rotate(this UIElement element, int angle)
        {
            var domElement = OpenSilver.Interop.GetDiv(element);
            OpenSilver.Interop.ExecuteJavaScript($"$0.style.transform='rotateY({angle}deg)'", domElement);
        }
    }
}
