using System.Runtime.CompilerServices;
using BumbleRatGames.StarshipFactory;
using BumbleRatGames.StarshipFactory.GameActions;
using HarmonyLib;

namespace BackupLandingBurnEngines;

[HarmonyPatch(typeof(FlightController), nameof(FlightController.InitFlightVariables))]
internal static class InitFlightVariables_Patch
{
	private static void Postfix(VehicleController ___controller)
	{
		if (___controller != null)
		{
			PairingBuilder.Build(___controller);
		}
	}
}

[HarmonyPatch(typeof(FlightController), nameof(FlightController.SetEngineThrottle))]
internal static class SetEngineThrottle_Patch
{
	private static void Postfix(float throttle, VehicleController ___controller)
	{
		if (___controller?.engineMountList == null)
		{
			return;
		}
		PairingState.VehicleState state = PairingState.Get(___controller);
		foreach (EngineMountController mount in ___controller.engineMountList)
		{
			if (mount != null)
			{
				state.lastCommandedThrottleByMount[mount] = throttle;
			}
		}
	}
}

[HarmonyPatch(typeof(FlightController), nameof(FlightController.RunVehicleEngines))]
internal static class RunVehicleEngines_Patch
{
	private static void Postfix(VehicleController ___controller)
	{
		if (___controller == null)
		{
			return;
		}
		foreach (EnginePairing pairing in PairingState.Get(___controller).pairings)
		{
			pairing.Tick();
		}
	}
}

// Every shutdown path in the game (staging, ring trims, landing
// sequencing) funnels through here, so this one patch protects an active
// backup from all of them.
[HarmonyPatch(typeof(EngineController), nameof(EngineController.ShutdownEngine))]
internal static class ShutdownEngine_Patch
{
	private static bool Prefix(EngineController __instance)
	{
		return !PairingState.Get(__instance).isCoveringForFailedPrimary;
	}
}

[HarmonyPatch(typeof(EngineController), nameof(EngineController.ResetEngine))]
internal static class ResetEngine_Patch
{
	private static void Postfix(EngineController __instance)
	{
		PairingState.Get(__instance).isCoveringForFailedPrimary = false;
	}
}

// Full hard stop the moment boostback ends, even for an active backup.
[HarmonyPatch(typeof(AscentFlightActionController), nameof(AscentFlightActionController.PerformActionFixed))]
internal static class AscentPerformActionFixed_Patch
{
	private static readonly ConditionalWeakTable<VehicleController, object> profileBeforeCallByVehicle = new ConditionalWeakTable<VehicleController, object>();

	private static void Prefix(VehicleController ___vehicleController)
	{
		if (___vehicleController == null)
		{
			return;
		}
		profileBeforeCallByVehicle.Remove(___vehicleController);
		profileBeforeCallByVehicle.Add(___vehicleController, ___vehicleController.telemetryData.flightProfile);
	}

	private static void Postfix(VehicleController ___vehicleController)
	{
		if (___vehicleController == null || !profileBeforeCallByVehicle.TryGetValue(___vehicleController, out object boxed))
		{
			return;
		}
		FlightProfile profileBefore = (FlightProfile)boxed;
		bool wasBoostback = profileBefore == FlightProfile.Boostback;
		bool stillBoostback = ___vehicleController.telemetryData.flightProfile == FlightProfile.Boostback;
		if (wasBoostback && !stillBoostback)
		{
			foreach (EngineMountController mount in ___vehicleController.engineMountList)
			{
				foreach (EngineController engine in mount.currentEngineList)
				{
					PairingState.Get(engine).isCoveringForFailedPrimary = false;
					engine.ShutdownEngine(false);
				}
				mount.isDisabled = true;
			}
			Plugin.Log.LogInfo($"EnginePairing: hard stop at end of boostback for {___vehicleController.prettySerialNumber}.");
		}
		profileBeforeCallByVehicle.Remove(___vehicleController);
	}
}

// A ring active only via backup coverage shouldn't count as fully lit for
// the landing burn's ring-lighting check, or the sequencer skips lighting
// the rest of it. Corrects that after the game's own logic runs, firing
// at most once per flight per ring so it doesn't fight the stagger
// coroutine.
[HarmonyPatch(typeof(LandingFlightActionController), nameof(LandingFlightActionController.PerformActionFixed))]
internal static class LandingPerformActionFixed_Patch
{
	private static void Postfix(VehicleController ___vehicleController, LandingFlightAction ___data)
	{
		if (___vehicleController?.engineMountList == null || ___data == null || ___data.engineMountLimit <= 0)
		{
			return;
		}
		PairingState.VehicleState state = PairingState.Get(___vehicleController);
		int correctedActiveCount = 0;
		int originalActiveCount = ___vehicleController.flightController.GetAmountOfActiveEngineMounts();
		bool anyUncorrectedUnderLitMount = false;
		foreach (EngineMountController mount in ___vehicleController.engineMountList)
		{
			if (mount.isDisabled)
			{
				continue;
			}
			bool hasNonBackupActive = false;
			bool hasAnyActive = false;
			foreach (EngineController engine in mount.currentEngineList)
			{
				if (!engine.isDisabled)
				{
					hasAnyActive = true;
					if (!PairingState.Get(engine).isCoveringForFailedPrimary)
					{
						hasNonBackupActive = true;
						break;
					}
				}
			}
			if (hasNonBackupActive)
			{
				correctedActiveCount++;
			}
			else if (hasAnyActive && !state.landingCorrectionApplied.Contains(mount))
			{
				anyUncorrectedUnderLitMount = true;
			}
		}
		if (anyUncorrectedUnderLitMount && originalActiveCount >= ___data.engineMountLimit && correctedActiveCount < ___data.engineMountLimit)
		{
			Plugin.Log.LogInfo($"EnginePairing: landing burn under-lit due to backup-only ring (original={originalActiveCount}, corrected={correctedActiveCount}, limit={___data.engineMountLimit}) -- lighting remaining engines for {___vehicleController.prettySerialNumber}.");
			___vehicleController.flightController.EnableEngineMountStaggered(___data.engineMountLimit, 0.35f);
			foreach (EngineMountController mount in ___vehicleController.engineMountList)
			{
				state.landingCorrectionApplied.Add(mount);
			}
		}
	}
}
