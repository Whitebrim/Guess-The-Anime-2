//
//  Updater.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


using System;
using System.Text;
using LunarConsolePluginInternal;
using UnityEditor;
using UnityEngine;

namespace LunarConsoleEditorInternal
{
    internal static class Updater
    {
        private static readonly string kMessageBoxTitle = "Lunar Mobile Console";

#pragma warning disable 0649
        private static LastCheckDate s_lastInstallCheckDate;
#pragma warning restore 0649

        public static void TryCheckForUpdates()
        {
#if !LUNAR_CONSOLE_UPDATER_DISABLED
            s_lastInstallCheckDate = new LastCheckDate(kPrefsKeyLastUpdateCheckDate);
            if (s_lastInstallCheckDate.CanCheckNow())
            {
                s_lastInstallCheckDate.SetToday();
                CheckForUpdates();
            }
            else
            {
                Log.d("Skipping update check. Next check: {0}", s_lastInstallCheckDate);
            }
#endif
        }

        public static void CheckForUpdates(bool silent = true)
        {
            var downloader = new LunarConsoleHttpClient(LunarConsoleConfig.fullVersion ? Constants.UpdateJsonURLFull : Constants.UpdateJsonURLFree);
            downloader.DownloadString(delegate(string response, Exception error)
            {
                if (error != null)
                {
                    Log.e(error, "Can't get updater info");
                    if (!silent)
                    {
                        Utils.ShowDialog(kMessageBoxTitle, "Update info is not available.\n\nTry again later.", "OK");
                    }

                    return;
                }

                UpdateInfo info;
                if (response != null && TryParseUpdateInfo(response, out info))
                {
                    if (IsShouldSkipVersion(info.version))
                    {
                        Log.d("User decided to skip version {0}", info.version);
                        return;
                    }

                    if (PluginVersionChecker.IsNewerVersion(info.version))
                    {
                        var message = new StringBuilder();
                        message.AppendFormat("A new version {0} is available!\n\n", info.version);
                        if (info.message != null && info.message.Length > 0)
                        {
                            message.Append(info.message);
                            message.AppendLine();
                        }

                        Utils.ShowDialog(kMessageBoxTitle, message.ToString(),
                            new DialogButton("Details...", delegate(string obj)
                            {
                                Application.OpenURL(info.url);
                                LunarConsoleEditorAnalytics.TrackEvent("Version", "updater_details");
                            }),
                            new DialogButton("Remind me later",
                                delegate(string obj) { LunarConsoleEditorAnalytics.TrackEvent("Version", "updater_later"); }),
                            new DialogButton("Skip this version", delegate(string obj)
                            {
                                SetShouldSkipVersion(info.version);
                                LunarConsoleEditorAnalytics.TrackEvent("Version", "updater_skip");
                            })
                        );
                    }
                    else
                    {
                        if (!silent)
                        {
                            Utils.ShowMessageDialog(kMessageBoxTitle, "Everything is up to date");
                        }

                        Debug.Log("Everything is up to date");
                    }
                }
                else
                {
                    Log.e("Unable to parse response: '{0}'", response);
                    if (!silent)
                    {
                        Utils.ShowDialog(kMessageBoxTitle, "Update info is not available.\n\nTry again later.", "OK");
                    }
                }
            });
        }

        public static void Reset()
        {
            s_lastInstallCheckDate.Reset();
        }

        private static bool TryParseUpdateInfo(string response, out UpdateInfo updateInfo)
        {
            try
            {
                JSONNode node = JSON.Parse(response);

                JSONClass root = node.AsObject;
                if (root != null)
                {
                    return TryParseUpdateInfo(root, out updateInfo);
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Unable to parse plugin update info");
            }

            updateInfo = default;
            return false;
        }

        private static bool TryParseUpdateInfo(JSONClass response, out UpdateInfo updateInfo)
        {
            string status = response["status"];
            if (status != "OK")
            {
                Log.e("Unexpected response 'status': {0}", status);

                updateInfo = default;
                return false;
            }

            string version = response["version"];
            if (version == null)
            {
                Log.e("Missing response 'version' string");

                updateInfo = default;
                return false;
            }

            string url = response["url"];
            if (url == null)
            {
                Log.e("Missing response 'url' string");

                updateInfo = default;
                return false;
            }

            string message = response["message"];
            if (message == null)
            {
                Log.e("Missing response 'message'");

                updateInfo = default;
                return false;
            }

            updateInfo = new UpdateInfo(version, url, message);
            return true;
        }

        private struct UpdateInfo
        {
            public readonly string version;
            public readonly string url;
            public readonly string message;

            public UpdateInfo(string version, string url, string message)
            {
                this.version = version;
                this.url = url;
                this.message = message;
            }

            public override string ToString()
            {
                return string.Format("UpdateInfo:\n\tversion={0}\n\turl={1}\n\tmessage={2}", version, url, message);
            }
        }

        private struct VersionInfo
        {
            public readonly int major;
            public readonly int minor;
            public readonly int maintenance;

            public VersionInfo(int major, int minor, int maintenace)
            {
                this.major = major;
                this.minor = minor;
                maintenance = maintenace;
            }

            public static bool TryParse(string value, out VersionInfo info)
            {
                if (value.Length > 1 && value.EndsWith("b"))
                {
                    value = value.Substring(0, value.Length - 1);
                }

                if (string.IsNullOrEmpty(value))
                {
                    info = default;
                    return false;
                }

                string[] tokens = value.Split('.');
                if (tokens.Length != 3)
                {
                    info = default;
                    return false;
                }

                int major;
                if (!int.TryParse(tokens[0], out major))
                {
                    info = default;
                    return false;
                }

                int minor;
                if (!int.TryParse(tokens[1], out minor))
                {
                    info = default;
                    return false;
                }

                int maintenance;
                if (!int.TryParse(tokens[2], out maintenance))
                {
                    info = default;
                    return false;
                }

                info = new VersionInfo(major, minor, maintenance);
                return true;
            }

            public int CompareTo(ref VersionInfo other)
            {
                int majorCmp = major.CompareTo(other.major);
                if (majorCmp != 0)
                {
                    return majorCmp;
                }

                int minorCmd = minor.CompareTo(other.minor);
                if (minorCmd != 0)
                {
                    return minorCmd;
                }

                return maintenance.CompareTo(other.maintenance);
            }
        }

        private static class PluginVersionChecker
        {
            public static bool IsNewerVersion(string version)
            {
                VersionInfo newVersion;
                if (!VersionInfo.TryParse(version, out newVersion))
                {
                    throw new ArgumentException("Bad version number: " + version);
                }

                string oldVersionStr = ResolvePluginVersion();
                VersionInfo oldVersion;

                if (oldVersionStr != null && VersionInfo.TryParse(oldVersionStr, out oldVersion))
                {
                    return oldVersion.CompareTo(ref newVersion) < 0;
                }

                return true;
            }

            private static string ResolvePluginVersion()
            {
                return Constants.Version;
            }
        }

        #region Last date checker

        private class LastCheckDate
        {
            private readonly string m_key;

            public LastCheckDate(string key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }

                m_key = key;
            }

            public bool CanCheckNow()
            {
                DateTime lastCheckDate = GetDate();
                var totalDays = (int)(DateTime.Now - lastCheckDate).TotalDays;
                return totalDays > 0;
            }

            public void SetToday()
            {
                SetDate(DateTime.Now);
            }

            public void SetNever()
            {
                SetDate(DateTime.MaxValue);
            }

            public void Reset()
            {
                EditorPrefs.DeleteKey(m_key);
            }

            private void SetDate(DateTime date)
            {
                EditorPrefs.SetString(m_key, date.ToShortDateString());
            }

            private DateTime GetDate()
            {
                string value = EditorPrefs.GetString(m_key);

                DateTime date;
                if (value != null && DateTime.TryParse(value, out date))
                {
                    return date;
                }

                return DateTime.MinValue;
            }

            public override string ToString()
            {
                return GetDate().ToString();
            }
        }

        #endregion

        #region Preferences

        private static readonly string kPrefsKeySkipVersion = Constants.EditorPrefsKeyBase + ".SkipVersion";
        private static readonly string kPrefsKeyLastUpdateCheckDate = Constants.EditorPrefsKeyBase + ".LastUpdateCheckDate";

        private static bool IsShouldSkipVersion(string version)
        {
            string skipVersion = EditorPrefs.GetString(kPrefsKeySkipVersion);
            return skipVersion == version;
        }

        private static void SetShouldSkipVersion(string version)
        {
            EditorPrefs.SetString(kPrefsKeySkipVersion, version);
        }

        private static bool GetPrefsFlag(string key)
        {
            return EditorPrefs.GetInt(key, 0) != 0;
        }

        private static void SetPrefsFlag(string key, bool flag)
        {
            EditorPrefs.SetInt(key, flag ? 1 : 0);
        }

        #endregion
    }
}