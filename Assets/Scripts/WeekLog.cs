using System.Collections.Generic;

public class RunLog
{
	public int seed;
	public List<WeekLog> weeks = new();
	public int finalMoney;
}

public class WeekLog
{
	public List<NightLog> nights = new();
	public List<crewLog> crewLogs = new();
	public int finalMoney;
	public int peak;
	public int peakNight;
	public int solvedNight;

	public List<CrewDefinition> crewBag = new();
	public List<string> crewsExtended = new();
}

public class NightLog
{
	public int nightNumber;
	public List<string> arrivals = new();
	public List<string> placements = new();
	public List<string> extends = new();
	public List<string> checkouts = new();
	public List<string> events = new();
	public int currentMoney;
	public string engineType;

	public int baseIncome;
	public int bonusIncome;
	public int killBonus;
	public int multiplier;
	public int totalIncome;

	public string[,] beforePlacement;
	public string[,] afterPlacement;
	public string[,] afterCreations;
	public string[,] afterKills;
	public string[,] endOfNight;
}

public class crewLog
{
	public CrewDefinition definition;
	public int timesOffered;
	public int timesPlaced;
	public int averageIncomeGenerated;

	public crewLog(CrewDefinition _definition)
	{
		definition = _definition;
	}
}