using UnityEngine;

public class CrewInstance
{
	public CrewDefinition Definition { get; }

	public int currentIncome;
	public int contractDurationRemaining;
	public bool isAlive = true;
	public bool isTemporary;
	public bool isResident;
	public Zone CurrentZone;

	public bool anchorSaveUsed = false;
	public bool detonatorUsed = false;
	public bool isArmedForDetonation = false;

	public bool eliminatedBySource = false;

	public bool canReposition = false;
	
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

		if (CurrentZone?.view != null)
			CurrentZone.view.GetSlate()?.UpdateContract();
	}

	public void ExtendContract(int amount)
	{
		contractDurationRemaining++;

		if (CurrentZone?.view != null)
			CurrentZone.view.GetSlate()?.UpdateContract();
	}

	public void SetAnchorUse(bool value)
	{
		anchorSaveUsed = value;
	}
}