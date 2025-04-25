
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using System.Collections.Generic;

namespace OpenSilverPdfViewer.Renderer
{
    public class LayoutRect
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Right => X + Width;
        public double Bottom => Y + Height;

        public LayoutRect() { }
        public LayoutRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public bool Intersects(Rect rect)
        {
            return ((Rect)this).IntersectsWith(rect);
        }
        public static implicit operator Rect(LayoutRect layoutRect)
        {
            return new Rect { X = layoutRect.X, Y = layoutRect.Y, Width = layoutRect.Width, Height = layoutRect.Height };
        }
    }
    public class PageRun
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Count { get; set; }

        public static List<PageRun> CreateTestList()
        {
            return new List<PageRun>()
            {
                new PageRun
                {
                    Width = 215900,
                    Height = 279400,
                    Count = 5
                },
                new PageRun
                {
                    Width = 279400,
                    Height = 431800,
                    Count = 1
                },
                new PageRun
                {
                    Width = 215900,
                    Height = 279400,
                    Count = 3
                },
                new PageRun
                {
                    Width = 215900,
                    Height = 355600,
                    Count = 2
                },
                new PageRun
                {
                    Width = 215900,
                    Height = 279400,
                    Count = 4
                }
            };
        }
    }
}