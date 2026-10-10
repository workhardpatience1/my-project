using System;
using System.IO;

namespace ObstacleDodge
{
    /// <summary>The computer version (Windows / Linux / macOS). Ads are a stub here, like in the Unity editor.</summary>
    sealed class DesktopPlatform : IPlatform
    {
        public IAdsProvider Ads { get; } = new StubAdsProvider();
        public bool IsMobile { get; set; }
        public int BannerHeightPx { get; set; }
        public int BannerWidthPx { get; set; }
        public string SaveDirectory { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ObstacleDodge");
        public void Vibrate(int milliseconds) { }
        public void Quit() { }
        public void ReportError(string text) => Console.Error.WriteLine("[Error] " + text);

        public void OpenUrl(string url)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception e) { Console.WriteLine("[Url] " + e.Message); }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var platform = new DesktopPlatform();
            string shots = null;
            int width = 1280, height = 720;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--shots": shots = args[++i]; break;
                    case "--size":
                        var p = args[++i].Split('x');
                        width = int.Parse(p[0]);
                        height = int.Parse(p[1]);
                        break;
                    case "--mobile":
                        // pretend to be a phone: arrow buttons + a 320x50 dp banner at density 2.75
                        platform.IsMobile = true;
                        platform.BannerWidthPx = 880;
                        platform.BannerHeightPx = 138;
                        break;
                }
            }

            using var game = new ObstacleDodgeGame(platform);
            if (shots != null)
            {
                platform.SaveDirectory = Path.Combine(shots, "save");
                if (Directory.Exists(platform.SaveDirectory)) Directory.Delete(platform.SaveDirectory, true);
                ScreenshotTour.Attach(game, shots, width, height);
            }
            game.Run();
        }
    }
}
