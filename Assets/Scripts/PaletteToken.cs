using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [ExecuteAlways]
public class PaletteToken : MonoBehaviour
{
	private DoAPalette Palette;

	public enum Token
	{
		// Backgrounds
		BG,
		Panel,
		Surface,
		Border,

		// Text
		TextL1,
		TextL2,
		TextL3,
		TextL4,

		// Accents
		Ochre,
		Verdigris,
		Wine,
		WineDim,
		Black,

		CrewRookie,
		CrewGhost,
		CrewHandler,
		CrewStrategist,
		CrewEnforcer,
		CrewConArtist,
		CrewGunslinger
	}

	public Token token;

	// Cached references — set once, reused
	private Image _image;
	private TMP_Text _tmp;
	private RawImage _raw;

	void OnEnable()
	{
		_image = GetComponent<Image>();
		_tmp = GetComponent<TMP_Text>();
		_raw = GetComponent<RawImage>();
		Apply();
	}

	void OnValidate()
	{
		_image = GetComponent<Image>();
		_tmp = GetComponent<TMP_Text>();
		_raw = GetComponent<RawImage>();
		Apply();
	}

	void Apply()
	{
		if (DoAPalette.Instance == null) return;

		Color c = DoAPalette.Instance.Resolve(token);

		if (_image != null) _image.color = c;
		else if (_tmp != null) _tmp.color = c;
		else if (_raw != null) _raw.color = c;
	}

	public void ApplyFromPalette()
	{
		_image = GetComponent<Image>();
		_tmp = GetComponent<TextMeshProUGUI>();
		_raw = GetComponent<RawImage>();
		Apply();
	}
}