﻿// TcNo Account Switcher - A Super fast account switcher
// Copyright (C) 2019-2021 TechNobo (Wesley Pyburn)
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TcNo_Acc_Switcher_Globals;
using TcNo_Acc_Switcher_Server.Pages.General;
using TcNo_Acc_Switcher_Server.Pages.General.Classes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Task = TcNo_Acc_Switcher_Server.Pages.General.Classes.Task;

namespace TcNo_Acc_Switcher_Server.Data
{
    public class AppSettings
    {
        private static AppSettings _instance = new();

        private static readonly object LockObj = new();

        public static AppSettings Instance
        {
            get
            {
                lock (LockObj)
                {
                    return _instance ??= new AppSettings();
                }
            }
            set => _instance = value;
        }

        // Variables
        private string _version = "2021-06-05_00";
        [JsonProperty("Version", Order = 0)] public string Version => _instance._version;

        private bool _updateAvailable;
        [JsonIgnore] public bool UpdateAvailable { get => _instance._updateAvailable; set => _instance._updateAvailable = value; }

        private bool _streamerModeEnabled = true;
        [JsonProperty("StreamerModeEnabled", Order = 1)] public bool StreamerModeEnabled { get => _instance._streamerModeEnabled; set => _instance._streamerModeEnabled = value; }

        private int _serverPort = 5000 ;
        [JsonProperty("ServerPort", Order = 2)] public int ServerPort { get => _instance._serverPort; set => _instance._serverPort = value; }

        private Point _windowSize = new() { X = 800, Y = 450 };
        [JsonProperty("WindowSize", Order = 3)] public Point WindowSize { get => _instance._windowSize; set => _instance._windowSize = value; }

        private bool _trayMinimizeNotExit;

        [JsonProperty("TrayMinimizeNotExit", Order = 4)]
        public bool TrayMinimizeNotExit
        {
            get => _instance._trayMinimizeNotExit;
            set
            {
                if (value)
                {
                    _ = GeneralInvocableFuncs.ShowToast("info", "On clicking the Exit button: I'll be on the Windows Tray! (Right of Start Bar)", duration: 15000, renderTo: "toastarea");
                    _ = GeneralInvocableFuncs.ShowToast("info", "Hint: Ctrl+Click the 'X' to close me completely, or via the Tray > 'Exit'", duration: 15000, renderTo: "toastarea");
                }
                _instance._trayMinimizeNotExit = value; 

            }
        }

        private bool _trayMinimizeLessMem;
        [JsonProperty("TrayMinimizeLessMem", Order = 4)] public bool TrayMinimizeLessMem { get => _instance._trayMinimizeLessMem; set => _instance._trayMinimizeLessMem = value; }


        private bool _desktopShortcut;
        [JsonIgnore] public bool DesktopShortcut { get => _instance._desktopShortcut; set => _instance._desktopShortcut = value; }
        private bool _startMenu;
        [JsonIgnore] public bool StartMenu { get => _instance._startMenu; set => _instance._startMenu = value; }
        private bool _trayStartup;
        [JsonIgnore] public bool TrayStartup { get => _instance._trayStartup; set => _instance._trayStartup = value; }

        private bool _currentlyElevated;
        [JsonIgnore] public bool CurrentlyElevated { get => _instance._currentlyElevated; set => _instance._currentlyElevated = value; }

        private string _selectedStylesheet;
        [JsonIgnore] public string SelectedStylesheet { get => _instance._selectedStylesheet; set => _instance._selectedStylesheet = value; }




        // Variables loaded from other files:
        // To create this: Take a standard YAML stylesheet and conver it to JSON,
        // Then replace:
        // "  " with "  { "
        // ",\r\n" with " },\r\n"
        // ": " with ", "
        // THEN DON'T FORGET TO ADD NEW ENTRIES INTO Shared\DynamicStylesheet.razor!
        private Dictionary<string, string> _defaultStylesheet = new()
        {
            {"name", "Default"},
            {"selectionColor", "#402B00"},
            {"selectionBackground", "#FFAA00"},
            {"contextMenuBackground", "#14151E"},
            {"contextMenuBackground-hover", "#1B2737"},
            {"contextMenuLeftBorder-hover", "#364E6E"},
            {"contextMenuTextColor", "#B0BEC5"},
            {"contextMenuTextColor-hover", "#FFFFFF"},
            {"contextMenuBoxShadow", "none"},
            {"headerbarBackground", "#14151E"},
            {"headerbarTCNOFill", "white"},
            {"windowControlsBackground-hover", "rgba(255,255,255,0.1)"},
            {"windowControlsBackground-active", "rgba(255,255,255,0.2)"},
            {"windowControlsCloseBackground", "#E81123"},
            {"windowControlsCloseBackground-active", "#F1707A"},
            {"windowTitleColor", "white"},
            {"footerBackground", "#222"},
            {"footerColor", "#DDD"},
            {"scrollbarTrackBackground", "#1F202D"},
            {"scrollbarThumbBackground", "#515164"},
            {"scrollbarThumbBackground-hover", "#555"},
            {"accountListItemWidth", "100px"},
            {"accountListItemHeight", "135px"},
            {"accountBackground-placeholder", "#28374E"},
            {"accountBorder-placeholder", "2px dashed #2777A4"},
            {"accountPColor", "#DDD"},
            {"accountColor", "white"},
            {"accountBackground-hover", "#28374E"},
            {"accountBackground-checked", "#274560"},
            {"accountBorder-hover", "#2777A4"},
            {"accountBorder-checked", "#26A0DA"},
            {"accountListBackground", "url(../img/noise.png), linear-gradient(90deg, #2e2f42, #28293A 100%)"},
            {"mainBackground", "#28293A"},
            {"defaultTextColor", "white"},
            {"linkColor", "#FFAA00"},
            {"linkColor-hover", "#FFDD00"},
            {"linkColor-active", "#CC7700"},
            {"borderedItemBorderColor", "#888"},
            {"borderedItemBorderColor-focus", "#888"},
            {"borderedItemBorderColorBottom-focus", "#FFAA00"},
            {"buttonBackground", "#333"},
            {"buttonBackground-active", "#222"},
            {"buttonBackground-hover", "#444"},
            {"buttonBorder", "#888"},
            {"buttonBorder-hover", "#888"},
            {"buttonBorder-active", "#FFAA00"},
            {"buttonColor", "white"},
            {"checkboxBorder", "white"},
            {"checkboxBorder-checked", "white"},
            {"checkboxBackground", "#28293A"},
            {"checkboxBackground-checked", "#FFAA00"},
            {"inputBackground", "#212529"},
            {"inputColor", "white"},
            {"dropdownBackground", "#333"},
            {"dropdownBorder", "#888"},
            {"dropdownColor", "white"},
            {"dropdownItemBackground-active", "#222"},
            {"dropdownItemBackground-hover", "#444"},
            {"listBackground", "#222"},
            {"listBackgroundColor-checked", "#FFAA00"},
            {"listColor", "white"},
            {"listColor-checked", "white"},
            {"listTextColor-before", "#FFAA00"},
            {"listTextColor-before-checked", "#945300"},
            {"listTextColor-after", "#3DFF89"},
            {"listTextColor-after-checked", "#945300"},
            {"settingsHeaderColor", "white"},
            {"settingsHeaderHrBorder", "#BBB"},
            {"modalBackground", "#00000055"},
            {"modalFgBackground", "#28293A"},
            {"modalInputBackground", "#212529"},
            {"modalTCNOFill", "white"},
            {"foundColor", "lime"},
            {"foundBackground", "green"},
            {"notFoundColor", "red"},
            {"notFoundBackground", "darkred"},
            {"limited", "yellow"},
            {"vac", "red"},
            {"notification-color-dark-text", "white"},
            {"notification-color-dark-border", "rgb(20, 20, 20)"},
            {"notification-color-info", "rgb(3, 169, 244)"},
            {"notification-color-info-light", "rgba(3, 169, 244, .25)"},
            {"notification-color-info-lighter", "#17132C"},
            {"notification-color-success", "rgb(76, 175, 80)"},
            {"notification-color-success-light", "rgba(76, 175, 80, .25)"},
            {"notification-color-success-lighter", "#17132C"},
            {"notification-color-warning", "rgb(255, 152, 0)"},
            {"notification-color-warning-light", "rgba(255, 152, 0, .25)"},
            {"notification-color-warning-lighter", "#17132C"},
            {"notification-color-error", "rgb(244, 67, 54)"},
            {"notification-color-error-light", "rgba(244, 67, 54, .25)"},
            {"notification-color-error-lighter", "#17132C"},
            {"updateBarBackground", "#FFAA00"},
            {"updateBarColor", "black"}
        };

        private Dictionary<string, string> _stylesheet;
        [JsonIgnore] public Dictionary<string, string> Stylesheet { get => _instance._stylesheet; set => _instance._stylesheet = value; }

        // Constants
        [JsonIgnore] public string SettingsFile = "WindowSettings.json";
        [JsonIgnore] public string StylesheetFile = "StyleSettings.yaml";
        [JsonIgnore] public bool StreamerModeTriggered;

        /// <summary>
        /// Check if any streaming software is running. Do let me know if you have a program name that you'd like to expand this list with!
        /// It's basically the program's .exe file, but without ".exe".
        /// </summary>
        /// <returns>True when streaming software is running</returns>
        public bool StreamerModeCheck()
        {
            Globals.DebugWriteLine(@"[Func:Data\AppSettings.StreamerModeCheck]");
            if (!_streamerModeEnabled) return false; // Don't hide anything if disabled.
            _instance.StreamerModeTriggered = false;
            foreach (var p in Process.GetProcesses())
            {
                //try
                //{
                //    if (p.MainModule == null) continue;
                //}
                //catch (System.ComponentModel.Win32Exception e)
                //{
                //    // This is just something the process can't access.
                //    // Ignore and move on.
                //    continue;
                //}

                switch (p.ProcessName.ToLower())
                {
                    case "obs":
                    case "obs32":
                    case "obs64":
                    case "streamlabs obs":
                    case "wirecast":
                    case "xsplit.core":
                    case "xsplit.gamecaster":
                    case "twitchstudio":
                        _instance.StreamerModeTriggered = true;
                        Console.WriteLine(p.ProcessName);
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Used in JS. Gets whether forget account is enabled (Whether to NOT show prompt, or show it).
        /// </summary>
        /// <returns></returns>
        [JSInvokable]
        public static Task<bool> GetTrayMinimizeNotExit() => System.Threading.Tasks.Task.FromResult(_instance.TrayMinimizeNotExit);

        /// <summary>
        /// Returns a block of CSS text to be used on the page. Used to hide or show certain things in certain ways, in components that aren't being added through Blazor.
        /// </summary>
        public string GetCssBlock() => ".streamerCensor { display: " + (_instance.StreamerModeEnabled && _instance.StreamerModeTriggered ? "none!important" : "block") + "}";

        public void ResetSettings()
        {
            _instance.StreamerModeEnabled = true;

            SaveSettings();
        }

        public void SetFromJObject(JObject j)
        {
            Globals.DebugWriteLine(@"[Func:Data\AppSettings.SetFromJObject]");
            var curSettings = j.ToObject<AppSettings>();
            if (curSettings == null) return;
            _instance.StreamerModeEnabled = curSettings.StreamerModeEnabled;
        }

        public void LoadFromFile()
        {
            Globals.DebugWriteLine(@"[Func:Data\AppSettings.LoadFromFile]");
            // Main settings
            if (!File.Exists(SettingsFile)) SaveSettings();
            else SetFromJObject(GeneralFuncs.LoadSettings(SettingsFile, GetJObject()));
            // Stylesheet
            LoadStylesheetFromFile();
        }

        #region STYLESHEET
        /// <summary>
        /// Swaps in a requested stylesheet, and loads styles from file.
        /// </summary>
        /// <param name="swapTo">Stylesheet name (without .json) to copy and load</param>
        public void SwapStylesheet(string swapTo)
        {
            File.Copy($"themes\\{swapTo.Replace(' ', '_')}.yaml", StylesheetFile, true);
            LoadStylesheetFromFile();
            _ = AppData.ActiveIJsRuntime.InvokeVoidAsync("location.reload");
        }

        /// <summary>
        /// Load stylesheet settings from stylesheet file.
        /// </summary>
        public void LoadStylesheetFromFile()
        {
            if (!File.Exists(StylesheetFile)) File.Copy("themes\\Default.yaml", StylesheetFile);
            // Load new stylesheet
            var desc = new DeserializerBuilder().WithNamingConvention(HyphenatedNamingConvention.Instance).Build();
            var attempts = 0;
            var text = File.ReadAllLines(StylesheetFile);
            while (attempts <= text.Length)
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(desc.Deserialize<object>(string.Join('\n', text))));
                    // Load default values, and copy in new values (Just in case some are missing)
                    _instance._stylesheet = _instance._defaultStylesheet;
                    if (dict != null)
                        foreach (var (key, val) in dict)
                        {
                            _instance._stylesheet[key] = val;
                        }

                    break;
                }
                catch (YamlDotNet.Core.SemanticErrorException e)
                {
                    // Check lines for common mistakes:
                    var line = e.End.Line - 1;
                    var currentLine = text[line];
                    var foundIssue = false;
                    // - Check for leading or trailing spaces:
                    if (currentLine[0] == ' ' || currentLine[^1] == ' ')
                    {
                        currentLine = currentLine.Trim();
                        foundIssue = true;
                    }

                    // - Check if there is a colon and a space (required for it to work), add space if no space.
                    if (currentLine.Contains(':') && currentLine[currentLine.IndexOf(':') + 1] != ' ')
                    {
                        currentLine = currentLine.Insert(currentLine.IndexOf(':') + 1, " ");
                        foundIssue = true;
                    }

                    if (!foundIssue)
                    {
                        // ReSharper disable once RedundantAssignment
                        attempts++;

                        // Comment out line:
                        currentLine = $"# -- ERROR -- # {currentLine}";
                    }

                    // Replace line and save new file
                    if (text[line] != currentLine)
                    {
                        text[line] = currentLine;
                        File.WriteAllLines(StylesheetFile, text);
                    }

                    if (attempts == text.Length)
                    {
                        // All 10 fix attempts have failed -> Copy in default file.
                        if (File.Exists("themes\\Default.yaml"))
                        {
                            // Check if haven't already copied default file into the stylesheet file
                            var curThemeHash = GeneralFuncs.GetFileMd5(StylesheetFile);
                            var defaultThemeHash = GeneralFuncs.GetFileMd5("themes\\Default.yaml");
                            if (curThemeHash != defaultThemeHash)
                            {
                                File.Copy("themes\\Default.yaml", StylesheetFile, true);
                                attempts = text.Length - 1; // One last attempt -> This time loads default settings now in that file.
                            }
                        }
                    }
                }
            }

            // Get name of current stylesheet
            GetCurrentStylesheet();
        }

        /// <summary>
        /// Returns a list of Stylesheets in the Stylesheet folder.
        /// </summary>
        /// <returns></returns>
        public string[] GetStyleList()
        {
            var list = Directory.GetFiles("themes");
            for (var i = 0; i < list.Length; i++)
            {
                var start = list[i].LastIndexOf("\\", StringComparison.Ordinal) + 1;
                var end = list[i].IndexOf(".yaml", StringComparison.OrdinalIgnoreCase);
                if (end == -1) end = 0;
                list[i] = list[i].Substring(start, end - start).Replace('_', ' ');
            }
            return list;
        }

        /// <summary>
        /// Gets the active stylesheet name
        /// </summary>
        public void GetCurrentStylesheet() => _instance._selectedStylesheet = _instance._stylesheet["name"];
        #endregion

        public JObject GetJObject() => JObject.FromObject(this);

        [JSInvokable]
        public void SaveSettings(bool mergeNewIntoOld = false) => GeneralFuncs.SaveSettings(SettingsFile, GetJObject(), mergeNewIntoOld);

        public JObject GetStylesJObject() => JObject.FromObject(_instance._stylesheet);

        #region SHORTCUTS
        public void CheckShortcuts()
        {
            Globals.DebugWriteLine(@"[Func:Data\AppSettings.CheckShortcuts]");
            _instance._desktopShortcut = File.Exists(Path.Join(Shortcut.Desktop, "TcNo Account Switcher.lnk"));
            _instance._startMenu = File.Exists(Path.Join(Shortcut.StartMenu, "TcNo Account Switcher.lnk")) && Directory.Exists(Path.Join(Shortcut.StartMenu, "Platforms"));
            _instance._trayStartup = Task.StartWithWindows_Enabled();
        }

        public void DesktopShortcut_Toggle()
        {
            Globals.DebugWriteLine(@"[Func:Data\Settings\Steam.DesktopShortcut_Toggle]");
            var s = new Shortcut();
            s.Shortcut_Switcher(Shortcut.Desktop);
            s.ToggleShortcut(!DesktopShortcut);
        }
        public void StartMenu_Toggle()
        {
            Globals.DebugWriteLine(@"[Func:Data\Settings\Steam.StartMenu_Toggle]");
            var platformsFolder = Path.Join(Shortcut.StartMenu, "Platforms");
            if (Directory.Exists(platformsFolder)) GeneralFuncs.RecursiveDelete(new DirectoryInfo(Path.Join(Shortcut.StartMenu, "Platforms")), false);
            else
            {
                Directory.CreateDirectory(platformsFolder);
                CreatePlatformShortcut(platformsFolder, "Steam", "steam");
                CreatePlatformShortcut(platformsFolder, "Origin", "origin");
                CreatePlatformShortcut(platformsFolder, "Ubisoft", "ubisoft");
            }

            var s = new Shortcut();
            s.Shortcut_Switcher(Shortcut.StartMenu);
            s.ToggleShortcut(!StartMenu, false);

            s.Shortcut_Tray(Shortcut.StartMenu);
            s.ToggleShortcut(!StartMenu, false);
        }
        public void Task_Toggle()
        {
            Globals.DebugWriteLine(@"[Func:Data\Settings\Steam.Task_Toggle]");
            Task.StartWithWindows_Toggle(!TrayStartup);
        }

        public void StartNow()
        {
            _ = Globals.StartTrayIfNotRunning() switch
            {
                "Started Tray" => GeneralInvocableFuncs.ShowToast("success", "Tray started!", renderTo: "toastarea"),
                "Already running" => GeneralInvocableFuncs.ShowToast("info", "Tray already open", renderTo: "toastarea"),
                "Tray users not found" => GeneralInvocableFuncs.ShowToast("error", "No tray users saved", renderTo: "toastarea"),
                _ => GeneralInvocableFuncs.ShowToast("error", "Could not start tray application!", renderTo: "toastarea")
            };
        }

        private void CreatePlatformShortcut(string folder, string platformName, string args)
        {
            var s = new Shortcut();
            s.Shortcut_Platform(folder, platformName, args);
            s.ToggleShortcut(!StartMenu, false);
        }
        #endregion
    }
}
