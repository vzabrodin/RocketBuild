using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace RocketBuild.Settings
{
    public class GlobalSettings
    {
        private const string FileName = "Global.json";

        private static readonly string DirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        private static string FullFilePath => Path.Combine(DirectoryPath, FileName);

        private static readonly Lazy<GlobalSettings> SettingsLazy = new Lazy<GlobalSettings>(Initialize);

        public static GlobalSettings Current => SettingsLazy.Value;

        public string AccountUrl { get; set; }

        public bool UseSsl { get; set; } = true;

        public string Project { get; set; }

        public string ApiKey { get; set; }

        public string ReleaseNameExtractVersionRegex { get; set; }

        public string BuildNameExtractVersionRegex { get; set; }

        public IList<int> LastSelectedBuildIds { get; } = new List<int>();

        public IDictionary<string, IEnumerable<int>> LastSelectedReleaseIds { get; } =
            new Dictionary<string, IEnumerable<int>>();

        public string LastSelectedEnvironment { get; set; }

        public static void Save()
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(FullFilePath, JsonConvert.SerializeObject(Current));
        }

        private static GlobalSettings Initialize()
        {
            try
            {
                return JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText(FullFilePath));
            }
            catch (Exception)
            {
                return new GlobalSettings();
            }
        }
    }
}
