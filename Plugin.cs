using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BackupLandingBurnEngines;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "b16setc.starshipfactory.backuplandingburnengines";
	public const string PluginName = "Backup Landing Burn Engines";
	public const string PluginVersion = "1.0.0";

	internal static ManualLogSource Log;

	private Harmony harmony;

	private void Awake()
	{
		Log = Logger;
		harmony = new Harmony(PluginGuid);
		harmony.PatchAll();
		Log.LogInfo($"{PluginName} v{PluginVersion} loaded -- {harmony.GetPatchedMethods().Count()} methods patched.");
	}

	private void OnDestroy()
	{
		harmony?.UnpatchSelf();
	}
}
