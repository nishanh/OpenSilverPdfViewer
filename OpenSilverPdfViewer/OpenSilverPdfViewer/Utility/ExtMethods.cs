using CSHTML5.Native.Html.Controls;
using System.Reflection;
using System;

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
    }
}
