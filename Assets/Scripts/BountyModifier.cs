using System;
using UnityEngine;

public enum BountyModifierType
{
	IncomeDrain,
	ZoneLockout,
	CrewThreat
}

[Serializable]
public class BountyModifier
{
	public BountyModifierType type;

	[Tooltip("Income Drain: ¥ drained per night. Crew Threat: elimination % (0–100). Zone Lockout: unused.")]
	public int value;

	[Tooltip("e.g. 'Protection Racket' or 'Sends a Message'")]
	public string displayName;

	[Tooltip("One line. Shown on the bounty bar.")]
	public string description;

	// Zone Lockout only — which grid indices are blocked (0–8, row-major)
	public int[] lockedZones;
}