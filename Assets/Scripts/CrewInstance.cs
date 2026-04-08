using UnityEngine;

public class CrewInstance
{
	public CrewDefinition Definition { get; }

	public int CurrentIncome;
	public int ContractDurationRemaining;
	public bool IsAlive = true;
	public bool IsTemporary;
	public bool IsResident;
	public Zone CurrentZone;

	public bool AnchorSaveUsed = false;
	public bool DetonatorUsed = false;
	public bool IsArmedForDetonation = false;

	public bool EliminatedBySource = false;

	public bool CanReposition = false;
	
	public CrewInstance(CrewDefinition definition)
	{
		Definition = definition;
		CurrentIncome = definition.baseIncome;
		IsTemporary = definition.isTemporary;
		ContractDurationRemaining = definition.contractDuration;
		IsResident = false;
	}

	public void DecreaseStay()
	{
		ContractDurationRemaining--;

		if (CurrentZone?.View != null)
			CurrentZone.View.GetSlate()?.UpdateContract();
	}

	public void ExtendContract(int amount)
	{
		ContractDurationRemaining++;

		if (CurrentZone?.View != null)
			CurrentZone.View.GetSlate()?.UpdateContract();
	}

	public void SetAnchorUse(bool value)
	{
		AnchorSaveUsed = value;
	}
}