using System;
using System.IO;
using System.Linq;
using System.Windows;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private readonly string DISCUSSIONS_PATH = Path.Combine(fast_quality_check.Util.GetWorkplacePath(), "discussions");

        private readonly Config _configQualityDiscussions = new(Manager.QUALITY_DISCUSSIONS_FILE_NAME);

        private int _currentDiscussion;

        private int DiscussionsCount { get => _configQualityDiscussions.Lines != null ? _configQualityDiscussions.Lines.Count : 0; }

        /// <summary>
        /// Returns current discussion Id or empty string if discussion was not found.
        /// </summary>
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

        /// <summary>
        /// Returns current discussion rating or empty string if discussion was not found.
        /// </summary>
        private string GetCurrentDiscussionRating()
        {
            var rating = _configQualityDiscussions.GetString(GetCurrentDiscussionId());
            return string.IsNullOrEmpty(rating) ? string.Empty : rating;
        }

        /// <summary>
        /// Gets and creates if necessary folder for current discussion entries files.
        /// </summary>
        /// <returns>Path to the folder</returns>
        private string GetCurrentDiscussionFolder()
        {
            string discussionPath = Path.Combine(DISCUSSIONS_PATH, GetCurrentDiscussionId());

            if (!Directory.Exists(discussionPath))
                Directory.CreateDirectory(discussionPath);

            return discussionPath;
        }

        /// <summary>
        /// Changes <see cref="_currentDiscussion""/> to the previous one and calls <see cref="ChangeDiscussion"/>.
        /// </summary>
        private void PreviousDiscussion()
        {
            if (_currentDiscussion > 0)
                _currentDiscussion -= 1;

            ChangeDiscussion();
        }

        /// <summary>
        /// Changes <see cref="_currentDiscussion"/> to the next one and calls <see cref="ChangeDiscussion"/>. 
        /// </summary>
        private void NextDiscussion()
        {
            if (_currentDiscussion + 1 < DiscussionsCount)
                _currentDiscussion += 1;

            ChangeDiscussion();
        }

        /// <summary>
        /// Stops ongoing speech and video, restores default window view and opens current discussion in <see cref="_webView"/>.
        /// </summary>
        private void ChangeDiscussion()
        {
            DiscussionChangeProcess();

            WebViewOpenCurrentDiscussion();           
        }

        /// <summary>
        /// Drops a discussion from <see cref="_configQualityDiscussions"/> list and opens discussion which took index in the list of the recently dropped discussion.
        /// </summary>
        private void DropDiscussion()
        {
            var currentDiscussionId = GetCurrentDiscussionId();

            if (!string.IsNullOrEmpty(currentDiscussionId))
            {
                _configQualityDiscussions.Remove(currentDiscussionId);

                if (_currentDiscussion >= DiscussionsCount)
                    _currentDiscussion -= 1;
            }

            DiscussionChangeProcess();

            if (string.IsNullOrEmpty(GetCurrentDiscussionId()))
                NoMoreDiscussions();
            else
                WebViewOpenCurrentDiscussion();

            
        }

        /// <summary>
        /// Stops ongoing speech and video, Hides <see cref="_buttonTextEditMode"/> and <see cref="_buttonDesignerMode"/> and Designer controls.
        /// Opens empty page in <see cref="_webView"/>.
        /// </summary>
        private void NoMoreDiscussions()
        {
            HideButtonsModes();
            DisplayDesignerControls(false);        

            WebViewOpenEmptyPage();
        }

        /// <summary>
        /// Stops played media, cleans any changes done by modes, sets video speed to default value and updates controls.
        /// </summary>
        private void DiscussionChangeProcess()
        {
            StopSpeech();
            StopVideo();

            CleanModesChanges();
            SetVideoSpeedToDefaultValue();

            UpdateControls(ControlUpdateType.All);
        }

        /// <summary>
        /// Removes every file in the current discussion's folder.
        /// </summary>
        private void RemoveDiscussionFiles()
        {
            var path = GetCurrentDiscussionFolder();

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
    }
}
