using UnityEngine;

public class Zone
{
	private Vector2Int _position;
	private bool _isPremium;

	private CrewInstance _occupant;

	public bool IsEmpty => _occupant == null;
	public Vector2Int Position => _position;
	public CrewInstance Occupant => _occupant;
	public ZoneView View;
	private int _index;
	public int Index => _index;

	public Zone(Vector2Int position, int index)
	{
		_position = position;
		_index = index;
	}

	public void SetOccupant(CrewInstance newOccupant)
	{
		_occupant = newOccupant;
		_occupant.CurrentZone = this;
		View.UpdateSlate(newOccupant);
	}

	public void ClearHideOccupant()
	{
		_occupant = null;
		View.HideSlate();
	}

	public void ClearOccupant()
	{
		_occupant = null;
	}
	

	public bool IsOccupied()
	{
		return _occupant != null;
	}
}