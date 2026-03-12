using UnityEngine;

public class CrewInstance
{
	public CrewDefinition Definition { get; }
	
	public int currentIncome;
	public int contractDurationRemaining;
	public bool isAlive = true;
	public bool isTemporary;
	public CrewView view;
	public bool isResident;
	public Room currentRoom;

	public CrewInstance(CrewDefinition _definition)
	{
		Definition = _definition;
		currentIncome = _definition.baseIncome;
		isTemporary = _definition.isTemporary;
		contractDurationRemaining = _definition.contractDuration;
		isResident = false;
	}
	
	public void DecreaseStay()
	{
		contractDurationRemaining--;

		if (view != null)
			view.UpdateContractDuration();
	}

	public void ExtendStay(int amount)
	{
		contractDurationRemaining++;

		if (view != null)
			view.UpdateContractDuration();
	}
}