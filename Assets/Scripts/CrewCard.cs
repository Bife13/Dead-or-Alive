using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrewCard : MonoBehaviour
{
	[SerializeField]
	private TMP_Text nameText;

	[SerializeField]
	private TMP_Text contractText;

	[SerializeField]
	private TMP_Text codenameText;

	
	[SerializeField]
	private TMP_Text abilityText;

	[SerializeField]
	private GameObject stat1;

	[SerializeField]
	private TMP_Text stat1Label1;

	[SerializeField]
	private TMP_Text stat1Value1;

	[SerializeField]
	private GameObject stat2;

	[SerializeField]
	private TMP_Text stat1Label2;

	[SerializeField]
	private TMP_Text stat1Value2;

	[SerializeField]
	private GameObject stat3;

	[SerializeField]
	private TMP_Text stat1Label3;

	[SerializeField]
	private TMP_Text stat1Value3;

	[SerializeField]
	private GameObject stat4;

	[SerializeField]
	private TMP_Text stat1Label4;

	[SerializeField]
	private TMP_Text stat1Value4;

	[SerializeField]
	private Image leftBorder;

	public void Populate(CrewDefinition definition)
	{
		nameText.text = definition.displayName;
		contractText.text = definition.contractType;
		abilityText.text = definition.abilityText;
		leftBorder.color = definition.crewColor;
		if (definition.statLabel1 != "")
		{
			stat1.SetActive(true);
			stat1Label1.text = definition.statLabel1;
			stat1Value1.text = definition.statValue1;
		}
		else
			stat1.SetActive(false);

		if (definition.statLabel2 != "")
		{
			stat1.SetActive(true);
			stat1Label2.text = definition.statLabel2;
			stat1Value2.text = definition.statValue2;
		}
		else
			stat1.SetActive(false);

		if (definition.statLabel3 != "")
		{
			stat1.SetActive(true);
			stat1Label3.text = definition.statLabel3;
			stat1Value3.text = definition.statValue3;
		}
		else
			stat1.SetActive(false);

		if (definition.statLabel4 != "")
		{
			stat1.SetActive(true);
			stat1Label4.text = definition.statLabel4;
			stat1Value4.text = definition.statValue4;
		}
		else
			stat1.SetActive(false);
	}
}