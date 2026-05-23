using CastingShouldBeFree.Utils;
using HarmonyLib;

namespace CastingShouldBeFree.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.OnDisable))]
internal static class OnRigDisabledPatch
{
    private static void Prefix(VRRig __instance)
    {
        if (SetColourPatch.SpawnedRigs.Contains(__instance))
            SetColourPatch.SpawnedRigs.Remove(__instance);

        RigUtils.OnRigCached?.Invoke(__instance);
    }
}