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
		var p = DoAPalette.Instance;

		switch (e.type)
		{
			case ReportEventType.Buff:
			case ReportEventType.Creation:
				SetLine("▲", e.label, e.sourceCrew, e.targetCrew, FormatValue(e.value), p.verdigris, p);
				break;
			case ReportEventType.BuffedIncome:
			case ReportEventType.KillBonus:
				SetLine("¥", e.label, e.sourceCrew, e.targetCrew, FormatValue(e.value), p.verdigris, p);
				break;
			case ReportEventType.BaseIncome:
				SetLine("¥", e.label, e.sourceCrew, e.targetCrew, FormatValue(e.value), p.ochre, p);
				break;
			case ReportEventType.Kill:
				SetLine("✕", e.label, e.sourceCrew, e.targetCrew, "−CREW", p.wine, p);
				break;
			case ReportEventType.Drain:
				SetLine("▼", e.label, e.sourceCrew, e.targetCrew, FormatValue(e.value), p.wine, p);
				break;
			case ReportEventType.Multiplier:
				SetLine("×", e.label, e.sourceCrew, e.targetCrew, $"×{e.value}", p.verdigris, p);
				break;
		}
	}

	private void SetLine(string glyph, string label, CrewType? source, CrewType? target,
		string value, Color accentColor, DoAPalette p)
	{
		glyphText.text = glyph;
		glyphText.color = accentColor;

		// Crew names injected as coloured spans, structural text stays TextL2
		labelText.text = BuildLabel(label, source, target, p);
		labelText.color = p.textL1;

		valueText.text = value;
		valueText.color = accentColor;
	}

	private string BuildLabel(string template, CrewType? source, CrewType? target, DoAPalette p)
	{
		string result = template;

		if (source.HasValue)
		{
			string hex = ColorUtility.ToHtmlStringRGB(p.GetCrewColor(source.Value));
			string name = GetDisplayName(source.Value);
			result = result.Replace("{0}", $"<color=#{hex}>{name}</color>");
		}

		if (target.HasValue)
		{
			string hex = ColorUtility.ToHtmlStringRGB(p.GetCrewColor(target.Value));
			string name = GetDisplayName(target.Value);
			result = result.Replace("{1}", $"<color=#{hex}>{name}</color>");
		}

		return result;
	}

	private string GetDisplayName(CrewType t) => t switch
	{
		CrewType.ConArtist => "Con Artist",
		_ => t.ToString()
	};

	private string FormatValue(int value)
	{
		return value >= 0 ? $"+¥{value:N0}" : $"−¥{Mathf.Abs(value):N0}";
	}
}