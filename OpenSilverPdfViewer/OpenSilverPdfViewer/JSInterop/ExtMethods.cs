
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

using CSHTML5.Native.Html.Controls;

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
    }
}
