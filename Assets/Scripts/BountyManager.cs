using System.Linq;
using UnityEngine;

public class BountyManager : MonoBehaviour
{
	[Header("Weekly Bounties")]
	[Tooltip("One per week. Index 0 = Week 1, 1 = Week 2, 2 = Week 3")]
	public BountyData[] weeklyBounties;

	public BountyData CurrentBounty { get; private set; }

	// -- Initialization --

	public void LoadBountyForWeek(int week)
	{
		int index = week - 1;
		if (index < 0 || index >= weeklyBounties.Length)
		{
			Debug.LogWarning($"BountyManager: no bounty data for week {week}.");
			CurrentBounty = null;
			return;
		}

		CurrentBounty = weeklyBounties[index];
	}

	// -- Modifier Queries --

	public bool HasModifier(BountyModifierType type)
	{
		if (CurrentBounty == null) return false;
		return CurrentBounty.modifiers.Any(m => m.type == type);
	}

	public BountyModifier GetModifier(BountyModifierType type)
	{
		if (CurrentBounty == null) return null;
		return CurrentBounty.modifiers.FirstOrDefault(m => m.type == type);
	}

	// -- Modifier Effects --

	// Called by GameManager after crew income resolves each night.
	// Returns the amount drained (0 if modifier not active).
	public int ApplyIncomeDrain()
	{
		var mod = GetModifier(BountyModifierType.IncomeDrain);
		if (mod == null) return 0;
		return mod.value;
	}

	// Called by GameManager during placement validation.
	// Returns true if the zone index is blocked.
	public bool IsZoneLocked(int zoneIndex)
	{
		var mod = GetModifier(BountyModifierType.ZoneLockout);
		if (mod == null) return false;
		if (mod.lockedZones == null) return false;
		return System.Array.IndexOf(mod.lockedZones, zoneIndex) >= 0;
	}

	// Called by GameManager at end of night, after income.
	// Returns the index of the eliminated crew member, or -1 if no elimination.
	public int ResolveCrewThreat(int deployedCount)
	{
		var mod = GetModifier(BountyModifierType.CrewThreat);
		if (mod == null) return -1;
		if (deployedCount == 0) return -1;

		float roll = Random.Range(0, 100);
		if (roll < mod.value)
			return Random.Range(0, deployedCount);

		return -1;
	}
}