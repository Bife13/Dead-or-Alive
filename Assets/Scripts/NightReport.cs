using System.Collections.Generic;

public class NightReport
{
	public int baseIncome;
	public int killBonus;
	public int summonBonus;
	public float multiplier = 1f;
	public int finalIncome;

	public List<string> events = new();
	public List<string> checkouts = new();
}