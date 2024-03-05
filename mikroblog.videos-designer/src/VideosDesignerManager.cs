using mikroblog.fast_quality_check;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace mikroblog.videos_designer
{
    internal class VideosDesignerManager
    {
        public class Entry
        {
            public int X; 
            public int Y; 
            public int Width; 
            public int Height;

            public string? Text;
        }

        private readonly Config _configQualityDiscussions = new(Manager.QUALITY_DISCUSSIONS_FILE_NAME);

        public int CurrentDiscussion { get; private set; }
        public int DiscussionsCount { get => _configQualityDiscussions.Lines != null ? _configQualityDiscussions.Lines.Count : 0; }


        public List<Entry> Entries { get; set; } = new List<Entry>();

        
        public string? GetCurrentDiscussionRating()
        {
            var discussionId = GetCurrentDiscussionId();

            if (discussionId == null)
                return null;

            return _configQualityDiscussions.GetString(discussionId);
        }

        public string? GetCurrentDiscussionId()
        {
            if (_configQualityDiscussions.Lines == null)
            {
                Log.WriteError("QualityDiscussions Config is null");
                return null;
            }

            if (_configQualityDiscussions.Lines.Count == 0)
                return null;

            return _configQualityDiscussions.Lines.ElementAt(CurrentDiscussion).Key;
        }

        public void PreviousDiscussion()
        {
            if (CurrentDiscussion <= 0)
                return;

            CurrentDiscussion -= 1;
        }

        public void NextDiscussion()
        {
            if (CurrentDiscussion + 1 >= DiscussionsCount)
                return;

            CurrentDiscussion += 1;
        }

        public void DropDiscussion()
        {
            var currentDiscussionId = GetCurrentDiscussionId();
            if (currentDiscussionId == null)
                return;

            _configQualityDiscussions.Remove(currentDiscussionId);

            if (CurrentDiscussion >= DiscussionsCount)
                CurrentDiscussion -= 1;
        }
    }
}
