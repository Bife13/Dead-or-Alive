using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrivalButtonView : MonoBehaviour
{
	public Button button;
	public TMP_Text label;

	private MonsterDefinition monster;

	public void Initialize(MonsterDefinition definition)
	{
		monster = definition;
		label.text = definition.displayName;

		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(OnClicked);
	}

	private void OnClicked()
	{
		PlacementManager.Instance.SelectMonster(monster);
	}
}