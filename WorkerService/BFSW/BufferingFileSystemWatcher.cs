using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService.BFSW
{
    public class BufferingFileSystemWatcher : Component
    {
        private FileSystemWatcher containedFSW = null;
        private FileSystemEventHandler onExistedHandler = null;
        private FileSystemEventHandler onAllChangesHandler = null;
        private FileSystemEventHandler onCreatedHandler = null;
        private FileSystemEventHandler onChangedHandler = null;
        private FileSystemEventHandler onDeletedHandler = null;
        private RenamedEventHandler onRenamedHandler = null;
        private ErrorEventHandler onErrorHandler = null;
        private BlockingCollection<FileSystemEventArgs> fileSystemEventBuffer = null;
        private CancellationTokenSource cancellationTokenSource = null;


        public bool EnableRaisingEvents
        {
            get
            {
                return containedFSW.EnableRaisingEvents;
            }
            set
            {
                if (containedFSW.EnableRaisingEvents == value)
                    return;

                StopRaisingBufferedEvents();
                cancellationTokenSource = new CancellationTokenSource();

                containedFSW.EnableRaisingEvents = value;
                if (value)
                    RaiseBufferedEventsUntilCancelled();
            }
        }
        public string Filter
        {
            get { return containedFSW.Filter; }
            set { containedFSW.Filter = value; }
        }
        public bool IncludeSubdirectories
        {
            get { return containedFSW.IncludeSubdirectories; }
            set { containedFSW.IncludeSubdirectories = value; }
        }
        public int InternalBufferSize
        {
            get { return containedFSW.InternalBufferSize; }
            set { containedFSW.InternalBufferSize = value; }
        }
        public NotifyFilters NotifyFilter
        {
            get { return containedFSW.NotifyFilter; }
            set { containedFSW.NotifyFilter = value; }
        }
        public string Path
        {
            get { return containedFSW.Path; }
            set { containedFSW.Path = value; }
        }
        public ISynchronizeInvoke SynchronizingObject
        {
            get { return containedFSW.SynchronizingObject; }
            set { containedFSW.SynchronizingObject = value; }
        }
        public override ISite Site
        {
            get { return containedFSW.Site; }
            set { containedFSW.Site = value; }
        }

        [DefaultValue(false)]
        public bool OrderByOldestFirst { get; set; } = false;
        public int EventQueueCapacity { get; set; } = int.MaxValue;


        public BufferingFileSystemWatcher()
        {
            containedFSW = new FileSystemWatcher();
        }
        public BufferingFileSystemWatcher(string path)
        {
            containedFSW = new FileSystemWatcher(path, "*.*");
        }
        public BufferingFileSystemWatcher(string path, string filter)
        {
            containedFSW = new FileSystemWatcher(path, filter);
        }


        private void BufferEvent(object _, FileSystemEventArgs e)
        {
            if (!fileSystemEventBuffer.TryAdd(e))
            {
                var ex = new EventQueueOverflowException($"Event queue size {fileSystemEventBuffer.BoundedCapacity} events exceeded.");
                InvokeHandler(onErrorHandler, new ErrorEventArgs(ex));
            }
        }
        private void StopRaisingBufferedEvents(object _ = null, EventArgs __ = null)
        {
            cancellationTokenSource?.Cancel();
            fileSystemEventBuffer = new BlockingCollection<FileSystemEventArgs>(EventQueueCapacity);
        }
        private void BufferingFileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            InvokeHandler(onErrorHandler, e);
        }
        private void RaiseBufferedEventsUntilCancelled()
        {
            Task.Run(() =>
            {
                try
                {
                    if (onExistedHandler != null || onAllChangesHandler != null)
                        NotifyExistingFiles();

                    foreach (FileSystemEventArgs e in fileSystemEventBuffer.GetConsumingEnumerable(cancellationTokenSource.Token))
                    {
                        if (onAllChangesHandler != null)
                            InvokeHandler(onAllChangesHandler, e);
                        else
                        {
                            switch (e.ChangeType)
                            {
                                case WatcherChangeTypes.Created:
                                    InvokeHandler(onCreatedHandler, e);
                                    break;
                                case WatcherChangeTypes.Changed:
                                    InvokeHandler(onChangedHandler, e);
                                    break;
                                case WatcherChangeTypes.Deleted:
                                    InvokeHandler(onDeletedHandler, e);
                                    break;
                                case WatcherChangeTypes.Renamed:
                                    InvokeHandler(onRenamedHandler, e as RenamedEventArgs);
                                    break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                { }
                catch (Exception ex)
                {
                    BufferingFileSystemWatcher_Error(this, new ErrorEventArgs(ex));
                }
            });
        }
        private void NotifyExistingFiles()
        {
            var searchSubDirectoriesOption = (IncludeSubdirectories) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (OrderByOldestFirst)
            {
                var sortedFileInfos = from fi in new DirectoryInfo(Path).GetFiles(Filter, searchSubDirectoriesOption)
                                      orderby fi.LastWriteTime ascending
                                      select fi;
                foreach (var fi in sortedFileInfos)
                {
                    InvokeHandler(onExistedHandler, new FileSystemEventArgs(WatcherChangeTypes.All, fi.DirectoryName, fi.Name));
                    InvokeHandler(onAllChangesHandler, new FileSystemEventArgs(WatcherChangeTypes.All, fi.DirectoryName, fi.Name));
                }
            }
            else
            {
                foreach (var fsi in new DirectoryInfo(Path).EnumerateFileSystemInfos(Filter, searchSubDirectoriesOption))
                {
                    InvokeHandler(onExistedHandler, new FileSystemEventArgs(WatcherChangeTypes.All, System.IO.Path.GetDirectoryName(fsi.FullName), fsi.Name));
                    InvokeHandler(onAllChangesHandler, new FileSystemEventArgs(WatcherChangeTypes.All, System.IO.Path.GetDirectoryName(fsi.FullName), fsi.Name));
                }
            }
        }
        private void InvokeHandler(FileSystemEventHandler eventHandler, FileSystemEventArgs e)
        {
            if (eventHandler != null)
            {
                if (containedFSW.SynchronizingObject != null && this.containedFSW.SynchronizingObject.InvokeRequired)
                    containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }
        private void InvokeHandler(RenamedEventHandler eventHandler, RenamedEventArgs e)
        {
            if (eventHandler != null)
            {
                if (containedFSW.SynchronizingObject != null && this.containedFSW.SynchronizingObject.InvokeRequired)
                    containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }
        private void InvokeHandler(ErrorEventHandler eventHandler, ErrorEventArgs e)
        {
            if (eventHandler != null)
            {
                if (containedFSW.SynchronizingObject != null && this.containedFSW.SynchronizingObject.InvokeRequired)
                    containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                containedFSW?.Dispose();
            }
            base.Dispose(disposing);
        }

        public event FileSystemEventHandler Created
        {
            add
            {
                if (onCreatedHandler == null)
                    containedFSW.Created += BufferEvent;
                onCreatedHandler += value;
            }
            remove
            {
                containedFSW.Created -= BufferEvent;
                onCreatedHandler -= value;
            }
        }
        public event FileSystemEventHandler Changed
        {
            add
            {
                if (onChangedHandler == null)
                    containedFSW.Changed += BufferEvent;
                onChangedHandler += value;
            }
            remove
            {
                containedFSW.Changed -= BufferEvent;
                onChangedHandler -= value;
            }
        }
        public event FileSystemEventHandler Deleted
        {
            add
            {
                if (onDeletedHandler == null)
                    containedFSW.Deleted += BufferEvent;
                onDeletedHandler += value;
            }
            remove
            {
                containedFSW.Deleted -= BufferEvent;
                onDeletedHandler -= value;
            }
        }
        public event RenamedEventHandler Renamed
        {
            add
            {
                if (onRenamedHandler == null)
                    containedFSW.Renamed += BufferEvent;
                onRenamedHandler += value;
            }
            remove
            {
                containedFSW.Renamed -= BufferEvent;
                onRenamedHandler -= value;
            }
        }
        public event ErrorEventHandler Error
        {
            add
            {
                if (onErrorHandler == null)
                    containedFSW.Error += BufferingFileSystemWatcher_Error;
                onErrorHandler += value;
            }
            remove
            {
                if (onErrorHandler == null)
                    containedFSW.Error -= BufferingFileSystemWatcher_Error;
                onErrorHandler -= value;
            }
        }

        public event FileSystemEventHandler Existed
        {
            add
            {
                onExistedHandler += value;
            }
            remove
            {
                onExistedHandler -= value;
            }
        }
        public event FileSystemEventHandler All
        {
            add
            {
                if (onAllChangesHandler == null)
                {
                    containedFSW.Created += BufferEvent;
                    containedFSW.Changed += BufferEvent;
                    containedFSW.Renamed += BufferEvent;
                    containedFSW.Deleted += BufferEvent;
                }
                onAllChangesHandler += value;
            }
            remove
            {
                containedFSW.Created -= BufferEvent;
                containedFSW.Changed -= BufferEvent;
                containedFSW.Renamed -= BufferEvent;
                containedFSW.Deleted -= BufferEvent;
                onAllChangesHandler -= value;
            }
        }

    }
}
