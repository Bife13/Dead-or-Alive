using UnityEngine;

public class CrewInstance
{
	public CrewDefinition Definition { get; }

	public int currentIncome;
	public int contractDurationRemaining;
	public bool isAlive = true;
	public bool isTemporary;
	public bool isResident;
	public Room currentRoom;

	public bool anchorSaveUsed = false;
	public bool detonatorUsed = false;
	public bool eliminatedBySource = false;
	
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

		if (currentRoom?.view != null)
			currentRoom.view.GetSlate()?.UpdateContract();
	}

	public void ExtendContract(int amount)
	{
		contractDurationRemaining++;

		if (currentRoom?.view != null)
			currentRoom.view.GetSlate()?.UpdateContract();
	}

	public void SetAnchorUse(bool value)
	{
		anchorSaveUsed = value;
	}
}