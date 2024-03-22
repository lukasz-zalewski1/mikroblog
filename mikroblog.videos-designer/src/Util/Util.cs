using System;
using System.IO;
using System.Reflection;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public class Util
    {
        /// <summary>
        /// Returns resource as a string.
        /// </summary>
        /// <returns>Resource string or null if not found</returns>
        public static string? GetResource(string name)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream == null)                
                    return null;

                using StreamReader streamReader = new(stream);
                return streamReader.ReadToEnd();
            }
            catch (Exception ex) 
            {
                Log.WriteError($"GetManifestResourceStream, Exception - {ex.Message}");
                return null;
            }
        }
    }
}
