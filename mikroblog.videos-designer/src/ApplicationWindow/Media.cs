using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private readonly string VIDEOS_PATH = Path.Combine(fast_quality_check.Util.GetWorkplacePath(), "videos");

        private SoundPlayer _soundPlayer = new();
        private readonly System.Timers.Timer _speechTimer = new();
        private bool _isSpeechPlayed = false;

        private bool _isVideoPlayed = false;

        /// <summary>
        /// Calls <see cref="CleanScreenshotViewer"/> and runs <see cref="RunScreenshotProcedure"/> on <see cref="_listboxEntries"/>.SelectedItem.
        /// </summary>
        private async Task Screenshot()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            CleanScreenshotViewer();

            await RunScreenshotProcedure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        /// <summary>
        /// Calls <see cref="CleanScreenshotViewer"/> and runs <see cref="RunScreenshotProcedure"/> on all items in <see cref="_listboxEntries"/>.
        /// </summary>
        private async Task ScreenshotAll()
        {
            CleanScreenshotViewer();

            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunScreenshotProcedure(i);
            }
        }

        /// <summary>
        /// Runs JS function "hideEntryNumberNode" with <paramref name="entryNumber"/> as the argument, 
        /// then waits for <see cref="SCREENSHOT_DELAY"/> ms and runs the following JS functions - "sendScreenshotData" and "showEntryNumberNode"
        /// with <paramref name="entryNumber"/> as the argument.
        /// </summary>
        private async Task RunScreenshotProcedure(int entryNumber)
        {
            await JS.ExecuteJSFunction(_webView, "hideEntryNumberNode", entryNumber.ToString());

            Thread.Sleep(SCREENSHOT_DELAY);

            await JS.ExecuteJSFunction(_webView, "sendScreenshotData", entryNumber.ToString());
            await JS.ExecuteJSFunction(_webView, "showEntryNumberNode", entryNumber.ToString());
        }

        /// <summary>
        /// Runs <see cref="RunSpeechProcedure"/> on <see cref="_listboxEntries"/>.SelectedItem.
        /// </summary>
        private async Task Speak()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            await RunSpeechProcedure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        /// <summary>
        /// Runs <see cref="RunSpeechProcedure"/> on all items in <see cref="_listboxEntries"/>.
        /// </summary>
        /// <returns></returns>
        private async Task SpeakAll()
        {
            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunSpeechProcedure(i);
            }
        }

        /// <summary>
        /// Runs JS function "sendSpeechData" with <paramref name="entryNumber"/> as the argument.
        /// </summary>
        private async Task RunSpeechProcedure(int entryNumber)
        {
            await JS.ExecuteJSFunction(_webView, "sendSpeechData", entryNumber.ToString());
        }

        /// <summary>
        /// Calls <see cref="Screenshot"/> and <see cref="Speak"/> methods.
        /// </summary>
        private async Task ScreenshotAndSpeak()
        {
            await Screenshot();
            await Speak();
        }

        /// <summary>
        /// Calls <see cref="RunScreenshotProcedure"/> and <see cref="RunSpeechProcedure"/> on all items in <see cref="_listboxEntries"/>.
        /// </summary>
        private async Task ScreenshotSpeakAll()
        {
            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunScreenshotProcedure(i);
                await RunSpeechProcedure(i);
            }
        }

        /// <summary>
        /// Stops and removed video from <see cref="_videoPlayer"/> if any was played or loaded. Runs <see cref="Console.CreateAndExecuteVideoScript"/>.
        /// </summary>
        private void CreateVideo()
        {
            StopVideo();
            _videoPlayer.Source = null;

            Console.CreateAndExecuteVideoScript(GetCurrentDiscussionFolder(), VIDEOS_PATH, GetCurrentDiscussionId());

            UpdateControls(ControlUpdateType.Video);
        }

        /// <summary>
        /// Loads screenshot from a file to MemoryStream and then displays it in <see cref="_screenshotViewer"/>.
        /// </summary>
        /// <param name="path"></param>
        private void LoadScreenshotToScreenshotViewer(string path)
        {
            try
            {
                byte[] array = File.ReadAllBytes(path);

                // Loading image via MemoryStream and caching on load means that we do not keep on using screenshot on the disk and we can delete it if needed
                using var memoryStream = new MemoryStream(array);

                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                _screenshotViewer.Source = bitmapImage;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Loading screenshot to screenshot viewer, Exception - {ex.Message}");
            }
        }

        /// <summary>
        /// Reads speech length from a file.
        /// </summary>
        /// <returns>Length of the speech or empty string if not found.</returns>
        private string ReadSpeechLengthFromFile()
        {
            if (_listboxEntries.SelectedItem == null)
                return string.Empty;

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".txt");

            string text = File.ReadAllText(path);

            if (!double.TryParse(text, out double length))
            {
                Log.WriteError($"Incorrect speech length - {length}");
                return string.Empty;
            }

            return length.ToString();
        }

        /// <summary>
        /// Updates speech length file with the value of <see cref="_textboxSpeechLength"/>.Text.
        /// </summary>
        private void UpdateSpeechLengthFile()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".txt");

            try
            {
                File.WriteAllText(path, _textboxSpeechLength.Text);
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't write speech length to a file - {ex.Message}");
            }
        }

        /// <summary>
        /// Reads audio file of the current discussion, start playing it on <see cref="_soundPlayer"/> and runs the speech timer by calling <see cref="StartSpeechTimer"/>.
        /// </summary>
        private void PlaySpeech()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".wav");

            if (!File.Exists(path))
            {
                Log.WriteWarning($"File - {path} was removed in an unexpected way");
                return;
            }

            _soundPlayer = new(path);

            _soundPlayer.Play();

            StartSpeechTimer();
            _isSpeechPlayed = true;

            UpdateControls(ControlUpdateType.Speech);
        }

        /// <summary>
        /// Sets <see cref="_speechTimer"/>.Interval to value taken from <see cref="_textboxSpeechLength"/>.Text and start <see cref="_speechTimer"/>.
        /// </summary>
        private void StartSpeechTimer()
        {
            if (!double.TryParse(_textboxSpeechLength.Text, out double interval))
                return;

            _speechTimer.Interval = interval * 1000;
            _speechTimer.Start();
        }

        /// <summary>
        /// Stops <see cref="_speechTimer"/> and <see cref="_soundPlayer"/>.
        /// </summary>
        private void StopSpeech()
        {
            _speechTimer.Stop();
            _soundPlayer.Stop();

            _isSpeechPlayed = false;

            UpdateControls(ControlUpdateType.Speech);
        }

        /// <summary>
        /// Reads video file of the current discussion. Hides <see cref="_screenshotViewer"/> and shows <see cref="_videoPlayer"/> and plays the video on it.
        /// </summary>
        private void PlayVideo()
        {
            string videoPath = Path.ChangeExtension(Path.Combine(VIDEOS_PATH, GetCurrentDiscussionId()), ".mp4");

            if (!File.Exists(videoPath))
                return;

            ScreenshotViewerAndVideoPlayerVisibility(ScreenshotViewerAndVideoPlayerVisibilityType.ShowVideoPlayer);

            _videoPlayer.Source = new Uri(videoPath);
            _videoPlayer.MediaEnded += VideoPlayer_MediaEnded;

            _videoPlayer.Play();
            _isVideoPlayed = true;

            UpdateControls(ControlUpdateType.Video);
        }

        /// <summary>
        /// Calls <see cref="_videoPlayer"/>.Stop();
        /// </summary>
        private void StopVideo()
        {
            _isVideoPlayed = false;

            _videoPlayer.Stop();
            UpdateControls(ControlUpdateType.Video);
        }
    }
}
