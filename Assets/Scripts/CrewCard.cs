using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CrewCard : MonoBehaviour
{
	public Button button;
	private CrewDefinition crew;


	[SerializeField]
	private TMP_Text nameText;

	[SerializeField]
	private TMP_Text contractText;

	[SerializeField]
	private TMP_Text codenameText;


	[SerializeField]
	private TMP_Text incomeText;

	[SerializeField]
	private TMP_Text descriptionText;

	[SerializeField]
	private GameObject stat1;

	[SerializeField]
	private TMP_Text stat1Label;

	[SerializeField]
	private TMP_Text stat1Value;

	[SerializeField]
	private GameObject stat2;

	[SerializeField]
	private TMP_Text stat2Label;

	[SerializeField]
	private TMP_Text stat2Value;

	[SerializeField]
	private GameObject stat3;

	[SerializeField]
	private TMP_Text stat3Label;

	[SerializeField]
	private TMP_Text stat3Value;

	[SerializeField]
	private Image leftBorder;

	[SerializeField]
	private List<GameObject> tapes;

	public void Populate(CrewDefinition definition)
	{
		nameText.text = definition.displayName;
		codenameText.text = definition.codename;
		contractText.text = definition.contractType;
		incomeText.text = definition.incomeText;
		descriptionText.text = definition.descriptionText;
		leftBorder.color = DoAPalette.Instance.GetCrewColor(definition.crewType);

		if (definition.statLabel1 != "")
		{
			stat1.SetActive(true);
			stat1Label.text = definition.statLabel1;
			stat1Value.text = definition.statValue1;
		}
		else
			stat1.SetActive(false);

		if (definition.statLabel2 != "")
		{
			stat2.SetActive(true);
			stat2Label.text = definition.statLabel2;
			stat2Value.text = definition.statValue2;
		}
		else
			stat2.SetActive(false);

		if (definition.statLabel3 != "")
		{
			stat3.SetActive(true);
			stat3Label.text = definition.statLabel3;
			stat3Value.text = definition.statValue3;
		}
		else
			stat3.SetActive(false);

		tapes[Random.Range(0, tapes.Count)].SetActive(true);
	}

	public void Initialize(CrewDefinition definition)
	{
		crew = definition;
		Populate(crew);
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(OnClicked);
	}

	private void OnClicked()
	{
		PlacementManager.Instance.SelectCrew(crew);
	}
}