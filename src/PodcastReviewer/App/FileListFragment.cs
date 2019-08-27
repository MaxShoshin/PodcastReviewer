using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Environment = Android.OS.Environment;
using ListFragment = Android.App.ListFragment;

namespace PodcastReviewer.App
{
    public sealed class FileListFragment : ListFragment
    {
        public static readonly string DefaultInitialDirectory = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath;

        private FileListAdapter _adapter;
        private DirectoryInfo _directory;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _adapter = new FileListAdapter(Activity, new FileSystemInfo[0]);
            ListAdapter = _adapter;
        }

        public override void OnListItemClick(ListView l, View v, int position, long id)
        {
            var fileSystemInfo = _adapter.GetItem(position);

            var intent = new Intent(Activity, typeof(MainActivity));
            intent.PutExtra("FileName", fileSystemInfo.FullName);
            Activity.SetResult(Result.Ok, intent);
            Activity.Finish();

            base.OnListItemClick(l, v, position, id);
        }

        public override void OnResume()
        {
            base.OnResume();
            RefreshFilesList(DefaultInitialDirectory);
        }

        public void RefreshFilesList(string directory)
        {
            IList<FileSystemInfo> visibleThings = new List<FileSystemInfo>();
            var dir = new DirectoryInfo(directory);

            try
            {
                foreach (var item in dir.GetFileSystemInfos()
                    .Where(item => item.IsVisible() && item.IsFile() && item.Extension == ".mp3"))
                {
                    visibleThings.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error("FileListFragment", "Couldn't access the directory " + _directory.FullName + "; " + ex);
                Toast.MakeText(Activity, "Problem retrieving contents of " + directory, ToastLength.Long).Show();
                return;
            }

            _directory = dir;

            _adapter.AddDirectoryContents(visibleThings);

            // If we don't do this, then the ListView will not update itself when then data set
            // in the adapter changes. It will appear to the user that nothing has happened.
            ListView.RefreshDrawableState();

            Log.Verbose("FileListFragment", "Displaying the contents of directory {0}.", directory);
        }
    }
}