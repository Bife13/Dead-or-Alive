using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightSummaryUI : MonoBehaviour
{
	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private Transform lineContainer;

	[SerializeField]
	private GameObject linePrefab;

	[SerializeField]
	private TMP_Text nightTotalText;

	[SerializeField]
	private TMP_Text weekRunningText;

	[SerializeField]
	private Image progressBar;

	[SerializeField]
	private GameObject reportPanel;


	public void ShowSummary(NightReport report, int night)
	{
		panel.SetActive(true);

		foreach (Transform child in lineContainer)
			Destroy(child.gameObject);

		foreach (NightReportEvent e in report.typedEvents)
		{
			GameObject go = Instantiate(linePrefab, lineContainer);
			ReportLineUI line = go.GetComponent<ReportLineUI>();
			line.Initialize(e);
		}

		// subtotalText.text = subtotalText.text = $"¥{report.finalIncome:N0}";
		nightTotalText.text = $"¥{report.finalIncome:N0}";
		weekRunningText.text =
			$"¥{FormatCurrency(GameManager.Instance.money)} / ¥{FormatCurrency(GameManager.Instance.weeklyTarget)}";
		progressBar.fillAmount = (float)GameManager.Instance.money / GameManager.Instance.weeklyTarget;
	}

	private string FormatCurrency(int value)
	{
		if (value >= 1000) return $"{value / 1000}K";
		return value.ToString();
	}

	// public void ShowSummary(NightReport report, int night)
	// {
	// 	panel.SetActive(true);
	//
	// 	string summary = "";
	//
	// 	summary += $"Night {night} Results\n\n";
	// 	summary += $"Base Income: {report.baseIncome}\n";
	// 	
	// 	if (report.bonusIncome > 0)
	// 	{
	// 		summary += $"Bonus Income: +{report.bonusIncome}\n";
	// 	}
	//
	// 	if (report.killBonus > 0)
	// 	{
	// 		summary += $"Kill Bonus: +{report.killBonus}\n";
	// 	}
	//
	//
	// 	summary += $"x Multiplier: x{report.multiplier}\n";
	// 	summary += "<color=#AAAAAA>────────────────</color>\n";
	// 	summary += $"<size=115%><b>Final Income: {report.finalIncome}</b></size>\n";
	//
	// 	// Checkouts
	// 	if (report.checkouts.Count > 0)
	// 	{
	// 		summary += "\nCheckouts:\n";
	// 		summary += string.Join("\n", report.checkouts);
	// 	}
	//
	// 	// Events
	// 	if (report.events.Count > 0)
	// 	{
	// 		summary += "\n\nEvents:\n";
	// 		summary += "- " + string.Join("\n", report.events);
	// 	}
	//
	// 	summaryText.text = summary;
	// }

	public void Hide()
	{
		panel.SetActive(false);
	}
}