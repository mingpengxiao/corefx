﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_Delete_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Directory_Delete()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string dirName = Path.Combine(testDirectory.Path, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.Delete(dirName);
                Action cleanup = () => Directory.CreateDirectory(dirName);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, expectedPath: dirName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_InNestedDirectory()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string dirName = Path.Combine(nestedDir.Path, "dir");
                Action action = () => Directory.Delete(dirName);
                Action cleanup = () => Directory.CreateDirectory(dirName);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, dirName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_DeepDirectoryStructure()
        {
            // List of created directories
            List<TempDirectory> lst = new List<TempDirectory>();

            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Priming directory
                lst.Add(new TempDirectory(Path.Combine(dir.Path, "dir")));

                // Create a deep directory structure and expect things to work
                for (int i = 1; i < 20; i++)
                {
                    // Test that the creation triggers an event correctly
                    string dirPath = Path.Combine(lst[i - 1].Path, String.Format("dir{0}", i));
                    Action action = () => Directory.Delete(dirPath);
                    Action cleanup = () => Directory.CreateDirectory(dirPath);
                    cleanup();

                    ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup);

                    // Create the directory so subdirectories may be created from it.
                    lst.Add(new TempDirectory(dirPath));
                }
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Delete_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var tempDir = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path), "*"))
            {
                // Make the symlink in our path (to the temp folder) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, "link");
                Action action = () => Directory.Delete(symLinkPath);
                Action cleanup = () => Assert.True(CreateSymLink(tempDir.Path, symLinkPath, true));
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, expectedPath: symLinkPath);
            }
        }
    }
}