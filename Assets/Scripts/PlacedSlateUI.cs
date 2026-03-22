using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlacedSlateUI : MonoBehaviour
{
	[SerializeField]
	private Image crewPlacedSlateIdentifier;

	[SerializeField]
	private TMP_Text crewPlacedSlateName;

	[SerializeField]
	private TMP_Text crewPlacedSlateAbility;

	public void InitializeSlate(CrewDefinition definition)
	{
		crewPlacedSlateIdentifier.color = DoAPalette.Instance.GetCrewColor(definition.crewType);
		;
		crewPlacedSlateName.text = definition.displayName;
		crewPlacedSlateAbility.text = definition.incomeText;
	}

	public void InitializeSlate(CrewInstance instance)
	{
		crewPlacedSlateIdentifier.color = DoAPalette.Instance.GetCrewColor(instance.Definition.crewType);
		crewPlacedSlateName.text = instance.Definition.displayName;
		crewPlacedSlateAbility.text = instance.Definition.incomeText;
	}
}