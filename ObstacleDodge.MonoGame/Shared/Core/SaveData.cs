using System;
using System.Globalization;
using System.IO;

namespace ObstacleDodge
{
    /// <summary>Progress that survives closing the game: a tiny "key=value" text file.</summary>
    public sealed class SaveData
    {
        public int Level { get; set; } = 1;       // the highest unlocked level
        public int BestLevel { get; set; }        // the highest finished level
        public bool SoundOn { get; set; } = true;

        string path;

        public static SaveData Load(string directory)
        {
            var data = new SaveData();
            if (string.IsNullOrEmpty(directory)) return data;
            data.path = Path.Combine(directory, "save.txt");
            try
            {
                if (!File.Exists(data.path)) return data;
                foreach (var line in File.ReadAllLines(data.path))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "level": data.Level = Math.Max(1, ParseInt(value, 1)); break;
                        case "best": data.BestLevel = Math.Max(0, ParseInt(value, 0)); break;
                        case "sound": data.SoundOn = value != "0"; break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Save] Could not read: " + e.Message);
            }
            return data;
        }

        public void Write()
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,
                    "level=" + Level.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "best=" + BestLevel.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "sound=" + (SoundOn ? "1" : "0") + "\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Save] Could not write: " + e.Message);
            }
        }

        static int ParseInt(string s, int fallback) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
    }
}
