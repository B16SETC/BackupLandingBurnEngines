using System.Collections.Generic;
using BumbleRatGames.StarshipFactory;

namespace BackupLandingBurnEngines;

internal static class PairingBuilder
{
	internal static void Build(VehicleController vehicle)
	{
		PairingState.VehicleState state = PairingState.Get(vehicle);
		state.pairings.Clear();
		state.landingCorrectionApplied.Clear();
		if (vehicle.engineMountList == null)
		{
			return;
		}
		Dictionary<int, EngineController> bySlot = new Dictionary<int, EngineController>();
		foreach (EngineMountController mount in vehicle.engineMountList)
		{
			if (mount == null)
			{
				continue;
			}
			foreach (EngineController engine in mount.currentEngineList)
			{
				if (engine == null || engine.parentEnginePos == null)
				{
					continue;
				}
				int slot = SlotMapping.ParseSlotNum(engine.parentEnginePos.name);
				if (slot != -1)
				{
					bySlot[slot] = engine;
					PairingState.Get(engine).slotNum = slot;
				}
			}
		}
		// Vehicles that don't use Center_N/Inner_N naming (e.g. the ship)
		// simply get no pairings.
		if (bySlot.Count == 0)
		{
			return;
		}
		foreach ((int primarySlot, int[][] backupTiers) in SlotMapping.EnginePairingTable)
		{
			if (!bySlot.TryGetValue(primarySlot, out EngineController primary))
			{
				Plugin.Log.LogWarning($"EnginePairing: couldn't find primary engine slot {primarySlot} on {vehicle.prettySerialNumber} -- check parentEnginePos naming (expects Center_N / Inner_N).");
				continue;
			}
			List<EngineController[]> tiers = new List<EngineController[]>();
			foreach (int[] tierSlots in backupTiers)
			{
				List<EngineController> tier = new List<EngineController>();
				foreach (int candidateSlot in tierSlots)
				{
					if (bySlot.TryGetValue(candidateSlot, out EngineController candidate))
					{
						tier.Add(candidate);
					}
					else
					{
						Plugin.Log.LogWarning($"EnginePairing: couldn't find backup engine slot {candidateSlot} (for primary {primarySlot}) on {vehicle.prettySerialNumber}.");
					}
				}
				if (tier.Count > 0)
				{
					tiers.Add(tier.ToArray());
				}
			}
			if (tiers.Count == 0)
			{
				Plugin.Log.LogWarning($"EnginePairing: no usable backup engines for primary slot {primarySlot} on {vehicle.prettySerialNumber}.");
				continue;
			}
			state.pairings.Add(new EnginePairing(vehicle, primary, tiers.ToArray()));
		}
	}
}
