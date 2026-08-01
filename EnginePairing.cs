using BumbleRatGames.StarshipFactory;

namespace BackupLandingBurnEngines;

internal class EnginePairing
{
	private readonly VehicleController vehicle;

	public readonly EngineController primaryEngine;

	public readonly EngineController backupEngine;

	private bool backupIsCoveringForPrimary;

	public EnginePairing(VehicleController vehicle, EngineController primaryEngine, EngineController backupEngine)
	{
		this.vehicle = vehicle;
		this.primaryEngine = primaryEngine;
		this.backupEngine = backupEngine;
	}

	public bool IsValid()
	{
		return primaryEngine != null && backupEngine != null;
	}

	public void Tick()
	{
		if (!IsValid())
		{
			return;
		}
		PairingState.VehicleState vehicleState = PairingState.Get(vehicle);
		PairingState.EngineState backupState = PairingState.Get(backupEngine);
		bool failed = primaryEngine.isDamaged;
		float commandedThrottle = (primaryEngine.engineMount != null && vehicleState.lastCommandedThrottleByMount.TryGetValue(primaryEngine.engineMount, out float value)) ? value : 0f;
		if (failed)
		{
			if (!backupIsCoveringForPrimary)
			{
				Plugin.Log.LogInfo($"EnginePairing: primary slot {backupState.slotNum} failed, commanded={commandedThrottle} -- engaging backup.");
			}
			CoverForPrimary(commandedThrottle, backupState);
		}
		else if (backupIsCoveringForPrimary)
		{
			Plugin.Log.LogInfo("EnginePairing: primary recovered/no command -- disengaging backup.");
			StopCovering(backupState);
		}
	}

	private void CoverForPrimary(float wantedThrottle, PairingState.EngineState backupState)
	{
		if (backupEngine.isDamaged)
		{
			return;
		}
		if (backupEngine.engineMount != null && backupEngine.engineMount.isDisabled)
		{
			backupEngine.engineMount.isDisabled = false;
		}
		if (backupEngine.isDisabled)
		{
			backupEngine.PrepEngine();
		}
		// SetThrottle(0f) is a no-op in the base game, so 0 throttle is
		// handled as an explicit idle instead of being passed through.
		if (wantedThrottle <= 0f)
		{
			backupEngine.ShutdownEngine(false);
		}
		else
		{
			backupEngine.SetThrottle(wantedThrottle);
		}
		backupState.isCoveringForFailedPrimary = true;
		backupIsCoveringForPrimary = true;
	}

	private void StopCovering(PairingState.EngineState backupState)
	{
		backupState.isCoveringForFailedPrimary = false;
		backupEngine.ShutdownEngine();
		backupIsCoveringForPrimary = false;
		if (backupEngine.engineMount != null && backupEngine.engineMount.GetAmountOfActiveEngines() == 0)
		{
			backupEngine.engineMount.isDisabled = true;
		}
	}
}
