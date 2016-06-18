//
// ShareWatcher.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

// XXX:
// CRAP. Directories can have more than one local path, becuase they get merged.
// We dont want to store this in the db at all, local_path should be nil for directories.
//

using System;
using System.Linq;
using System.Data;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using MFS = FileFind.Meshwork.Filesystem;
using System.ComponentModel.Composition;

namespace FileFind.Meshwork
{
    public interface IShareWatcher
    {
        void Start();
        void Stop();
    }

    //TODO: cleanup - IShareWatcher
    
}
