//
// EventArgs.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public ErrorEventArgs(Exception exception)
            : base()
        {
            Exception = exception;
        }
    }
}
