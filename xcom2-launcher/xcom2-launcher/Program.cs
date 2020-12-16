using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JR.Utils.GUI.Forms;
using log4net;
using Sentry;
using Sentry.Protocol;
using XCOM2Launcher.Classes;
using XCOM2Launcher.Classes.Steam;
using XCOM2Launcher.Forms;
using XCOM2Launcher.Helper;
using XCOM2Launcher.Mod;
using XCOM2Launcher.Steam;
using XCOM2Launcher.XCOM;

namespace XCOM2Launcher
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(Program));
        public static readonly bool IsDebugBuild;
        public static XcomEnvironment XEnv;

        static Program()
        {
#if DEBUG
            IsDebugBuild = false;
#else
            IsDebugBuild = false;
#endif

            // Log.Info($"Application started (AML {GitVersionInfo.FullSemVer} {GitVersionInfo.Sha})");
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            if (!IsDebugBuild)
            {
                // Capture all unhandled Exceptions
                AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleUnhandledException(args.ExceptionObject as Exception, "UnhandledException");
                Application.ThreadException += (sender, args) => HandleUnhandledException(args.Exception, "ThreadException");
            }

            // Mutex is used to check if another instance of AML is already running
            Mutex mutex = new Mutex(true, "E3241D27-3DD8-4615-888A-502252B9E2A9", out var isFirstInstance);
            IDisposable sentrySdkInstance = null;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                InitAppSettings();
                sentrySdkInstance = InitSentry();

                if (!CheckDotNet4_7_2())
                {
                    Log.Warn("需要安装.NET Framework v4.7.2(百度)");

                    var result = MessageBox.Show("此程序需要Microsoft .NET Framework v4.7.2或更高版本。您要立即打开下载页面吗?", "错误", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                    if (result == DialogResult.Yes)
                        Tools.StartProcess("https://dotnet.microsoft.com/download/dotnet-framework");

                    return;
                }

                if (!SteamAPIWrapper.Init())
                {
                    Log.Warn("Failed to detect Steam");

                    StringBuilder message = new StringBuilder();
                    message.AppendLine("请确保：");
                    message.AppendLine("- Steam已经启动");
                    message.AppendLine("- AML文件夹中存在文件steam_appid.txt");
                    message.AppendLine("- Steam和AML均未使用管理员模式运行（或同时运行）\n ");
                    MessageBox.Show(message.ToString(), "错误-无法检测Steam!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Load settings
                var settings = InitializeSettings();
                if (settings == null)
                {
                    Log.Error("Failed to initialize settings");
                    return;
                }

                // Exit if another instance of AML is already running and multiple instances are disabled.
                if (!settings.AllowMultipleInstances && !isFirstInstance)
                {
                    MessageBox.Show("AML的另一个实例已经在运行.", "AML已经启动", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // clean up old files
                if (File.Exists(XEnv.DefaultConfigDir + @"\DefaultModOptions.ini.bak"))
                {
                    // Restore backup
                    File.Copy(XEnv.DefaultConfigDir + @"\DefaultModOptions.ini.bak", XEnv.DefaultConfigDir + @"\DefaultModOptions.ini", true);
                    File.Delete(XEnv.DefaultConfigDir + @"\DefaultModOptions.ini.bak");
                }

                Application.Run(new MainForm(settings));
                SteamAPIWrapper.Shutdown();
            }
            finally
            {
                Log.Info("Shutting down...");
                sentrySdkInstance?.Dispose();
                GlobalSettings.Instance.Save();
                GC.KeepAlive(mutex); // prevent the mutex from being garbage collected early
                mutex.Dispose();
            }
        }

        private static void HandleUnhandledException(Exception e, string source)
        {
            Log.Fatal("Unhandled exception", e);
            // File.WriteAllText("error.log", $"Version: {GetCurrentVersionString(true)}\n" + $"Sentry GUID: {GlobalSettings.Instance.Guid}\n" + $"Source: {source}\n" + $"Message: {e.Message}\n\n" + $"Stack:\n{e.StackTrace}");

            var dlg = new UnhandledExceptionDialog(e);
            dlg.ShowDialog();
            Application.Exit();
        }

        /// <summary>
        /// Initializes the Sentry environment.
        /// Sentry is an open-source application monitoring platform that help to identify issues.
        /// </summary>
        /// <returns></returns>
        private static IDisposable InitSentry()
        {
            if (!GlobalSettings.Instance.IsSentryEnabled || IsDebugBuild)
            {
                Log.Info("Sentry is disabled");
                return null;
            }

            Log.Info("Initializing Sentry");

            IDisposable sentrySdkInstance = null;

            try
            {
                string environment = "Release";

#if DEBUG
                // environment = "Debug";
#endif

                sentrySdkInstance = SentrySdk.Init(o =>
                {
                    o.Dsn = new Dsn("https://3864ad83bed947a2bc16d88602ac0d87@sentry.io/1478084");
                    // o.Release = "AML@" + GitVersionInfo.SemVer; // prefix because releases are global per organization
                    o.Debug = false;
                    o.Environment = environment;
                    o.MaxBreadcrumbs = 50;
                    o.BeforeSend = sentryEvent =>
                    {
                        sentryEvent.User.Email = null;
                        return sentryEvent;
                    };
                });

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new User
                    {
                        Id = GlobalSettings.Instance.Guid,
                        Username = GlobalSettings.Instance.UserName,
                        IpAddress = null
                    };
                });

                Log.Info($"Sentry initialized ({GlobalSettings.Instance.Guid})");
            }
            catch (Exception ex)
            {
                // If Sentry wasn't initialized correctly we at least try to send one message to report this.
                // (this won't throw another Ex, even if Init() failed)
                Log.Error("Sentry setup failed", ex);
                SentrySdk.Close();
                Debug.WriteLine(ex.Message);
            }

            return sentrySdkInstance;
        }

        /// <summary>
        /// Initializes GlobalSettings class.
        /// Used for all settings that we want to persist, even if the user decides to delete the
        /// json settings file or starts AML from different folders.
        /// </summary>
        private static void InitAppSettings()
        {
            var appSettings = GlobalSettings.Instance;
            // var currentVersion = new Version(GitVersionInfo.MajorMinorPatch);

            // if (appSettings.MaxVersion < currentVersion)
            // {
            //     appSettings.MaxVersion = currentVersion;
            // }

            // AML will either be used for XCOM2 or Chimera Squad
            // Create Steam application id file if it does not exist (depending on game choice of the user)
            if (!File.Exists(Workshop.APPID_FILENAME))
            {
                // Show Welcome Dialog and ask user to opt-in for Sentry error reporting.
                WelcomeDialog dlg = new WelcomeDialog();
                dlg.ShowDialog();
                appSettings.IsSentryEnabled = dlg.UseSentry;

                try
                {
                    using (var file = File.CreateText(Workshop.APPID_FILENAME))
                    {
                        file.WriteLine((uint)dlg.Game);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法建立 {Workshop.APPID_FILENAME}. {Environment.NewLine} {ex.Message} ");
                    return;
                }
            }

            // Use Steam Application id file to determine which game this AML installation is used for.
            string appIdStr;

            try
            {
                appIdStr = File.ReadAllText(Workshop.APPID_FILENAME);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法访问 {Workshop.APPID_FILENAME}. {Environment.NewLine} {ex.Message} ");
                return;
            }

            if (uint.TryParse(appIdStr, out uint appId))
            {
                switch ((GameId)appId)
                {
                    case GameId.X2:
                        XEnv = new Xcom2Env();
                        break;

                    case GameId.ChimeraSquad:
                        XEnv = new XComChimeraSquadEnv();
                        break;

                    default:
                        MessageBox.Show($" {Workshop.APPID_FILENAME} 文件包含意外的应用程序ID: {appId}.", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                }
            }

            appSettings.Save();
        }

        /// <summary>
        /// Checks if .Net Framework 4.7.2 or later installed.
        /// Verifies that the method SortedSet.TryGetValue() (which was added with 4.7.2) is available.
        /// </summary>
        /// <returns>true if version 4.7.2 or later is installed</returns>
        private static bool CheckDotNet4_7_2()
        {
            try
            {
                return typeof(SortedSet<>).GetMethod("TryGetValue") != null;
                // return typeof(DateTimeOffset).GetMethod("FromUnixTimeSeconds") != null; // obsolete .NET 4.6 check
            }
            catch (AmbiguousMatchException)
            {
                // ambiguous means there is more than one result
                return true;
            }
        }

        public static Settings InitializeSettings()
        {
            var firstRun = !File.Exists("settings.json");

            var settings = firstRun ? new Settings() : Settings.Instance;

            // Logic behind this:
            // If the field ShowUpgradeWarning doesn't exists in the loaded settings file; it will be initialized to its default value "true".
            // In that case, an old incompatible settings version is assumed and we report a warning.
            if (settings.ShowUpgradeWarning && !firstRun)
            {
                Log.Warn("Incompatible settings.json");

                MessageBoxManager.Cancel = "退出";
                MessageBoxManager.OK = "继续";
                MessageBoxManager.Register();
                var choice = MessageBox.Show("此启动器版本与旧的\"settings.json\"文件不兼容.\n" + "立即停止并启动旧版本以导出您的mod的配置文件，包括分组!\n" + "完成后，将旧的“ settings.json”文件移至“安全位置”，然后继续.\n" + "加载后，导入保存的配置文件以恢复分组。\n\n" + "如果您尚未准备好执行此操作，请单击“退出”以保留所有更改.", "警告!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (choice == DialogResult.Cancel)
                    Environment.Exit(0);

                Log.Warn("User ignored incompatibility");
                MessageBoxManager.Unregister();
            }

            if (!firstRun)
            {
                // Check if settings file matches target game
                if (settings.Game != XEnv.Game)
                {
                    var targetGame = settings.Game == GameId.X2 ? "XCOM 2" : "XCOM Chimera Squad";
                    var activeGame = XEnv.Game == GameId.X2 ? "XCOM 2" : "XCOM Chimera Squad";
                    MessageBox.Show($"当前设置是针对 '{targetGame}', 但此AML副本已配置为运行 '{activeGame}'. " + "要解决此问题,请关闭AML然后执行以下操作:\n\n" + "a) 删除文件“ settings.json”以重置设置\n\n" + "    或者\n\n" + $"b) 删除文件“ steam_appid.txt”并在启动时选择' {targetGame}'", "警告", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    Environment.Exit(0);
                }
            }

            settings.Game = XEnv.Game;
            settings.ShowUpgradeWarning = false;

            // Verify Game Path
            if (!Directory.Exists(settings.GamePath))
                settings.GamePath = XEnv.DetectGameDir();

            if (settings.GamePath == "")
            {
                Log.Warn("Unable to detect installation path");
                MessageBox.Show(@"无法检测到安装目录。请从设置中手动选择.", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Make sure, that all mod paths have a trailing backslash
            var pathsWithMissingTrailingBackSlash = settings.ModPaths.Where(m => !m.EndsWith(@"\")).ToList();
            for (var i = 0; i < pathsWithMissingTrailingBackSlash.Count; i++)
            {
                pathsWithMissingTrailingBackSlash[i] += @"\";
            }

            // Check and potentially add new mod paths from XCOM ini file.
            var modDirs = XEnv.DetectModDirs();

            if (modDirs != null)
            {
                settings.ModPaths.AddRange(modDirs.Where(modPath => !settings.ModPaths.Contains(modPath)));
            }
            else
            {
                MessageBox.Show("无法从\" XComEngine.ini\"读取mod目录。有关详细信息，请参见文件“ AML.log”.", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Remove obsolete mod paths
            settings.ModPaths.RemoveAll(modPath => !Directory.Exists(modPath));

            if (settings.ModPaths.Count == 0)
            {
                Log.Warn("No mod directories configured");
                MessageBox.Show(@"无法检测到mod目录。请从设置对话框中手动添加.", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (settings.Mods.Entries.Count > 0)
            {
                settings.Mods.MarkDuplicates();
                settings.Mods.InitCategoryIndices();

                // Verify Mods
                foreach (var mod in settings.Mods.All)
                {
                    if (!settings.ModPaths.Any(mod.IsInModPath))
                    {
                        Log.Warn($"The mod {mod.ID} is not located in any of the configured mod directories -> ModState.NotLoaded");
                        mod.AddState(ModState.NotLoaded);
                    }

                    if (!Directory.Exists(mod.Path))
                    {
                        Log.Warn($"The mod {mod.ID} is no longer available in the directory {mod.Path} -> ModState.NotInstalled");
                        mod.AddState(ModState.NotInstalled);
                    }
                    else if (!File.Exists(mod.GetModInfoFile()))
                    {
                        string newModInfo = settings.Mods.FindModInfo(mod.Path);
                        if (newModInfo != null)
                        {
                            mod.ID = Path.GetFileNameWithoutExtension(newModInfo);
                        }
                        else
                        {
                            Log.Warn($"The XComMod file for the mod {mod.ID} is missing -> ModState.NotInstalled");
                            mod.AddState(ModState.NotInstalled);
                        }
                    }

                    // tags clean up
                    mod.Tags = mod.Tags.Where(t => settings.Tags.ContainsKey(t.ToLower())).ToList();

                    // If the duplicate mod workaround is disabled, make sure that all mod info files are enabled.
                    if (!settings.EnableDuplicateModIdWorkaround)
                    {
                        mod.EnableModFile();
                    }

                    settings.Mods.UpdatedModDependencyState(mod);
                }

                var newlyBrokenMods = settings.Mods.All.Where(m => (m.State == ModState.NotLoaded || m.State == ModState.NotInstalled) && !m.isHidden).ToList();
                if (newlyBrokenMods.Count > 0)
                {
                    if (newlyBrokenMods.Count == 1)
                        FlexibleMessageBox.Show($"mod'{newlyBrokenMods[0].Name}'不再存在，并且已被隐藏.");
                    else
                        FlexibleMessageBox.Show($"{newlyBrokenMods.Count} mod已不存在且已被隐藏:\r\n\r\n" + string.Join("\r\n", newlyBrokenMods.Select(m => m.Name)));

                    foreach (var m in newlyBrokenMods)
                        m.isHidden = true;
                }
            }

            // import mods
            settings.ImportMods();

            return settings;
        }
    }
}