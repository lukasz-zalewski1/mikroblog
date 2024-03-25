using System;
using System.IO;
using System.Windows;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private enum ControlUpdateType
        {
            All,
            ModeChange,
            Designer,
            Screenshot,
            Speech,
            Video,         
        }

        private enum ScreenshotViewerAndVideoPlayerVisibilityType
        {
            ShowScreenshotViewer,
            ShowVideoPlayer
        }

        /// <summary>
        /// Calls <see cref="DisplayDesignerControls(false)"/> and <see cref="UpdateControls(ControlUpdateType.All)"/>.
        /// </summary>
        private void InitializeControls()
        {
            DisplayDesignerControls(false);

            UpdateControls(ControlUpdateType.All);
        }

        /// <summary>
        /// Calls methods which update controls depending on <paramref name="controlUpdateType"/>.
        /// </summary>
        private void UpdateControls(ControlUpdateType controlUpdateType)
        {
            if (controlUpdateType == ControlUpdateType.All)
            {
                UpdateLabelDiscussionNumber();
                UpdateLabelDiscussionId();
                UpdateLabelDiscussionQuality();
            }

            // Always
            UpdateGridRemoveDiscussionFiles();

            if (controlUpdateType == ControlUpdateType.All || controlUpdateType == ControlUpdateType.ModeChange)
                UpdateButtonsContentModes();
           
            if (controlUpdateType == ControlUpdateType.All || controlUpdateType == ControlUpdateType.Designer || controlUpdateType == ControlUpdateType.Speech)
                UpdateControlsSpeech();

            if (controlUpdateType == ControlUpdateType.All || controlUpdateType == ControlUpdateType.Designer || controlUpdateType == ControlUpdateType.Screenshot)
                UpdateScreenshotViewer();

            if (controlUpdateType == ControlUpdateType.All || controlUpdateType == ControlUpdateType.Designer || controlUpdateType == ControlUpdateType.Video)
                UpdateControlsPlayVideo();
        }

        /// <summary>
        /// Updates <see cref="_labelDiscussionNumber"/> content.
        /// </summary>
        private void UpdateLabelDiscussionNumber()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _labelDiscussionNumber.Content = $"{Strings.LabelDiscussionNumber} {_currentDiscussion + 1} / {DiscussionsCount}";
            }));
        }

        /// <summary>
        /// Updates <see cref="_labelDiscussionId"/> content.
        /// </summary>
        private void UpdateLabelDiscussionId()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _labelDiscussionId.Content = $"{Strings.LabelDiscussionId} {GetCurrentDiscussionId()}";
            }));
        }

        /// <summary>
        /// Updates <see cref="_labelDiscussionQuality" content./>
        /// </summary>
        private void UpdateLabelDiscussionQuality()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _labelDiscussionQuality.Content = $"{Strings.LabelDiscussionQuality} {GetCurrentDiscussionRating()}";
            }));
        }

        /// <summary>
        /// Checks if discussion folder contains any files and displays <see cref="_gridRemoveDiscussionFiles"/> if so.
        /// </summary>
        private void UpdateGridRemoveDiscussionFiles()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (string.IsNullOrEmpty(GetCurrentDiscussionId()))
                {
                    _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                    return;
                }

                if (!Directory.Exists(GetCurrentDiscussionFolder()))
                {
                    _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                    return;
                }

                if (Directory.GetFiles(GetCurrentDiscussionFolder()).Length <= 0)
                {
                    _gridRemoveDiscussionFiles.Visibility = Visibility.Hidden;
                    return;
                }

                _gridRemoveDiscussionFiles.Visibility = Visibility.Visible;
            }));
        }

        /// <summary>
        /// Updates content of <see cref="_buttonTextEditMode"/> and <see cref="_buttonDesignerMode"/> content depending on <see cref="_mode"/>.
        /// </summary>
        private void UpdateButtonsContentModes()
        {
            switch (_mode)
            {
                case Mode.None:
                    _buttonTextEditMode.Content = Strings.ButtonContentEnableTextEditMode;
                    _buttonDesignerMode.Content = Strings.ButtonContentEnableDesignerMode;
                    break;
                case Mode.TextEdit:
                    _buttonTextEditMode.Content = Strings.ButtonContentDisableTextEditMode;
                    _buttonDesignerMode.Content = Strings.ButtonContentEnableDesignerMode;
                    break;
                case Mode.Designer:
                    _buttonTextEditMode.Content = Strings.ButtonContentEnableTextEditMode;
                    _buttonDesignerMode.Content = Strings.ButtonContentDisableDesignerMode;
                    break;
            }
        }

        /// <summary>
        /// Calls <see cref="UpdateButtonPlaySpeechContent"/> and then checks if audio and speech length files exist. If they do it enables speech controls, if not it disables them.
        /// </summary>
        private void UpdateControlsSpeech()
        {
            UpdateButtonPlaySpeechContent();

            if (_listboxEntries.SelectedItem == null)
            {
                DisableControlsSpeech();
                return;
            }

            string pathSpeechLength = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".txt");
            string pathAudio = Path.ChangeExtension(Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId(), _listboxEntries.SelectedItem.ToString()), ".wav");

            if (!(File.Exists(pathSpeechLength) && File.Exists(pathAudio)))
            {
                DisableControlsSpeech();
                return;
            }

            _textboxSpeechLength.IsEnabled = true;
            _textboxSpeechLength.Text = ReadSpeechLengthFromFile();
            _buttonPlaySpeech.IsEnabled = true;
        }

        /// <summary>
        /// Updates <see cref="_buttonPlaySpeech"/> content depending on <see cref="_isSpeechPlayed"/> value.
        /// </summary>
        private void UpdateButtonPlaySpeechContent()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (_isSpeechPlayed)
                    _buttonPlaySpeech.Content = Strings.ButtonContentStopSpeech;
                else
                    _buttonPlaySpeech.Content = Strings.ButtonContentPlaySpeech;
            }));
        }

        /// <summary>
        /// Calls <see cref="UpdateButtonPlayVideoContent"/> and enables or disabled <see cref="_buttonPlayVideo"/> depending on video existance.
        /// </summary>
        private void UpdateControlsPlayVideo()
        {
            UpdateButtonPlayVideoContent();

            string pathVideo = Path.ChangeExtension(Path.Combine(VIDEOS_PATH, GetCurrentDiscussionId()), ".mp4");

            if (!File.Exists(pathVideo))
            {
                _buttonPlayVideo.IsEnabled = false;
                return;
            }

            _buttonPlayVideo.IsEnabled = true;
        }

        /// <summary>
        /// Updates <see cref="_buttonPlayVideo"/> content depending on <see cref="_isVideoPlayed"/> value.
        /// </summary>
        private void UpdateButtonPlayVideoContent()
        {
            if (_isVideoPlayed)
                _buttonPlayVideo.Content = Strings.ButtonContentStopVideo;
            else
                _buttonPlayVideo.Content = Strings.ButtonContentPlayVideo;
        }

        /// <summary>
        /// Updates <see cref="_screenshotViewer"/> based on selected item in <see cref="_listboxEntries"/>
        /// </summary>
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

            LoadScreenshotToScreenshotViewer(path);
        }

        /// <summary>
        /// Hides <see cref="_buttonTextEditMode"/> and <see cref="_buttonDesignerMode"/> buttons.
        /// </summary>
        private void HideButtonsModes()
        {
            _buttonTextEditMode.Visibility = Visibility.Hidden;
            _buttonDesignerMode.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Displays or hides <see cref="_gridDesignerMenu"/> and <see cref="_gridMedia"/>.
        /// </summary>
        /// <param name="display"></param>
        private void DisplayDesignerControls(bool display)
        {
            Visibility visibility = display ? Visibility.Visible : Visibility.Hidden;

            _gridDesignerMenu.Visibility = visibility;
            _gridMedia.Visibility = visibility;
        }

        /// <summary>
        /// Disables <see cref="_textboxSpeechLength" and <see cref="_buttonPlaySpeech"/> controls and empties <see cref="_textboxSpeechLength"/> text./>
        /// </summary>
        private void DisableControlsSpeech()
        {
            _textboxSpeechLength.IsEnabled = false;
            _textboxSpeechLength.Text = string.Empty;
            _buttonPlaySpeech.IsEnabled = false;
        }

        /// <summary>
        /// Stops displaying image on <see cref="_screenshotViewer"/>.
        /// </summary>
        private void CleanScreenshotViewer()
        {
            _screenshotViewer.Source = null;
        }

        /// <summary>
        /// Display one of <see cref="_screenshotViewer"/> or <see cref="_videoPlayer"/> depending on parameter - <paramref name="type"/>.
        /// </summary>
        /// <param name="type">What to display</param>
        private void ScreenshotViewerAndVideoPlayerVisibility(ScreenshotViewerAndVideoPlayerVisibilityType type)
        {
            if (type == ScreenshotViewerAndVideoPlayerVisibilityType.ShowScreenshotViewer)
            {
                _screenshotViewer.Visibility = Visibility.Visible;
                _videoPlayer.Visibility = Visibility.Hidden;

                StopVideo();
            }
            else
            {
                _screenshotViewer.Visibility = Visibility.Hidden;
                _videoPlayer.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Opens current discussion page in <see cref="_webView"/>.
        /// </summary>
        private void WebViewOpenCurrentDiscussion()
        {
            var uri = new Uri($"{DiscussionDownloader.DISCUSSION_NAME_TEMPLATE}{GetCurrentDiscussionId()}");
            try
            {
                _webView.Source = uri;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't open - {uri}, Exception - {ex.Message}");
            }
        }

        /// <summary>
        /// Opens "about:blank" in <see cref="_webView"/>.
        /// </summary>
        private void WebViewOpenEmptyPage()
        {
            _webView.Source = new Uri("about:blank");
        }
    }
}
