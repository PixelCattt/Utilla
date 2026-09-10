using HarmonyLib;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaTagger), nameof(GorillaTagger.Start))]
    internal static class PostInitializedPatch
    {
        public static void Postfix() => EventHandling.Events.Instance.TriggerGameInitialized();
    }
}