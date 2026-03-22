using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BountyBar : MonoBehaviour
{
	[Header("Identity Panel")]
	[SerializeField]
	private TMP_Text caseMeta;

	[SerializeField]
	private TMP_Text bountyName;

	[SerializeField]
	private TMP_Text alias;

	[SerializeField]
	private TMP_Text stampText;

	[Header("Modifier Panel")]
	[SerializeField]
	private Image modIcon;

	[SerializeField]
	private TMP_Text modIconText;

	[SerializeField]
	private TMP_Text modType;

	[SerializeField]
	private TMP_Text modName;

	[SerializeField]
	private TMP_Text modValue;

	[SerializeField]
	private GameObject miniGrid;

	[SerializeField]
	private GameObject[] miniGridCells;

	[Header("Cost Panel")]
	[SerializeField]
	private TMP_Text costValue;

	[Header("Threat Panel")]
	[SerializeField]
	private Image[] threatPips;

	[SerializeField]
	private TMP_Text threatWord;

	[SerializeField]
	private Sprite emptyPipImage;

	[SerializeField]
	private Sprite filledPipImage;

	// -- Public --

	public void Populate(BountyData data, int runningCost)
	{
		if (data == null) return;

		SetIdentity(data);
		SetModifier(data);
		SetCost(runningCost);
		SetThreat(data.threatLevel);
	}

	// Call this each night for Income Drain - updates the value display
	public void RefreshModifierValue(BountyData data)
	{
		if (data == null) return;

		var mod = System.Array.Find(data.modifiers, m => m.type == BountyModifierType.IncomeDrain);
		if (mod != null)
			modValue.text = $"−¥{mod.value} / Night";
	}

	// -- Private -- 

	private void SetIdentity(BountyData data)
	{
		caseMeta.text = $"#{data.caseNumber}";
		bountyName.text = data.bountyName;
		alias.text = data.alias;
		stampText.text = "Dead or Alive";
	}

	private void SetModifier(BountyData data)
	{
		// Hide mini grid by default
		miniGrid.SetActive(false);

		if (data.modifiers == null || data.modifiers.Length == 0)
		{
			modType.text = string.Empty;
			modName.text = string.Empty;
			modValue.text = string.Empty;
			return;
		}

		// Use first modifier for display 
		var palette = DoAPalette.Instance;
		var mod = data.modifiers[0];

		modName.text = mod.displayName;

		switch (mod.type)
		{
			case BountyModifierType.CrewThreat:

				modIcon.color = palette.wine;
				modIconText.color = palette.wine;
				modType.color = palette.wine;
				modValue.color = palette.wine;

				modIconText.text = "!";
				modType.text = "Crew Threat";
				modValue.text = $"{mod.value}% Kill";
				break;

			case BountyModifierType.ZoneLockout:
				modIcon.color = palette.wineBright;
				modIconText.color = palette.wineBright;
				modType.color = palette.wineBright;
				modValue.color = palette.textL2;

				modIconText.text = "X";
				modType.text = "ZoneLockout";
				modValue.text = $"{mod.lockedZones.Length} Zones";
				miniGrid.SetActive(true);
				RefreshMiniGrid(mod.lockedZones);
				break;

			case BountyModifierType.IncomeDrain:
				modIcon.color = palette.ochre;
				modIconText.color = palette.ochre;
				modType.color = palette.ochre;
				modValue.color = palette.ochreDim;

				modIconText.text = "¥";
				modType.text = "Income Drain";
				modValue.text = $"-¥{mod.value} / Night";
				break;
		}
	}

	private void SetCost(int runningCost)
	{
		costValue.text = $"¥{runningCost}";
	}

	private void SetThreat(int level)
	{
		var palette = DoAPalette.Instance;
		// Level 1 = Low, 2 = Moderate, 3 = High

		for (int i = 0; i < threatPips.Length; i++)
		{
			threatPips[i].sprite = i < level ? filledPipImage : emptyPipImage;
			switch (i)
			{
				case 1:
					threatPips[i].color = palette.textL3;
					break;
				case 2:
					threatPips[i].color = palette.ochre;
					break;
				case 3:
					threatPips[i].color = palette.wine;
					break;
			}
		}

		switch (level)
		{
			case 1:
				threatWord.text = "Low";
				threatWord.color = palette.textL3;
				break;
			case 2:
				threatWord.text = "Moderate";
				threatWord.color = palette.ochre;
				break;
			case 3:
				threatWord.text = "High";
				threatWord.color = palette.wine;
				break;
			default:
				threatWord.text = string.Empty;
				break;
		}
	}

	private void RefreshMiniGrid(int[] lockedZones)
	{
		for (int i = 0; i < miniGridCells.Length; i++)
		{
			miniGridCells[i].SetActive(false);
			bool locked = System.Array.IndexOf(lockedZones, i) >= 0;
			if (locked)
				miniGridCells[i].SetActive(true);
		}
	}
}