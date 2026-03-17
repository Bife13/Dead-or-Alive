using System.Collections.Generic;

public class NightReport
{
	public int baseIncome;
	public int bonusIncome;
	public int killBonus;
	public int summonBonus;
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
}