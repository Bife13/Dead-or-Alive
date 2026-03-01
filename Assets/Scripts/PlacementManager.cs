using TMPro;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
	private static PlacementManager _instance;

	public static PlacementManager Instance => _instance;

	// Arrival Monster
	public MonsterDefinition selectedMonster;

	// Placed Monster
	public MonsterInstance selectedInstance;

	[SerializeField]
	private TMP_Text selectedMonsterText;

	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(gameObject);
		else
			_instance = this;
	}

	public void SelectMonster(MonsterDefinition definition)
	{
		ClearSelection();
		selectedMonster = definition;
		selectedMonsterText.text = selectedMonster.displayName;
		Debug.Log("Selected: " + definition.displayName);
	}

	public void SelectInstance(MonsterInstance instance)
	{
		ClearSelection();
		selectedInstance = instance;
		selectedMonsterText.text = selectedInstance.Definition.displayName;
	}

	public void ClearSelection()
	{
		selectedMonster = null;
		selectedInstance = null;
		selectedMonsterText.text = "";
	}
}