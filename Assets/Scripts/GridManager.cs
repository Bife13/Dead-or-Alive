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
	private GameObject roomPrefab;

	private Room[,] rooms;
	public Room[,] Rooms => rooms;

	[SerializeField]
	private float roomSpacing = 3.2f;

	public void Initialize()
	{
		rooms = new Room[width, height];

		float xOffset = (width - 1) * roomSpacing / 2f;
		float yOffset = (height - 1) * roomSpacing / 2f;

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Vector2Int position = new Vector2Int(x, y);
				rooms[x, y] = new Room(position);

				float worldX = x * roomSpacing - xOffset;
				float worldY = y * roomSpacing - yOffset;

				GameObject roomGO = Instantiate(
					roomPrefab,
					new Vector3(worldX, worldY, 0),
					Quaternion.identity,
					transform
				);

				RoomView view = roomGO.GetComponent<RoomView>();
				view.Initialize(rooms[x, y]);

				Rooms[x, y].view = view;
			}
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