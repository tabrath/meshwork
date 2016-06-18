//
// ShareBuilder.cs: Index shared directories
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IO = System.IO;
using System.Threading;
using FileFind.Meshwork.Filesystem;
using System.Data;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;

namespace FileFind.Meshwork
{
    public interface IShareBuilder
    {
        event EventHandler StartedIndexing;
        event EventHandler FinishedIndexing;
        event EventHandler StoppedIndexing;
        event EventHandler<FilenameEventArgs> IndexingFile;
        event EventHandler<ErrorEventArgs> ErrorIndexing;

        bool Going { get; }

        void Start();
        void Stop();
    }

    //TODO: Rename to ShareIndexer
    
}
