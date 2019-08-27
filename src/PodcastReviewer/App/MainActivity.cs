using System;
using System.IO;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Util.Zip;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Environment = System.Environment;
using AndroidEnvironment = Android.OS.Environment;

namespace PodcastReviewer.App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public sealed class MainActivity : AppCompatActivity
    {
        private LinearLayout _layout;
        private MediaPlayer _player;
        private MediaRecorder _recorder;

        private SeekBar _seekBar;
        private Timer _timer;
        private Button _pauseButton;
        private Button _playButton;
        private TextView _timeView;
        private TextView _fileView;
        private Button _markButton;
        private string _fileName = "Unknown.mp3";
        private bool _recorderPrepeared;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            ActivityCompat.RequestPermissions(
                this,
                new[]
                {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage,
                    Manifest.Permission.RecordAudio
                },
                0);

            _timer = new Timer();
            _timer.Elapsed += UpdatePosition;

            _layout = FindViewById<LinearLayout>(Resource.Id.layout);

            _playButton = FindViewById<Button>(Resource.Id.play);
            _playButton.Enabled = false;
            _playButton.Click += delegate { Play(); };

            _timeView = FindViewById<TextView>(Resource.Id.time);
            _fileView = FindViewById<TextView>(Resource.Id.file_name);

            _pauseButton = FindViewById<Button>(Resource.Id.pause);
            _pauseButton.Enabled = false;
            _pauseButton.Click += (sender, args) => Pause();

            _markButton = FindViewById<Button>(Resource.Id.mark_button);
            _markButton.Touch += MarkButtonTouch;
            _markButton.Enabled = false;

            _seekBar = FindViewById<SeekBar>(Resource.Id.seekBar);
            _seekBar.Enabled = false;
            _seekBar.ProgressChanged += ProgressChanged;

            _player = new MediaPlayer();
            _player.SetWakeMode(this, WakeLockFlags.Full);
            _player.Completion += Complete;

            _recorder = new MediaRecorder();
            _recorder.SetAudioSource(AudioSource.Mic);
            _recorder.SetOutputFormat(OutputFormat.ThreeGpp);
            _recorder.SetAudioEncoder(AudioEncoder.AmrNb);
        }

        private void MarkButtonTouch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                Pause();
                _playButton.Enabled = false;

                var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var fileName = Path.Combine(path, _player.CurrentPosition + ".mp3");
                var index = 0;

                while (File.Exists(fileName))
                {
                    fileName = Path.Combine(path, $"{_player.CurrentPosition}_{index++}.mp3");
                }

                var position = (double)_player.CurrentPosition / 1000;

                using (var writer = new StreamWriter(Path.Combine(path, "label.txt")))
                {
                    writer.WriteLine($"{position:F6}\t{position:F6}\tNote");
                }

                _recorder.SetOutputFile(fileName);
                if (!_recorderPrepeared)
                {
                    _recorder.Prepare();
                    _recorderPrepeared = true;
                }

                _recorder.Start();
            }
            else if (e.Event.Action == MotionEventActions.Up)
            {
                _recorder.Stop();

                var bitEarlier = _player.CurrentPosition - 500;
                if (bitEarlier < 0)
                {
                    bitEarlier = 0;
                }

                _player.SeekTo(bitEarlier);
                Play();
            }
        }

        private void ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            _player.SeekTo(e.Progress * 1000);
        }

        private void Play()
        {
            _player.Start();

            _playButton.Enabled = false;
            _pauseButton.Enabled = true;
        }

        private void Complete(object sender, EventArgs e)
        {
            _player.SeekTo(0);

            Pause();
        }

        private void Pause()
        {
            _player.Pause();

            _playButton.Enabled = true;
            _pauseButton.Enabled = false;

            _timer.Stop();
        }

        private void Start()
        {
            _player.Start();

            _playButton.Enabled = false;
            _pauseButton.Enabled = true;

            _timer.Start();
        }

        private void UpdatePosition(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _seekBar.Progress = _player.CurrentPosition / 1000;

                var duration = TimeSpan.FromMilliseconds(_player.Duration);
                var current = TimeSpan.FromMilliseconds(_player.CurrentPosition);

                _timeView.Text = string.Format("{0:hh\\:mm\\:ss\\.fff} of {1:hh\\:mm\\:ss\\.fff}", current, duration);
            });
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options, menu);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.open_file)
            {
                StartActivityForResult(typeof(FilePickerActivity), 0);
            }
            else if (item.ItemId == Resource.Id.save_results)
            {
                SaveResults();
            }
            else if (item.ItemId == Resource.Id.clear)
            {
                new AlertDialog.Builder(this)
                    .SetPositiveButton("Yes", (sender, args) =>
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            File.Delete(file);
                        }
                    })
                    .SetNegativeButton("No", (sender, args) =>
                    {
                    })
                    .SetMessage("Clear all current results?")
                    .SetTitle("Confirm clear")
                    .Show();
            }

            return base.OnOptionsItemSelected(item);
        }

        private void SaveResults()
        {
            var path = AndroidEnvironment.GetExternalStoragePublicDirectory(AndroidEnvironment.DirectoryDownloads).AbsolutePath;
            var fileName = Path.GetFileNameWithoutExtension(_fileName);
            var output = Path.ChangeExtension(Path.Combine(path, fileName), ".zip");
            var index = 0;
            while (File.Exists(output))
            {
                index++;
                output = Path.ChangeExtension(Path.Combine(path, fileName + "_" + index), ".zip");
            }

            using (var fileStream = File.Create(output))
            using(var zip = new ZipOutputStream(fileStream))
            {
                var internalStorage = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var files = Directory.GetFiles(internalStorage);

                foreach (var file in files)
                {
                    var entry = new ZipEntry(Path.GetFileName(file));
                    zip.PutNextEntry(entry);

                    using (var sourceFile = File.OpenRead(file))
                    using (var sourceStream = new BufferedStream(sourceFile))
                    {
                        var buffer = new byte[4096];

                        int count;
                        while ((count = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
                            zip.Write(buffer, 0, count);
                        }
                    }

                    zip.CloseEntry();
                }
            }

            new AlertDialog.Builder(this)
                .SetNeutralButton("Ok", delegate {  })
                .SetMessage("Saved to:" + Environment.NewLine + output)
                .SetTitle("Saved successfully")
                .Show();
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (_player.IsPlaying)
            {
                Pause();
            }
        }

        protected override void OnStop()
        {
            _player.Release();
            _recorder.Release();

            base.OnStop();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok)
            {
                return;
            }

            _fileName = data.GetStringExtra("FileName");

            try
            {
                _fileView.Text = Path.GetFileNameWithoutExtension(_fileName);
                _player.SetDataSource(_fileName);
                _player.Prepare();

                _seekBar.Enabled = true;
                _seekBar.Max = _player.Duration / 1000;

                _markButton.Enabled = true;
                Start();

                _layout.KeepScreenOn = true;
            }
            catch
            {
                Toast.MakeText(this, "Unable to play file", ToastLength.Short);
            }
        }
    }
}