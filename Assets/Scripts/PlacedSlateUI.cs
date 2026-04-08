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
		if (_instance.Definition.crewType == CrewType.Scavenger)
			ScavengerCounterUpdate();


		UpdateContract();

		bool isDetonator = instance.Definition.crewType == CrewType.Detonator;
		bool hasAbility = isDetonator;
		abilityButton.SetActive(hasAbility);

		abilityTrigger.onClick.RemoveAllListeners();
		abilityTrigger.onClick.AddListener(OnAbilityPressed);
		if (isDetonator)
			RefreshDetonatorButton(instance);
	}

	public void ScavengerCounterUpdate()
	{
		crewPlacedSlateAbility.text =
			_instance.Definition.incomeText + $" ({GameManager.Instance.WeeklyDeathCount})";
	}

	public void RefreshDetonatorButton(CrewInstance instance)
	{
		bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.PlanningPhase;
		bool canInteract = isPlanning && !instance.DetonatorUsed;

		abilityTrigger.interactable = canInteract;

		var p = DoAPalette.Instance;

		if (instance.DetonatorUsed)
		{
			abilityLabel.text = "SPENT";
			abilityLabel.color = p.textL4;
		}
		else if (instance.IsArmedForDetonation)
		{
			abilityLabel.text = "ARMED";
			abilityLabel.color = p.wineBright;
		}
		else
		{
			abilityLabel.text = "DETONATE";
			abilityLabel.color = p.ochre;
		}
	}

	private void OnAbilityPressed()
	{
		// switch (_instance.Definition.crewType)
		// {
		// 	case CrewType.Detonator:
		// 		GameManager.Instance.TriggerDetonator(_instance.currentRoom); // pass room reference
		// 		break;
		// }
		//
		// RefreshDetonatorButton(_instance);

		if (_instance.Definition.crewType != CrewType.Detonator) return;
		if (_instance.DetonatorUsed) return;

		_instance.IsArmedForDetonation = !_instance.IsArmedForDetonation;
		RefreshDetonatorButton(_instance);
	}

	public void UpdateContract()
	{
		if (_instance == null) return;
		int nights = _instance.ContractDurationRemaining;
		var p = DoAPalette.Instance;

		crewPlacedSlateContract.text = nights == 1
			? "LAST NIGHT"
			: $"{nights} NIGHTS";

		crewPlacedSlateContract.color = nights <= 1
			? p.wineBright
			: p.textL3;
	}
}