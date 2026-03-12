using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrivalButtonView : MonoBehaviour
{
	public Button button;
	public TMP_Text label;

	private CrewDefinition _crew;

	public void Initialize(CrewDefinition definition)
	{
		_crew = definition;
		label.text = definition.displayName;

		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(OnClicked);
	}

	private void OnClicked()
	{
		PlacementManager.Instance.SelectCrew(_crew);
	}
}