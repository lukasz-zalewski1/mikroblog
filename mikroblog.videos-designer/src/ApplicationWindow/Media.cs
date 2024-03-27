using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        /// Size of canvas where screenshot of screen's area will be drawn. In the end - the size of the image.
        /// </summary>
        private const int CANVAS_WIDTH = 1080;
        private const int CANVAS_HEIGHT = 1920;
        private const float CANVAS_RATIO = 16F / 9F;

        private const float SCREENSHOT_ON_CANVAS_WIDTH_RATIO = 0.88F;
        private const float SCREENSHOT_ON_CANVAS_POSITION_X_RATION = (1.0F - SCREENSHOT_ON_CANVAS_WIDTH_RATIO) / 2.0F;

        private const int SCREENSHOT_RECTANGLE_ARC_RADIUS = 20;

        private readonly Brush CANVAS_BACKGROUND_COLOR = new SolidBrush(Color.FromArgb(41, 2, 2));

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

        /// <summary>
        /// Takes a screenshot of given in <paramref name="rect"/> area of the screen. Puts the screenshot into a round rectangle border on <see cref="CANVAS_BACKGROUND_COLOR"/> colored canvas.
        /// </summary>
        /// <param name="rect">Area of the screen to take screenshot of</param>
        /// <returns>Bitmap of canvas with screenshot on it or null if failed</returns>
        private Bitmap? GetCanvasWithScreenshotDrawnInsideOfRoundedRect(Rectangle rect)
        {
            try
            {
                // Screenshot given screen area
                Bitmap bitmapScreenshot = new(rect.Width, rect.Height);
                using Graphics graphicsScreenshot = Graphics.FromImage(bitmapScreenshot);
                graphicsScreenshot.CopyFromScreen(rect.X, rect.Y, 0, 0, new System.Drawing.Size((int)(rect.Width * CANVAS_RATIO), (int)(rect.Height * CANVAS_RATIO)));

                Bitmap bitmapCanvas = new(CANVAS_WIDTH, CANVAS_HEIGHT);
                using var graphicsCanvas = Graphics.FromImage(bitmapCanvas);

                // int screenshotWidthOnCanvas = (int)(bitmapScreenshot.Width * CANVAS_RATIO); // 906 
                int screenshotWidthOnCanvas = (int)(CANVAS_WIDTH * SCREENSHOT_ON_CANVAS_WIDTH_RATIO);
                int screenshotHeightOnCanvas = (int)(bitmapScreenshot.Height * CANVAS_RATIO);
                // int screenshotPositionOnCanvasX = (CANVAS_WIDTH / 2) - (screenshotWidthOnCanvas / 2); // 87
                int screenshotPositionOnCanvasX = (int)(CANVAS_WIDTH * SCREENSHOT_ON_CANVAS_POSITION_X_RATION);
                int screenshotPositionOnCanvasY = (CANVAS_HEIGHT / 2) - (screenshotHeightOnCanvas / 2);

                // Draws screenshot on canvas
                graphicsCanvas.DrawImage(bitmapScreenshot, screenshotPositionOnCanvasX, screenshotPositionOnCanvasY, screenshotWidthOnCanvas, screenshotHeightOnCanvas);

                // Makes lines of the rounded border smooth
                graphicsCanvas.SmoothingMode = SmoothingMode.AntiAlias;

                // Draws an overlay which covers everything besides screenshot, leaving it in a rounded rectangle border
                var boundingRect = new Rectangle(screenshotPositionOnCanvasX, screenshotPositionOnCanvasY, screenshotWidthOnCanvas, screenshotHeightOnCanvas);
                GraphicsPath overlay = RoundedRectangle(boundingRect, SCREENSHOT_RECTANGLE_ARC_RADIUS);
                overlay.AddRectangle(new Rectangle(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT));
                graphicsCanvas.FillPath(CANVAS_BACKGROUND_COLOR, overlay);
                

                return bitmapCanvas;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Preparing canvas with screenshot failed, Exception - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates and returns GraphicsPath object of a rounded rectangle.
        /// </summary>
        /// <param name="radius">Arc radius</param>
        private GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            System.Drawing.Size size = new(diameter, diameter);
            Rectangle arc = new(bounds.Location, size);
            GraphicsPath path = new();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Top left arc  
            path.AddArc(arc, 180, 90);

            // Top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
