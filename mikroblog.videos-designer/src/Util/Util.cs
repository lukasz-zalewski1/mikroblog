using mikroblog.fast_quality_check;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace mikroblog.videos_designer
{
    class Util
    {
        public static string? GetResource(string name)
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream == null)
            {
                Log.WriteError($"Resource not found - {name}");
                return null;
            }

            using StreamReader streamReader = new(stream);
            return streamReader.ReadToEnd();
        }
    }
}
