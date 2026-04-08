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
	private List<GameObject> zoneObjects;

	private Zone[,] _zones;
	public Zone[,] Zones => _zones;


	public void Initialize()
	{
		_zones = new Zone[width, height];

		int x = 0;
		int y = 0;
		int index = 0;

		foreach (GameObject zone in zoneObjects)
		{
			Vector2Int position = new Vector2Int(x, y);
			_zones[x, y] = new Zone(position, index);

			ZoneView view = zone.GetComponent<ZoneView>();
			view.Initialize(_zones[x, y]);
			Zones[x, y].View = view;

			index++;
			x++;
			if (x < width) continue;
			y++;
			x = 0;
		}
	}

	public void InitializeLocationNames()
	{
		int index = 0;
		foreach (Zone zone in GetAllZones())
		{
			zone.View.SetZoneName(GameManager.Instance.GetCurrentBounty().zoneNames[index]);
			zone.View.SetLocked(GameManager.Instance.IsZoneLocked(index));
			index++;
		}
	}

	public List<Zone> GetAdjacentZones(Zone zone)
	{
		List<Zone> result = new();

		Vector2Int position = zone.Position;

		Vector2Int[] directions =
		{
			Vector2Int.up,
			Vector2Int.right,
			Vector2Int.down,
			Vector2Int.left
		};

		foreach (var direction in directions)
		{
			Vector2Int newPosition = position + direction;

			if (IsInside(newPosition))
				result.Add(_zones[newPosition.x, newPosition.y]);
		}

		return result;
	}

	private bool IsInside(Vector2Int position)
	{
		return position.x >= 0 && position.x < width &&
		       position.y >= 0 && position.y < height;
	}

	public IEnumerable<Zone> GetAllZones()
	{
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				yield return Zones[x, y];
			}
		}
	}
}