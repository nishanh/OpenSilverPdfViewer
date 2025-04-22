
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Windows.Input;

#pragma warning disable CS0067 // The event 'CanExecuteChanged' is never used

namespace OpenSilverPdfViewer.Utility
{
    public sealed class DelegateCommand : ICommand
    {
        private Predicate<object> _canExecute;
        private Action<object> _method;

        public DelegateCommand(Action<object> method, Predicate<object> canExecute)
        {
            _method = method;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _method(parameter);
        }
        public event EventHandler CanExecuteChanged;
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
