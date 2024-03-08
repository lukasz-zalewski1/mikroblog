using System.Text;

namespace mikroblog.fast_quality_check
{
    public class Config
    {
        public static readonly string CONFIGS_PATH = Path.Combine(Util.WORKPLACE_PATH, "configs");

        private readonly string _path;

        private const char KEY_VALUE_SEPARATOR = '=';

        private Dictionary<string, string>? _config = null;
        public Dictionary<string, string>? Lines { get => _config; }

        /// <summary>
        /// Initializes config's path and tries to read it. It doesn't have to exist yet.
        /// </summary>
        /// <param name="name">Name of the config file</param>
        public Config(string name)
        {
            _path = Path.Combine(CONFIGS_PATH, name);
            _path = Path.ChangeExtension(_path, ".txt");

            Read();
        }
    
        /// <summary>
        /// Reads config values from a config file.
        /// </summary>
        private void Read() 
        {
            Log.Write($"Trying to read config - {_path}");

            var configData = TryReadFile(_path);
            if (configData == null)
            {
                Log.WriteError("Config not found or empty");
                return;
            }

            _config = ConvertConfigLinesToDictionary(configData);
        }

        /// <summary>
        /// Tries to read a config file.
        /// </summary>
        /// <param name="path">Path to the config file</param>
        /// <returns>Array with lines in the config file or null</returns>
        private string[]? TryReadFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var lines = File.ReadAllLines(path);
                lines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (!lines.Any())
                    return null;

                return lines;
            }
            catch (Exception ex)
            {
                Log.WriteError("ConfigReader can't read config file - " + path + ", Exception - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Tries to convert lines from the config file to a dictionary.
        /// </summary>
        /// <param name="configData">Lines from the config file</param>
        /// <returns>Dictionary with config data or null</returns>
        private Dictionary<string, string>? ConvertConfigLinesToDictionary(string[] configData)
        {
            Dictionary<string, string> config = new();

            foreach (var line in configData)
            {
                if (!TryReadOneConfigLine(line, ref config))
                    return null;
            }

            return config;
        }

        /// <summary>
        /// Tries to read a single line from the config file and saves the data into config dictionary.
        /// </summary>
        /// <param name="line">Line from the config file</param>
        /// <param name="config">Config dictionary</param>
        /// <returns>True if success</returns>
        private bool TryReadOneConfigLine(string line, ref Dictionary<string, string> config)
        {
            int splitIndex = line.IndexOf(KEY_VALUE_SEPARATOR);

            if (splitIndex < 0)
            {
                Log.WriteError($"ConfigReader can't create config dictionary - incorrect data at line - {line}");
                return false;
            }

            try
            {
                string key = line[..splitIndex];
                string value = line[(splitIndex + 1)..];

                config[key] = value;
            }
            catch (Exception ex)
            {
                Log.WriteError($"ConfigReader can't create config dictionary - incorrect data at line - {line}, Exception - {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a string value from the config.
        /// </summary>
        /// <returns>value or null in case of error</returns>
        public string? GetString(string key)
        {
            if (_config == null)
            {
                Log.WriteError($"GetString() can't read an empty config, key - {key}");
                return null;
            }

            if (!_config.TryGetValue(key, out var value))
            {
                Log.WriteError($"GetString() can't find key in the config - {key}");
                return null;
            }

            return value;
        }

        /// <summary>
        /// Gets an integer value from the config.
        /// </summary>
        /// <returns>value or null in case of errors</returns>
        public int? GetInt(string key)
        {
            if (_config == null)
            {
                Log.WriteError($"GetInt() can't read an empty config, key - {key}");
                return null;
            }

            if (!_config.TryGetValue(key, out var valueString))
            {
                Log.WriteError($"GetInt() can't find key in the config, key - {key}");
                return null;
            }

            if (!int.TryParse(valueString, out var value))
            {
                Log.WriteError($"GetInt() can't return non-integer value, key - {key}");
                return null;
            }

            return value;
        }

        /// <summary>
        /// Saves config to a file.
        /// </summary>
        private void Save()
        {
            if (_config == null)
            {
                Log.WriteError("Can't save an empty config");
                return;
            }

            StringBuilder stringBuilder = new();
            foreach (var line in _config)
            {
                stringBuilder.AppendLine($"{line.Key}{KEY_VALUE_SEPARATOR}{line.Value}");
            }

            try
            {
                File.WriteAllText(_path, stringBuilder.ToString());
            }
            catch (Exception ex) 
            {
                Log.WriteError($"Config can't be saved, Exception - {ex.Message}");
            }
        }

        /// <summary>
        /// Adds new value or changes existing value in the config and saves it.
        /// </summary>
        public void Add(string key, string value)
        {
            _config ??= new Dictionary<string, string>();

            _config[key] = value;

            Save();
        }

        /// <summary>
        /// Removes key from the config and saves it.
        /// </summary>
        /// <param name="key">Key to remove</param>
        public void Remove(string key)
        {
            if (_config == null)
            {
                Log.WriteError($"Can't remove a key from an empty config - {_path}");
                return;
            }

            if (!_config.ContainsKey(key))
            {
                Log.WriteError($"{key} not found in config - {_path}");
                return;
            }

            _config.Remove(key);
            Save();
        }
    }
}
