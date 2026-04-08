using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class WeekDossierUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text weekLabel;

	[SerializeField]
	private TMP_Text locationText;

	[SerializeField]
	private TMP_Text bountyNameText;

	[SerializeField]
	private TMP_Text caseNumberText;

	[SerializeField]
	private TMP_Text modifierNameText;

	[SerializeField]
	private TMP_Text modifierDescText;

	[SerializeField]
	private TMP_Text runningCostText;

	[SerializeField]
	private List<Image> threatPips;

	[SerializeField]
	private Sprite filledPip;

	public void Awake()
	{
		gameObject.SetActive(false);
	}

	public void Show(int week, BountyData bountyData, int weeklyTarget)
	{
		weekLabel.text = $"WEEK {week:D2}";
		locationText.text = bountyData.location.ToUpper();
		bountyNameText.text = $"{bountyData.bountyName}";
		caseNumberText.text = bountyData.caseNumber;

		// Modifier
		if (bountyData.modifiers.Length > 0)
		{
			modifierNameText.text = bountyData.modifiers[0].displayName.ToUpper();
			modifierDescText.text = bountyData.modifiers[0].description;
		}

		// Running cost
		runningCostText.text = FormatMoney(weeklyTarget);

		// Threat pips
		for (int i = 0; i < threatPips.Count; i++)
		{
			bool active = i < bountyData.threatLevel;
			if (active)
				threatPips[i].sprite = filledPip;
		}

		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}

	private string FormatMoney(int value)
	{
		if (value >= 1000) return $"¥{value / 1000}K";
		return $"¥{value}";
	}
}