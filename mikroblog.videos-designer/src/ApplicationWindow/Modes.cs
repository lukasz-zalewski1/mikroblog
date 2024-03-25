using System.Windows;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        private enum Mode
        {
            None,
            TextEdit,
            Designer
        }

        private Mode _mode;

        private const string RESOURCE_NAME_JS_EDIT_MODE = "mikroblog.videos_designer.src.JS.EditMode.js";
        private const string RESOURCE_NAME_JS_DESIGNER_MODE = "mikroblog.videos_designer.src.JS.DesignerMode.js";

        /// <summary>
        /// Enables Text Edit Mode, executes "EditMode.js" script and calls "enableEditMode" JS function.
        /// </summary>
        private async void EnableTextEditMode()
        {
            JS.ExecuteJSScript(_webView, RESOURCE_NAME_JS_EDIT_MODE);

            if (_mode == Mode.Designer)
                DisableDesignerMode();

            _mode = Mode.TextEdit;
            UpdateControls(ControlUpdateType.ModeChange);

            DisplayDesignerControls(false);

            await JS.ExecuteJSFunction(_webView, "enableEditMode");
        }

        /// <summary>
        /// Disables Text Edit Mode and calls "disableEditMode" JS function.
        /// </summary>
        private async void DisableTextEditMode()
        {
            _mode = Mode.None;
            UpdateControls(ControlUpdateType.ModeChange); 

            await JS.ExecuteJSFunction(_webView, "disableEditMode");
        }

        /// <summary>
        /// Enables Designer Mode, executes "DesignerMode.js" script and calls "enableDesignerMode" JS function.
        /// </summary>
        private async void EnableDesignerMode()
        {
            JS.ExecuteJSScript(_webView, RESOURCE_NAME_JS_DESIGNER_MODE);

            if (_mode == Mode.TextEdit)
                DisableTextEditMode();

            _mode = Mode.Designer;
            UpdateControls(ControlUpdateType.ModeChange);

            DisplayDesignerControls(true);

            await JS.ExecuteJSFunction(_webView, "enableDesignerMode");
        }

        /// <summary>
        /// Disables DesignerMode and calls "disableDesignerMode" JS function.
        /// </summary>
        private async void DisableDesignerMode()
        {
            _mode = Mode.None;
            UpdateControls(ControlUpdateType.ModeChange);

            DisplayDesignerControls(false);

            await JS.ExecuteJSFunction(_webView, "disableDesignerMode");
        }

        /// <summary>
        /// Clears items of <see cref="_listboxEntries"/> and calls "cleanEntries" JS function.
        /// </summary>
        private async void CleanDesignerEntries()
        {
            _listboxEntries.Items.Clear();

            await JS.ExecuteJSFunction(_webView, "cleanEntries");            
        }

        /// <summary>
        /// Calls <see cref="DisableTextEditMode"/>, <see cref="DisableDesignerMode"/>, <see cref="CleanDesignerEntries"/> and <see cref="CleanScreenshotViewer"/> methods.
        /// </summary>
        private void CleanModesChanges()
        {
            DisableTextEditMode();
            DisableDesignerMode();
            CleanDesignerEntries();
            CleanScreenshotViewer();
        }
    }
}
