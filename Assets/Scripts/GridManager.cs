using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
	[SerializeField]
	private int height = 0;

	public int Height => height;

	[SerializeField]
	private int width = 0;

	public int Width => width;

	[SerializeField]
	private List<GameObject> roomObjects;

	private Room[,] rooms;
	public Room[,] Rooms => rooms;


	public void Initialize()
	{
		rooms = new Room[width, height];

		int x = 0;
		int y = 0;
		int index = 0;

		foreach (GameObject room in roomObjects)
		{
			Vector2Int position = new Vector2Int(x, y);
			rooms[x, y] = new Room(position);

			RoomView view = room.GetComponent<RoomView>();
			view.Initialize(rooms[x, y]);
			Rooms[x, y].view = view;

			index++;
			x++;
			if (x < width) continue;
			y++;
			x = 0;
		}

		// for (int x = 0; x < width; x++)
		// {
		// 	for (int y = 0; y < height; y++)
		// 	{
		// 		Vector2Int position = new Vector2Int(x, y);
		// 		rooms[x, y] = new Room(position);
		//
		// 		// RoomView view = roomObjects[x][y].GetComponent<RoomView>();
		// 		// view.Initialize(rooms[x, y]);
		// 		//
		// 		// Rooms[x, y].view = view;
		// 	}
		// }
	}

	public void InitializeLocationNames()
	{
		int index = 0;
		foreach (Room room in GetAllRooms())
		{
			room.view.SetZoneName(GameManager.Instance.GetCurrentBounty().zoneNames[index]);
			room.view.SetLocked(GameManager.Instance.IsZoneLocked(index));
			index++;
		}
	}

	public List<Room> GetAdjacentRooms(Room room)
	{
		List<Room> result = new();

		Vector2Int position = room.Position;

		Vector2Int[] directions =
		{
			Vector2Int.up,
			Vector2Int.down,
			Vector2Int.left,
			Vector2Int.right
		};

		foreach (var direction in directions)
		{
			Vector2Int newPosition = position + direction;

			if (IsInside(newPosition))
				result.Add(rooms[newPosition.x, newPosition.y]);
		}

		return result;
	}

	private bool IsInside(Vector2Int position)
	{
		return position.x >= 0 && position.x < width &&
		       position.y >= 0 && position.y < height;
	}

	public IEnumerable<Room> GetAllRooms()
	{
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				yield return Rooms[x, y];
			}
		}
	}
}