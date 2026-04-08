using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunEndUI : MonoBehaviour
{
	[Header("Week Rows")]
	[SerializeField]
	private List<TMP_Text> weekLabels;

	[SerializeField]
	private List<TMP_Text> locationTexts;

	[SerializeField]
	private List<TMP_Text> earningsTexts;

	[SerializeField]
	private List<TMP_Text> statusTexts;

	[Header("Summary")]
	[SerializeField]
	private TMP_Text finalTakeValue;

	[Header("Stamp")]
	[SerializeField]
	private Image stamp;
	
	[SerializeField]
	private TMP_Text stampText;

	public void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ShowWin(List<int> weeklyEarnings, BountyManager bountyManager)
	{
		for (int i = 0; i < 3; i++)
		{
			BountyData bounty = bountyManager.GetBountyData(i + 1);
			weekLabels[i].text = $"WK {(i + 1):D2}";
			locationTexts[i].text = bounty.location;
			earningsTexts[i].text = $"¥{weeklyEarnings[i]:N0}";
			earningsTexts[i].color = DoAPalette.Instance.ochre;
			statusTexts[i].text = "CLEARED";
			statusTexts[i].color = DoAPalette.Instance.verdigris;
		}

		finalTakeValue.text = $"¥{weeklyEarnings[2]:N0}";
		finalTakeValue.color = DoAPalette.Instance.ochre;


		stamp.color = DoAPalette.Instance.verdigris;
		stampText.color = DoAPalette.Instance.verdigris;
		stampText.text = "RUN COMPLETE";
		
		gameObject.SetActive(true);
	}
	
	public void ShowLoss(List<int> weeklyEarnings, int failedWeek, int target, BountyManager bountyManager)
	{
		for (int i = 0; i < 3; i++)
		{
			BountyData bounty = bountyManager.GetBountyData(i + 1);
			weekLabels[i].text = $"WK {(i + 1):D2}";
			locationTexts[i].text = bounty.location;

			if (i < failedWeek - 1)
			{
				// Cleared weeks
				earningsTexts[i].text = $"¥{weeklyEarnings[i]:N0}";
				earningsTexts[i].color = DoAPalette.Instance.ochre;
				statusTexts[i].text = "CLEARED";
				statusTexts[i].color = DoAPalette.Instance.verdigris;
			}
			else if (i == failedWeek - 1)
			{
				// Failed week
				earningsTexts[i].text = $"¥{weeklyEarnings[i]:N0}";
				earningsTexts[i].color = DoAPalette.Instance.wine;
				statusTexts[i].text = "FAILED";
				statusTexts[i].color = DoAPalette.Instance.wine;
			}
			else
			{
				// Unreached weeks
				earningsTexts[i].text = "---";
				earningsTexts[i].color = DoAPalette.Instance.textL3;
				statusTexts[i].text = "---";
				statusTexts[i].color = DoAPalette.Instance.textL3;
			}
		}

		int earned = weeklyEarnings[failedWeek - 1];

		finalTakeValue.text = $"¥{earned:N0}";
		finalTakeValue.color = DoAPalette.Instance.wine;
		
		stamp.color = DoAPalette.Instance.wine;
		stampText.color = DoAPalette.Instance.wine;
		stampText.text = "RUN FAILED";
		
		gameObject.SetActive(true);
	}
	
	public void Hide()
	{
		gameObject.SetActive(false);
	}
}