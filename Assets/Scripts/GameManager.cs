using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
	private static GameManager _instance;

	public static GameManager Instance => _instance;

	[SerializeField]
	private GridManager gridManager;

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
	}

	public void StartNight()
	{
		if (!runActive)
			return;

		deathsThisNight = 0;
		multiplier = 1f;

		ResetIncome();

		ApplyAdjacencyBuffs();
		ApplySummons();
		ApplyKillEffects();
		CleanupDead();
		ApplyMultipliers();
		int income = CalculateIncome();
		money += income;
		CleanupTemporary();

		Debug.Log("Night ended. Earned: " + income + " | Total Money: " + money);

		currentNight++;
		UpdateText();

		dailyArrivals.Clear();

		if (currentNight > totalNights)
			EndWeek();
		else
			GenerateDailyArrivals();
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
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant != null)
				{
					room.Occupant.currentIncome =
						room.Occupant.Definition.baseIncome;
				}
			}
		}
	}

	public void CleanupDead()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant != null && !room.Occupant.isAlive)
				{
					if (room.Occupant.view != null)
						Destroy(room.Occupant.view.gameObject);

					room.ClearOccupant();
				}
			}
		}
	}

	private void CleanupTemporary()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant != null && room.Occupant.isTemporary)
				{
					if (room.Occupant.view != null)
						Destroy(room.Occupant.view.gameObject);

					room.ClearOccupant();
				}
			}
		}
	}

	private void ApplyAdjacencyBuffs()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

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
					}
				}
			}
		}
	}

	private void ApplySummons()
	{
		List<(Room room, MonsterDefinition definition)> summons = new();

		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

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
					}
				}
			}
		}

		foreach (var summon in summons)
		{
			SpawnMonsterInRoom(summon.room, summon.definition);
		}
	}

	private void ApplyKillEffects()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

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

				monster.currentIncome += kills * monster.Definition.effectValue;
			}
		}
	}

	public void ApplyMultipliers()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant == null)
					continue;

				var monster = room.Occupant;

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
			}
		}
	}

	private int CalculateIncome()
	{
		int income = 0;

		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant != null)
					income += room.Occupant.currentIncome;
			}
		}

		income = Mathf.RoundToInt(income * multiplier);

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

		UpdateText();
		ClearBoard();
		GenerateDailyArrivals();
	}

	private void ClearBoard()
	{
		for (int x = 0; x < gridManager.Width; x++)
		{
			for (int y = 0; y < gridManager.Height; y++)
			{
				Room room = gridManager.Rooms[x, y];

				if (room.Occupant != null)
				{
					Destroy(room.Occupant.view.gameObject);
					room.ClearOccupant();
				}
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