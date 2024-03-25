using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Web.WebView2.Core;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        /// <summary>
        /// Initializes non-control events.
        /// </summary>
        private void InitializeEvents()
        {
            _speechTimer.Elapsed += SpeechTimer_Elapsed;
        }      

        /// <summary>
        /// Closes the application when escape was clicked.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        /// <summary>
        /// Calls <see cref="PreviousDiscussion"/> method.
        /// </summary>
        private void ButtonPreviousDiscussion_Click(object sender, RoutedEventArgs e)
        {
            PreviousDiscussion();
        }

        /// <summary>
        /// Calls <see cref="NextDiscussion"/> method.
        /// </summary>
        private void ButtonNextDiscussion_Click(object sender, RoutedEventArgs e)
        {
            NextDiscussion();
        }

        /// <summary>
        /// Calls <see cref="DropDiscussion"/> method.
        /// </summary>
        private void ButtonDropDiscussion_Click(object sender, RoutedEventArgs e)
        {
            DropDiscussion();
        }

        /// <summary>
        /// Calls <see cref="RemoveDiscussionFiles"/> method.
        /// </summary>
        private void ButtonRemoveDiscussionFiles_Click(object sender, RoutedEventArgs e)
        {
            RemoveDiscussionFiles();
        }

        /// <summary>
        /// Calls <see cref="EnableTextEditMode"/> or <see cref="DisableTextEditMode"/> method depending on <see cref="_mode"/> value.
        /// </summary>
        private void ButtonTextEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (_mode != Mode.TextEdit)
                EnableTextEditMode();
            else
                DisableTextEditMode();
        }

        /// <summary>
        /// Calls <see cref="EnableDesignerMode"/> or <see cref="DisableDesignerMode"/> method depending on <see cref="_mode"/> value.
        /// </summary>
        private void ButtonDesignerMode_Click(object sender, RoutedEventArgs e)
        {
            if (_mode != Mode.Designer)
                EnableDesignerMode();
            else
                DisableDesignerMode();
        }

        /// <summary>
        /// Calls <see cref="ScreenshotViewerAndVideoPlayerVisibility(ScreenshotViewerAndVideoPlayerVisibilityType.ShowScreenshotViewer)"/> and <see cref="UpdateControls(ControlUpdateType.Designer)"/>.
        /// </summary>
        private void ListboxEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScreenshotViewerAndVideoPlayerVisibility(ScreenshotViewerAndVideoPlayerVisibilityType.ShowScreenshotViewer);

            UpdateControls(ControlUpdateType.Designer);
        }

        /// <summary>
        /// Calls <see cref="Screenshot"/> method.
        /// </summary>
        private async void ButtonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            await Screenshot();
        }

        /// <summary>
        /// Calls <see cref="ScreenshotAll"/> method.
        /// </summary>
        private async void ButtonScreenshotAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotAll();
        }

        /// <summary>
        /// Calls <see cref="Speak"/> method.
        /// </summary>
        private async void ButtonSpeak_Click(object sender, RoutedEventArgs e)
        {
            await Speak();
        }

        /// <summary>
        /// Calls <see cref="SpeakAll"/> method.
        /// </summary>
        private async void ButtonSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await SpeakAll();
        }

        /// <summary>
        /// Calls <see cref="ScreenshotSpeak"/> method.
        /// </summary>
        private async void ButtonScreenshotSpeak_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeak();
        }

        /// <summary>
        /// Calls <see cref="ScreenshotSpeakAll"/> method.
        /// </summary>
        private async void ButtonScreenshotSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeakAll();
        }

        /// <summary>
        /// Calls <see cref="UpdateSpeechLengthFile"/> method.
        /// </summary>
        private void TextboxSpeechLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSpeechLengthFile();
        }

        /// <summary>
        /// Constrains textbox input to floating point numbers only.
        /// </summary>
        private void TextboxSpeechLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        /// <summary>
        /// Calls method <see cref="StopSpeech"/> or <see cref="PlaySpeech"/> depending on <see cref="_isSpeechPlayed"/> value.
        /// </summary>
        private void ButtonPlaySpeech_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpeechPlayed)
                StopSpeech();
            else
                PlaySpeech();
        }

        /// <summary>
        /// Calls <see cref="StopSpeech"/> method.
        /// </summary>
        private void SpeechTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            StopSpeech();
        }

        /// <summary>
        /// Calls <see cref="CreateVideo"/> method.
        /// </summary>
        private void ButtonCreateVideo_Click(object sender, RoutedEventArgs e)
        {
            CreateVideo();
        }

        /// <summary>
        /// Calls method <see cref="StopVideo"/> or <see cref="PlayVideo"/> depending on <see cref="_isVideoPlayed"/> value
        /// </summary>
        private void ButtonPlayVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_isVideoPlayed)
                StopVideo();
            else
                PlayVideo();
        }

        /// <summary>
        /// Calls <see cref="StopVideo"/> method.
        /// </summary>
        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        /// <summary>
        /// Calls <see cref="ParseJsonMessage"/> with received message from <see cref="_webView"/> as parameter.
        /// </summary>
        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            ParseJsonMessage(e.WebMessageAsJson);
        }
    }
}
