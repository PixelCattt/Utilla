using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using GorillaNetworking;

namespace Utilla.ConsoleScripts
{
    public class ServerData : MonoBehaviour
    {
        #region Configuration
        // The Enpoint used for all API Requests.
        public const string ServerEndpoint = "https://gtag-api.pixelcatt.workers.dev";

        // The URL used for Downloading Console Assets.
        public const string AssetURL = "https://raw.githubusercontent.com/PixelCattt/Console/refs/heads/master/ServerData";

        // The Dictionary used to assign the admins only seen in your Mod.
        public static readonly Dictionary<string, string> LocalAdmins = new Dictionary<string, string>()
        {
            // { "Placeholder Admin UserID", "Placeholder Admin Name" },
        };
        #endregion

        #region Server Data Code
        private static ServerData instance;

        private static readonly List<string> DetectedModsLabelled = new List<string>();

        private static int LoadAttempts;

        private static float DataLoadTime = -1f;
        private static float ReloadTime = -1f;

        public static bool OutdatedVersion;

        private static bool GivenAdminMods;

        public void Awake()
        {
            instance = this;
            DataLoadTime = Time.time + 5f;

            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinRoom;

            NetworkSystem.Instance.OnPlayerJoined += UpdatePlayerCount;
            NetworkSystem.Instance.OnPlayerLeft += UpdatePlayerCount;
        }

        public void Update()
        {
            if (DataLoadTime > 0f && Time.time > DataLoadTime && GorillaComputer.instance.isConnectedToMaster)
            {
                DataLoadTime = Time.time + 5f;

                LoadAttempts++;
                if (LoadAttempts >= 3)
                {
                    Console.Log("Server Data could not be loaded");
                    DataLoadTime = -1f;
                    return;
                }

                Console.Log("Attempting to load Web Data");
                instance.StartCoroutine(RefreshServerData());
            }

            if (ReloadTime > 0f)
            {
                if (Time.time > ReloadTime)
                {
                    ReloadTime = Time.time + 30f;
                    instance.StartCoroutine(RefreshServerData());
                }
            }
            else
            {
                if (GorillaComputer.instance.isConnectedToMaster)
                    ReloadTime = Time.time + 5f;
            }

            if (!(Time.time > DataSyncDelay) && NetworkSystem.Instance.InRoom) return;
            if (NetworkSystem.Instance.InRoom && PhotonNetwork.PlayerList.Length != PlayerCount)
            {
                instance.StartCoroutine(PlayerDataSync(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CloudRegion));
                NetworkSystem.Instance.PlayerListOthers.ForEach(p => ShouldWeReport(p.GetPlayerRef()));
            }

            PlayerCount = NetworkSystem.Instance.InRoom ? PhotonNetwork.PlayerList.Length : -1;
        }

        private IEnumerator RefreshServerData()
        {
            yield return LoadServerData();
            yield return LoadReportData();
        }

        public static void OnJoinRoom()
        {
            instance.StartCoroutine(TelemetryRequest(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.NickName, PhotonNetwork.CloudRegion, PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.CurrentRoom.IsVisible, PhotonNetwork.PlayerList.Length, NetworkSystem.Instance.GameModeString));
            NetworkSystem.Instance.PlayerListOthers.ForEach(p => ShouldWeReport(p.GetPlayerRef()));
        }

        public static int PlayerCount;
        public static void UpdatePlayerCount(NetPlayer Player)
        {
            NetworkSystem.Instance.PlayerListOthers.ForEach(p => ShouldWeReport(p.GetPlayerRef()));
            PlayerCount = -1;
        }

        public static void ShouldWeReport(Player player)
        {
            if (!reportData.TryGetValue(player.UserId, out var entry))
                return;

            if (Administrators.ContainsKey(player.UserId))
                return;

            lock (entry.ReportedIn)
            {
                if (!entry.ReportedIn.Add(NetworkSystem.Instance.RoomName))
                    return;

                GorillaPlayerScoreboardLine.ReportPlayer(
                    player.UserId,
                    entry.ReportType,
                    player.NickName
                );

                if (Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                {
                    Console.SendNotification(
                        $"<color=grey>[</color><color=purple>ARS</color><color=grey>]</color> Player {player.NickName} (also known as {entry.Name}) has been reported for {entry.Reason}.",
                        10000
                    );
                }
            }
        }

        public static bool IsPlayerSteam(VRRig Player)
        {
            string concat = Player._playerOwnedCosmetics.Concat();
            int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

            return concat.Contains("S. FIRST LOGIN") ? true : concat.Contains("FIRST LOGIN") || customPropsCount >= 2;
        }

        public static string CleanString(string input, int maxLength = 12)
        {
            input = new string(Array.FindAll(input.ToCharArray(), Utils.IsASCIILetterOrDigit));

            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static string NoASCIIStringCheck(string input, int maxLength = 12)
        {
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static int VersionToNumber(string version)
        {
            string[] parts = version.Split('.');
            if (parts.Length != 3)
                return -1; // Version must be in 'major.minor.patch' format

            return int.Parse(parts[0]) * 100 + int.Parse(parts[1]) * 10 + int.Parse(parts[2]);
        }

        public static IEnumerator TelemetryRequest(string directory, string identity, string region, string userid, bool isPrivate, int playerCount, string gameMode)
        {
            UnityWebRequest request = new UnityWebRequest($"{ServerEndpoint}/telemetry", "POST");

            string json = JsonConvert.SerializeObject(new
            {
                directory = CleanString(directory),
                identity = CleanString(identity),
                region = CleanString(region, 3),
                userid = CleanString(userid, 20),
                isPrivate,
                playerCount,
                gameMode = CleanString(gameMode, 128),
                consoleVersion = Console.ConsoleVersion,
                menuName = Console.ModName,
                menuVersion = Console.ModVersion
            });

            byte[] raw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(raw);
            request.SetRequestHeader("Content-Type", "application/json");

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
        }

        public static readonly Dictionary<string, string> Administrators = new Dictionary<string, string>();
        public static readonly List<string> SuperAdministrators = new List<string>();
        public static readonly List<string> Owners = new List<string>();
        public static IEnumerator LoadServerData()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{ServerEndpoint}/serverdata"))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Console.Log($"Failed to load server data:\nError: {request.error}\nResult: {request.result}\nResponse Code: {request.responseCode}\nBody (if any): {request.downloadHandler?.text}");
                    yield break;
                }

                string json = request.downloadHandler.text;
                DataLoadTime = -1f;

                JObject data = JObject.Parse(json);

                if (data == null)
                {
                    Console.Log($"Failed to parse server data: JSON Response is empty or null");
                    yield break;
                }

                // Version Check
                string minimumVersion = (string)data["min-utilla-version"];
                string version = (string)data["utilla-version"];

                if (VersionToNumber(Console.ModVersion) < VersionToNumber(minimumVersion))
                {
                    if (!OutdatedVersion)
                    {
                        OutdatedVersion = true;
                        Console.Log("Version is severely outdated");
                        GorillaComputer.instance.GeneralFailureMessage("Please update your menu. For safety purposes, you have been blocked from joining rooms.");
                        if (NetworkSystem.Instance.InRoom)
                            NetworkSystem.Instance.ReturnToSinglePlayer();
                        Console.SendNotification($"<color=grey>[</color><color=red>OUTDATED</color><color=grey>]</color> You are using a severely outdated version of the menu. Please update your menu if available. For safety purposes, you have been blocked from joining rooms.", 10000);
                    }
                }
                else if (VersionToNumber(version) > VersionToNumber(Console.ModVersion))
                {
                    if (!OutdatedVersion)
                    {
                        OutdatedVersion = true;
                        Console.Log("Version is outdated");
                        Console.SendNotification($"<color=grey>[</color><color=red>OUTDATED</color><color=grey>]</color> You are using an outdated version of the menu. Please update to version {version}.", 10000);
                    }
                }

                string minConsoleVersion = (string)data["min-console-version"];
                if (VersionToNumber(Console.ConsoleVersion) >= VersionToNumber(minConsoleVersion))
                    yield return LoadAdminData();
                else
                    Console.Log("On extreme outdated version of Console, not loading Administrators");

                // Detected Mods
                JArray detectedMods = (JArray)data["detected-mods"];
                if (detectedMods == null || detectedMods.Count() == 0)
                    yield break;

                foreach (var detectedMod in detectedMods)
                {
                    string detectedModName = detectedMod.ToString();
                    if (DetectedModsLabelled.Contains(detectedModName)) continue;
                    
                    // Your Code for disabling detected Mods
                }
            }

            yield return null;
        }

        public static IEnumerator LoadAdminData()
        {
            using UnityWebRequest request = UnityWebRequest.Get($"{ServerEndpoint}/admindata");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                yield break;

            string json = request.downloadHandler.text;
            JObject adminData = JObject.Parse(json);

            if (adminData == null)
                yield break;

            // Admin Dictionary
            Administrators.Clear();

            JArray admins = (JArray)adminData["admins"];
            foreach (var admin in admins)
            {
                string name = admin["name"].ToString();
                string userId = admin["user-id"].ToString();
                Administrators[userId] = name;
            }

            // Super Admin Dictionary
            SuperAdministrators.Clear();

            JArray superAdmins = (JArray)adminData["super-admins"];
            foreach (var superAdmin in superAdmins)
                SuperAdministrators.Add(superAdmin.ToString());

            // Owner Dictionary
            Owners.Clear();

            JArray owners = (JArray)adminData["owners"];
            foreach (var owner in owners)
                Owners.Add(owner.ToString());

            // Add Local Admins
            foreach (var localAdmin in LocalAdmins)
            {
                Administrators[localAdmin.Key] = localAdmin.Value;

                SuperAdministrators.Add(localAdmin.Value);

                Owners.Add(localAdmin.Value);
            }

            // Spawn Admin-Panel
            if (!GivenAdminMods && PhotonNetwork.LocalPlayer.UserId != null && Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out var administrator))
            {
                GivenAdminMods = true;
                Console.SetupAdminPanel(administrator);
            }
        }

        public static readonly Dictionary<string, ReportEntry> reportData = new Dictionary<string, ReportEntry>();
        public class ReportEntry
        {
            public string Name;
            public string UserID;
            public GorillaPlayerLineButton.ButtonType ReportType;
            public string Reason;
            public HashSet<string> ReportedIn = new HashSet<string>();
        }

        private IEnumerator LoadReportData()
        {
            using UnityWebRequest request = UnityWebRequest.Get($"{ServerEndpoint}/reportdata");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                yield break;

            try
            {
                reportData.Clear();

                JObject json = JObject.Parse(request.downloadHandler.text);
                if (json == null)
                    yield break;

                JArray reports = (JArray)json["reports"];
                if (reports == null || reports.Count() == 0)
                    yield break;

                foreach (JToken report in reports)
                {
                    string name = report["name"]?.ToString() ?? "Unknown";
                    string userId = report["user-id"]?.ToString() ?? "Unknown";

                    if (!(report["report-type"]?.Value<int>() >= 1 && report["report-type"]?.Value<int>() <= 3))
                        continue;

                    ReportEntry entry = new ReportEntry
                    {
                        Name = name,
                        UserID = userId,
                        ReportType = report["report-type"]?.Value<int>() switch
                        {
                            1 => GorillaPlayerLineButton.ButtonType.Cheating,
                            2 => GorillaPlayerLineButton.ButtonType.Toxicity,
                            3 => GorillaPlayerLineButton.ButtonType.HateSpeech,
                            _ => GorillaPlayerLineButton.ButtonType.Cancel
                        },
                        Reason = report["reason"]?.ToString() ?? "No reason found",
                    };

                    if (reportData.TryGetValue(userId, out var existing))
                        entry.ReportedIn = existing.ReportedIn;

                    reportData[userId] = entry;
                }

                if (NetworkSystem.Instance.InRoom)
                    NetworkSystem.Instance.PlayerListOthers.ForEach(p => ShouldWeReport(p.GetPlayerRef()));
            }
            catch { }
        }

        private static float DataSyncDelay;
        public static IEnumerator PlayerDataSync(string directory, string region)
        {
            DataSyncDelay = Time.time + 5f;
            yield return new WaitForSeconds(5f);

            if (!NetworkSystem.Instance.InRoom)
                yield break;

            Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();

            foreach (Player identification in PhotonNetwork.PlayerList)
            {
                VRRig rig = Console.GetVRRigFromPlayer(identification) ?? VRRig.LocalRig;
                data.Add(identification.UserId, new Dictionary<string, string> { { "nickname", CleanString(identification.NickName) }, { "cosmetics", rig._playerOwnedCosmetics.Concat() }, { "color", $"{Math.Round(rig.playerColor.r * 255)} {Math.Round(rig.playerColor.g * 255)} {Math.Round(rig.playerColor.b * 255)}" }, { "platform", IsPlayerSteam(rig) ? "STEAM" : "QUEST" } });
            }

            UnityWebRequest request = new UnityWebRequest(ServerEndpoint + "/syncdata", "POST");

            string json = JsonConvert.SerializeObject(new
            {
                directory = CleanString(directory),
                region = CleanString(region, 3),
                data
            });

            byte[] raw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(raw);
            request.SetRequestHeader("Content-Type", "application/json");

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
        }
        #endregion
    }
}