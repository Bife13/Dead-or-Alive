using TMPro;
using UnityEngine;

public class NightSummaryUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text summaryText;

	[SerializeField]
	private GameObject panel;

	public void ShowSummary(NightReport report, int night)
	{
		panel.SetActive(true);

		string summary = "";

		summary += $"Night {night} Results\n\n";
		summary += $"Base Income: {report.baseIncome}\n";
		if (report.killBonus > 0)
		{
			summary += $"Kill Bonus: +{report.killBonus}\n";
		}

		summary += $"x Multiplier: x{report.multiplier}\n";
		summary += "<color=#AAAAAA>────────────────</color>\n";
		summary += $"<size=115%><b>Final Income: {report.finalIncome}</b></size>\n";

		// Checkouts
		if (report.checkouts.Count > 0)
		{
			summary += "\nCheckouts:\n";
			summary += string.Join("\n", report.checkouts);
		}

		// Events
		if (report.events.Count > 0)
		{
			summary += "\n\nEvents:\n";
			summary += "- " + string.Join("\n", report.events);
		}

		summaryText.text = summary;
	}

	public void Hide()
	{
		panel.SetActive(false);
	}
}