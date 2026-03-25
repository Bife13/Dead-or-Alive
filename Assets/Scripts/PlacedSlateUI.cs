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

	[SerializeField]
	private TMP_Text crewPlacedSlateContract;

	private CrewInstance _instance;

	public void InitializeSlate(CrewInstance instance)
	{
		_instance = instance;

		crewPlacedSlateIdentifier.color = DoAPalette.Instance.GetCrewColor(instance.Definition.crewType);
		crewPlacedSlateName.text = instance.Definition.displayName;
		crewPlacedSlateAbility.text = instance.Definition.incomeText;

		UpdateContract();
	}

	public void UpdateContract()
	{
		if (_instance == null) return;
		int nights = _instance.contractDurationRemaining;
		var p = DoAPalette.Instance;

		crewPlacedSlateContract.text = nights == 1
			? "LAST NIGHT"
			: $"{nights} NIGHTS";

		crewPlacedSlateContract.color = nights <= 1
			? p.wineBright
			: p.textL3;
	}
}