using UnityEngine;

public class MonsterInstance
{
	public MonsterDefinition Definition { get; }
	
	public int currentIncome;
	public int nightsRemaining;
	public bool isAlive = true;
	public bool isTemporary;
	public MonsterView view;
	public bool isResident;
	public Room currentRoom;

	public MonsterInstance(MonsterDefinition _definition)
	{
		Definition = _definition;
		currentIncome = _definition.baseIncome;
		isTemporary = _definition.isTemporary;
		nightsRemaining = _definition.stayDuration;
		isResident = false;
	}
	
	public void DecreaseStay()
	{
		nightsRemaining--;

		if (view != null)
			view.UpdateNightsRemaining();
	}

	public void ExtendStay(int amount)
	{
		nightsRemaining++;

		if (view != null)
			view.UpdateNightsRemaining();
	}
}