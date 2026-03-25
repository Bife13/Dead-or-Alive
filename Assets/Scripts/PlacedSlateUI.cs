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

	[SerializeField]
	private CanvasGroup canvasGroup;

	public CanvasGroup CanvasGroup => canvasGroup;

	[SerializeField]
	private GameObject abilityButton;

	[SerializeField]
	private Button abilityTrigger;

	[SerializeField]
	private TMP_Text abilityLabel;


	private CrewInstance _instance;

	public void InitializeSlate(CrewInstance instance)
	{
		_instance = instance;

		crewPlacedSlateIdentifier.color = DoAPalette.Instance.GetCrewColor(instance.Definition.crewType);
		crewPlacedSlateName.text = instance.Definition.displayName;
		crewPlacedSlateAbility.text = instance.Definition.incomeText;

		UpdateContract();

		bool isDetonator = instance.Definition.crewType == CrewType.Detonator;
		abilityButton.SetActive(isDetonator);

		if (isDetonator)
			RefreshDetonatorButton(instance);
	}

	public void RefreshDetonatorButton(CrewInstance instance)
	{
		bool canUse = !instance.detonatorUsed
		              && GameManager.Instance.CurrentPhase == GamePhase.PlanningPhase;

		abilityTrigger.interactable = canUse;
		abilityLabel.color = canUse
			? DoAPalette.Instance.wineBright
			: DoAPalette.Instance.textL4;
		abilityLabel.text = instance.detonatorUsed ? "SPENT" : "DETONATE";
	}

	public void OnDetonatorPressed()
	{
		GameManager.Instance.TriggerDetonator(_instance.currentRoom); // pass room reference
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