using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

using mikroblog.fast_quality_check;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Drawing.Imaging;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        public enum StateType
        {
            None,
            TextEdit,
            Designer
        }

        private readonly VideosDesignerManager _manager = new();

        private const string RESOURCE_NAME_JS_ENABLE_EDIT_MODE = "mikroblog.videos_designer.src.JS.EnableEditMode.js";
        private const string RESOURCE_NAME_JS_DISABLE_EDIT_MODE = "mikroblog.videos_designer.src.JS.DisableEditMode.js";
        private const string RESOURCE_NAME_JS_ENABLE_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.EnableDesignerMode.js";
        private const string RESOURCE_NAME_JS_DISABLE_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.DisableDesignerMode.js";
        private const string RESOURCE_NAME_JS_CLEAN_DESIGNER_ENTRIES = "mikroblog.videos_designer.src.JS.CleanDesignerEntries.js";

        private StateType _state;

        public VideosDesignerWindow()
        {
            InitializeComponent();

            InitializeEvents();

            InitializeControls();

            InitializeMikroblogBrowser();

            
        }

        #region Controls
        private void InitializeControls()
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            UpdateLabelsContent();
        }
 
        private void UpdateLabelsContent()
        {
            UpdateLabelDiscussionId();
            UpdateLabelDiscussionNumber();
            UpdateLabelDiscussionQuality();
        }

        private void UpdateLabelDiscussionId()
        {
            _labelDiscussionId.Content = $"Discussion Id: {_manager.GetCurrentDiscussionId()}";
        }

        private void UpdateLabelDiscussionNumber()
        {
            _labelDiscussionNumber.Content = $"Discussion: {_manager.CurrentDiscussion + 1} / {_manager.DiscussionsCount}";
        }

        private void UpdateLabelDiscussionQuality()
        {
            _labelDiscussionQuality.Content = $"Quality: {_manager.GetCurrentDiscussionRating()}";
        }
        #endregion Controls

        #region Events        
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

        private void InitializeWebViewEvents()
        {
            _webView.WebMessageReceived += _webView_WebMessageReceived;
        }

        private void _webView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {         
            try
            {
                var json = JsonSerializer.Deserialize<List<JsonObject>>(e.WebMessageAsJson);

                if (json == null)
                    return;
           
                ParseJsonMessage(json);
            }
            catch (Exception ex)
            {
                Log.WriteError($"Error when deserializing json, Exception - {ex.Message}");
            }
        }

        private void ParseJsonMessage(List<JsonObject> json)
        {
            _manager.Entries = new();

            foreach (var entryJson in json)
            {
                if (entryJson["x"] == null || entryJson["y"] == null || entryJson["width"] == null || entryJson["height"] == null || entryJson["text"] == null)
                {
                    Log.WriteError("One of the entry properties wasn't found in the json message.");
                    return;
                }

                VideosDesignerManager.Entry entry = new();

#pragma warning disable CS8604 // Possible null reference argument.
                if (!TryGetIntValueFromJson(entryJson["x"], out entry.X) ||
                    !TryGetIntValueFromJson(entryJson["y"], out entry.Y) ||
                    !TryGetIntValueFromJson(entryJson["width"], out entry.Width) ||
                    !TryGetIntValueFromJson(entryJson["height"], out entry.Height))
                {
                    Log.WriteError("One of the entry json properties has an invalid value.");
                    return;
                }

                entry.Text = GetStringValueFromJson(entryJson["text"]);
#pragma warning restore CS8604 // Possible null reference argument.

                _manager.Entries.Add(entry);
            }
        }

        private bool TryGetIntValueFromJson(JsonNode json, out int value)
        {
            if (int.TryParse(json.ToString(), out value))
                return true;

            if (float.TryParse(json.ToString(), out float floatValue))
            {
                value = Convert.ToInt32(floatValue);
                return true;
            }

            return false;
        }

        private string GetStringValueFromJson(JsonNode json)
        {
            return json.ToString();
        }

        private void _buttonPreviousDiscussion_Click(object sender, RoutedEventArgs e)
        {
            _manager.PreviousDiscussion();
            OpenDiscussion(_manager.GetCurrentDiscussionId());

            CleanupModesChanges();

            UpdateControls();
        }

        private void _buttonNextDiscussion_Click(object sender, RoutedEventArgs e)
        {
            _manager.NextDiscussion();
            OpenDiscussion(_manager.GetCurrentDiscussionId());

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
            _manager.DropDiscussion();

            var currentDiscussionId = _manager.GetCurrentDiscussionId();
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
        #endregion Events

        private void EnableTextEditMode()
        {
            if (_state == StateType.Designer)
                DisableDesignerMode();

            _state = StateType.TextEdit;
            _buttonTextEditMode.Content = "Disable Text Edit Mode";

            ExecuteJSScript(RESOURCE_NAME_JS_ENABLE_EDIT_MODE);
        }

        private void DisableTextEditMode()
        {
            _state = StateType.None;
            _buttonTextEditMode.Content = "Enable Text Edit Mode";

            ExecuteJSScript(RESOURCE_NAME_JS_DISABLE_EDIT_MODE);
        }

        private void EnableDesignerMode()
        {
            if (_state == StateType.TextEdit)
                DisableTextEditMode();

            _state = StateType.Designer;
            _buttonDesigner.Content = "Disable Designer Mode";

            ExecuteJSScript(RESOURCE_NAME_JS_ENABLE_DESIGNER_MODE);
        }

        private void DisableDesignerMode()
        {
            _state = StateType.None;
            _buttonDesigner.Content = "Enable Designer Mode";

            ExecuteJSScript(RESOURCE_NAME_JS_DISABLE_DESIGNER_MODE);
        }

        private void CleanDesignerEntries()
        {
            ExecuteJSScript(RESOURCE_NAME_JS_CLEAN_DESIGNER_ENTRIES);
        }

        private void _buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bmp = new Bitmap(926, 440);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(966, 549, 0, 0, new System.Drawing.Size(926, 440));
            bmp.Save(@"c:\users\lza\desktop\workplace\test\image.png", ImageFormat.Png);
        }

        #region WebView Events
        private async void ExecuteJSScript(string name)
        {
            string? script = Util.GetResource(name);
            if (script == null)
                return;

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        #endregion WebView Events

        #region WebView
        private void InitializeMikroblogBrowser()
        {
            OpenDiscussion(_manager.GetCurrentDiscussionId());
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
    }
}
