using TMPro;
using UnityEngine;

public class ReportLineUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text glyphText;

	[SerializeField]
	private TMP_Text labelText;

	[SerializeField]
	private TMP_Text valueText;


	public void Initialize(NightReportEvent e)
	{
		var palette = DoAPalette.Instance;
		
		switch (e.type)
		{
			case ReportEventType.Buff:
			case ReportEventType.Creation:
				SetLine("▲", e.label, FormatValue(e.value), palette.verdigris);
				break;
			case ReportEventType.BuffedIncome:
			case ReportEventType.KillBonus:
				SetLine("¥", e.label, FormatValue(e.value), palette.verdigris);
				break;
			case ReportEventType.BaseIncome:
				SetLine("¥", e.label, FormatValue(e.value), palette.ochre);
				break;
			case ReportEventType.Kill:
				SetLine("✕", e.label, "−CREW", palette.wine);
				break;
			case ReportEventType.Drain:
				SetLine("▼", e.label, FormatValue(e.value), palette.wine);
				break;
			case ReportEventType.Multiplier:
				SetLine("×", e.label, $"×{e.value}", palette.verdigris);
				break;
		}
	}

	private void SetLine(string glyph, string label, string value, Color color)
	{
		glyphText.text = glyph;
		labelText.text = label;
		valueText.text = value;
		glyphText.color = color;
		labelText.color = color;
		valueText.color = color;
	}

	private string FormatValue(int value)
	{
		return value >= 0 ? $"+¥{value:N0}" : $"−¥{Mathf.Abs(value):N0}";
	}
	
}