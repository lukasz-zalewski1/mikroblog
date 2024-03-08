using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace mikroblog.videos_designer
{
    internal class JS
    {
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

        public static async void ExecuteJSScript(WebView2 webView, string name)
        {
            string? script = Util.GetResource(name);
            if (script == null)
                return;

            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        public static async Task ExecuteJSFunction(WebView2 webView, string name, List<string> args)
        {
            string argString = string.Empty;
            for (int i = 0; i < args.Count; ++i)
            {
                argString += args[i];
                if (i + 1 != args.Count)
                    argString += ", ";
            }

            await webView.CoreWebView2.ExecuteScriptAsync($"{name}({argString});");
        }

        public static async Task ExecuteJSFunction(WebView2 webView, string name, string arg)
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"{name}({arg});");
        }

        public static async Task ExecuteJSFunction(WebView2 webView, string name)
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"{name}();");
        }
    }
}
