using System.Collections.Generic;
using UnityEngine;

public class ActionBarUI : MonoBehaviour
{
	public static ActionBarUI Instance { get; private set; }

	[SerializeField]
	private Transform hand;

	[SerializeField]
	private GameObject actionCardPrefab;

	private List<ActionCardUI> _cards = new();
	private ActionCardUI _selectedCard;
	private ActionInstance _selectedInstance;
	public bool HasSelection => _selectedInstance != null;

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(gameObject);
		else Instance = this;
	}

	public void PopulateHand(List<ActionInstance> powerUps)
	{
		foreach (Transform child in hand)
			Destroy(child.gameObject);

		_cards.Clear();
		_selectedCard = null;
		_selectedInstance = null;

		foreach (var instance in powerUps)
		{
			var go = Instantiate(actionCardPrefab, hand);
			var card = go.GetComponent<ActionCardUI>();
			card.Initialize(instance);
			_cards.Add(card);
		}
	}

	public void SelectCard(ActionCardUI card, ActionInstance instance)
	{
		// Deselect previous
		if (_selectedCard != null)
			_selectedCard.SetSelected(false);

		// Toggle off if same card
		if (_selectedCard == card)
		{
			_selectedCard = null;
			_selectedInstance = null;
			return;
		}

		_selectedCard = card;
		_selectedInstance = instance;
		card.SetSelected(true);
	}

	public bool TryApplyToRoom(Room room)
	{
		if (_selectedInstance == null) return false;
		if (_selectedInstance.Definition.targetType != ActionTargetType.TargetCrew) return false;
		if (room.Occupant == null) return false;

		if (GameManager.Instance.TrySpendMoney(_selectedInstance.Definition.cost))
		{
			ApplyAction(_selectedInstance, room);
			_selectedCard.SetUsed();
			DeselectCurrent();

			return true;
		}

		DeselectCurrent();
		return false;
	}

	private void ApplyAction(ActionInstance instance, Room room)
	{
		// For now just Retain — extend contract by 1
		room.Occupant.ExtendContract(1);
	}

	private void DeselectCurrent()
	{
		if (_selectedCard != null)
			_selectedCard.SetSelected(false);

		_selectedCard = null;
		_selectedInstance = null;
	}
}