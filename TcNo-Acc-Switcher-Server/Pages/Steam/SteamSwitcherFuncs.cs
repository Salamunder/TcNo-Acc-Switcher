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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TcNo_Acc_Switcher_Server.Pages.General;
using TcNo_Acc_Switcher_Globals;

using Steamuser = TcNo_Acc_Switcher_Server.Pages.Steam.Index.Steamuser;

namespace TcNo_Acc_Switcher_Server.Pages.Steam
{
    public class SteamSwitcherFuncs
    {
        private static readonly Data.Settings.Steam Steam = Data.Settings.Steam.Instance;

        #region STEAM_SWITCHER_MAIN
        /// <summary>
        /// Main function for Steam Account Switcher. Run on load.
        /// Collects accounts from Steam's loginusers.vdf
        /// Prepares images and VAC/Limited status
        /// Prepares HTML Elements string for insertion into the account switcher GUI.
        /// </summary>
        /// <param name="jsRuntime"></param>
        /// <returns>Whether account loading is successful, or a path reset is needed (invalid dir saved)</returns>
        public static async ValueTask<bool> LoadProfiles(IJSRuntime jsRuntime)
        {
            // Checks if Steam path set properly, and can load.
            Steam.LoadFromFile();
            if (Steam.LoginUsersVdf() == "RESET_PATH") return false;
            
            var userAccounts = GetSteamUsers(Steam.LoginUsersVdf()); 
            var vacStatusList = new List<VacStatus>();
            var loadedVacCache = LoadVacInfo(ref vacStatusList);

            foreach (var ua in userAccounts)
            {
                var va = new VacStatus();
                if (loadedVacCache)
                {
                    PrepareProfileImage(ua); // Just get images
                    foreach (var vsi in vacStatusList.Where(vsi => vsi.SteamId == ua.SteamId))
                    {
                        va = vsi;
                        break;
                    }
                }
                else
                {
                    va = PrepareProfileImage(ua); // Get VAC status as well
                    va.SteamId = ua.SteamId;
                    vacStatusList.Add(va);
                }
                
                var extraClasses = (va.Vac ? " status_vac" : "") + (va.Ltd ? " status_limited" : "");

                var element =
                    $"<input type=\"radio\" id=\"{ua.AccName}\" class=\"acc\" name=\"accounts\" Username=\"{ua.AccName}\" SteamId64=\"{ua.SteamId}\" Line1=\"{ua.AccName}\" Line2=\"{ua.Name}\" Line3=\"{ua.LastLogin}\" ExtraClasses=\"{extraClasses}\" onchange=\"SelectedItemChanged()\" />\r\n" +
                    $"<label for=\"{ua.AccName}\" class=\"acc {extraClasses}\">\r\n" +
                    $"<img class=\"{extraClasses}\" src=\"{ua.ImgUrl}\" draggable=\"false\" />\r\n" +
                    $"<p>{ua.AccName}</p>\r\n" +
                    $"<h6>{ua.Name}</h6>\r\n" +
                    $"<p>{UnixTimeStampToDateTime(ua.LastLogin)}</p>\r\n</label>";

                await jsRuntime.InvokeVoidAsync("jQueryAppend", new object[] { "#acc_list", element });
            }

            SaveVacInfo(vacStatusList);
            await jsRuntime.InvokeVoidAsync("initContextMenu");

            return true;
        }

        /// <summary>
        /// Takes loginusers.vdf and iterates through each account, loading details into output Steamuser list.
        /// </summary>
        /// <param name="loginUserPath">loginusers.vdf path</param>
        /// <returns>List of Steamuser classes, from loginusers.vdf</returns>
        public static List<Steamuser> GetSteamUsers(string loginUserPath)
        {
            var userAccounts = new List<Steamuser>();

            userAccounts.Clear();
            Directory.CreateDirectory("wwwroot/img/profiles");
            try
            {
                var loginUsersVToken = VdfConvert.Deserialize(File.ReadAllText(loginUserPath));
                var loginUsers = new JObject() { loginUsersVToken.ToJson() };

                if (loginUsers["users"] != null)
                {
                    userAccounts.AddRange(from user in loginUsers["users"]
                    let steamId = user.ToObject<JProperty>()?.Name
                    where !string.IsNullOrEmpty(steamId) && !string.IsNullOrEmpty(user.First?["AccountName"]?.ToString())
                    select new Steamuser()
                    {
                        Name = user.First?["PersonaName"]?.ToString(),
                        AccName = user.First?["AccountName"]?.ToString(),
                        SteamId = steamId,
                        ImgUrl = "img/QuestionMark.jpg",
                        LastLogin = user.First?["Timestamp"]?.ToString(),
                        OfflineMode = (!string.IsNullOrEmpty(user.First?["WantsOfflineMode"]?.ToString()) ? user.First?["WantsOfflineMode"]?.ToString() : "0")
                    });
                }
            }
            catch (FileNotFoundException ex)
            {
                //MessageBox.Show(Strings.ErrLoginusersNonExist, Strings.ErrLoginusersNonExistHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                //MessageBox.Show($"{Strings.ErrInformation} {ex}", Strings.ErrLoginusersNonExistHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(2);
            }

            return userAccounts;
        }

        /// <summary>
        /// Deletes cached VAC/Limited status file
        /// </summary>
        /// <returns>Whether deletion successful</returns>
        public static bool DeleteVacCacheFile()
        {
            if (!File.Exists(Steam.VacCacheFile)) return true;
            File.Delete(Steam.VacCacheFile);
            return true;
        }

        /// <summary>
        /// Loads List of VacStatus classes into input cache from file, or deletes if outdated.
        /// </summary>
        /// <param name="vsl">Reference to List of VacStatus</param>
        /// <returns>Whether file was loaded. False if deleted ~ failed to load.</returns>
        public static bool LoadVacInfo(ref List<VacStatus> vsl)
        {
            GeneralFuncs.DeletedOutdatedFile(Steam.VacCacheFile);
            if (!File.Exists(Steam.VacCacheFile)) return false;
            vsl = JsonConvert.DeserializeObject<List<VacStatus>>(File.ReadAllText(Steam.VacCacheFile));
            return true;
        }

        /// <summary>
        /// Saves List of VacStatus into cache file as JSON.
        /// </summary>
        public static void SaveVacInfo(List<VacStatus> vsList) => File.WriteAllText(Steam.VacCacheFile, JsonConvert.SerializeObject(vsList));

        /// <summary>
        /// Converts Unix Timestamp string to DateTime
        /// </summary>
        public static string UnixTimeStampToDateTime(string stringUnixTimeStamp)
        {
            double.TryParse(stringUnixTimeStamp, out var unixTimeStamp);
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Class for storing SteamID, VAC status and Limited status.
        /// </summary>
        public class VacStatus
        {
            [JsonProperty("SteamID", Order = 0)] public string SteamId { get; set; }
            [JsonProperty("Vac", Order = 1)] public bool Vac { get; set; }
            [JsonProperty("Ltd", Order = 2)] public bool Ltd { get; set; }
        }

        /// <summary>
        /// Deletes outdated/invalid profile images (If they exist)
        /// Then downloads a new copy from Steam
        /// </summary>
        /// <param name="su"></param>
        /// <returns></returns>
        private static VacStatus PrepareProfileImage(Steamuser su)
        {
            Directory.CreateDirectory(Steam.SteamImagePath);
            var dlDir = $"{Steam.SteamImagePath}{su.SteamId}.jpg";
            // Delete outdated file, if it exists
            GeneralFuncs.DeletedOutdatedFile(dlDir, Steam.ImageExpiryTime);
            // ... & invalid files
            GeneralFuncs.DeletedInvalidImage(dlDir);

            var vs = new VacStatus();
            
            // Download new copy of the file
            if (!File.Exists(dlDir))
            {
                var imageUrl = GetUserImageUrl(ref vs, su);
                if (string.IsNullOrEmpty(imageUrl)) return vs;
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(new Uri(imageUrl), dlDir);
                    }
                    su.ImgUrl = $"{Steam.SteamImagePathHtml}{su.SteamId}.jpg";
                }
                catch (WebException ex)
                {
                    if (ex.HResult != -2146233079) // Ignore currently in use error, for when program is still writing to file.
                    {
                        su.ImgUrl = "img/QuestionMark.jpg";
                        Console.WriteLine("ERROR: Could not connect and download Steam profile's image from Steam servers.\nCheck your internet connection.\n\nDetails: " + ex);
                        //MessageBox.Show($"{Strings.ErrImageDownloadFail} {ex}", Strings.ErrProfileImageDlFail, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                su.ImgUrl = $"{Steam.SteamImagePathHtml}{su.SteamId}.jpg";
                var profileXml = new XmlDocument();
                var cachedFile = $"profilecache/{su.SteamId}.xml";
                profileXml.Load((File.Exists(cachedFile))? cachedFile : $"https://steamcommunity.com/profiles/{su.SteamId}?xml=1");
                if (!File.Exists(cachedFile)) profileXml.Save(cachedFile);

                if (profileXml.DocumentElement == null ||
                    profileXml.DocumentElement.SelectNodes("/profile/privacyMessage")?.Count != 0) return vs;

                    XmlGetVacLimitedStatus(ref vs, profileXml);
            }

            return vs;
        }

        /// <summary>
        /// Read's Steam's public XML data on user (& Caches).
        /// Gets user's image URL and checks for VAC bans, and limited account.
        /// </summary>
        /// <param name="vs">Reference to VacStatus variable</param>
        /// <param name="su">Steamuser to be checked</param>
        /// <returns>User's image URL for downloading</returns>
        private static string GetUserImageUrl(ref VacStatus vs, Steamuser su)
        {
            var imageUrl = "";
            var profileXml = new XmlDocument();
            try
            {
                profileXml.Load($"https://steamcommunity.com/profiles/{su.SteamId}?xml=1");
                // Cache for later
                Directory.CreateDirectory("profilecache");
                profileXml.Save($"profilecache/{su.SteamId}.xml");

                if (profileXml.DocumentElement != null && profileXml.DocumentElement.SelectNodes("/profile/privacyMessage")?.Count == 0) // Fix for accounts that haven't set up their Community Profile
                {
                    try
                    {
                        imageUrl = profileXml.DocumentElement.SelectNodes("/profile/avatarFull")[0].InnerText;
                        XmlGetVacLimitedStatus(ref vs, profileXml);
                    }
                    catch (NullReferenceException) // User has not set up their account, or does not have an image.
                    {
                        imageUrl = "";
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: Is this necessary? Catch errors from the whole project later in crash handler?
                imageUrl = "";
                Directory.CreateDirectory("Errors");
                using (var sw = File.AppendText($"Errors\\AccSwitcher-Error-{DateTime.Now:dd-MM-yy_hh-mm-ss.fff}.txt"))
                {
                    sw.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + "\t" + /*Strings.ErrUnhandledCrash +*/ "Unhandled error crash: " + e + Environment.NewLine + Environment.NewLine);
                }
                using (var sw = File.AppendText($"Errors\\AccSwitcher-Error-{DateTime.Now:dd-MM-yy_hh-mm-ss.fff}.txt"))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(profileXml));
                }
            }
            return imageUrl;
        }

        /// <summary>
        /// Gets VAC & Limited status from input XML Document.
        /// </summary>
        /// <param name="vs">Reference to VacStatus object to be edited</param>
        /// <param name="profileXml">User's profile XML string</param>
        private static void XmlGetVacLimitedStatus(ref VacStatus vs, XmlDocument profileXml)
        {
            if (profileXml.DocumentElement == null) return;
            try
            {
                if (profileXml.DocumentElement.SelectNodes("/profile/vacBanned")?[0] != null)
                    vs.Vac = profileXml.DocumentElement.SelectNodes("/profile/vacBanned")?[0].InnerText == "1";
                if (profileXml.DocumentElement.SelectNodes("/profile/isLimitedAccount")?[0] != null)
                    vs.Ltd = profileXml.DocumentElement.SelectNodes("/profile/isLimitedAccount")?[0].InnerText == "1";
            }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Restart Steam with a new account selected. Leave args empty to log into a new account.
        /// </summary>
        /// <param name="steamId">(Optional) User's SteamID</param>
        /// <param name="accName">(Optional) User's login username</param>
        /// <param name="autoStartSteam">(Optional) Whether Steam should start after switching [Default: true]</param>
        public static void SwapSteamAccounts(string steamId = "", string accName = "", bool autoStartSteam = true)
        {
            if (steamId != "" && !VerifySteamId(steamId))
            {
                // await JsRuntime.InvokeVoidAsync("createAlert", "Invalid SteamID" + steamid);
                return;
            }

            CloseSteam();
            UpdateLoginUsers(steamId, accName);

            if (!autoStartSteam) return;
            if (Steam.Admin)
                Process.Start(Steam.SteamExe());
            else
                Process.Start(new ProcessStartInfo("explorer.exe", Steam.SteamExe()));
        }
        
        /// <summary>
        /// Verify whether input Steam64ID is valid or not
        /// </summary>
        public static bool VerifySteamId(string steamId)
        {
            const long steamIdMin = 0x0110000100000001;
            const long steamIdMax = 0x01100001FFFFFFFF;
            if (!IsDigitsOnly(steamId) || steamId.Length != 17) return false;
            // Size check: https://stackoverflow.com/questions/33933705/steamid64-minimum-and-maximum-length#40810076
            var steamIdVal = double.Parse(steamId);
            return steamIdVal > steamIdMin && steamIdVal < steamIdMax;
        }
        private static bool IsDigitsOnly(string str) => str.All(c => c >= '0' && c <= '9');
        #endregion

        #region STEAM_MANAGEMENT
        /// <summary>
        /// Kills Steam processes when run via cmd.exe
        /// </summary>
        public static void CloseSteam()
        {
            // This is what Administrator permissions are required for.
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C TASKKILL /F /T /IM steam*"
            };
            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Updates loginusers and registry to select an account as "most recent"
        /// </summary>
        /// <param name="selectedSteamId"></param>
        /// <param name="accName"></param>
        public static void UpdateLoginUsers(string selectedSteamId, string accName = "")
        {
            var userAccounts = SteamSwitcherFuncs.GetSteamUsers(Steam.LoginUsersVdf());
            // -----------------------------------
            // ----- Manage "loginusers.vdf" -----
            // -----------------------------------
            var tempFile = Steam.LoginUsersVdf() + "_temp";
            File.Delete(tempFile);

            // MostRec is "00" by default, just update the one that matches SteamID.
            userAccounts.Single(x => x.SteamId == selectedSteamId).MostRec = "1";
            
            // Save updated loginusers.vdf
            SaveSteamUsersIntoVdf(userAccounts);

            var user = userAccounts.Single(x => x.SteamId == selectedSteamId);
            // -----------------------------------
            // --------- Manage registry ---------
            // -----------------------------------
            /*
            ------------ Structure ------------
            HKEY_CURRENT_USER\Software\Valve\Steam\
                --> AutoLoginUser = username
                --> RememberPassword = 1
            */
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Valve\Steam");
            key.SetValue("AutoLoginUser", user.AccName); // Account name is not set when changing user accounts from launch arguments (part of the viewmodel). -- Can be "" if no account
            key.SetValue("RememberPassword", 1);

            // -----------------------------------
            // ------Update Tray users list ------
            // -----------------------------------
            var trayUsers = TrayUser.ReadTrayUsers();
            TrayUser.AddUser(ref trayUsers, "Steam", new TrayUser() { Arg = "+s:" + user.SteamId, Name = Steam.TrayAccName ? user.AccName : user.Name });
            TrayUser.SaveUsers(trayUsers);
        }

        /// <summary>
        /// Save updated list of Steamuser into loginusers.vdf, in vdf format.
        /// </summary>
        /// <param name="userAccounts">List of Steamuser to save into loginusers.vdf</param>
        /// <param name="steamPath">Path of loginusers.vdf</param>
        public static void SaveSteamUsersIntoVdf(List<Steamuser> userAccounts)
        {
            // Convert list to JObject list, ready to save into vdf.
            var outJObject = new JObject();
            foreach (var ua in userAccounts)
            {
                outJObject[ua.SteamId] = (JObject)JToken.FromObject(ua);
            }

            // Write changes to files.
            var tempFile = Steam.LoginUsersVdf() + "_temp";
            File.WriteAllText(tempFile, @"""users""" + Environment.NewLine + outJObject.ToVdf());
            File.Replace(tempFile, Steam.LoginUsersVdf(), Steam.LoginUsersVdf() + "_last");
        }

        /// <summary>
        /// Clears backups of forgotten accounts
        /// </summary>
        public static async void ClearForgotten(IJSRuntime js = null)
        {
            await GeneralInvocableFuncs.ShowModal(js, "confirm:ClearSteamBackups:" + "Are you sure you want to clear backups of forgotten accounts?".Replace(' ', '_'));
            // Confirmed in GeneralInvocableFuncs.GiConfirmAction for rest of function
        }
        /// <summary>
        /// Fires after being confirmed by above function, and actually performs task.
        /// </summary>
        public static void ClearForgotten_Confirmed()
        {
            var legacyBackupPath = Path.Combine(Steam.FolderPath, "config\\\\TcNo-Acc-Switcher-Backups\\\\");
            if (Directory.Exists(legacyBackupPath))
                    Directory.Delete(legacyBackupPath, true);

            // Handle new method:
            if (File.Exists("SteamForgotten.json")) File.Delete("SteamForgotten.json");
        }

        /// <summary>
        /// Clears images folder of contents, to re-download them on next load.
        /// </summary>
        /// <returns>Whether files were deleted or not</returns>
        public static async void ClearImages(IJSRuntime js = null)
        {
            if (!Directory.Exists(Steam.SteamImagePath))
            {
                await GeneralInvocableFuncs.ShowToast(js, "error", "Could not clear images", "Error", "toastarea"); 
            }
            foreach (var file in Directory.GetFiles(Steam.SteamImagePath))
            {
                File.Delete(file);
            }
            await GeneralInvocableFuncs.ShowToast(js, "success", "Cleared images", renderTo: "toastarea");
        }
        #endregion

        #region STEAM_SETTINGS
        /* OTHER FUNCTIONS*/
        // STEAM SPECIFIC -- Move to a new file in the future.

        /// <summary>
        /// Used in JS. Gets whether forget account is enabled (Whether to NOT show prompt, or show it).
        /// </summary>
        /// <returns></returns>
        [JSInvokable]
        public static Task<bool> GetSteamForgetAcc() => Task.FromResult(Steam.ForgetAccountEnabled);

        /// <summary>
        /// Creates a backup of the LoginUsers.vdf file
        /// </summary>
        /// <param name="backupName">(Optional) Name for the backup file (including .vdf)</param>
        public static void BackupLoginUsers(string backupName = "")
        {
            var backup = Path.Combine(Steam.FolderPath, $"config\\TcNo-Acc-Switcher-Backups\\");
            var backupFileName = backupName != "" ? backupName : $"loginusers-{DateTime.Now:dd-MM-yyyy_HH-mm-ss.fff}.vdf";

            Directory.CreateDirectory(backup);
            File.Copy(Steam.LoginUsersVdf(), Path.Combine(backup, backupFileName), true);
        }

        /// <summary>
        /// Purely a class used for backing up forgotten Steam users, used in ForgetAccount() and TODO: RestoreAccount()
        /// </summary>
        public class ForgottenSteamuser
        {
            [JsonProperty("SteamId", Order = 0)] public string SteamId { get; set; }
            [JsonProperty("SteamUser", Order = 1)] public Steamuser Steamuser { get; set; }
        }

        /// <summary>
        /// Remove requested account from loginusers.vdf
        /// </summary>
        /// <param name="steamId">SteamId of account to be removed</param>
        public static bool ForgetAccount(string steamId)
        {
            // Load and remove account that matches SteamID above.
            var userAccounts = GetSteamUsers(Steam.LoginUsersVdf());
            var forgottenUser = userAccounts.Where(x => x.SteamId == steamId)?.First(); // Get the removed user to save into restore file
            userAccounts.RemoveAll(x => x.SteamId == steamId);

            // Instead of backing up EVERY TIME like the previous version (if used now, it's updated to: BackupLoginUsers(settings: settings);)
            // Rather just save the users in a file, for better restoring later if necessary.
            var fFileContents = File.Exists(Steam.ForgottenFile) ? File.ReadAllText(Steam.ForgottenFile) : "";
            var fUsers = fFileContents == "" ? new List<ForgottenSteamuser>() : JsonConvert.DeserializeObject<List<ForgottenSteamuser>>(fFileContents);
            if (fUsers.All(x => x.SteamId != forgottenUser.SteamId)) fUsers.Add(new ForgottenSteamuser() { SteamId = forgottenUser.SteamId, Steamuser = forgottenUser }); // Add to list if user with SteamID doesn't exist in it.
            File.WriteAllText(Steam.ForgottenFile, JsonConvert.SerializeObject(fUsers));

            // Save updated loginusers.vdf file
            SaveSteamUsersIntoVdf(userAccounts);
            return true;
        }

        public static bool RestoreAccounts(string[] requestedSteamIds)
        {
            if (!File.Exists(Steam.ForgottenFile)) return false;
            var forgottenAccounts = JsonConvert.DeserializeObject<List<ForgottenSteamuser>>(File.ReadAllText(Steam.ForgottenFile));

            // Load existing accounts
            var userAccounts = GetSteamUsers(Steam.LoginUsersVdf());
            // Create list of existing SteamIds (as to not add duplicates)
            var existingIds = userAccounts.Select(ua => ua.SteamId).ToList();

            var selectedForgottenPossibleDuplicates = forgottenAccounts.Where(fsu => requestedSteamIds.Contains(fsu.SteamId)).ToList(); // To remove items in Loginusers from forgotten list
            var selectedForgotten = selectedForgottenPossibleDuplicates.Where(fsu => !existingIds.Contains(fsu.SteamId)).ToList(); // To add new items to Loginusers (So there's no duplicates)
            foreach (var fa in selectedForgotten)
            {
                var su = fa.Steamuser;
                su.SteamId = fa.SteamId;
                userAccounts.Add(su);
            }
            
            // Save updated loginusers.vdf file
            SaveSteamUsersIntoVdf(userAccounts);

            // Update & Save SteamForgotten.json
            forgottenAccounts = forgottenAccounts.Except(selectedForgottenPossibleDuplicates).ToList<ForgottenSteamuser>();
            File.WriteAllText(Steam.ForgottenFile, JsonConvert.SerializeObject(forgottenAccounts));
            return true;
        }

        /// <summary>
        /// Only runs ForgetAccount, but allows Javascript to wait for it's completion before refreshing, instead of just doing it instantly >> Not showing proper results.
        /// </summary>
        /// <param name="steamId">SteamId of account to be removed</param>
        /// <returns>true</returns>
        [JSInvokable]
        public static Task<bool> ForgetAccountJs(string steamId)
        {
            return Task.FromResult(ForgetAccount(steamId));
        }


        #endregion
    }
}
