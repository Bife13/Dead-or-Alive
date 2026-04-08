using System.Collections.Generic;
using UnityEngine;

public class NightReport
{
	public int BaseIncome;
	public int BonusIncome;
	public int KillBonus;
	public int CreationBonus;
	public int Multiplier = 1;
	public int FinalIncome;

	public List<string> Events = new();
	public List<NightReportEvent> TypedEvents = new();
	public List<string> Checkouts = new();
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
	public ReportEventType Type;
	public string Label;
	public int Value;

	public CrewType? SourceCrew;
	public CrewType? TargetCrew;
	
	public Vector2Int? SourcePosition;  // cell to flash for source crew
	public Vector2Int? TargetPosition;  // cell to flash for target crew (kills, buffs)
}