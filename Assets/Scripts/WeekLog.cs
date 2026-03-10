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
	public List<MonsterLog> monsterLogs = new();
	public int finalMoney;
	public int peak;
	public int peakNight;
	public int solvedNight;

	public List<MonsterDefinition> monsterBag = new();
	public List<string> monstersExtended = new();
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
	public string[,] afterSummons;
	public string[,] afterKills;
	public string[,] endOfNight;
}

public class MonsterLog
{
	public MonsterDefinition definition;
	public int timesOffered;
	public int timesPlaced;
	public int averageIncomeGenerated;

	public MonsterLog(MonsterDefinition _definition)
	{
		definition = _definition;
	}
}