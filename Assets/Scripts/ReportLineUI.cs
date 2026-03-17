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

	private static readonly Color Verdigris = new Color(0.29f, 0.47f, 0.44f);
	private static readonly Color Ochre = new Color(0.78f, 0.49f, 0.09f);
	private static readonly Color Wine = new Color(0.42f, 0.08f, 0.13f);

	public void Initialize(NightReportEvent e)
	{
		switch (e.type)
		{
			case ReportEventType.Buff:
			case ReportEventType.Creation:
				SetLine("▲", e.label, FormatValue(e.value), Verdigris);
				break;
			case ReportEventType.BuffedIncome:
			case ReportEventType.KillBonus:
				SetLine("¥", e.label, FormatValue(e.value), Verdigris);
				break;
			case ReportEventType.BaseIncome:
				SetLine("¥", e.label, FormatValue(e.value), Ochre);
				break;
			case ReportEventType.Kill:
				SetLine("✕", e.label, "−CREW", Wine);
				break;
			case ReportEventType.Drain:
				SetLine("▼", e.label, FormatValue(e.value), Wine);
				break;
			case ReportEventType.Multiplier:
				SetLine("×", e.label, $"×{e.value}", Verdigris);
				break;
		}
	}

	private void SetLine(string glyph, string label, string value, Color colour)
	{
		glyphText.text = glyph;
		labelText.text = label;
		valueText.text = value;
		glyphText.color = colour;
		labelText.color = colour;
		valueText.color = colour;
	}

	private string FormatValue(int value)
	{
		return value >= 0 ? $"+¥{value:N0}" : $"−¥{Mathf.Abs(value):N0}";
	}
}