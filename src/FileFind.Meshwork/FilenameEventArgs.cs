//
// EventArgs.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork
{
    public class FilenameEventArgs : EventArgs
    {
        public string Filename { get; }

        public FilenameEventArgs(string filename)
            : base()
        {
            Filename = filename;
        }
    }
}
