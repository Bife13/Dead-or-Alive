using System.Collections.Generic;
using UnityEngine;

public class NightReport
{
	public int baseIncome;
	public int bonusIncome;
	public int killBonus;
	public int creationBonus;
	public int multiplier = 1;
	public int finalIncome;

	public List<string> events = new();
	public List<NightReportEvent> typedEvents = new();
	public List<string> checkouts = new();
}

public enum ReportEventType
{
	Buff,
	Creation,
	Kill,
	KillBonus,
	Drain,
	BaseIncome,
	BuffedIncome,
	Multiplier
}

public struct NightReportEvent
{
	public ReportEventType type;
	public string label;
	public int value;

	public CrewType? sourceCrew;
	public CrewType? targetCrew;
	
	public Vector2Int? sourcePosition;  // cell to flash for source crew
	public Vector2Int? targetPosition;  // cell to flash for target crew (kills, buffs)
}