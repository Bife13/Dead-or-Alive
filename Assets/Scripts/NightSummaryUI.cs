using TMPro;
using UnityEngine;

public class NightSummaryUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text summaryText;

	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private Transform lineContainer;

	[SerializeField]
	private GameObject linePrefab;

	[SerializeField]
	private TMP_Text subtotalText;

	[SerializeField]
	private TMP_Text nightTotalText;

	[SerializeField]
	private TMP_Text weekRunningText;

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
		weekRunningText.text = $"¥{GameManager.Instance.money:N0} / ¥{GameManager.Instance.weeklyTarget:N0}";
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