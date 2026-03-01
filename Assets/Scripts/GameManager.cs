using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum GamePhase
{
	PlanningPhase,
	ResolutionPhase,
	ScorePhase
}

public class GameManager : MonoBehaviour
{
	private static GameManager _instance;

	public static GameManager Instance => _instance;

	[SerializeField]
	private GridManager gridManager;

	[SerializeField]
	private NightSummaryUI summaryUI;

	[SerializeField]
	private GameObject startButton;

	[SerializeField]
	private TMP_Text nightText;

	[SerializeField]
	private TMP_Text moneyText;

	[SerializeField]
	private TMP_Text targetText;

	[SerializeField]
	private Transform arrivalButtonContainer;

	[SerializeField]
	private GameObject arrivalButtonPrefab;

	[SerializeField]
	private List<MonsterDefinition> curatedPool;

	public int extendStayCost;

	public int arrivalsPerDay = 2;


	public int money = 0;
	public int currentNight = 1;
	public int totalNights = 1;

	public int currentWeek;
	public int weeklyTarget = 25;
	public bool runActive = true;

	private int deathsThisNight;
	private float multiplier;
	private List<MonsterDefinition> dailyArrivals = new();

	private GamePhase currentPhase;
	public GamePhase CurrentPhase => currentPhase;

	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(gameObject);
		else
			_instance = this;
	}

	public void Start()
	{
		gridManager.Initialize();
		GenerateDailyArrivals();
		UpdateText();
		currentPhase = GamePhase.PlanningPhase;
	}

	public void StartNight()
	{
		if (!runActive)
			return;

		startButton.SetActive(false);
		currentPhase = GamePhase.ResolutionPhase;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
				room.Occupant.isResident = true;
		}

		PlacementManager.Instance.ClearSelection();

		deathsThisNight = 0;
		multiplier = 1f;

		NightReport report = new NightReport();

		ResetIncome();

		ApplyAdjacencyBuffs(report);
		ApplySummons(report);
		ApplyKillEffects(report);
		CleanupDead();
		ApplyMultipliers(report);
		int income = CalculateIncome(report);
		money += income;
		CleanupTemporary();
		DecreaseStayAndCheckout(report);

		summaryUI.ShowSummary(report, currentNight);
		currentNight++;
		Debug.Log("Night ended. Earned: " + income + " | Total Money: " + money);
		currentPhase = GamePhase.ScorePhase;
	}

	public void NextDay()
	{
		UpdateText();

		dailyArrivals.Clear();

		if (currentNight > totalNights)
			EndWeek();
		else
		{
			GenerateDailyArrivals();
			currentPhase = GamePhase.PlanningPhase;
		}

		startButton.SetActive(true);
	}

	private void GenerateDailyArrivals()
	{
		dailyArrivals.Clear();

		for (int i = 0; i < arrivalsPerDay; i++)
		{
			int index = Random.Range(0, curatedPool.Count);
			dailyArrivals.Add(curatedPool[index]);
		}

		UpdateArrivalUI();
	}

	private void ResetIncome()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
			{
				room.Occupant.currentIncome =
					room.Occupant.Definition.baseIncome;
			}
		}
	}

	public void CleanupDead()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null && !room.Occupant.isAlive)
			{
				if (room.Occupant.view != null)
					Destroy(room.Occupant.view.gameObject);

				room.ClearOccupant();
			}
		}
	}

	private void CleanupTemporary()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null && room.Occupant.isTemporary)
			{
				if (room.Occupant.view != null)
					Destroy(room.Occupant.view.gameObject);

				room.ClearOccupant();
			}
		}
	}

	private void DecreaseStayAndCheckout(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			room.Occupant.DecreaseStay();

			if (room.Occupant.nightsRemaining <= 0)
			{
				report.checkouts.Add(room.Occupant.Definition.displayName);

				if (room.Occupant.view != null)
					Destroy(room.Occupant.view.gameObject);

				room.ClearOccupant();
			}
		}
	}

	private void ApplyAdjacencyBuffs(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			var monster = room.Occupant;

			if (monster.Definition.effectType != EffectType.BuffAdjacentFlat)
				continue;

			var adjacentRooms = gridManager.GetAdjacentRooms(monster.Position);

			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant != null)
				{
					adjRoom.Occupant.currentIncome += monster.Definition.effectValue;
					report.events.Add(
						$"{monster.Definition.displayName} buffed {adjRoom.Occupant.Definition.displayName}.");
				}
			}
		}
	}

	private void ApplySummons(NightReport report)
	{
		List<(Room room, MonsterDefinition definition)> summons = new();

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			var monster = room.Occupant;

			if (monster.Definition.effectType != EffectType.SummonAdjacent)
				continue;

			var adjacentRooms = gridManager.GetAdjacentRooms(monster.Position);

			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant == null &&
				    !summons.Contains((adjRoom, monster.Definition.summonDefinition)))
				{
					summons.Add((adjRoom, monster.Definition.summonDefinition));
					monster.currentIncome += monster.Definition.effectValue;
				}
			}

			report.summonBonus += summons.Count;
			report.events.Add(
				$"{monster.Definition.displayName} summoned {summons.Count} {monster.Definition.summonDefinition.displayName}.");
		}

		foreach (var summon in summons)
		{
			SpawnMonsterInRoom(summon.room, summon.definition);
		}
	}

	private void ApplyKillEffects(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			var monster = room.Occupant;

			if (monster.Definition.effectType != EffectType.KillAdjacent)
				continue;

			var adjacentRooms = gridManager.GetAdjacentRooms(monster.Position);

			int kills = 0;

			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant != null && adjRoom.Occupant.isAlive)
				{
					adjRoom.Occupant.isAlive = false;
					kills++;
					deathsThisNight++;
				}
			}

			int totalKillIncome = kills * monster.Definition.effectValue;

			monster.currentIncome += totalKillIncome;
			report.killBonus += totalKillIncome;
			report.events.Add($"{monster.Definition.displayName} killed {kills} monsters (+{totalKillIncome}).");
		}
	}

	public void ApplyMultipliers(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			var monster = room.Occupant;

			if (monster.Definition.effectType is EffectType.ConditionalMultNoDeaths or EffectType.FlatMultAlways)
			{
				switch (monster.Definition.effectType)
				{
					case EffectType.ConditionalMultNoDeaths:
						if (deathsThisNight == 0)
							multiplier += monster.Definition.effectValue;
						break;
					case EffectType.FlatMultAlways:
						multiplier += monster.Definition.effectValue;
						break;
				}

				report.multiplier = multiplier;
				report.events.Add($"{monster.Definition.displayName} increased multiplier to x{report.multiplier}");
			}
		}
	}

	private int CalculateIncome(NightReport report)
	{
		int income = 0;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
				income += room.Occupant.currentIncome;
		}

		report.baseIncome = income;
		income = Mathf.RoundToInt(income * multiplier);
		report.finalIncome = income;

		return income;
	}

	public void EndWeek()
	{
		runActive = false;
		if (money >= weeklyTarget)
			Debug.Log("YOU WIN! Money:" + money);
		else
			Debug.Log("YOU LOSE! Money:" + money);
	}

	public bool CanPlaceSelected()
	{
		return dailyArrivals.Contains(PlacementManager.Instance.selectedMonster);
	}

	public void TryExtendStay()
	{
		var monster = PlacementManager.Instance.selectedInstance;

		if (currentPhase != GamePhase.PlanningPhase ||
		    monster == null ||
		    !monster.isResident ||
		    money < extendStayCost)
		{
			Debug.Log("Can't extend the stay of this monster");
			return;
		}

		money -= extendStayCost;

		monster.ExtendStay(1);

		UpdateText();
	}

	public void PlaceSelectedMonster(Room room)
	{
		var selected = PlacementManager.Instance.selectedMonster;

		if (!dailyArrivals.Contains(selected))
			return;

		SpawnMonsterInRoom(room, selected);

		dailyArrivals.Remove(selected);

		PlacementManager.Instance.ClearSelection();
		UpdateArrivalUI();
	}

	public void MoveSelectedMonsterTo(Room targetRoom)
	{
		var selected = PlacementManager.Instance.selectedInstance;

		if (selected == null || selected.isResident)
			return;

		Room oldRoom = selected.currentRoom;

		oldRoom.ClearOccupant();
		targetRoom.SetOccupant(selected);

		selected.currentRoom = targetRoom;

		selected.view.transform.position = targetRoom.view.transform.position;
		selected.view.transform.parent = targetRoom.view.transform;
		// PlacementManager.Instance.selectedInstance = null;
	}

	public void SpawnMonsterInRoom(Room room, MonsterDefinition definition)
	{
		MonsterInstance instance = new MonsterInstance(definition, room.Position);
		room.SetOccupant(instance);

		Vector3 spawnPos = room.view.transform.position;

		GameObject monsterGO = Instantiate(
			definition.monsterPrefab,
			spawnPos,
			Quaternion.identity,
			room.view.transform
		);

		MonsterView view = monsterGO.GetComponent<MonsterView>();
		view.Initialize(instance);
		instance.view = view;
	}

	public void ResetRun()
	{
		money = 0;
		currentNight = 1;
		runActive = true;
		currentPhase = GamePhase.PlanningPhase;

		UpdateText();
		ClearBoard();
		GenerateDailyArrivals();
	}

	private void ClearBoard()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
			{
				Destroy(room.Occupant.view.gameObject);
				room.ClearOccupant();
			}
		}
	}

	private void UpdateText()
	{
		nightText.text = "Night: " + currentNight + " / " + totalNights;
		moneyText.text = "Money: " + money;
		targetText.text = "Target: " + weeklyTarget;
	}

	private void UpdateArrivalUI()
	{
		foreach (Transform child in arrivalButtonContainer)
		{
			Destroy(child.gameObject);
		}

		foreach (var monster in dailyArrivals)
		{
			GameObject buttonGO = Instantiate(
				arrivalButtonPrefab,
				arrivalButtonContainer
			);

			var view = buttonGO.GetComponent<ArrivalButtonView>();
			view.Initialize(monster);
		}
	}
}