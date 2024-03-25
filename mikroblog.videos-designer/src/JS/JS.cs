using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Microsoft.Web.WebView2.Wpf;
using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    internal class JS
    {
        /// <summary>
        /// Tries to get integer value from <paramref name="node"/>. If value is floating number it rounds it to the nearest integer value.
        /// </summary>
        /// <returns>True if success, otherwise false</returns>
        public static bool TryGetIntFromJsonNode(JsonNode node, out int value)
        {
            var result = int.TryParse(node.ToString(), out value);

            if (!result)
            {
                result = double.TryParse(node.ToString(), out double valueDouble);

                if (result)
                    value = (int)valueDouble;
            }

            return result;
        }

        /// <summary>
        /// Tries to get boolean value from <paramref name="node"/>.
        /// </summary>
        /// <returns>True if success, otherwise false</returns>
        public static bool TryGetBoolFromJsonNode(JsonNode node, out bool value)
        {
            return bool.TryParse(node.ToString(), out value);        
        }

        /// <summary>
        /// Executes JS script in <paramref name="webView"/> if found in program resources.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        public static async void ExecuteJSScript(WebView2 webView, string name)
        {
            string? script = Util.GetResource(name);
            if (script == null)
            {
                Log.WriteError($"Resource not found - {name}");
                return;
            }

            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Executes JS function in <paramref name="webView"/> with parameter.
        /// </summary>
        /// <param name="name">Name of the called function</param>
        /// <param name="arg">Parameter passed to the called function</param>
        public static async Task ExecuteJSFunction(WebView2 webView, string name, string arg)
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"{name}({arg});");
        }

        /// <summary>
        /// Executes JS function in <paramref name="webView"/>.
        /// </summary>
        /// <param name="name">Name of the function</param>
        public static async Task ExecuteJSFunction(WebView2 webView, string name)
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"{name}();");
        }
    }
}
