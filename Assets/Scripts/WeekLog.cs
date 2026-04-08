using System.Collections.Generic;

public class RunLog
{
	public int Seed;
	public List<WeekLog> Weeks = new();
	public int FinalMoney;
}

public class WeekLog
{
	public List<NightLog> Nights = new();
	public List<CrewLog> CrewLogs = new();
	public int FinalMoney;
	public int Peak;
	public int PeakNight;
	public int SolvedNight;

	public List<CrewDefinition> CrewBag = new();
	public List<string> CrewsExtended = new();
}

public class NightLog
{
	public int NightNumber;
	public List<string> Arrivals = new();
	public List<string> Placements = new();
	public List<string> Extends = new();
	public List<string> Checkouts = new();
	public List<string> Events = new();
	public List<NightReportEvent> TypedEvents = new();
	public int CurrentMoney;
	public string EngineType;

	public int BaseIncome;
	public int BonusIncome;
	public int KillBonus;
	public int Multiplier;
	public int TotalIncome;

	public string[,] BeforePlacement;
	public string[,] AfterPlacement;
	public string[,] AfterCreations;
	public string[,] AfterKills;
	public string[,] EndOfNight;
}

public class CrewLog
{
	public CrewDefinition Definition;
	public int TimesOffered;
	public int TimesPlaced;
	public int AverageIncomeGenerated;

	public CrewLog(CrewDefinition definition)
	{
		Definition = definition;
	}
}