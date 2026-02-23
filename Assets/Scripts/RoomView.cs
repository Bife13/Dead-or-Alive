using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class RoomView : MonoBehaviour, IPointerClickHandler
{
	private Room room;
	public MonsterDefinition testMonsterDefinition;
	public GameObject monsterPrefab;

	public void Initialize(Room roomData)
	{
		room = roomData;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (room.Occupant != null)
			return;

		if (PlacementManager.Instance.selectedMonster == null)
			return;

		if (!GameManager.Instance.CanPlaceSelected())
			return;

		GameManager.Instance.PlaceSelectedMonster(room);
	}
}