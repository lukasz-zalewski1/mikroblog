﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private readonly string DISCUSSIONS_PATH = Path.Combine(fast_quality_check.Util.GetWorkplacePath(), "discussions");
        private readonly string VIDEOS_PATH = Path.Combine(fast_quality_check.Util.GetWorkplacePath(), "videos");

        // private readonly Config _configQualityDiscussions = new(Manager.QUALITY_DISCUSSIONS_FILE_NAME);
        private readonly Config _configQualityDiscussions = new("workon");  

        private int _currentDiscussion;
        private int DiscussionsCount { get => _configQualityDiscussions.Lines != null ? _configQualityDiscussions.Lines.Count : 0; }

        private const string RESOURCE_NAME_JS_EDIT_MODE = "mikroblog.videos_designer.src.JS.EditMode.js";
        private const string RESOURCE_NAME_JS_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.DesignerMode.js";

        private const int SCREENSHOT_DELAY = 100;

        private readonly TextToSpeech _speechService = new();

        private SoundPlayer _soundPlayer = new();
        private readonly System.Timers.Timer _speechTimer = new();
        private bool _isSpeechPlayed = false;

        private bool _isVideoPlayed = false;    

        public VideosDesignerWindow()
        {
            InitializeComponent();

            InitializeControls();

            InitializeEvents();

            WebViewOpenCurrentDiscussion();
        }

        private void RemoveDiscussionFiles()
        {
            var path = GetCurrentDiscussionVideoFolder();

            if (!Directory.Exists(path))
                return;

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Log.WriteError($"Removing discussion directory wasn't possible, Exception - {ex.Message}");
            }
          
            UpdateControls(ControlUpdateType.Designer);
        }

        private string ReadSpeechLengthFromFile(string path)
        {
            string text = File.ReadAllText(path);

            if (!double.TryParse(text, out double length))
            {
                Log.WriteError($"Incorrect speech length - {length}");
                return string.Empty;
            }

            return length.ToString();
        }

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

        private void StartSpeechTimer()
        {
            if (!double.TryParse(_textboxSpeechLength.Text, out double interval))
                return;

            _speechTimer.Interval = interval * 1000;
            _speechTimer.Start();
        }    

        private void StopSpeech()
        {
            _speechTimer.Stop();
            _soundPlayer.Stop();

            _isSpeechPlayed = false;

            UpdateControls(ControlUpdateType.Speech);
        }

        private void CreateVideo()
        {
            StopVideo();
            _videoPlayer.Source = null;
            Console.ExecuteCreateVideoScript(GetCurrentDiscussionVideoFolder(), VIDEOS_PATH, GetCurrentDiscussionId());

            UpdateControls(ControlUpdateType.Video);
        }

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

        private void StopVideo()
        {
            _isVideoPlayed = false;

            _videoPlayer.Stop();
            UpdateControls(ControlUpdateType.Video);
        }

        /// <summary/>
        /// <param name="path">Path to an existing screenshot</param>
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



        private void PreviousDiscussion()
        {
            if (_currentDiscussion > 0)
                _currentDiscussion -= 1;

            ChangeDiscussion();
        }

        private void NextDiscussion()
        {
            if (_currentDiscussion + 1 < DiscussionsCount)
                _currentDiscussion += 1;

            ChangeDiscussion();
        }

        private void ChangeDiscussion()
        {
            StopSpeech();

            WebViewOpenCurrentDiscussion();

            CleanupModesChanges();

            UpdateControls(ControlUpdateType.All);
        }

        private void DropDiscussion()
        {
            var currentDiscussionId = GetCurrentDiscussionId();

            if (!string.IsNullOrEmpty(currentDiscussionId))
            {
                _configQualityDiscussions.Remove(currentDiscussionId);

                if (_currentDiscussion >= DiscussionsCount)
                    _currentDiscussion -= 1;
            }

            if (string.IsNullOrEmpty(GetCurrentDiscussionId()))
                NoMoreDiscussions();
            else
                WebViewOpenCurrentDiscussion();

            UpdateControls(ControlUpdateType.All);
        }

        private void NoMoreDiscussions()
        {
            WebViewOpenEmptyPage();
            HideButtonsModes();
            DisplayDesignerControls(false);

            DisableTextEditMode();
            DisableDesignerMode();
        }

        private async Task Screenshot()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            CleanScreenshotViewer();

            await RunScreenshotProcedure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        private async Task ScreenshotAll()
        {
            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunScreenshotProcedure(i);
            }
        }

        private async Task RunScreenshotProcedure(int entryNumber)
        {
            await JS.ExecuteJSFunction(_webView, "hideEntryNumberNode", entryNumber.ToString());

            Thread.Sleep(SCREENSHOT_DELAY);

            await JS.ExecuteJSFunction(_webView, "sendScreenshotData", entryNumber.ToString());
            await JS.ExecuteJSFunction(_webView, "showEntryNumberNode", entryNumber.ToString());
        }

        private async Task Speak()
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            await RunSpeechProcedure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        private async Task SpeakAll()
        {
            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunSpeechProcedure(i);
            }
        }

        private async Task RunSpeechProcedure(int entryNumber)
        {
            await JS.ExecuteJSFunction(_webView, "sendSpeechData", entryNumber.ToString());
        }

        private async Task ScreenshotSpeak()
        {
            await Screenshot();
            await Speak();
        }

        private async Task ScreenshotSpeakAll()
        {
            for (int i = 0; i < _listboxEntries.Items.Count; i++)
            {
                await RunScreenshotProcedure(i);
                await RunSpeechProcedure(i);
            }
        }

        #region JS
        private void ParseJsonMessage(string jsonMessage)
        {
            JsonObject? json = null;

            try
            {
                json = JsonSerializer.Deserialize<JsonObject>(e.WebMessageAsJson);

                if (json == null)
                    return;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Error when deserializing json, Exception - {ex.Message}");
                return;
            }

            if (json["message"] == null)
            {
                Log.WriteError("Incorrect json message");
                return;
            }

            switch (json["message"].ToString())
            {
                case "EntriesLength":
                    JsonMessageEntriesLength(json);
                    break;
                case "ScreenshotData":
                    JsonMessageScreenshotData(json);
                    break;
                case "SpeechData":
                    JsonMessageSpeechData(json);
                    break;
            }
        }

        private void JsonMessageEntriesLength(JsonObject json)
        {
            var valueJson = json["value"];

            if (valueJson == null)
            {
                Log.WriteError($"Json {json["message"]} missing parameter");
                return;
            }

            if (!JS.TryGetIntFromJsonNode(valueJson, out int value))
            {
                Log.WriteError($"Json parameter value is not an int");
                return;
            }

            _listboxEntries.Items.Clear();
            for (int i = 0; i < value; ++i)
            {
                _listboxEntries.Items.Add((i + 1).ToString());
            }
        }

        private void JsonMessageScreenshotData(JsonObject json)
        {
            if (!ValidateScreenshotData(json, out Rectangle rect, out int entryNumber))
                return;

            TakeScreenshot(entryNumber, CalculateActualScreenshotRectangle(rect));
        }

        private bool ValidateScreenshotData(JsonObject json, out Rectangle rect, out int entryNumber)
        {
            rect = new();
            entryNumber = -1;

            if (json["entryNumber"] == null || json["x"] == null || json["y"] == null || json["width"] == null || json["height"] == null)
            {
                Log.WriteError("Invalid screenshot data");
                return false;
            }

            if (!JS.TryGetIntFromJsonNode(json["entryNumber"], out entryNumber) || 
                !JS.TryGetIntFromJsonNode(json["x"], out int x) ||
                !JS.TryGetIntFromJsonNode(json["y"], out int y) ||
                !JS.TryGetIntFromJsonNode(json["width"], out int width) ||
                !JS.TryGetIntFromJsonNode(json["height"], out int height)
                )
            {
                Log.WriteError("Invalid screenshot data");
                return false;
            }

            rect = new(x, y, width, height);

            return true;
        }

        private Rectangle CalculateActualScreenshotRectangle(Rectangle rect)
        {
            double displayScaling = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;

            rect.X = (int)(rect.X * displayScaling);
            rect.X += (int)_webView.Margin.Left;
            rect.Y = (int)(rect.Y * displayScaling) + (int)_webView.Margin.Top;
            rect.Width = (int)(rect.Width * displayScaling);
            rect.Height = (int)(rect.Height * displayScaling);

            return rect;
        }

        private void TakeScreenshot(int entryNumber, Rectangle rect)
        {
            Thread.Sleep(SCREENSHOT_DELAY);

            try
            {
                Bitmap bitmap = new(rect.Width, rect.Height);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, new System.Drawing.Size(rect.Width * 16 / 9, rect.Height * 16 / 9));

                    string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionVideoFolder(), (entryNumber + 1).ToString()), ".png");

                    Bitmap canvas = new Bitmap(1080, 1920);
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.Clear(System.Drawing.Color.Black);

                        var bitmapWidth = bitmap.Width * 16 / 9;
                        var bitmapHeight = bitmap.Height * 16 / 9;
                        g.DrawImage(bitmap, 540 - bitmapWidth / 2, 960 - bitmapHeight / 2, bitmapWidth, bitmapHeight);

                        try
                        {
                            canvas.Save(path, ImageFormat.Png);
                        }
                        catch (Exception ex)
                        {
                            Log.WriteError($"Couldn't save a screenshot {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteError($"Couldn't take a screenshot, Exception - {ex.Message}");
            }

            UpdateControls(ControlUpdateType.Screenshot);
        }

        private async void JsonMessageSpeechData(JsonObject json)
        {
            ValidateSpeechData(json, out int entryNumber, out string text, out bool isMale);

            string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionVideoFolder(), (entryNumber + 1).ToString()), ".wav");
            var speechLength = await _speechService.GenerateAudioFile(path, PrepareTextForSpeech(text), isMale);

            SaveSpeechLengthToFile(entryNumber, speechLength);

            UpdateControls(ControlUpdateType.Speech);       
        }

        private bool ValidateSpeechData(JsonObject json, out int entryNumber, out string text, out bool isMale)
        {
            text = string.Empty;
            entryNumber = -1;
            isMale = true;

            if (json["entryNumber"] == null || json["text"] == null || json["isMale"] == null)
            {
                Log.WriteError("Invalid speech data");
                return false;
            }

            if (!JS.TryGetIntFromJsonNode(json["entryNumber"], out entryNumber) ||
                !JS.TryGetBoolFromJsonNode(json["isMale"], out isMale))
            {
                Log.WriteError("Invalid speech data");
                return false;
            }

            text = json["text"].ToString();

            return true;
        }

        private string PrepareTextForSpeech(string text)
        {
            if (!text.Contains('@'))
                return text;

            while (text[0] == '\n')
            {
                text = text.Substring(text.IndexOf('\n') + 1);
            }

            while (text[0] == '@')
            {
                text = text.Substring(text.IndexOf(' ') + 1);
            }

            return text;
        }

        private void SaveSpeechLengthToFile(int entryNumber, double speechLength)
        {
            string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionVideoFolder(), (entryNumber + 1).ToString()), ".txt");

            try
            {
                File.WriteAllText(path, speechLength.ToString());
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't create speech length file - {ex.Message}");
            }
        }

        private string GetCurrentDiscussionVideoFolder()
        {
            string discussionPath = Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId());

            if (!Directory.Exists(discussionPath))
                Directory.CreateDirectory(discussionPath);

            return discussionPath;
        }
        #endregion JS
        #region Discussions
        private string GetCurrentDiscussionId()
        {
            if (_configQualityDiscussions.Lines == null)
            {
                Log.WriteError("QualityDiscussions Config is null");
                return string.Empty;
            }

            if (_configQualityDiscussions.Lines.Count == 0)
                return string.Empty;

            try
            {
                var result = _configQualityDiscussions.Lines.ElementAt(_currentDiscussion).Key;

                return result;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Couldn't get current discussion Id, Exception - {ex.Message}");
                return string.Empty;
            }
        }

        private string GetCurrentDiscussionRating()
        {
            var rating = _configQualityDiscussions.GetString(GetCurrentDiscussionId());
            return string.IsNullOrEmpty(rating) ? string.Empty : rating;
        }
        #endregion Discussions
    }
}
#pragma warning restore CS8602