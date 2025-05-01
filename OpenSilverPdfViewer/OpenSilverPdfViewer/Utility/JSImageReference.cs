
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

namespace OpenSilverPdfViewer.Utility
{
    public class JSImageReference
    {
        public int Id { get; private set; }
        public CacheStatus Status { get; set; }
        public JSImageReference(int id, CacheStatus status)
        {
            Id = id;
            Status = status;
        }
    }
}
