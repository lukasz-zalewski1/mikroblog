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

        private readonly Config _configQualityDiscussions = new(Manager.QUALITY_DISCUSSIONS_FILE_NAME);

        private int _currentDiscussion;
        private int DiscussionsCount { get => _configQualityDiscussions.Lines != null ? _configQualityDiscussions.Lines.Count : 0; }

        private const string RESOURCE_NAME_JS_EDIT_MODE = "mikroblog.videos_designer.src.JS.EditMode.js";
        private const string RESOURCE_NAME_JS_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.DesignerMode.js";

        private const int SCREENSHOT_DELAY = 100;

        private StateType _state;

        private TextToSpeech _speechService = new();

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
            UpdateControls();
        }

        private void UpdateControls()
        {
            UpdateLabelDiscussionId();
            UpdateLabelDiscussionNumber();
            UpdateLabelDiscussionQuality();
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

        #region Window Events        
        private void InitializeEvents()
        {
            _window.KeyDown += OnKeyDown;

            InitializeWebViewEvents();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                _window.Close();
        }

        private void _buttonPreviousDiscussion_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDiscussion > 0)
                _currentDiscussion -= 1;

            OpenDiscussion(GetCurrentDiscussionId());

            CleanupModesChanges();

            UpdateControls();
        }

        private void _buttonNextDiscussion_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDiscussion + 1 < DiscussionsCount)
                _currentDiscussion += 1;

            OpenDiscussion(GetCurrentDiscussionId());

            CleanupModesChanges();

            UpdateControls();
        }

        private void CleanupModesChanges()
        {
            DisableTextEditMode();
            DisableDesignerMode();
            CleanDesignerEntries();
        }

        private void _buttonDropDiscussion_Click(object sender, RoutedEventArgs e)
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
            {
                OpenEmptyPage();
            }
            else
                OpenDiscussion(currentDiscussionId);

            UpdateControls();
        }

        private void _buttonTextEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (_state != StateType.TextEdit)
                EnableTextEditMode();
            else
                DisableTextEditMode();
        }

        private void _buttonDesigner_Click(object sender, RoutedEventArgs e)
        {
            if (_state != StateType.Designer)
                EnableDesignerMode();
            else
                DisableDesignerMode();
        }

        private async void _buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            await RunScreenshotProcedure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        private async void _buttonScreenshotAll_Click(object sender, RoutedEventArgs e)
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

        private async void _buttonSpeak_Click(object sender, RoutedEventArgs e)
        {
            if (_listboxEntries.SelectedItem == null)
                return;

            await RunSpeakProdecure(int.Parse((string)_listboxEntries.SelectedItem) - 1);
        }

        private async Task RunSpeakProdecure(int entryNumber)
        {
            await JS.ExecuteJSFunction(_webView, "sendSpeechData", entryNumber.ToString());
        }
        #endregion Window Events

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

            Screenshot(entryNumber, CalculateActualScreenshotRectangle(rect));
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
            rect.X += (int)_webView.Width;
            rect.Y = (int)(rect.Y * displayScaling);
            rect.Width = (int)(rect.Width * displayScaling);
            rect.Height = (int)(rect.Height * displayScaling);

            return rect;
        }

        private void Screenshot(int entryNumber, Rectangle rect)
        {
            Bitmap bitmap = new(rect.Width, rect.Height);
            Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, new System.Drawing.Size(rect.Width, rect.Height));

            bitmap.Save($"c:\\users\\lza\\desktop\\workplace\\test\\{entryNumber}.png", ImageFormat.Png);
        }

        private async void JsonMessageSpeechData(JsonObject json)
        {
            ValidateSpeechData(json, out string text, out int entryNumber);

            await _speechService.GenerateAudioFile($"c:\\users\\lza\\desktop\\workplace\\test\\{entryNumber}.wav", PrepareTextForSpeech(text), true);
        }

        private bool ValidateSpeechData(JsonObject json, out string text, out int entryNumber)
        {
            text = string.Empty;
            entryNumber = -1;

            if (json["entryNumber"] == null || json["text"] == null)
            {
                Log.WriteError("Invalid speech data");
                return false;
            }

            if (!JS.TryGetIntFromJsonNode(json["entryNumber"], out entryNumber))
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

            while (text[0] == '@')
            {
                text = text.Substring(text.IndexOf(' ') + 1);
            }

            return text;
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
            _buttonDesigner.Content = "Disable Designer Mode";

            await JS.ExecuteJSFunction(_webView, "enableDesignerMode");
        }

        private async void DisableDesignerMode()
        {
            _state = StateType.None;
            _buttonDesigner.Content = "Enable Designer Mode";

            await JS.ExecuteJSFunction(_webView, "disableDesignerMode");
        }

        private async void CleanDesignerEntries()
        {
            await JS.ExecuteJSFunction(_webView, "cleanEntries");
            _listboxEntries.Items.Clear();
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