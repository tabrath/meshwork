//
// FileSystemProvider.cs: The root of the virtual filesystem
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2006 FileFind.net (http://filefind.net/)
//

using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;
using Mono.Data.Sqlite;
using Mono.Data;
using System.Data;
using Hyena.Query;
using Meshwork.Logging;
using System.ComponentModel.Composition;

namespace FileFind.Meshwork.Filesystem
{
    public interface IFileSystemProvider
    {
        RootDirectory RootDirectory { get; }
        long TotalDirectories { get; }
        long TotalFiles { get; }
        long TotalBytes { get; }
        long YourTotalFiles { get; }
        long YourTotalBytes { get; }

        bool BeginGetDirectory(string path, DirectoryCallback callback);
        bool BeginGetFileDetails(string path, FileCallback callback);
        LocalDirectory GetLocalDirectory(string path);
        IDirectory GetDirectory(string path);
        IFile GetFile(string path);
        T UseConnection<T>(DbMethod<T> method);
        T UseConnection<T>(DbMethod<T> method, bool write);
        void UseConnection(DbMethod method);
        void UseConnection(DbMethod method, bool write);
        SearchResultInfo SearchFiles(string query);
        void InvalidateCache();
        void AddParameter(IDbCommand command, string name, object value);
        DataSet ExecuteDataSet(IDbCommand command);
        object ExecuteScalar(string query);
        object ExecuteScalar(IDbCommand command);
        int ExecuteNonQuery(IDbCommand command);
        void PurgeMissing();
        void ProcessRespondDirListingMessage(Network network, Node messageFrom, SharedDirectoryInfo info);
        void ProcessFileDetailsMessage(Network network, Node messageFrom, SharedFileListing info);

    }

}
