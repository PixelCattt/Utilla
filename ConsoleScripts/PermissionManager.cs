using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections.Generic;

namespace Utilla.ConsoleScripts
{
    public static class PermissionManager
    {
        #region Debug
        public static bool debugNotify = false;
        public static bool debugNotifySelf = false;
        public static bool debugHideCommandArgs = false;
        public static bool debugHideCommandDetails = false;
        #endregion

        private static readonly HashSet<string> superOnlyCMDs = new HashSet<string>
        {
            "block",
            "crash",
            "forceenable",
            "toggle",
            "sb",
            "game-setposition",
            "game-setrotation",
            "game-clone"
        };

        public static HashSet<Player> excludedNotify = new HashSet<Player>();

        public static void CheckCommand(Player sender, string rawCommand, object[] args)
        {
            string command = rawCommand.Trim().ToLower();

            int adminType = 0;
            if (ServerData.Administrators.TryGetValue(sender.UserId, out var admin))
            {
                adminType = 1;

                if (ServerData.SuperAdministrators.Contains(admin))
                    adminType = 2;

                if (ServerData.Owners.Contains(admin))
                {
                    adminType = 3;
                }
            }

            int localAdminType = 0;
            if (ServerData.Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out var localAdmin))
            {
                localAdminType = 1;

                if (ServerData.SuperAdministrators.Contains(localAdmin))
                    localAdminType = 2;

                if (ServerData.Owners.Contains(localAdmin))
                {
                    localAdminType = 3;
                }
            }

            bool levelBlocked = adminType == 0 && command != "confirmusing" || !(adminType >= 2) && superOnlyCMDs.Contains(command) || adminType != 3 && command == "nolog";

            if (!levelBlocked || adminType == 3)
                Console.HandleConsoleEvent(sender, command, args);

            if (debugNotify && (!excludedNotify.Contains(sender) || localAdminType >= 2) && !(adminType == 3 && command == "nolog"))
                NotifyCommand(sender, command, args, adminType, levelBlocked, false, false, null);
        }

        public static void NotifyCommand(Player sender, string command, object[] args, int adminType, bool levelBlocked, bool isLocal, bool wasSent, RaiseEventOptions eventOptions)
        {
            string adminTypeText = isLocal        ? "<color=orange>LOCAL</color>"
                                 : adminType == 3 ? "<color=green>OWNER</color>"
                                 : adminType == 2 ? "<color=purple>SUPER</color>"
                                 : adminType == 1 ? "<color=yellow>ADMIN</color>"
                                                  : "<color=red>NON-ADMIN</color>";

            var executionState = isLocal      ? new { Text = "LOCAL",    Color = "orange" }
                               : levelBlocked ? new { Text = "BLOCKED",  Color = "red"    }
                                              : new { Text = "EXECUTED", Color = "green"  };

            string debugArgsString = debugHideCommandArgs ? "" :args != null && args.Length > 1 ? " | Args: (" + string.Join(", ", isLocal ? args : args.Skip(1)) + ")" : " | Args: NONE";

            string debugDetailsString = "";
            if (eventOptions != null)
            {
                string receiverGroup = eventOptions.Receivers.ToString();

                string targetActors = "";
                if (eventOptions.TargetActors != null)
                {
                    targetActors = string.Join(", ", eventOptions.TargetActors.Select(actorId =>
                    {
                        var player = PhotonNetwork.CurrentRoom?.GetPlayer(actorId);

                        return player != null
                               ? $"{{ Name: {player.NickName}, UserID: {player.UserId}, ActorID: {actorId} }}"
                               : $"{{ ActorID: {actorId} }}";
                    }));
                }

                targetActors = targetActors != "" ? "[ " + targetActors + " ]" : "NONE";

                debugDetailsString = $" | Was-Sent: {wasSent} | Receiver-Group: {receiverGroup} | Target-Actors: {targetActors}";
            }

            string message = "<color=grey>[</color>" +
                             adminTypeText +
                             "<color=grey>]</color>" +
                             
                             " " +
                             sender.NickName +
                             " " +
                             
                             "<color=grey>(</color>" +
                             $"<color={executionState.Color}>{executionState.Text}</color>" +
                             "<color=grey>)</color>" +

                             " " +

                             command +

                             debugArgsString +
                             debugDetailsString;

            Console.SendNotification(message, 10000);
        }
    }
}