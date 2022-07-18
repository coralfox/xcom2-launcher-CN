﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrightIdeasSoftware;
using JR.Utils.GUI.Forms;
using log4net;
using XCOM2Launcher.Classes.Steam;
using XCOM2Launcher.Mod;
using XCOM2Launcher.XCOM;
using Timer = System.Windows.Forms.Timer;

namespace XCOM2Launcher.Forms
{
    public partial class MainForm
    {
        private const string ExclamationIconKey = "Exclamation";
        private static readonly ILog Log = LogManager.GetLogger(nameof(MainForm));
        private CancellationTokenSource ModUpdateCancelSource;

        private Task ModUpdateTask;

        public MainForm(Settings settings)
        {
            InitializeComponent();

            appRestartPendingLabel.Visible = false;
            progress_toolstrip_progressbar.Visible = false;
            aboutToolStripMenuItem.DropDownDirection = ToolStripDropDownDirection.BelowLeft;

            // Settings
            SteamAPIWrapper.Init();
            Settings = settings;

<<<<<<< HEAD
            // Restore states
            showHiddenModsToolStripMenuItem.Checked = settings.ShowHiddenElements;
            cShowStateFilter.Checked = settings.ShowStateFilter;
            cEnableGrouping.Checked = settings.ShowModListGroups;
            cShowPrimaryDuplicates.Checked = Settings.ShowPrimaryDuplicateAsDependency;
            cShowPrimaryDuplicates.Visible = Settings.EnableDuplicateModIdWorkaround;
            modlist_ListObjectListView.UseTranslucentSelection = Settings.UseTranslucentModListSelection;
            olvRequiredMods.UseTranslucentSelection = Settings.UseTranslucentModListSelection;
            olvDependentMods.UseTranslucentSelection = Settings.UseTranslucentModListSelection;

            // Set visibility of some controls depending on game type
            var wotcAvailable = Directory.Exists(settings.GamePath + @"\XCom2-WarOfTheChosen");
            runXCOM2ToolStripMenuItem.Visible = Program.XEnv.Game == GameId.X2;
            runWarOfTheChosenToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2;
            runChallengeModeToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2;
            importFromWotCToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2;
            importFromXCOM2ToolStripMenuItem.Visible = Program.XEnv.Game == GameId.X2;
            runChimeraSquadToolStripMenuItem.Visible = Program.XEnv.Game == GameId.ChimeraSquad;
            importFromChimeraSquadToolStripMenuItem.Visible = Program.XEnv.Game == GameId.ChimeraSquad;

            if (Program.XEnv.Game != GameId.X2)
            {
                modlist_ListObjectListView.AllColumns.Remove(olvForWOTC);
                modlist_ListObjectListView.RebuildColumns();
                olvDependentMods.AllColumns.Remove(olvColDepModsWotc);
                olvDependentMods.RebuildColumns();
                olvRequiredMods.AllColumns.Remove(olvColReqModsWotc);
                olvRequiredMods.RebuildColumns();
            }

            // If game path is not configured, hide several function/options.
            if (string.IsNullOrEmpty(settings.GamePath))
            {
                runWarOfTheChosenToolStripMenuItem.Enabled = false;
                runChallengeModeToolStripMenuItem.Enabled = false;
                importFromWotCToolStripMenuItem.Enabled = false;
                importFromXCOM2ToolStripMenuItem.Enabled = false;
                importFromChimeraSquadToolStripMenuItem.Enabled = false;
            }
=======
            // Restore states 
            InitMainGui(settings);
>>>>>>> origin/master

            // Init interface
            InitModListView();
            InitDependencyListViews();
            UpdateInterface();
            RegisterEvents();

            // Other intialization
            InitializeTabImages();

            // Init the argument checkboxes
            InitQuickArgumentsMenu(settings);

<<<<<<< HEAD
#if !DEBUG
=======
            #if !DEBUG
>>>>>>> origin/master
            // Update mod information
            var mods = Settings.Mods.All.ToList();

            if (settings.OnlyUpdateEnabledOrNewModsOnStartup)
            {
                mods = mods.Where(mod => mod.isActive || mod.State.HasFlag(ModState.New)).ToList();
            }

            UpdateMods(mods, () =>
            {
                modlist_ListObjectListView.RefreshObjects(mods);
            });
            #endif

            // Run callbacks
            var t1 = new Timer();
            t1.Tick += (sender, e) =>
            {
                SteamAPIWrapper.RunCallbacks();
            };
            t1.Interval = 10;
            t1.Start();

            /*
                        // Check for running downloads
            #if DEBUG
                        if (Settings.GetWorkshopPath() != null)
                        {
                            CheckSteamForNewMods();

                            var t2 = new Timer();
                            t2.Tick += (sender, e) => { CheckSteamForNewMods(); };
                            t2.Interval = 30000;
                            t2.Start();
                        }
            #endif
            */
        }

        private bool IsModUpdateTaskRunning => ModUpdateTask != null && !ModUpdateTask.IsCompleted;

        public Settings Settings { get; set; }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Text += " " + Program.GetCurrentVersionString(true);
        }

        private void InitializeTabImages()
        {
            tabImageList.Images.Add(ExclamationIconKey, error_provider.Icon);
        }

<<<<<<< HEAD
        #region Export
=======
/*
        // This was only called for DEBUG builds, but currently isn't required (maybe part of an abandoned feature?)
        private void CheckSteamForNewMods()
        {
            SetStatus("Checking for new mods...");

            ulong[] subscribedIDs;
            try
            {
                subscribedIDs = Workshop.GetSubscribedItems();
            }
            catch (InvalidOperationException ex)
            {
                // Steamworks not initialized?
                // Game taking over?
                Log.Error("Error checking for new mods", ex);
                SetStatus("Error checking for new mods.");
                return;
            }

            var change = false;
            foreach (var id in subscribedIDs)
            {
                var status = Workshop.GetDownloadStatus(id);

                if (status.HasFlag(EItemState.k_EItemStateInstalled))
                    // already installed
                    continue;

                if (Downloads.Any(d => d.WorkshopID == (long) id))
                    // already observing
                    continue;

                // Get info
                var detailsRequest = new ItemDetailsRequest(id).Send().WaitForResult();
                var details = detailsRequest.Result[0];
                var link = detailsRequest.GetPreviewURL();

                var downloadMod = new ModEntry
                {
                    Name = details.m_rgchTitle,
                    DateCreated = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeCreated).DateTime,
                    DateUpdated = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated).DateTime,
                    //Path = Path.Combine(Settings.GetWorkshopPath(), "" + id),
                    Image = link,
                    WorkshopID = (int) id
                };

                downloadMod.SetSource(ModSource.SteamWorkshop);
                downloadMod.AddState(ModState.New | ModState.NotInstalled);

                // Start download
                Workshop.DownloadItem(id);
                //
                Downloads.Add(downloadMod);
                change = true;
            }

            if (change)
                RefreshModList();

            SetStatusIdle();
        }
*/
        #region GUI

        /// <summary>
        /// Initializes GUI elements that depend on current settings and selected game type.
        /// </summary>
        /// <param name="settings"></param>
        private void InitMainGui(Settings settings)
        {
            showHiddenModsToolStripMenuItem.Checked = settings.ShowHiddenElements;
            cShowStateFilter.Checked = settings.ShowStateFilter;
            cEnableGrouping.Checked = settings.ShowModListGroups;
            cShowPrimaryDuplicates.Checked = settings.ShowPrimaryDuplicateAsDependency;
            cShowPrimaryDuplicates.Visible = settings.EnableDuplicateModIdWorkaround;
            modlist_ListObjectListView.UseTranslucentSelection = settings.UseTranslucentModListSelection;
            olvRequiredMods.UseTranslucentSelection = settings.UseTranslucentModListSelection;
            olvDependentMods.UseTranslucentSelection = settings.UseTranslucentModListSelection;

            // Set visibility of some controls depending on game type
            var wotcAvailable = Directory.Exists(settings.GamePath + @"\XCom2-WarOfTheChosen");
            runXCOM2ToolStripMenuItem.Visible = Program.XEnv.Game == GameId.X2 && (!settings.HideXcom2Button || !wotcAvailable);
            runWarOfTheChosenToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2;
            runChallengeModeToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2 && !settings.HideChallengeModeButton;
            importFromWotCToolStripMenuItem.Visible = wotcAvailable && Program.XEnv.Game == GameId.X2;
            importFromXCOM2ToolStripMenuItem.Visible = Program.XEnv.Game == GameId.X2;
            runChimeraSquadToolStripMenuItem.Visible = Program.XEnv.Game == GameId.ChimeraSquad;
            importFromChimeraSquadToolStripMenuItem.Visible = Program.XEnv.Game == GameId.ChimeraSquad;

            if (Program.XEnv.Game != GameId.X2)
            {
                modlist_ListObjectListView.AllColumns.Remove(olvForWOTC);
                modlist_ListObjectListView.RebuildColumns();
                olvDependentMods.AllColumns.Remove(olvColDepModsWotc);
                olvDependentMods.RebuildColumns();
                olvRequiredMods.AllColumns.Remove(olvColReqModsWotc);
                olvRequiredMods.RebuildColumns();
            }

            // If game path is not configured, hide several function/options.
            if (string.IsNullOrEmpty(settings.GamePath))
            {
                runWarOfTheChosenToolStripMenuItem.Enabled = false;
                runChallengeModeToolStripMenuItem.Enabled = false;
                importFromWotCToolStripMenuItem.Enabled = false;
                importFromXCOM2ToolStripMenuItem.Enabled = false;
                importFromChimeraSquadToolStripMenuItem.Enabled = false;
            }
        }

        private void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(text)));
                return;
            }

            status_toolstrip_label.Text = text;
        }

        private void SetStatusIdle()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetStatusIdle));
                return;
            }

            status_toolstrip_label.Text = "Ready.";
            progress_toolstrip_progressbar.Visible = false;
        }

        #endregion

		#region Export
>>>>>>> origin/master

        private void UpdateExport()
        {
            var str = new StringBuilder();

            if (!Mods.Active.Any())
            {
                export_richtextbox.Text = "没有启用的Mod.";
                return;
            }

            var showCategories = export_group_checkbox.Checked;
            var showLink = export_workshop_link_checkbox.Checked;
            var showAllMods = export_all_mods_checkbox.Checked;

            var nameLength = showAllMods ? Mods.All.Max(m => m.Name.Length) : Mods.Active.Max(m => m.Name.Length);
            var idLength = showAllMods ? Mods.All.Max(m => m.ID.Length) : Mods.Active.Max(m => m.ID.Length);
            var workshopIDLength = showAllMods ? Mods.All.Max(m => m.WorkshopID.ToString().Length) : Mods.Active.Max(m => m.WorkshopID.ToString().Length);

            foreach (var entry in Mods.Entries.Where(e => e.Value.Entries.Any(m => m.isActive)))
            {
                List<ModEntry> mods;
                //         if (showAllMods)
                //          mods = entry.Value.Entries.ToList();
                //else
                mods = entry.Value.Entries.Where(m => m.isActive).ToList();

                if (showCategories)
                    str.AppendLine($"{entry.Key} ({mods.Count()}):");

                foreach (var mod in mods)
                {
                    if (showCategories)
                        str.Append("\t");

                    str.Append(string.Format("{0,-" + nameLength + "} ", mod.Name));
                    str.Append("\t");
                    str.Append(string.Format("{0,-" + idLength + "} ", mod.ID));
                    str.Append("\t");

                    // add workshop ID or link
                    if (mod.WorkshopID == -1)
                        str.Append("未知");
                    else if (showLink)
                        str.Append(string.Format("{0,-" + workshopIDLength + "} ", mod.GetWorkshopLink()));
                    else
                        str.Append(string.Format("{0,-" + workshopIDLength + "} ", mod.WorkshopID));
                    str.Append("\t");

                    str.Append(string.Join(";", mod.Tags));

                    str.AppendLine();
                }

                if (export_group_checkbox.Checked)
                    str.AppendLine();
            }

            export_richtextbox.Text = str.ToString();
        }

        #endregion Export

        /*
                // This was only called for DEBUG builds, but currently isn't required (maybe part of an abandoned feature?)
                private void CheckSteamForNewMods()
                {
                    SetStatus("Checking for new mods...");

                    ulong[] subscribedIDs;
                    try
                    {
                        subscribedIDs = Workshop.GetSubscribedItems();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Steamworks not initialized?
                        // Game taking over?
                        Log.Error("Error checking for new mods", ex);
                        SetStatus("Error checking for new mods.");
                        return;
                    }

                    var change = false;
                    foreach (var id in subscribedIDs)
                    {
                        var status = Workshop.GetDownloadStatus(id);

                        if (status.HasFlag(EItemState.k_EItemStateInstalled))
                            // already installed
                            continue;

                        if (Downloads.Any(d => d.WorkshopID == (long) id))
                            // already observing
                            continue;

                        // Get info
                        var detailsRequest = new ItemDetailsRequest(id).Send().WaitForResult();
                        var details = detailsRequest.Result[0];
                        var link = detailsRequest.GetPreviewURL();

                        var downloadMod = new ModEntry
                        {
                            Name = details.m_rgchTitle,
                            DateCreated = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeCreated).DateTime,
                            DateUpdated = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated).DateTime,
                            //Path = Path.Combine(Settings.GetWorkshopPath(), "" + id),
                            Image = link,
                            WorkshopID = (int) id
                        };

                        downloadMod.SetSource(ModSource.SteamWorkshop);
                        downloadMod.AddState(ModState.New | ModState.NotInstalled);

                        // Start download
                        Workshop.DownloadItem(id);
                        //
                        Downloads.Add(downloadMod);
                        change = true;
                    }

                    if (change)
                        RefreshModList();

                    SetStatusIdle();
                }
        */

        #region GUI

        private void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(text)));
                return;
            }

            status_toolstrip_label.Text = text;
        }

        private void SetStatusIdle()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetStatusIdle));
                return;
            }

            status_toolstrip_label.Text = "就绪.";
            progress_toolstrip_progressbar.Visible = false;
        }

        #endregion GUI

        #region Basic

        private void Reset()
        {
            if (IsModUpdateTaskRunning)
            {
                ShowModUpdateRunningMessageBox();
                return;
            }

            modlist_ListObjectListView.Clear();
            Settings = Program.InitializeSettings();
            InitModListView();
        }

        private void Save(bool WotC = false)
        {
            try
            {
                if (Program.XEnv is Xcom2Env x2Env)
                {
                    x2Env.UseWotC = WotC;
                }

                Program.XEnv.SaveChanges(Settings, ChallengeMode);
                Settings.SaveFile("settings.json");
            }
            catch (Exception ex)
            {
                // lets report any issues that occur while writing the settings.json or the ini files
                Log.Warn("Failed so save/apply settings", ex);
                MessageBox.Show("保存更改时发生错误." + Environment.NewLine + Environment.NewLine + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowModUpdateRunningMessageBox()
        {
            MessageBox.Show("正在进行Mod更新，请等待完成.", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RunChimeraSquad()
        {
            if (IsModUpdateTaskRunning)
            {
                ShowModUpdateRunningMessageBox();
                return;
            }

            Save();

            Program.XEnv.RunGame(Settings.GamePath, Settings.GetArgumentString());

            if (Settings.CloseAfterLaunch)
                Close();
        }

        private void RunVanilla()
        {
            if (IsModUpdateTaskRunning)
            {
                ShowModUpdateRunningMessageBox();
                return;
            }

            // Check for WOTC only mods
            if (Settings.Mods.Active.Count(e => e.BuiltForWOTC) > 0)
            {
                if (FlexibleMessageBox.Show(this, "您确定要继续吗？请注意，这很可能使您的游戏崩溃。不兼容的mod:\r\n" + string.Join("\r\n", Settings.Mods.Active.Where(e => e.BuiltForWOTC).Select(e => e.Name)), "您正在尝试使用为WOTC构建的Mod来启动原版游戏", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Log.Warn("User chose to run Vanilla XCOM with WotC mods");
                }
                else
                {
                    return;
                }
            }

            Settings.Instance.LastLaunchedWotC = false;
            ChallengeMode = false;
            Save();

            if (Program.XEnv is Xcom2Env x2Env)
            {
                x2Env.UseWotC = false;
                Program.XEnv.RunGame(Settings.GamePath, Settings.GetArgumentString());
            }

            if (Settings.CloseAfterLaunch)
                Close();
        }

        private void RunWotC()
        {
            if (IsModUpdateTaskRunning)
            {
                ShowModUpdateRunningMessageBox();
                return;
            }

            Settings.Instance.LastLaunchedWotC = true;
            ChallengeMode = false;
            Save(true);

            if (Program.XEnv is Xcom2Env x2Env)
            {
                x2Env.UseWotC = true;
                Program.XEnv.RunGame(Settings.GamePath, Settings.GetArgumentString());
            }

            if (Settings.CloseAfterLaunch)
                Close();
        }

        private bool ChallengeMode;

        private void RunChallengeMode()
        {
            if (IsModUpdateTaskRunning)
            {
                ShowModUpdateRunningMessageBox();
                return;
            }

            Settings.Instance.LastLaunchedWotC = true;
            ChallengeMode = true;
            Save(true);

            if (Program.XEnv is Xcom2Env x2Env)
            {
                x2Env.UseWotC = true;
                Program.XEnv.RunGame(Settings.GamePath, Settings.GetArgumentString().ToLower().Replace("-allowconsole", ""));
            }

            if (Settings.CloseAfterLaunch)
                Close();
        }

        #endregion Basic

        #region Interface updates

        private void UpdateInterface()
        {
            error_provider.Clear();

            UpdateConflictInfo();
            UpdateModInfo(modlist_ListObjectListView.SelectedObject as ModEntry);
            UpdateLabels();
            UpdateStateFilterLabels();
        }

        private void UpdateLabels()
        {
            //
            var hasConflicts = NumConflicts > 0;
            modlist_tab.Text = $"Mod ({Mods.Active.Count()} / {Mods.All.Count()})";
            conflicts_tab.Text = "覆写" + (hasConflicts ? $" ({NumConflicts} 冲突)" : "");
            conflicts_tab.ImageKey = hasConflicts ? ExclamationIconKey : null;
        }

        /// <summary>
        /// Updates labels from filter controls with number of currently matching mods.
        /// </summary>
        private void UpdateStateFilterLabels()
        {
            var allMods = Mods.All.ToList();
            cFilterConflicted.Text = $"冲突 ({allMods.Count(m => m.State.HasFlag(ModState.ModConflict))})";
            cFilterDuplicate.Text = $"重复 ({allMods.Count(m => m.State.HasFlag(ModState.DuplicateID))})";
            cFilterNew.Text = $"新 ({allMods.Count(m => m.State.HasFlag(ModState.New))})";
            cFilterNotInstalled.Text = $"未安装 ({allMods.Count(m => m.State.HasFlag(ModState.NotInstalled))})";
            cFilterNotLoaded.Text = $"未加载 ({allMods.Count(m => m.State.HasFlag(ModState.NotLoaded))})";
            cFilterMissingDependency.Text = $"缺少依赖 ({allMods.Count(m => m.isActive && m.State.HasFlag(ModState.MissingDependencies))})";
            cFilterHidden.Text = $"隐藏 ({allMods.Count(m => m.isHidden)})";
        }

        public int NumConflicts;

        /// <summary>
        /// Update incompatibility warnings and overrides grid.
        /// </summary>
        private void UpdateConflictInfo()
        {
            // Incremented later in GetDuplicatesString() and GetOverridesString()
            NumConflicts = 0;

            // Clear and refill conflicts_datagrid
            conflicts_datagrid.Rows.Clear();

            foreach (var m in Mods.Active)
            {
                foreach (var classOverride in m.GetOverrides(true))
                {
                    var oldClass = classOverride.OldClass;

                    if (classOverride.OverrideType == ModClassOverrideType.UIScreenListener)
                        oldClass += " (UIScreenListener)";

                    conflicts_datagrid.Rows.Add(m.Name, oldClass, classOverride.NewClass);
                }
            }

            // Conflict log
            conflicts_textbox.Text = GetDuplicatesString() + GetOverridesString();

            // Update interface
            modlist_ListObjectListView.UpdateObjects(ModList.Objects.ToList());
            UpdateLabels();
        }

        private void UpdateConflictsForMods(List<ModEntry> mods)
        {
            // Incremented later in GetDuplicatesString() and GetOverridesString()
            NumConflicts = 0;

            // Update conflicts_datagrid
            foreach (var m in mods)
            {
                if (m.isActive)
                {
                    foreach (var classOverride in m.GetOverrides(true))
                    {
                        var oldClass = classOverride.OldClass;

                        if (classOverride.OverrideType == ModClassOverrideType.UIScreenListener)
                            oldClass += " (UIScreenListener)";

                        conflicts_datagrid.Rows.Add(m.Name, oldClass, classOverride.NewClass);
                    }
                }
                else
                {
                    foreach (var classOverride in m.GetOverrides(true))
                    {
                        foreach (var row in conflicts_datagrid.Rows.Cast<DataGridViewRow>())
                        {
                            var oldClass = classOverride.OldClass;

                            if (classOverride.OverrideType == ModClassOverrideType.UIScreenListener)
                                oldClass += " (UIScreenListener)";

                            if ((string) row.Cells[0].Value == m.Name && (string) row.Cells[1].Value == oldClass && (string) row.Cells[2].Value == classOverride.NewClass)
                            {
                                conflicts_datagrid.Rows.Remove(row);
                                break;
                            }
                        }
                    }
                }
            }

            // Conflict log
            conflicts_textbox.Text = GetDuplicatesString() + GetOverridesString();
        }

        private string GetDuplicatesString()
        {
            var str = new StringBuilder();

            var duplicates = Mods.GetDuplicates().Where(delegate(IGrouping<string, ModEntry> entries)
            {
                // Only show duplicate-groups that have not been resolved (ModState.DuplicateDisabled / ModState.DuplicatePrimary)
                return entries?.All(m => m.State.HasFlag(ModState.DuplicateID)) == true;
            }).ToList();

            if (duplicates.Any())
            {
                str.AppendLine("找到具有相同模组ID的Mod!");
                if (Settings.EnableDuplicateModIdWorkaround)
                {
                    str.AppendLine("您可以从mod列表上下文菜单中设置主选副本来解决此问题.");
                }
                else
                {
                    str.AppendLine("这些只能一起激活/禁用.");
                }

                str.AppendLine();

                foreach (var grouping in duplicates)
                {
                    NumConflicts++;

                    str.AppendLine(grouping.Key);

                    foreach (var m in grouping)
                        str.AppendLine($"\t{m.Name}");

                    str.AppendLine();
                }

                str.AppendLine();
            }

            return str.ToString();
        }

        private string GetOverridesString()
        {
            var conflicts = Mods.GetActiveConflicts().ToList();
            if (!conflicts.Any())
                return "";

            var showUIScreenListenerMessage = false;

            var str = new StringBuilder();

            str.AppendLine("找到具有冲突的Mod!");
            str.AppendLine("这些mod可能无法按预期运行，或者一起运行会导致不稳定。");
            str.AppendLine();

            foreach (var conflict in conflicts)
            {
                str.AppendLine($"发现冲突--'{conflict.ClassName}':");
                var hasMultipleUIScreenListeners = conflict.Overrides.Count(o => o.OverrideType == ModClassOverrideType.UIScreenListener) > 1;

                foreach (var classOverride in conflict.Overrides.OrderBy(o => o.OverrideType).ThenBy(o => o.Mod.Name))
                {
                    if (hasMultipleUIScreenListeners && classOverride.OverrideType == ModClassOverrideType.UIScreenListener)
                    {
                        showUIScreenListenerMessage = true;
                        str.AppendLine($"\t* {classOverride.Mod.Name}");
                    }
                    else
                    {
                        str.AppendLine($"\t{classOverride.Mod.Name}");
                    }
                }

                str.AppendLine();

                NumConflicts++;
            }

            error_provider.SetError(conflicts_log_label, "找到 " + NumConflicts + " 冲突");

            if (showUIScreenListenerMessage)
            {
                str.AppendLine("* (这些mod使用UIScreenListeners，这意味着它们不会相互冲突)");
                str.AppendLine();
            }

            return str.ToString();
        }

        /// <summary>
        /// Updates the mod description user interface with the description from the provided Mod.
        /// </summary>
        /// <param name="m">The desired mod. Use null to clear/reset description.</param>
        private void UpdateModDescription(ModEntry m)
        {
            modinfo_info_DescriptionRichTextBox.Clear();

            if (m != null)
            {
                modinfo_info_DescriptionRichTextBox.Font = DefaultFont;
                modinfo_info_DescriptionRichTextBox.Rtf = m.GetDescription(true);
            }

            btnDescSave.Enabled = false;
            btnDescUndo.Enabled = false;
        }

        private void UpdateDependencyInformation(ModEntry m)
        {
            if (m == null)
                return;

            // update dependency information
            olvRequiredMods.ClearObjects();
            olvRequiredMods.AddObjects(Mods.GetRequiredMods(m, cShowPrimaryDuplicates.Checked));
            olvDependentMods.ClearObjects();
            olvDependentMods.AddObjects(Mods.GetDependentMods(m, false));
        }

        /// <summary>
        /// Update mod information panel with data from specified mod.
        /// </summary>
        /// <param name="m"></param>
        private void UpdateModInfo(ModEntry m)
        {
            if (m == null)
            {
                modinfo_info_TitleTextBox.Text = "未选择任何Mod";
                modinfo_info_AuthorTextBox.Clear();
                modinfo_info_DateCreatedTextBox.Clear();
                modinfo_info_InstalledTextBox.Clear();
                modinfo_readme_RichTextBox.Clear();
                modinfo_changelog_richtextbox.Clear();
                UpdateModDescription(null);
                modinfo_image_picturebox.ImageLocation = null;
                modinfo_inspect_propertygrid.SelectedObject = null;
                modinfo_config_FileSelectCueComboBox.Items.Clear();
                modinfo_config_LoadButton.Enabled = false;
                modinfo_config_RemoveButton.Enabled = false;
                modinfo_ConfigFCTB.Clear();
                modinfo_ConfigFCTB.ReadOnly = true;
                olvRequiredMods.ClearObjects();
                olvDependentMods.ClearObjects();
                return;
            }

            // show panel
            horizontal_splitcontainer.Panel2Collapsed = false;

            // Update data
            modinfo_info_TitleTextBox.Text = m.Name;
            modinfo_info_AuthorTextBox.Text = m.Author;
            modinfo_info_DateCreatedTextBox.Text = m.DateCreated?.ToString() ?? "";
            modinfo_info_InstalledTextBox.Text = m.DateAdded?.ToString() ?? "";
            UpdateModDescription(m);
            UpdateModChangeLog(m);
            modinfo_readme_RichTextBox.Text = m.GetReadMe();
            modinfo_image_picturebox.ImageLocation = m.Image;
            UpdateDependencyInformation(m);

            // Init handler for property changes
            var sel_obj = m.GetProperty();

            sel_obj.PropertyChanged += (sender, e) =>
            {
                RefreshModList();
                modinfo_inspect_propertygrid.Refresh();
            };

            modinfo_inspect_propertygrid.SelectedObject = sel_obj;

            #region Config

            // config files
            string[] configFiles = m.GetConfigFiles();

            // clear
            modinfo_config_FileSelectCueComboBox.Items.Clear();
            modinfo_ConfigFCTB.Text = "";
            modinfo_config_LoadButton.Enabled = false;
            modinfo_config_RemoveButton.Enabled = false;

            if (configFiles.Length > 0)
            {
                foreach (var configFile in configFiles)
                {
                    if (configFile != null) modinfo_config_FileSelectCueComboBox.Items.Add(CurrentMod.GetPathRelative(configFile));
                }
            }

            #endregion Config
        }

        /// <summary>
        /// Updates the quick launch menu items check-states, depending on if the the respective argument is enabled in the settings.
        /// </summary>
        private void InitQuickArgumentsMenu(Settings settings)
        {
            LauchOptionsPanel.Visible = settings.ShowQuickLaunchArguments && settings.QuickToggleArguments.Any();

            quickLaunchToolstripButton.DropDownItems.Clear();
            foreach (var arg in settings.QuickToggleArguments)
            {
                var item = new ToolStripMenuItem(arg)
                {
                    CheckOnClick = true
                };
                item.Click += QuickArgumentItemClick;
                quickLaunchToolstripButton.DropDownItems.Add(item);
            }

            foreach (ToolStripMenuItem item in quickLaunchToolstripButton.DropDownItems)
            {
                item.Checked = Settings.ArgumentList.Any(arg => arg.Equals(item.Text, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion Interface updates

        #region Dependency ObjectListViews

        private void InitDependencyListViews()
        {
            olvColReqModsState.AspectGetter = StateAspectGetter;
            olvColDepModsState.AspectGetter = StateAspectGetter;

            olvRequiredMods.BooleanCheckStatePutter = BooleanCheckStatePutter;
            olvDependentMods.BooleanCheckStatePutter = BooleanCheckStatePutter;

            olvColReqModsIgnore.AspectGetter += rowObject =>
            {
                if (CurrentMod == null || !(rowObject is ModEntry mod))
                    return false;

                return CurrentMod.IgnoredDependencies.Contains(mod.WorkshopID);
            };

            olvColReqModsIgnore.AspectPutter += (rowObject, value) =>
            {
                if (CurrentMod == null || !(rowObject is ModEntry mod) || !(value is bool checkState))
                    return;

                // Add the mod id to or remove it from the IgnoredDependencies list.
                if (checkState)
                {
                    if (!CurrentMod.IgnoredDependencies.Contains(mod.WorkshopID))
                    {
                        CurrentMod.IgnoredDependencies.Add(mod.WorkshopID);
                    }
                }
                else
                {
                    if (CurrentMod.IgnoredDependencies.Contains(mod.WorkshopID))
                    {
                        CurrentMod.IgnoredDependencies.Remove(mod.WorkshopID);
                    }
                }

                Mods.UpdatedModDependencyState(CurrentMod);
                modlist_ListObjectListView.RefreshObject(CurrentMod);
            };

            olvRequiredMods.SubItemChecking += (sender, args) =>
            {
                if (CurrentMod == null && !(args.RowObject is ModEntry))
                {
                    args.NewValue = args.CurrentValue;
                }
            };
        }

        private bool BooleanCheckStatePutter(object rowobject, bool newValue)
        {
            var mod = rowobject as ModEntry;
            newValue = ProcessNewModState(mod, newValue);

            // change check state for the mod in main list accordingly
            if (newValue)
            {
                modlist_ListObjectListView.CheckObject(mod);
            }
            else
            {
                modlist_ListObjectListView.UncheckObject(mod);
            }

            return newValue;
        }

        private void olvDependencyMods_ItemActivate(object sender, EventArgs e)
        {
            if (sender is ObjectListView olv)
            {
                // view mod in main mod list on double-click
                if (ModList.Objects.Contains(olv.SelectedObject))
                {
                    modlist_ListObjectListView.SelectedObject = olv.SelectedObject;
                    modlist_ListObjectListView.EnsureModelVisible(olv.SelectedObject);
                }
            }
        }

        private void olvRequiredMods_FormatRow(object sender, FormatRowEventArgs e)
        {
            var mod = e.Model as ModEntry;
            Contract.Assume(mod != null);

            SetModListItemColor(e.Item, mod);
        }

        #endregion Dependency ObjectListViews
    }
}