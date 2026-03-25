using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ActionCardUI : MonoBehaviour, IPointerClickHandler
{
	[SerializeField]
	private Image background;

	[SerializeField]
	private Image bar;

	[SerializeField]
	private Image outline;

	[SerializeField]
	private TMP_Text nameText;

	[SerializeField]
	private TMP_Text costText;

	[SerializeField]
	private TMP_Text descriptionText;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private ActionInstance _instance;
	public ActionInstance Instance => _instance;
	
	public void Initialize(ActionInstance instance)
	{
		_instance = instance;

		ActionDefinition def = instance.Definition;

		nameText.text = def.displayName;
		costText.text = $"−¥{def.cost:N0}";
		descriptionText.text = def.description;
		bar.color = def.accentColor;

		UpdateVisuals();
	}

	public void SetSelected(bool selected)
	{
		if (_instance.isUsed) return;

		var palette = DoAPalette.Instance;
		outline.color = selected ? palette.ochre : palette.border;
		background.color = selected
			? palette.surface
			: palette.panel;
		nameText.color = selected ? palette.ochre : palette.textL1;
	}


	public void SetUsed()
	{
		var palette = DoAPalette.Instance;

		_instance.isUsed = true;
		canvasGroup.alpha = 0.3f;
		outline.color = palette.border;
		background.color = palette.panel;
		nameText.color = palette.textL1;
	}

	private void UpdateVisuals()
	{
		var palette = DoAPalette.Instance;
		canvasGroup.alpha = _instance.isUsed ? 0.3f : 1f;
		outline.color = palette.border;
		background.color = palette.panel;
		nameText.color = palette.textL1;
		costText.color = palette.ochre;
		descriptionText.color = palette.textL3;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (_instance.isUsed) return;
		if (GameManager.Instance.CurrentPhase != GamePhase.PlanningPhase) return;

		ActionBarUI.Instance.SelectCard(this, _instance);
	}
}