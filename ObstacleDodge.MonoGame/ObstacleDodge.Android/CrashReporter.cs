using System;
using System.IO;
using Android.App;
using Android.Runtime;
using Android.Content;
using Android.Widget;

namespace ObstacleDodge
{
    /// <summary>
    /// Catches every error (C# and Java), saves its text, and shows it in a window with a
    /// "copy" button — so instead of "the app stopped" the player can send the exact reason.
    /// </summary>
    static class CrashReporter
    {
        static string path;
        static bool showing;

        public static void Install(Activity activity)
        {
            path = Path.Combine(activity.FilesDir.AbsolutePath, "last_error.txt");
            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) => Save(e.Exception?.ToString());
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Save(e.ExceptionObject?.ToString());
            var previous = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = new JavaHandler(previous);
        }

        public static void Save(string text)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(text)) return;
            try
            {
                string info = "Obstacle Dodge " + typeof(CrashReporter).Assembly.GetName().Version +
                              ", Android " + Android.OS.Build.VERSION.Release + " (API " + (int)Android.OS.Build.VERSION.SdkInt + "), " +
                              Android.OS.Build.Manufacturer + " " + Android.OS.Build.Model + "\n";
                File.WriteAllText(path, info + DateTime.Now.ToString("u") + "\n" + text);
            }
            catch { }
        }

        /// <summary>The error saved by the previous run (and forgets it), or null.</summary>
        public static string TakeSaved()
        {
            try
            {
                if (path == null || !File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                File.Delete(path);
                return text;
            }
            catch { return null; }
        }

        public static void Show(Activity activity, string title, string text, Action onClose = null)
        {
            activity.RunOnUiThread(() =>
            {
                if (showing) return;
                showing = true;
                try
                {
                    var view = new TextView(activity) { Text = text, TextSize = 12 };
                    view.SetTextIsSelectable(true);
                    view.SetPadding(40, 20, 40, 20);
                    var scroll = new ScrollView(activity);
                    scroll.AddView(view);
                    new AlertDialog.Builder(activity)
                        .SetTitle(title)
                        .SetView(scroll)
                        .SetCancelable(false)
                        .SetPositiveButton("Nusxa olish", (s, e) =>
                        {
                            try
                            {
                                var clipboard = (ClipboardManager)activity.GetSystemService(Context.ClipboardService);
                                clipboard.PrimaryClip = ClipData.NewPlainText("Obstacle Dodge xato", text);
                                Toast.MakeText(activity, "Nusxa olindi", ToastLength.Short).Show();
                            }
                            catch { }
                            showing = false;
                            onClose?.Invoke();
                        })
                        .SetNegativeButton("Yopish", (s, e) => { showing = false; onClose?.Invoke(); })
                        .Show();
                }
                catch
                {
                    showing = false;
                    onClose?.Invoke();
                }
            });
        }

        sealed class JavaHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
        {
            readonly Java.Lang.Thread.IUncaughtExceptionHandler previous;
            public JavaHandler(Java.Lang.Thread.IUncaughtExceptionHandler previous) { this.previous = previous; }

            public void UncaughtException(Java.Lang.Thread thread, Java.Lang.Throwable error)
            {
                Save(Android.Util.Log.GetStackTraceString(error));
                previous?.UncaughtException(thread, error);
            }
        }
    }
}
