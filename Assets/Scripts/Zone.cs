using UnityEngine;

public class Zone
{
	private Vector2Int _position;
	private bool IsPremium;

	private CrewInstance occupant;

	public bool IsEmpty => occupant == null;
	public Vector2Int Position => _position;
	public CrewInstance Occupant => occupant;
	public ZoneView view;
	private int _index;
	public int Index => _index;

	public Zone(Vector2Int position, int index)
	{
		_position = position;
		_index = index;
	}

	public void SetOccupant(CrewInstance newOccupant)
	{
		occupant = newOccupant;
		occupant.CurrentZone = this;
		view.UpdateSlate(newOccupant);
	}

	public void ClearHideOccupant()
	{
		occupant = null;
		view.HideSlate();
	}

	public void ClearOccupant()
	{
		occupant = null;
	}
	

	public bool IsOccupied()
	{
		return occupant != null;
	}
}