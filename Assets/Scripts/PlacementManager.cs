using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlacementManager : MonoBehaviour
{
	private static PlacementManager _instance;

	public static PlacementManager Instance => _instance;

	// Arrival Monster
	public CrewDefinition selectedCrew;

	// Placed Monster
	public CrewInstance SelectedInstance;
	
	
	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(gameObject);
		else
			_instance = this;
	}

	public void SelectCrew(CrewDefinition definition)
	{
		ClearSelection();
		selectedCrew = definition;
		Debug.Log("Selected: " + definition.displayName);
	}

	public void SelectInstance(CrewInstance instance)
	{
		ClearSelection();
		SelectedInstance = instance;
	}

	public void ClearSelection()
	{
		selectedCrew = null;
		SelectedInstance = null;
	}
}