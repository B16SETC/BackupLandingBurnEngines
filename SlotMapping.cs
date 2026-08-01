namespace BackupLandingBurnEngines;

internal static class SlotMapping
{
	// (primarySlot, backupSlot)
	internal static readonly (int primarySlot, int backupSlot)[] EnginePairingTable = new (int, int)[]
	{
		(1, 7),
		(2, 10),
		(3, 1)
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
