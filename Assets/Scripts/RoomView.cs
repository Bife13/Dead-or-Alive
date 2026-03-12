using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class RoomView : MonoBehaviour, IPointerClickHandler
{
	private Room room;

	public void Initialize(Room roomData)
	{
		room = roomData;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (GameManager.Instance.CurrentPhase != GamePhase.PlanningPhase)
			return;

		// If clicking a room with an arrival monster
		if (room.Occupant != null)
		{
			PlacementManager.Instance.SelectInstance(room.Occupant);
			return;
		}

		// If clicking empty room and we have a selected instance ALREADY IN HOTEL
		if (room.Occupant == null && PlacementManager.Instance.selectedInstance != null)
		{
			GameManager.Instance.MoveSelectedCrewTo(room);
			return;
		}

		// If clicking empty room and we have selected definition FROM ARRIVAL
		if (room.Occupant == null && PlacementManager.Instance.selectedCrew != null)
		{
			GameManager.Instance.PlaceSelectedCrew(room);
			return;
		}

		// if (PlacementManager.Instance.selectedMonster == null)
		// 	return;
		//
		// if (!GameManager.Instance.CanPlaceSelected())
		// 	return;
	}
}