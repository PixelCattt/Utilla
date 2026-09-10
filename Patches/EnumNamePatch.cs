using GorillaGameModes;
using HarmonyLib;
using System;
using System.Reflection;
using Utilla.Utility;

namespace Utilla.Patches
{
    [HarmonyPatch]
    public class EnumNamePatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(EnumUtilExt)
                .GetMethod(nameof(EnumUtilExt.GetName), BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(GameModeType));
        }

        public static bool Prefix(GameModeType e, ref string __result)
        {
            if (!Enum.IsDefined(typeof(GameModeType), (int)e))
            {
                __result = GameModeUtils.GetGameModeInstance(e).GameTypeName();
                return false;
            }

            return true;
        }
    }
}