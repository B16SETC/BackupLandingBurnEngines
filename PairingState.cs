using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BumbleRatGames.StarshipFactory;

namespace BackupLandingBurnEngines;

// Tracks state we'd normally add as fields on EngineController /
// EngineMountController, without modifying those classes.
internal static class PairingState
{
	internal class EngineState
	{
		public int slotNum = -1;
		public bool isCoveringForFailedPrimary;
	}

	private static readonly ConditionalWeakTable<EngineController, EngineState> engineStates = new ConditionalWeakTable<EngineController, EngineState>();

	internal static EngineState Get(EngineController engine)
	{
		return engineStates.GetOrCreateValue(engine);
	}

	internal class VehicleState
	{
		public readonly Dictionary<EngineMountController, float> lastCommandedThrottleByMount = new Dictionary<EngineMountController, float>();

		public readonly List<EnginePairing> pairings = new List<EnginePairing>();

		// Mounts already given a corrective landing-burn light-up this
		// flight, so it only fires once instead of every tick.
		public readonly HashSet<EngineMountController> landingCorrectionApplied = new HashSet<EngineMountController>();
	}

	private static readonly ConditionalWeakTable<VehicleController, VehicleState> vehicleStates = new ConditionalWeakTable<VehicleController, VehicleState>();

	internal static VehicleState Get(VehicleController vehicle)
	{
		return vehicleStates.GetOrCreateValue(vehicle);
	}
}
