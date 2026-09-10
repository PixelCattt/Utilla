<div align="center">
  <h1>Utilla</h1>

  A Gorilla Tag Mod that allows you to Join Modded Rooms and lets Developers Add various Room-Related Things.

  <a href="https://github.com/PixelCattt/Utilla/compare/88ca464...2.0.0">
    <img src="https://img.shields.io/badge/view-changes-lime?style=for-the-badge"</img>
  </a>

  <a href="https://github.com/PixelCattt/Utilla/releases">
    <img src="https://img.shields.io/github/downloads/PixelCattt/Utilla/total?style=for-the-badge&label=Downloads%20Total&color=lime"</img>
  </a>

  <a href="https://github.com/PixelCattt/Utilla/releases/tag/v2.0.0">
    <img src="https://img.shields.io/github/downloads/PixelCattt/Utilla/v2.0.0/Utilla.dll?style=for-the-badge&label=Downloads%20v2.0.0&color=lime"</img>
  </a>
</div>

---

## Enabling your Mod
The `[ModdedGamemode]` must be applied to the plugin class of your mod to use the other attributes.
`[ModdedGamemodeJoin]` and `[ModdedGamemodeLeave]` can be applied to any void method within this class, with an optional string parameter containing the complete gamemode string.
These methods are called when a modded room is joined or left, respectively.

```cs
using System;
using BepInEx;
using Utilla;

namespace ExamplePlugin
{
    [BepInPlugin("org.example.plugin", "Example Plugin", "1.0.0")]
    [BepInDependency("com.pixelcatt.gtag.utilla", "2.0.0")] // Make sure to add Utilla as a dependency!
    [ModdedGamemode] // Enable callbacks in default modded gamemodes
    public class ExamplePlugin : BaseUnityPlugin
    {
        bool inAllowedRoom = false;

        private void Update()
        {
            if (inAllowedRoom)
            {
                // Do mod stuff
            }
        }

        [ModdedGamemodeJoin]
        private void RoomJoined(string gamemode)
        {
            // The room is modded. Enable mod stuff.
            inAllowedRoom = true;
        }

        [ModdedGamemodeLeave]
        private void RoomLeft(string gamemode)
        {
            // The room was left. Disable mod stuff.
            inAllowedRoom = false;
        }
    }
}
```

## Using the Initialization Event
Utilla provides an event that is triggered after Gorilla Tag initializes, use this if you are getting null reference errors on singleton objects such as `GorillaLocomotion.Player.Instance`

```cs
using System;
using BepInEx;
using Utilla;

namespace ExamplePlugin
{
    [BepInPlugin("org.example.plugin", "Example Plugin", "1.0.0")]
    [BepInDependency("com.pixelcatt.gtag.utilla", "2.0.0")] // Make sure to add Utilla as a dependency!
    public class ExamplePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            Utilla.EventHandling.Events.GameInitialized += GameInitialized;
        }

        private void GameInitialized(object sender, EventArgs e)
        {
            // Player instance has been created
            UnityEngine.Debug.Log(GorillaLocomotion.Player.Instance.jumpMultiplier);
        }
    }
}
```

## Adding Custom Gamemodes
A mod can register custom gamemodes through the `[ModdedGamemode]` attribute, and will appear next to the default gamemodes in game.

```cs
[BepInPlugin("org.example.plugin", "Example Plugin", "1.0.0")]
[BepInDependency("com.pixelcatt.gtag.utilla", "2.0.0")] // Make sure to add Utilla as a dependency!
[ModdedGamemode("mygamemodeid", "MY GAMEMODE", Models.BaseGamemode.Casual)] // Enable callbacks in a new casual gamemode called "MY GAMEMODE"
public class ExamplePlugin : BaseUnityPlugin {}
```

Additionally, a completely custom game manager can be used, by creating a class that inherits `GorillaGameManager`. Creating a custom gamemode requires advanced knowledge of Gorilla Tag's networking code. Currently, matchmaking does not work for fully custom gamemodes, but they can still be used through room codes.

```cs
[BepInPlugin("org.example.plugin", "Example Plugin", "1.0.0")]
[BepInDependency("com.pixelcatt.gtag.utilla", "2.0.0")] // Make sure to add Utilla as a dependency!
[ModdedGamemode("mygamemodeid", "MY GAMEMODE", typeof(MyGameManager))] // Enable callbacks in a new custom gamemode using MyGameManager
public class ExamplePlugin : BaseUnityPlugin {}

public class MyGameManager : GorillaGameManager
{
    // The game calls this when this is the gamemode for the room.
    public override void StartPlaying()
    {
        // Base needs to run for base GorillaGamanger functionality to run.
        base.StartPlaying();
    }

    // Called by game when you leave a room or gamemode is changed.
    public override void StopPlaying()
    {
        // Base needs to run for the game mode stop base GorillaGameManager functionality from running.
        base.StopPlaying();
    }

    // Called by the game when you leave a room after StopPlaying, use this for all important clean up and resetting the state of your game mode.
    public override void Reset()
    {
    }

    // Gamemode names must not have spaces and must not contain "CASUAL", "INFECTION", "HUNT", or "BATTLE".
    // Names that contain the name of other custom gamemodes will confilict.
    public override string GameModeName()
    {
        return "CUSTOM";
    }

    // GameModeType is an enum which is really an int, so any int value will work. 
    // Make sure to use a unique value not taken by other game modes.
    public override GameModeType GameType()
    {
        return (GameModeType)765;
    }

    public override int MyMatIndex(Player forPlayer)
    {
        return 3;
    }
}
```

## Using Room Join Events
Utilla provides events to broadcast when any room is joined. They are not recommended to enable and disable your mod, for that use attributes, described above. Utilla provides two events for room joining and leaving, `Utilla.Events.RoomJoined` and `Utilla.Events.RoomLeft` 

```cs
using System;
using BepInEx;
using Utilla;

namespace ExamplePlugin
{
    [BepInPlugin("org.example.plugin", "Example Plugin", "1.0.0")]
    [BepInDependency("com.pixelcatt.gtag.utilla", "2.0.0")] // Make sure to add Utilla as a dependency!
    public class ExamplePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            Utilla.EventHandling.Events.RoomJoined += RoomJoined;
            Utilla.EventHandling.Events.RoomLeft += RoomLeft;
        }

        private void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            UnityEngine.Debug.Log($"Private room: {e.isPrivate}, Gamemode: {e.Gamemode}");
        }

        private void RoomLeft(object sender, Events.RoomJoinedArgs e)
        {
            UnityEngine.Debug.Log($"Private room: {e.isPrivate}, Gamemode: {e.Gamemode}");
        }
    }
}
```

## Manually Joining Private Lobbies
If you'd like to join custom private lobbies with your mod, Utilla implements methods for that too.
```cs
Utilla.Utility.RoomUtils.JoinPrivateLobby()                  // Joins a private lobby with a random 6 character code
Utilla.Utility.RoomUtils.JoinPrivateLobby("TestLobby")       // Joins a private lobby with the code TestLobby
Utilla.Utility.RoomUtils.JoinPrivateLobby("TestLobby", true) // Joins a private casual lobby with the code TestLobby
```

## Using Custom Queues
If you'd like to use custom queues with your mod, Utilla implements methods for that as well.
```cs
Utilla.Utility.RoomUtils.JoinModdedLobby("TestQueue")       // Joins a random room in the queue TestQueue
Utilla.Utility.RoomUtils.JoinModdedLobby("TestQueue", true) // Joins a random casual room in the queue TestQueue
```
