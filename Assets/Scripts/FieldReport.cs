using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FieldReport : MonoBehaviour
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
	private TMP_Text reportTitle;

	[SerializeField]
	private TMP_Text badgeText;

	[Header("Timing")]
	[SerializeField]
	private float delayBetweenLines = 0.18f;

	[SerializeField]
	private float initialDelay = 0.3f;

	private Coroutine _activeScroll;

	public void ShowSummary(NightReport report, int night)
	{
		panel.SetActive(true);

		if (_activeScroll != null) StopCoroutine(_activeScroll);
		_activeScroll = StartCoroutine(ScrollLog(report, night));

		// foreach (Transform child in lineContainer)
		// 	Destroy(child.gameObject);
		//
		// foreach (NightReportEvent e in report.typedEvents)
		// {
		// 	GameObject go = Instantiate(linePrefab, lineContainer);
		// 	ReportLineUI line = go.GetComponent<ReportLineUI>();
		// 	line.Initialize(e);
		// }
		//
		// // subtotalText.text = subtotalText.text = $"¥{report.finalIncome:N0}";
		// badgeText.text = $"Night 0{night} · Complete";
		// reportTitle.text = $"— Night 0{night} begins —";
		// nightTotalText.text = $"¥{report.finalIncome:N0}";
		// weekRunningText.text =
		// 	$"¥{FormatCurrency(GameManager.Instance.money)} / ¥{FormatCurrency(GameManager.Instance.weeklyTarget)}";
		// progressBar.fillAmount = (float)GameManager.Instance.money / GameManager.Instance.weeklyTarget;
	}

	private IEnumerator ScrollLog(NightReport report, int night)
	{
		foreach (Transform child in lineContainer)
			Destroy(child.gameObject);

		reportTitle.text = $"— Night 0{night} begins —";
		badgeText.text = $"Night 0{night} · Complete";
		nightTotalText.text = "";

		var gm = GameManager.Instance;
		int previousMoney = gm.money - report.finalIncome;

		weekRunningText.text = $"¥{FormatCurrency(previousMoney)} / ¥{FormatCurrency(gm.weeklyTarget)}";
		progressBar.fillAmount = Mathf.Clamp01((float)previousMoney / gm.weeklyTarget);

		yield return new WaitForSeconds(initialDelay);

		foreach (NightReportEvent e in report.typedEvents)
		{
			SpawnLine(e);
			yield return new WaitForSeconds(delayBetweenLines);
		}

		yield return new WaitForSeconds(0.1f);
		nightTotalText.text = $"¥{report.finalIncome:N0}";

		yield return StartCoroutine(TickWeekRunning(previousMoney));
		yield return StartCoroutine(AnimateProgressBar(previousMoney));
	}

	private void SpawnLine(NightReportEvent e)
	{
		var go = Instantiate(linePrefab, lineContainer);
		var line = go.GetComponent<ReportLineUI>();
		line.Initialize(e);
	}

	private IEnumerator TickWeekRunning(int from)
	{
		var gm = GameManager.Instance;
		int to = gm.money;
		float duration = 0.5f;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			int current = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
			weekRunningText.text = $"¥{FormatCurrency(current)} / ¥{FormatCurrency(gm.weeklyTarget)}";
			yield return null;
		}

		weekRunningText.text = $"¥{FormatCurrency(to)} / ¥{FormatCurrency(gm.weeklyTarget)}";
	}

	private IEnumerator AnimateProgressBar(int previousMoney)
	{
		var gm = GameManager.Instance;
		float startFill = Mathf.Clamp01((float)previousMoney / gm.weeklyTarget);
		float targetFill = Mathf.Clamp01((float)gm.money / gm.weeklyTarget);
		float elapsed = 0f;
		float duration = 0.4f;

		var p = DoAPalette.Instance;
		progressBar.color = targetFill >= 0.85f ? p.wineBright : p.ochre;
		progressBar.fillAmount = startFill;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			progressBar.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
			yield return null;
		}

		progressBar.fillAmount = targetFill;
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