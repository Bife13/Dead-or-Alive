using TMPro;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
	private static PlacementManager _instance;

	public static PlacementManager Instance => _instance;

	public MonsterDefinition selectedMonster;

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
		selectedMonster = definition;
		selectedMonsterText.text = selectedMonster.displayName;
		Debug.Log("Selected: " + definition.displayName);
	}

	public void ClearSelection()
	{
		selectedMonster = null;
		selectedMonsterText.text = "";

	}
}