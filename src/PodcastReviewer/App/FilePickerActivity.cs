using System.Reflection.Emit;
using Android.App;
using Android.OS;
using Android.Support.V7.App;

namespace PodcastReviewer.App
{
    [Activity(Label="Select mp3 file")]
    public sealed class FilePickerActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.file_picker);
        }
    }
}