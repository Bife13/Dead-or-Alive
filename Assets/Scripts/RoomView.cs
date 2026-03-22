using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RoomView : MonoBehaviour, IPointerClickHandler
{
	private Room room;

	[SerializeField]
	private PlacedSlateUI crewPlacedSlate;

	public void Initialize(Room roomData)
	{
		room = roomData;
		HideSlate();
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

	public void UpdateSlate(CrewDefinition definition)
	{
		crewPlacedSlate.gameObject.SetActive(true);
		crewPlacedSlate.InitializeSlate(definition);
	}
	
	public void UpdateSlate(CrewInstance instance)
	{
		crewPlacedSlate.gameObject.SetActive(true);
		crewPlacedSlate.InitializeSlate(instance);
	}

	public void HideSlate()
	{
		crewPlacedSlate.gameObject.SetActive(false);
	}
}