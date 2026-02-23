using UnityEngine;

public class Room
{
	private Vector2Int position;
	private bool IsPremium;

	private MonsterInstance occupant;

	public bool IsEmpty => occupant == null;
	public Vector2Int Position => position;
	public MonsterInstance Occupant => occupant;
	public RoomView view;

	public Room(Vector2Int pos)
	{
		position = pos;
	}

	public void SetOccupant(MonsterInstance newOccupant)
	{
		occupant = newOccupant;
	}

	public void ClearOccupant()
	{
		occupant = null;
	}
}