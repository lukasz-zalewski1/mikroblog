using mikroblog.fast_quality_check;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private void UpdateControls()
        {
            UpdateLabelDiscussionNumber();
            UpdateLabelDiscussionId();       
            UpdateLabelDiscussionQuality();

            UpdateGridRemoveDiscussionFiles();

            UpdateSpeechButton();

            UpdateScreenshotViewer();
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
            }));
        }

        /// <summary>
        /// Updates <see cref="_buttonPlaySpeech"/> content depending of <see cref="_isSpeechPlayed"/> value.
        /// </summary>
        private void UpdateSpeechButton()
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
    }
}
