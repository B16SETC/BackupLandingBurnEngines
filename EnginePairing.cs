using System.Collections.Generic;
using BumbleRatGames.StarshipFactory;

namespace BackupLandingBurnEngines;

internal class EnginePairing
{
	private readonly VehicleController vehicle;

	public readonly EngineController primaryEngine;

	// Ordered fallback tiers. See SlotMapping.EnginePairingTable.
	private readonly EngineController[][] backupTiers;

	// The backup currently covering for the primary, or null if none.
	private EngineController currentBackup;

	public EnginePairing(VehicleController vehicle, EngineController primaryEngine, EngineController[][] backupTiers)
	{
		this.vehicle = vehicle;
		this.primaryEngine = primaryEngine;
		this.backupTiers = backupTiers;
	}

	public bool IsValid()
	{
		return primaryEngine != null && backupTiers != null && backupTiers.Length > 0;
	}

	public void Tick()
	{
		if (!IsValid())
		{
			return;
		}
		PairingState.VehicleState vehicleState = PairingState.Get(vehicle);
		bool failed = primaryEngine.isDamaged;
		float commandedThrottle = (primaryEngine.engineMount != null && vehicleState.lastCommandedThrottleByMount.TryGetValue(primaryEngine.engineMount, out float value)) ? value : 0f;
		if (failed)
		{
			// The backup we were using has failed too -- release it and
			// fall through to pick the next one.
			if (currentBackup != null && currentBackup.isDamaged)
			{
				Plugin.Log.LogInfo($"EnginePairing: backup slot {PairingState.Get(currentBackup).slotNum} also failed -- picking another backup.");
				PairingState.Get(currentBackup).isCoveringForFailedPrimary = false;
				currentBackup = null;
			}
			if (currentBackup == null)
			{
				currentBackup = PickBackup();
				if (currentBackup == null)
				{
					return;
				}
				Plugin.Log.LogInfo($"EnginePairing: primary slot {PairingState.Get(primaryEngine).slotNum} failed, commanded={commandedThrottle} -- engaging backup slot {PairingState.Get(currentBackup).slotNum}.");
			}
			CoverForPrimary(commandedThrottle, currentBackup);
		}
		else if (currentBackup != null)
		{
			Plugin.Log.LogInfo("EnginePairing: primary recovered/no command -- disengaging backup.");
			StopCovering(currentBackup);
		}
	}

	// First tier with any non-damaged engine wins; random pick within
	// the tier if more than one is healthy. Returns null if every
	// candidate is damaged.
	private EngineController PickBackup()
	{
		List<EngineController> healthy = new List<EngineController>();
		foreach (EngineController[] tier in backupTiers)
		{
			healthy.Clear();
			foreach (EngineController candidate in tier)
			{
				if (candidate != null && !candidate.isDamaged)
				{
					healthy.Add(candidate);
				}
			}
			if (healthy.Count > 0)
			{
				return healthy[UnityEngine.Random.Range(0, healthy.Count)];
			}
		}
		return null;
	}

	private void CoverForPrimary(float wantedThrottle, EngineController backup)
	{
		if (backup.isDamaged)
		{
			return;
		}
		if (backup.engineMount != null && backup.engineMount.isDisabled)
		{
			backup.engineMount.isDisabled = false;
		}
		if (backup.isDisabled)
		{
			backup.PrepEngine();
		}
		// SetThrottle(0f) is a no-op in the base game, so 0 throttle is
		// handled as an explicit idle instead of being passed through.
		if (wantedThrottle <= 0f)
		{
			backup.ShutdownEngine(false);
		}
		else
		{
			backup.SetThrottle(wantedThrottle);
		}
		PairingState.Get(backup).isCoveringForFailedPrimary = true;
	}

	private void StopCovering(EngineController backup)
	{
		PairingState.Get(backup).isCoveringForFailedPrimary = false;
		backup.ShutdownEngine();
		currentBackup = null;
		if (backup.engineMount != null && backup.engineMount.GetAmountOfActiveEngines() == 0)
		{
			backup.engineMount.isDisabled = true;
		}
	}
}
