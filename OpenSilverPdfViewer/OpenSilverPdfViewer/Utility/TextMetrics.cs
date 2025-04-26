
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

namespace OpenSilverPdfViewer.Utility
{
    public sealed class TextMetrics
    {
        public double ActualAscent { get; set; }
        public double ActualDescent { get; set; }
        public double BoundingBoxLeft { get; set; }
        public double BoundingBoxRight { get; set; }
        public double FontAscent { get; set; }
        public double FontDescent { get; set; }
        public double HangingBaseline { get; set; }
        public double IdeographicBaseline { get; set; }
        public double Width { get; set; }  
    }
}
