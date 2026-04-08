using System;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ZoneView : MonoBehaviour, IPointerClickHandler
{
	private Zone _zone;

	[SerializeField]
	private PlacedSlateUI crewPlacedSlate;

	[SerializeField]
	private Image flashOverlay;

	[SerializeField]
	private RectTransform placedStateRect;

	[SerializeField]
	private TMP_Text zoneName;

	[SerializeField]
	private Image border;

	private Coroutine _flashCoroutine;
	private Coroutine _scaleCoroutine;

	public PlacedSlateUI GetSlate() => crewPlacedSlate;

	private bool _isLocked = false;

	public void Initialize(Zone zoneData)
	{
		_zone = zoneData;
		HideSlate();
	}


	public void OnPointerClick(PointerEventData eventData)
	{
		if (GameManager.Instance.CurrentPhase != GamePhase.PlanningPhase)
			return;

		if (ActionBarUI.Instance.HasSelection)
		{
			ActionBarUI.Instance.TryApplyToZone(_zone);
			return;
		}

		if (eventData.button == PointerEventData.InputButton.Left)
		{
			// If clicking a zone with an arrival crew
			if (_zone.Occupant != null)
			{
				PlacementManager.Instance.SelectInstance(_zone.Occupant);
				return;
			}

			// If clicking empty zone and we have a selected instance ALREADY IN HOTEL
			if (_zone.Occupant == null && PlacementManager.Instance.SelectedInstance != null)
			{
				int index = _zone.Position.y * GameManager.Instance.GridWidth + _zone.Position.x;
				if (GameManager.Instance.IsZoneLocked(index))
					return;

				GameManager.Instance.MoveSelectedCrewTo(_zone);
				return;
			}

			// If clicking empty zone and we have selected definition FROM ARRIVAL
			if (_zone.Occupant == null && PlacementManager.Instance.selectedCrew != null)
			{
				int index = _zone.Position.y * GameManager.Instance.GridWidth + _zone.Position.x;
				if (GameManager.Instance.IsZoneLocked(index))
					return;

				GameManager.Instance.PlaceSelectedCrew(_zone);
				return;
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			Debug.Log("RIGHT CLICK");
		}
		else
		{
			Debug.Log("Middle CLICK");
			if (_zone.IsOccupied())
			{
				GameManager.Instance.ReturnSelectedCrew(_zone);
			}
		}
	}

	public void UpdateSlate(CrewInstance instance)
	{
		crewPlacedSlate.gameObject.SetActive(true);
		crewPlacedSlate.InitializeSlate(instance);
		border.color = DoAPalette.Instance.GetCrewColor(instance.Definition.crewType);
	}

	public void HideSlate()
	{
		crewPlacedSlate.gameObject.SetActive(false);
		border.color = _isLocked
			? DoAPalette.ColorHueShift(DoAPalette.Instance.wine, 0.4f)
			: DoAPalette.Instance.border;
	}

	public void SetZoneName(string name)
	{
		zoneName.text = name;
	}

	public void Flash(Color flashColor, float duration = 1f)
	{
		if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
		if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);

		_flashCoroutine = StartCoroutine(FlashRoutine(flashColor, duration));

		if (crewPlacedSlate.gameObject.activeSelf)
			_scaleCoroutine = StartCoroutine(ScalePunch(duration));
	}

	public IEnumerator FlashRoutine(Color flashColor, float duration)
	{
		// Set colour, start at 0 alpha
		flashColor.a = 0f;
		flashOverlay.color = flashColor;

		float targetAlpha = 0.35f;
		float half = duration * 0.5f;
		float elapsed = 0f;

		// Fade in
		while (elapsed < half)
		{
			elapsed += Time.deltaTime;
			flashColor.a = Mathf.Lerp(0f, targetAlpha, elapsed / half);
			flashOverlay.color = flashColor;
			yield return null;
		}

		// Fade out
		elapsed = 0f;
		while (elapsed < half)
		{
			elapsed += Time.deltaTime;
			flashColor.a = Mathf.Lerp(targetAlpha, 0f, elapsed / half);
			flashOverlay.color = flashColor;
			yield return null;
		}

		flashColor.a = 0f;
		flashOverlay.color = flashColor;
		_flashCoroutine = null;
	}

	public IEnumerator ScalePunch(float duration)
	{
		Vector3 originalScale = Vector3.one;
		float punchScale = 1.06f;
		float punchDuration = duration * 0.25f; // punch is quick, first quarter of flash
		float elapsed = 0f;

		// Scale up
		while (elapsed < punchDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / punchDuration;
			placedStateRect.localScale = Vector3.Lerp(originalScale, Vector3.one * punchScale, t);
			yield return null;
		}

		// Scale back
		elapsed = 0f;
		while (elapsed < punchDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / punchDuration;
			placedStateRect.localScale = Vector3.Lerp(Vector3.one * punchScale, originalScale, t);
			yield return null;
		}

		placedStateRect.localScale = originalScale;
		_scaleCoroutine = null;
	}

	public void SetLocked(bool locked)
	{
		_isLocked = locked;
		// Grey out the cell, show a visual indicator
		border.color =
			locked ? DoAPalette.ColorHueShift(DoAPalette.Instance.wine, 0.4f) : DoAPalette.Instance.border;
		zoneName.color =
			locked ? DoAPalette.ColorHueShift(DoAPalette.Instance.wine, 0.6f) : DoAPalette.Instance.textL4;
		// Optionally disable the BG slightly
	}

	public IEnumerator FadeOutSlate(float duration = 0.3f)
	{
		if (!crewPlacedSlate.gameObject.activeSelf) yield break;

		CanvasGroup cg = crewPlacedSlate.CanvasGroup;

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
			yield return null;
		}

		cg.alpha = 1f;
		HideSlate();
	}
}