using UnityEngine;

public class Room
{
	private Vector2Int position;
	private bool IsPremium;

	private CrewInstance occupant;

	public bool IsEmpty => occupant == null;
	public Vector2Int Position => position;
	public CrewInstance Occupant => occupant;
	public RoomView view;

	public Room(Vector2Int pos)
	{
		position = pos;
	}

	public void SetOccupant(CrewInstance newOccupant)
	{
		occupant = newOccupant;
		occupant.currentRoom = this;
	}

	public void ClearOccupant()
	{
		occupant = null;
	}
}