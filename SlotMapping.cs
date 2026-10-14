namespace BackupLandingBurnEngines;

internal static class SlotMapping
{
	// (primarySlot, backupTiers)
	// Tiers are tried in order. The first tier that has at least one
	// non-damaged engine is used; if that tier has several healthy
	// engines, one is picked at random.
	internal static readonly (int primarySlot, int[][] backupTiers)[] EnginePairingTable = new (int, int[][])[]
	{
		(1, new int[][] { new[] { 7 }, new[] { 6 } }),
		(2, new int[][] { new[] { 10 }, new[] { 9, 11 } }),
		(3, new int[][] { new[] { 4 }, new[] { 13 } })
	};

	// Center_N -> slot N, Inner_N -> slot N + 3
	internal static int ParseSlotNum(string mountName)
	{
		if (string.IsNullOrEmpty(mountName))
		{
			return -1;
		}

		string[] parts = mountName.Split('_');
		if (parts.Length != 2 || !int.TryParse(parts[1], out int result))
		{
			return -1;
		}

		if (parts[0] == "Center")
		{
			return result;
		}

		if (parts[0] == "Inner")
		{
			return result + 3;
		}

		return -1;
	}
}
