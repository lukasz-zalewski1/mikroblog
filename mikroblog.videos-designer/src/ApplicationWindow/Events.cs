using Microsoft.Web.WebView2.Core;
using mikroblog.fast_quality_check;
using System.Text.Json.Nodes;
using System.Text.Json;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace mikroblog.videos_designer
{
    /// <summary>
    /// Part of VideosDesignerClass responsible for events.
    /// </summary>
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
        /// Calls PreviousDiscussion method.
        /// </summary>
        private void ButtonPreviousDiscussion_Click(object sender, RoutedEventArgs e)
        {
            PreviousDiscussion();
        }

        /// <summary>
        /// Calls NextDiscussion method.
        /// </summary>
        private void ButtonNextDiscussion_Click(object sender, RoutedEventArgs e)
        {
            NextDiscussion();
        }

        /// <summary>
        /// Calls DropDiscussion method.
        /// </summary>
        private void ButtonDropDiscussion_Click(object sender, RoutedEventArgs e)
        {
            DropDiscussion();
        }

        /// <summary>
        /// Calls RemoveDiscussionFiles method.
        /// </summary>
        private void ButtonRemoveDiscussionFiles_Click(object sender, RoutedEventArgs e)
        {
            RemoveDiscussionFiles();
        }

        /// <summary>
        /// Calls TextEditMode method.
        /// </summary>
        private void ButtonTextEditMode_Click(object sender, RoutedEventArgs e)
        {
            TextEditMode();
        }

        /// <summary>
        /// Calls DesignerMode method.
        /// </summary>
        private void ButtonDesignerMode_Click(object sender, RoutedEventArgs e)
        {
            DesignerMode();
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void ListboxEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScreenshotViewerVideoPlayerVisibility(true);

            UpdateControls();

            UpdatePlaySpeechControls();
            UpdateSpeechLengthTextbox();
        }

        /// <summary>
        /// Calls Screenshot method.
        /// </summary>
        private async void ButtonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            await Screenshot();
        }

        /// <summary>
        /// Calls ScreenshotAll method.
        /// </summary>
        private async void ButtonScreenshotAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotAll();
        }

        /// <summary>
        /// Calls Speak method.
        /// </summary>
        private async void ButtonSpeak_Click(object sender, RoutedEventArgs e)
        {
            await Speak();
        }

        /// <summary>
        /// Calls SpeakAll method.
        /// </summary>
        private async void ButtonSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await SpeakAll();
        }

        /// <summary>
        /// Calls ScreenshotSpeak method.
        /// </summary>
        private async void ButtonScreenshotSpeak_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeak();
        }

        /// <summary>
        /// Calls ScreenshotSpeakAll method.
        /// </summary>
        private async void ButtonScreenshotSpeakAll_Click(object sender, RoutedEventArgs e)
        {
            await ScreenshotSpeakAll();
        }

        /// <summary>
        /// Calls UpdateSpeechLengthFile method.
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
        /// Calls method StopSpeech or PlaySpeech depending on _isSpeechPlayed value.
        /// </summary>
        private void ButtonPlaySpeech_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpeechPlayed)
                StopSpeech();
            else
                PlaySpeech();
        }

        /// <summary>
        /// Calls StopSpeech method.
        /// </summary>
        private void SpeechTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            StopSpeech();
        }

        /// <summary>
        /// Calls CreateVideo method.
        /// </summary>
        private void ButtonCreateVideo_Click(object sender, RoutedEventArgs e)
        {
            CreateVideo();
        }

        /// <summary>
        /// Calls method StopVideo or PlayVideo depending on _isVideoPlayed
        /// </summary>
        private void ButtonPlayVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_isVideoPlayed)
                StopVideo();
            else
                PlayVideo();
        }

        /// <summary>
        /// Calls StopVideo method.
        /// </summary>
        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
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
    }
}
