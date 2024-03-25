using System.Windows;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        /// <summary>
        /// Calls the following methods: <see cref="InitializeComponent"/>, <see cref="InitializeControls"/>,
        /// <see cref="InitializeEvents"/> and <see cref="WebViewOpenCurrentDiscussion"/>
        /// </summary>
        public VideosDesignerWindow()
        {
            InitializeComponent();

            InitializeControls();

            InitializeEvents();

            WebViewOpenCurrentDiscussion();
        } 
    }
}