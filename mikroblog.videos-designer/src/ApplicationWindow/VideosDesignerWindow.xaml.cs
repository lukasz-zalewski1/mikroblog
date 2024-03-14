using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Microsoft.Web.WebView2.Core;

using mikroblog.fast_quality_check;
using System.IO;
using System.Windows.Media.Imaging;
using System.Media;
using System.Windows.Controls;
using Microsoft.VisualBasic;

#pragma warning disable CS8602
#pragma warning disable CS8604
namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private enum StateType
        {
            None,
            TextEdit,
            Designer
        }

        private enum ShortcutCommand
        {
            Exit,
            PreviousDiscussion,
            NextDiscussion,
            DropDiscussion,
            TextEditMode,
            DesignerMode,
            Screenshot,
            ScrenshotAll,
            Speak,
            SpeakAll,
            ScreenshotAndSpeak,
            ScreenshotAndSpeakAll
        }

        private readonly string DISCUSSIONS_PATH = Path.Combine(fast_quality_check.Util.WORKPLACE_PATH, "discussions");
        private readonly string VIDEOS_PATH = Path.Combine(fast_quality_check.Util.WORKPLACE_PATH, "videos");

        // private readonly Config _configQualityDiscussions = new(Manager.QUALITY_DISCUSSIONS_FILE_NAME);
        private readonly Config _configQualityDiscussions = new("workon");

        private int _currentDiscussion;
        private int DiscussionsCount { get => _configQualityDiscussions.Lines != null ? _configQualityDiscussions.Lines.Count : 0; }

        private const string RESOURCE_NAME_JS_EDIT_MODE = "mikroblog.videos_designer.src.JS.EditMode.js";
        private const string RESOURCE_NAME_JS_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.DesignerMode.js";

        private const int SCREENSHOT_DELAY = 100;

        private StateType _state;

        private TextToSpeech _speechService = new();
        private System.Timers.Timer? _speechTimer = new();
        private bool _isSpeechPlayed = false;

        private SoundPlayer _soundPlayer = new();
      
        public VideosDesignerWindow()
        {
            InitializeComponent();

            InitializeControls();

            InitializeEvents();
           
            InitializeMikroblogBrowser(); 
        }

        #region Controls
        private void InitializeControls()
        {
            DisplayDesignerControls(false);

            UpdateControls();
        }

        private void UpdateControls()
        {
            UpdateGridRemoveDiscussionFiles();

            UpdateLabelDiscussionId();
            UpdateLabelDiscussionNumber();
            UpdateLabelDiscussionQuality();
        }

        private void UpdateGridRemoveDiscussionFiles()
        {
            var discussionId = GetCurrentDiscussionId();

            if (discussionId == null)
            {
                _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                return;
            }

            if (!Directory.Exists(GetCurrentDiscussionVideoFolder()))
            {
                _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                return;
            }

            if (Directory.GetFiles(GetCurrentDiscussionVideoFolder()).Length <= 0)
            {
                _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                return;
            }

            _gridRemoveDiscussionFiles.Visibility = Visibility.Visible;
        }

        private void UpdateLabelDiscussionId()
        {
            _labelDiscussionId.Content = $"Discussion Id: {GetCurrentDiscussionId()}";
        }
    
        private void UpdateLabelDiscussionNumber()
        {
            _labelDiscussionNumber.Content = $"Discussion: {_currentDiscussion + 1} / {DiscussionsCount}";
        }

        private void UpdateLabelDiscussionQuality()
        {
            _labelDiscussionQuality.Content = $"Quality: {GetCurrentDiscussionRating()}";
        }
        #endregion Controls

        private void InitializeEvents()
        {
            _window.KeyDown += OnKeyDown;

            _listboxEntries.SelectionChanged += _listboxEntries_SelectionChanged;

            _textboxSpeechLength.TextChanged += _textboxSpeechLength_TextChanged;
            _textboxSpeechLength.PreviewTextInput += _textboxSpeechLength_PreviewTextInput;

            _speechTimer.Elapsed += _speechTimer_Elapsed;

            InitializeWebViewEvents();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void _listboxEntries_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateScreenshotViewer();
            UpdatePlaySpeechButton();
            UpdateSpeechLengthTextbox();
        }

        private void _textboxSpeechLength_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateSpeechLengthFile();
        }

        private void _textboxSpeechLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool approvedDecimalPoint = false;

            if (e.Text == ".")
            {
                if (!((TextBox)sender).Text.Contains('.'))
                    approvedDecimalPoint = true;
            }

            if (!(char.IsDigit(e.Text, e.Text.Length - 1) || approvedDecimalPoint))
                e.Handled = true;
        }

        #region ButtonsEvents
        private void _buttonPreviousDiscussion_Click(object sender, RoutedEventArgs e)
        {
            PreviousDiscussion();
        }

        private void _buttonNextDiscussion_Click(object sender, RoutedEventArgs e)
        {
            NextDiscussion();
        }

        private void _buttonDropDiscussion_Click(object sender, RoutedEventArgs e)
        {
            DropDiscussion();
        }

        private void _buttonRemoveDiscussionFiles_Click(object sender, RoutedEventArgs e)
        {
            RemoveDiscussionFiles();
        }

        private void _buttonTextEditMode_Click(object sender, RoutedEventArgs e)
        {
            TextEditMode();
        }

        private void _buttonDesignerMode_Click(object sender, RoutedEventArgs e)
        {
            DesignerMode();
        }

        private async void _buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            await Screenshot();
        }

        private async void _buttonScreenshotAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotAll();
        }

        private async void _buttonSpeak_Click(object sender, RoutedEventArgs e)
        {
            await Speak();
        }

        private async void _buttonSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await SpeakAll();
        }

        private async void _buttonScreenshotSpeak_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeak();
        }

        private async void _buttonScreenshotSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeakAll();
        }

        private void _buttonPlaySpeech_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpeechPlayed)
                StopSpeech();
            else
                PlaySpeech();
        }

        private void _buttonCreateVideo_Click(object sender, RoutedEventArgs e)
        {
            Console.ExecuteCreateVideoScript(GetCurrentDiscussionVideoFolder(), VIDEOS_PATH);
        }

        private void _speechTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            StopSpeech();
        }
        #endregion ButtonsEvents

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

            UpdateSpeechButton();
            UpdateScreenshotViewer();
            UpdateControls();
        }

        private void UpdateScreenshotViewer()
        {
            if (_listboxEntries.SelectedItem == null)
            {
                CleanScreenshotViewer();
                return;
            }

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".png");

            if (!File.Exists(path))
            {
                CleanScreenshotViewer();
                return;
            }

            byte[] array = File.ReadAllBytes(path);

            using (var memoryStream = new MemoryStream(array))
            {
                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                _screenshotViewer.Source = bitmapImage;
            }        
        }

        private void UpdatePlaySpeechButton()
        {
            if (_listboxEntries.SelectedItem == null)
            {
                _buttonPlaySpeech.IsEnabled = false;
                return;
            }

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".wav");

            if (!File.Exists(path))
            {
                _buttonPlaySpeech.IsEnabled = false;
                return;
            }

            _buttonPlaySpeech.IsEnabled = true;
        }

        private void UpdateSpeechLengthTextbox()
        {
            if (_listboxEntries.SelectedItem == null)
            {
                _textboxSpeechLength.IsEnabled = false;
                _textboxSpeechLength.Text = string.Empty;
                return;
            }

            string path = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".txt");

            if (!File.Exists(path))
            {
                _textboxSpeechLength.IsEnabled = false;
                _textboxSpeechLength.Text = string.Empty;
                return;
            }

            _textboxSpeechLength.IsEnabled = true;

            _textboxSpeechLength.Text = ReadSpeechLengthFromFile(path);
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
            UpdateSpeechButton();
        }

        private void StartSpeechTimer()
        {
            if (!double.TryParse(_textboxSpeechLength.Text, out double interval))
                return;

            _speechTimer.Interval = interval * 1000;
            _speechTimer.Start();
        }

        private void UpdateSpeechButton()
        {
            if (_isSpeechPlayed)
                _buttonPlaySpeech.Content = "Stop Speech";
            else
                _buttonPlaySpeech.Content = "Play Speech";
        }

        private void StopSpeech()
        {
            _speechTimer.Stop();
            _soundPlayer.Stop();

            _isSpeechPlayed = false;
            UpdateSpeechButton();
        }

        private void CleanupModesChanges()
        {
            DisableTextEditMode();
            DisableDesignerMode();
            CleanDesignerEntries();
            CleanScreenshotViewer();
        }

        private void Exit()
        {
            _window.Close();
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

            OpenDiscussion(GetCurrentDiscussionId());

            CleanupModesChanges();

            UpdateControls();
        }

        private void DropDiscussion()
        {
            var currentDiscussionId = GetCurrentDiscussionId();
            if (currentDiscussionId != null)
            {
                _configQualityDiscussions.Remove(currentDiscussionId);

                if (_currentDiscussion >= DiscussionsCount)
                    _currentDiscussion -= 1;
            }

            currentDiscussionId = GetCurrentDiscussionId();
            if (currentDiscussionId == null)
                NoMoreDiscussions();
            else
                OpenDiscussion(currentDiscussionId);

            UpdateControls();
        }

        private void NoMoreDiscussions()
        {
            OpenEmptyPage();
            HideModeButtons();
            DisplayDesignerControls(false);

            DisableTextEditMode();
            DisableDesignerMode();
        }

        private void HideModeButtons()
        {
            _buttonTextEditMode.Visibility = Visibility.Hidden;
            _buttonDesignerMode.Visibility = Visibility.Hidden;
        }

        private void TextEditMode()
        {
            if (_state != StateType.TextEdit)
                EnableTextEditMode();
            else
                DisableTextEditMode();
        }

        private void DesignerMode()
        {
            if (_state != StateType.Designer)
                EnableDesignerMode();
            else
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

        #region WebView Events
        private void InitializeWebViewEvents()
        {
            _webView.WebMessageReceived += _webView_WebMessageReceived;
        }

        private void _webView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonObject>(e.WebMessageAsJson);

                if (json == null)
                    return;

                ParseJsonMessage(json);
            }
            catch (Exception ex)
            {
                Log.WriteError($"Error when deserializing json, Exception - {ex.Message}");
            }
        }
        #endregion WebView Events

        #region JS
        private void ParseJsonMessage(JsonObject json)
        {
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
            rect.Y = (int)(rect.Y * displayScaling);
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
                Graphics graphics = Graphics.FromImage(bitmap);

                graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, new System.Drawing.Size(rect.Width * 16 / 9, rect.Height * 16 / 9));


                string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionVideoFolder(), (entryNumber + 1).ToString()), ".png");


                Bitmap canvas = new Bitmap(1080, 1920);
                using (var g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Black);

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
            catch (Exception ex)
            {
                Log.WriteError($"Couldn't take a screenshot, Exception - {ex.Message}");
            }

            UpdateControls();
            UpdateScreenshotViewer();
        }

        private async void JsonMessageSpeechData(JsonObject json)
        {
            ValidateSpeechData(json, out int entryNumber, out string text, out bool isMale);

            string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionVideoFolder(), (entryNumber + 1).ToString()), ".wav");
            var speechLength = await _speechService.GenerateAudioFile(path, PrepareTextForSpeech(text), isMale);

            SaveSpeechLengthToFile(entryNumber, speechLength);

            UpdateControls();
            UpdatePlaySpeechButton();
            UpdateSpeechLengthTextbox();
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
            var discussionId = GetCurrentDiscussionId();

            if (discussionId == null)
                return string.Empty;

            string discussionPath = Path.Combine(DISCUSSIONS_PATH, discussionId);

            if (!Directory.Exists(discussionPath))
                Directory.CreateDirectory(discussionPath);

            return discussionPath;
        }
        #endregion JS

        #region Modes
        private async void EnableTextEditMode()
        {
            JS.ExecuteJSScript(_webView, RESOURCE_NAME_JS_EDIT_MODE);

            if (_state == StateType.Designer)
                DisableDesignerMode();

            _state = StateType.TextEdit;
            _buttonTextEditMode.Content = "Disable Text Edit Mode";

            DisplayDesignerControls(false);

            await JS.ExecuteJSFunction(_webView, "enableEditMode");
        }

        private async void DisableTextEditMode()
        {
            _state = StateType.None;
            _buttonTextEditMode.Content = "Enable Text Edit Mode";

            await JS.ExecuteJSFunction(_webView, "disableEditMode");
        }

        private async void EnableDesignerMode()
        {
            JS.ExecuteJSScript(_webView, RESOURCE_NAME_JS_DESIGNER_MODE);

            if (_state == StateType.TextEdit)
                DisableTextEditMode();

            _state = StateType.Designer;
            _buttonDesignerMode.Content = "Disable Designer Mode";

            DisplayDesignerControls(true);

            await JS.ExecuteJSFunction(_webView, "enableDesignerMode");
        }

        private async void DisableDesignerMode()
        {
            _state = StateType.None;
            _buttonDesignerMode.Content = "Enable Designer Mode";

            DisplayDesignerControls(false);

            await JS.ExecuteJSFunction(_webView, "disableDesignerMode");
        }

        private async void CleanDesignerEntries()
        {
            await JS.ExecuteJSFunction(_webView, "cleanEntries");
            _listboxEntries.Items.Clear();
        }

        private void DisplayDesignerControls(bool display)
        {
            Visibility visibility = display ? Visibility.Visible : Visibility.Hidden;

            _gridDesignerMenu.Visibility = visibility;
            _gridScreenshotViewer.Visibility = visibility;
        }

        private void CleanScreenshotViewer()
        {
            _screenshotViewer.Source = null;
        }
        #endregion Modes

        #region WebView
        private void InitializeMikroblogBrowser()
        {
            OpenDiscussion(GetCurrentDiscussionId());
        }

        private void OpenDiscussion(string? currentDiscussionId)
        {
            if (currentDiscussionId == null)
                return;

            var uri = new Uri(DiscussionDownloader.DISCUSSION_NAME_TEMPLATE + currentDiscussionId);
            try
            {
                _webView.Source = uri;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't open - {uri}, Exception - {ex.Message}");
            }
        }

        public void OpenEmptyPage()
        {
            _webView.Source = new Uri("about:blank");
        }
        #endregion WebView

        #region Discussions
        private string? GetCurrentDiscussionId()
        {
            if (_configQualityDiscussions.Lines == null)
            {
                Log.WriteError("QualityDiscussions Config is null");
                return null;
            }

            if (_configQualityDiscussions.Lines.Count == 0)
                return null;

            return _configQualityDiscussions.Lines.ElementAt(_currentDiscussion).Key;
        }

        private string? GetCurrentDiscussionRating()
        {
            var discussionId = GetCurrentDiscussionId();

            if (discussionId == null)
                return null;

            return _configQualityDiscussions.GetString(discussionId);
        }

        #endregion Discussions
    }
}
#pragma warning restore CS8602