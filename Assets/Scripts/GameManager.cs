using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

	private RunLog currentRunLog = new();
	private WeekLog currentWeekLog = new();
	private NightLog currentNightLog = new();
	private int currentSeed;
	private int seedValue = 0;

	[SerializeField]
	private GridManager gridManager;

	[SerializeField]
	private FieldReport fieldReport;

	[SerializeField]
	private GameObject startButton;

	[SerializeField]
	private List<CrewDefinition> curatedPool;

	public List<CrewDefinition> crewBag;
	private int bagIndex;

	[SerializeField]
	private int crewBagSize;

	public int extendContractCost;

	public int arrivalsPerDay = 2;


	public int money = 0;
	public int currentNight = 1;
	public int totalNights = 1;

	public int currentWeek;
	public int totalWeeks = 3;
	public int weeklyTarget = 25;
	public List<int> weeklyTargets;
	public bool runActive = true;

	private int deathsThisNight;
	private int multiplier;
	private List<CrewDefinition> dailyArrivals = new();

	private GamePhase currentPhase;
	public GamePhase CurrentPhase => currentPhase;

	[Header("Actions")]
	[SerializeField]
	private List<ActionDefinition> availableActions;

	private List<ActionInstance> _currentPowerUps = new();

	[Header("Bounty")]
	[SerializeField]
	private BountyBar bountyBar;

	[SerializeField]
	private BountyManager bountyManager;


	private int _lastPopulatedWeek = -1;

	[SerializeField]
	private CandidatesUI candidatesUI;

	public int GridWidth => gridManager.Width;


	private enum ResolutionStep
	{
		Buff = 0,
		Kill = 1,
		Income = 2,
		Multiplier = 3,
		Creation = 4,
	}

	[FormerlySerializedAs("flashTimes")]
	[SerializeField]
	[Tooltip("0 = Buffs / 1 = Kills / 2 = Income / 3 = Multiplier / 4 = Creation")]
	private List<float> resolutionDelays = new() { 0.35f, 0.5f, 0.25f, 0.4f, 0.4f };

	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(gameObject);
		else
			_instance = this;
	}

	public void Start()
	{
		ResetRun();
		currentNightLog.beforePlacement = CaptureBoardSnapshot();
	}

	public void StartNight()
	{
		if (!runActive) return;

		startButton.SetActive(false);
		currentPhase = GamePhase.ResolutionPhase;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
				room.Occupant.isResident = true;
		}

		PlacementManager.Instance.ClearSelection();

		StartCoroutine(ResolveNight());
	}

	private IEnumerator ResolveNight()
	{
		deathsThisNight = 0;
		multiplier = 1;

		NightReport report = new NightReport();
		currentNightLog.afterPlacement = CaptureBoardSnapshot();
		currentNightLog.engineType = DetectEngine(currentNightLog);

		ResetIncome();

		// Adjacency Buffs
		yield return StartCoroutine(ResolveAdjacencyBuffs(report));

		// Creations
		yield return StartCoroutine(ResolveCreations(report));

		currentNightLog.afterCreations = CaptureBoardSnapshot();

		// Kills
		yield return StartCoroutine(ResolveKillEffects(report));

		CleanupDead();
		currentNightLog.afterKills = CaptureBoardSnapshot();

		// Income
		yield return StartCoroutine(ResolveIncome(report));

		// Finalize
		money += report.finalIncome;

		yield return StartCoroutine(ResolveKillBounty(report));

		if ((money >= weeklyTarget - 5000 || report.finalIncome >= weeklyTarget - 5000) &&
		    currentWeekLog.solvedNight == 0)
			currentWeekLog.solvedNight = currentNight;

		CleanupTemporary();

		DecreaseAndFinishContract(report);

		currentNightLog.endOfNight = CaptureBoardSnapshot();

		fieldReport.ShowSummary(report, currentNight);

		currentNightLog.currentMoney = money;
		currentNightLog.checkouts.AddRange(report.checkouts);
		currentNightLog.typedEvents.AddRange(report.typedEvents);
		currentNightLog.baseIncome = report.baseIncome;
		currentNightLog.bonusIncome = report.bonusIncome;
		currentNightLog.killBonus = report.killBonus;
		currentNightLog.multiplier = report.multiplier;
		currentNightLog.totalIncome = report.finalIncome;
		currentWeekLog.nights.Add(currentNightLog);

		currentNight++;
		currentPhase = GamePhase.ScorePhase;
		currentNightLog = new NightLog();
		currentNightLog.nightNumber = currentNight;
	}

	public void NextDay()
	{
		InitializePowerUps();
		dailyArrivals.Clear();
		currentNightLog.beforePlacement = CaptureBoardSnapshot();

		if (currentNight > totalNights)
			EndWeek();
		else
		{
			GenerateDailyArrivals();
			currentPhase = GamePhase.PlanningPhase;
			startButton.SetActive(true);
		}
	}

	private void GenerateDailyArrivals()
	{
		dailyArrivals.Clear();

		for (int i = 0; i < arrivalsPerDay; i++)
		{
			CrewDefinition crew = crewBag[bagIndex];

			dailyArrivals.Add(crew);
			currentNightLog.arrivals.Add(crew.displayName);
			bagIndex++;

			foreach (crewLog log in currentWeekLog.crewLogs)
			{
				if (log.definition == crew)
					log.timesOffered++;
			}
		}

		candidatesUI.UpdateArrivalUI(dailyArrivals);
	}

	private void GenerateWeeklyBag()
	{
		crewBag.Clear();
		for (int i = 0; i < crewBagSize; i++)
		{
			CrewDefinition crew = GetRandomcrew(curatedPool);
			crewBag.Add(crew);
		}

		ShufflecrewBag();
	}

	public void ShufflecrewBag()
	{
		System.Random rng = new System.Random(currentSeed);
		Shuffle(crewBag, rng);
		bagIndex = 0;
		currentWeekLog.crewBag = crewBag;
	}

	void Shuffle<T>(List<T> list, System.Random rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);

			T temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}
	}

	CrewDefinition GetRandomcrew(List<CrewDefinition> pool)
	{
		int totalWeight = 0;

		foreach (CrewDefinition definition in pool)
			totalWeight += definition.weight;

		int roll = Random.Range(0, totalWeight);

		int cumulative = 0;

		foreach (CrewDefinition definition in pool)
		{
			cumulative += definition.weight;

			if (roll < cumulative)
				return definition;
		}

		return pool[0];
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
				room.ClearOccupant();
			}
		}
	}

	private void DecreaseAndFinishContract(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null)
				continue;

			room.Occupant.DecreaseStay();

			if (room.Occupant.contractDurationRemaining <= 0)
			{
				report.checkouts.Add(room.Occupant.Definition.displayName);

				room.ClearOccupant();
			}
		}
	}

	private IEnumerator ResolveAdjacencyBuffs(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null) continue;
			var crew = room.Occupant;
			if (crew.Definition.effectType != EffectType.BuffAdjacentFlat) continue;

			var adjacentRooms = gridManager.GetAdjacentRooms(room);
			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant == null) continue;

				adjRoom.Occupant.currentIncome += crew.Definition.effectValue;

				report.typedEvents.Add(new NightReportEvent
				{
					type = ReportEventType.Buff,
					label = "{0} buffs {1}",
					sourceCrew = crew.Definition.crewType,
					targetCrew = adjRoom.Occupant.Definition.crewType,
					value = crew.Definition.effectValue,
					sourcePosition = room.Position,
					targetPosition = adjRoom.Position,
				});

				// FLASH THE CELL
				room.view.Flash(DoAPalette.Instance.verdigris);
				adjRoom.view.Flash(DoAPalette.Instance.verdigris);
				yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Buff]);
			}
		}
	}

	private IEnumerator ResolveCreations(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null) continue;
			var crew = room.Occupant;
			if (crew.Definition.effectType != EffectType.CreateAdjacent) continue;

			List<(Room room, CrewDefinition definition)> creations = new();
			var adjacentRooms = gridManager.GetAdjacentRooms(room);

			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant == null &&
				    !creations.Contains((adjRoom, crew.Definition.creationDefinition)))
				{
					creations.Add((adjRoom, crew.Definition.creationDefinition));
					crew.currentIncome += crew.Definition.effectValue;
				}
			}

			if (creations.Count <= 0) continue;

			report.creationBonus += creations.Count;

			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Creation,
				label = $"{{0}} creates {creations.Count} {crew.Definition.creationDefinition.displayName}",
				value = creations.Count * crew.Definition.effectValue,
				sourceCrew = crew.Definition.crewType,
				sourcePosition = room.Position,
			});

			room.view.Flash(DoAPalette.Instance.verdigris);

			yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Creation]);

			foreach (var creation in creations)
			{
				SpawnCrewInRoom(creation.room, creation.definition);
				room.view.Flash(DoAPalette.Instance.verdigris);
				creation.room.view.Flash(DoAPalette.Instance.verdigris);
			}

			yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Creation]);
		}
	}

	private IEnumerator ResolveKillEffects(NightReport report)
	{
		int totalKillIncome = 0;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null) continue;
			var crew = room.Occupant;
			if (crew.Definition.effectType != EffectType.KillAdjacent) continue;

			var adjacentRooms = gridManager.GetAdjacentRooms(room);
			int kills = 0;

			foreach (var adjRoom in adjacentRooms)
			{
				if (adjRoom.Occupant != null && adjRoom.Occupant.isAlive &&
				    adjRoom.Occupant.Definition != crew.Definition)
				{
					adjRoom.Occupant.isAlive = false;
					kills++;
					deathsThisNight++;

					// FLASHED killed cell
					adjRoom.view.Flash(DoAPalette.Instance.wine);
				}
			}

			if (kills <= 0) continue;

			int tempKillIncome = kills * crew.Definition.effectValue;
			totalKillIncome += tempKillIncome;
			//crew.currentIncome += totalKillIncome;


			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Kill,
				label = $"{{0}} eliminates {kills} crew",
				value = 0,
				sourceCrew = crew.Definition.crewType,
				sourcePosition = room.Position,
			});
			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.KillBonus,
				label = "{0} kill bonus",
				value = tempKillIncome,
				sourceCrew = crew.Definition.crewType,
				sourcePosition = room.Position,
			});

			yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Kill]);
		}

		report.killBonus = totalKillIncome;
	}

	private IEnumerator ApplyMultipliers(NightReport report)
	{
		int aliveCount = 0;
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null && !room.Occupant.isTemporary)
				aliveCount++;
		}

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null) continue;
			var crew = room.Occupant;

			if (crew.Definition.effectType is EffectType.ConditionalMultNoDeaths or EffectType.FlatMultAlways)
			{
				switch (crew.Definition.effectType)
				{
					case EffectType.ConditionalMultNoDeaths:
						if (deathsThisNight == 0 && aliveCount >= crew.Definition.effectRequirement)
						{
							multiplier += crew.Definition.effectValue;

							report.typedEvents.Add(new NightReportEvent
							{
								type = ReportEventType.Multiplier,
								label = "{0} conditions met",
								value = multiplier,
								sourceCrew = crew.Definition.crewType,
								sourcePosition = room.Position,
							});

							room.view.Flash(DoAPalette.Instance.verdigris);
							yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Multiplier]);
						}

						break;

					case EffectType.FlatMultAlways:
						multiplier += crew.Definition.effectValue;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.Multiplier,
							label = "{0} conditions met",
							value = multiplier,
							sourceCrew = crew.Definition.crewType,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris);
						yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Multiplier]);
						break;
				}

				report.multiplier = multiplier;
			}
		}
	}

	private IEnumerator ResolveIncome(NightReport report)
	{
		int income = report.killBonus;
		int baseIncome = 0;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant == null) continue;
			int finalIncome = room.Occupant.currentIncome;
			baseIncome += room.Occupant.currentIncome;

			int trueBase = room.Occupant.Definition.baseIncome;
			if (trueBase > 0)
			{
				report.typedEvents.Add(new NightReportEvent
				{
					type = ReportEventType.BaseIncome,
					label = "{0} base income",
					value = trueBase,
					sourceCrew = room.Occupant.Definition.crewType,
					sourcePosition = room.Position,
				});

				room.view.Flash(DoAPalette.Instance.ochre);
				yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Income]);
			}

			switch (room.Occupant.Definition.effectType)
			{
				case EffectType.EmptyAdjacent:
					int rooms = CountEmptyAdjacent(room);
					int tempIncome = rooms * room.Occupant.Definition.effectValue;
					if (tempIncome > room.Occupant.Definition.effectRequirement)
						tempIncome = room.Occupant.Definition.effectRequirement;

					finalIncome += tempIncome;

					if (rooms > 0)
					{
						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = $"{{0}} {rooms} empty adj zones",
							value = tempIncome,
							sourceCrew = room.Occupant.Definition.crewType,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris);
						yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Income]);
					}

					break;
				case EffectType.ExactAdjacency:
					if (CountFilledAdjacent(room) == room.Occupant.Definition.effectRequirement)
					{
						finalIncome += room.Occupant.Definition.effectValue;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = "{0} adj bonus",
							value = room.Occupant.Definition.effectValue,
							sourceCrew = room.Occupant.Definition.crewType,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris);
						yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Income]);
					}

					break;
			}

			income += finalIncome;
		}

		int drain = bountyManager.ApplyIncomeDrain();
		if (drain > 0)
		{
			income -= drain;
			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Drain,
				label = "Protection drain applies",
				value = -drain
			});
			yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Kill]);
		}

		report.baseIncome = baseIncome;
		report.bonusIncome = income - baseIncome - report.killBonus;
		yield return StartCoroutine(ApplyMultipliers(report));
		income = Mathf.RoundToInt(income * multiplier);
		report.finalIncome = income;

		if (income > currentWeekLog.peak)
		{
			currentWeekLog.peak = income;
			currentWeekLog.peakNight = currentNight;
		}
	}

	private IEnumerator ResolveKillBounty(NightReport report)
	{
		var allRooms = gridManager.GetAllRooms().Where(r => r.Occupant != null && !r.Occupant.isTemporary).ToList();
		int threatIndex = bountyManager.ResolveCrewThreat(allRooms.Count);

		if (threatIndex >= 0)
		{
			Room targetRoom = allRooms[threatIndex];
			CrewInstance target = targetRoom.Occupant;

			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Kill,
				label = "Bounty targets {0}",
				value = 0,
				sourceCrew = target.Definition.crewType,
				sourcePosition = targetRoom.Position
			});

			targetRoom.view.Flash(DoAPalette.Instance.wine, 0.8f);
			yield return new WaitForSeconds(resolutionDelays[(int)ResolutionStep.Kill]);

			target.isAlive = false;
			CleanupDead();
		}
	}

	public int CountEmptyAdjacent(Room room)
	{
		int total = 0;

		List<Room> adjacentRooms = gridManager.GetAdjacentRooms(room);

		foreach (Room adjRoom in adjacentRooms)
		{
			if (adjRoom.Occupant == null)
				total++;
		}

		return total;
	}

	public int CountFilledAdjacent(Room room)
	{
		int total = 0;

		List<Room> adjacentRooms = gridManager.GetAdjacentRooms(room);

		foreach (Room adjRoom in adjacentRooms)
		{
			if (adjRoom.Occupant != null)
				total++;
		}

		return total;
	}

	public void EndWeek()
	{
		runActive = false;

		currentWeekLog.finalMoney = money;
		currentRunLog.weeks.Add(currentWeekLog);

		if (money >= weeklyTarget)
		{
			Debug.Log($"YOU COMPLETED WEEK {currentWeek} Money:" + money);
			if (currentWeek < totalWeeks)
				weeklyTarget = weeklyTargets[currentWeek];
			else
			{
				EndRun();
				return;
			}


			currentWeek++;
			currentNight = 1;
			runActive = true;

			currentWeekLog = new WeekLog();

			foreach (var crew in curatedPool)
			{
				currentWeekLog.crewLogs.Add(new crewLog(crew));
			}

			ClearBoard();

			if (currentWeek != _lastPopulatedWeek)
			{
				bountyManager.LoadBountyForWeek(currentWeek);
				bountyBar.Populate(bountyManager.CurrentBounty, weeklyTargets[currentWeek - 1]);
				gridManager.InitializeLocationNames();
				_lastPopulatedWeek = currentWeek;
			}

			currentNightLog = new NightLog();
			currentNightLog.nightNumber = currentNight;
			currentPhase = GamePhase.PlanningPhase;
			startButton.SetActive(true);
			currentNightLog.beforePlacement = CaptureBoardSnapshot();

			GenerateWeeklyBag();
			GenerateDailyArrivals();

			runActive = true;
		}
		else
			EndRun();
	}

	public void EndRun()
	{
		currentRunLog.finalMoney = money;
		ExportRunToFile();
		ResetRun();
	}

	public bool CanPlaceSelected()
	{
		return dailyArrivals.Contains(PlacementManager.Instance.selectedCrew);
	}

	public void TryExtendContract()
	{
		var crew = PlacementManager.Instance.selectedInstance;

		if (currentPhase != GamePhase.PlanningPhase ||
		    crew == null ||
		    !crew.isResident ||
		    money < extendContractCost)
		{
			Debug.Log("Can't extend the stay of this crew");
			return;
		}

		money -= extendContractCost;
		crew.ExtendContract(1);

		currentNightLog.extends.Add(crew.Definition.displayName + " extended to " + crew.contractDurationRemaining);
		currentWeekLog.crewsExtended.Add(crew.Definition.displayName);
	}

	public void PlaceSelectedCrew(Room room)
	{
		var selected = PlacementManager.Instance.selectedCrew;

		if (!dailyArrivals.Contains(selected))
			return;

		CrewInstance instance = new CrewInstance(selected);
		room.SetOccupant(instance);

		currentNightLog.placements.Add("Placed " + selected.displayName + " at " + room.Position);

		foreach (crewLog log in currentWeekLog.crewLogs)
		{
			if (log.definition == selected)
				log.timesPlaced++;
		}

		dailyArrivals.Remove(selected);

		PlacementManager.Instance.ClearSelection();
		candidatesUI.UpdateArrivalUI(dailyArrivals);
	}

	public void MoveSelectedCrewTo(Room targetRoom)
	{
		var selected = PlacementManager.Instance.selectedInstance;

		if (selected == null || selected.isResident)
			return;

		Room oldRoom = selected.currentRoom;

		oldRoom.ClearOccupant();

		targetRoom.SetOccupant(selected);

		currentNightLog.placements.Add("Moved " + selected.Definition.displayName + " to " + targetRoom.Position);

		// PlacementManager.Instance.selectedInstance = null;
	}

	public void SpawnCrewInRoom(Room room, CrewDefinition definition)
	{
		CrewInstance instance = new CrewInstance(definition);
		room.SetOccupant(instance);
	}

	public void ResetRun()
	{
		InitializePowerUps();
		gridManager.Initialize();
		weeklyTarget = weeklyTargets[0];
		money = 0;
		currentNight = 1;
		currentWeek = 1;
		runActive = true;

		currentRunLog = new RunLog();
		currentSeed = Random.Range(int.MinValue, int.MaxValue);
		int finalSeed = seedValue != 0 ? seedValue : currentSeed;
		currentRunLog.seed = finalSeed;
		Random.InitState(finalSeed);

		currentWeekLog = new WeekLog();
		foreach (var crew in curatedPool)
		{
			currentWeekLog.crewLogs.Add(new crewLog(crew));
		}

		ClearBoard();

		if (currentWeek != _lastPopulatedWeek)
		{
			bountyManager.LoadBountyForWeek(currentWeek);
			bountyBar.Populate(bountyManager.CurrentBounty, weeklyTargets[currentWeek - 1]);
			gridManager.InitializeLocationNames();
			_lastPopulatedWeek = currentWeek;
		}

		currentNightLog = new NightLog();
		currentNightLog.nightNumber = currentNight;
		currentPhase = GamePhase.PlanningPhase;
		startButton.SetActive(true);
		currentNightLog.beforePlacement = CaptureBoardSnapshot();

		GenerateWeeklyBag();
		GenerateDailyArrivals();
	}

	public BountyData GetCurrentBounty()
	{
		return bountyManager.CurrentBounty;
	}

	public bool IsZoneLocked(int index)
	{
		return bountyManager.IsZoneLocked(index);
	}

	private void InitializePowerUps()
	{
		_currentPowerUps.Clear();
		foreach (var def in availableActions)
			_currentPowerUps.Add(new ActionInstance(def));

		ActionBarUI.Instance.PopulateHand(_currentPowerUps);
	}

	private void ClearBoard()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (room.Occupant != null)
			{
				room.ClearOccupant();
			}
		}
	}

	public bool TrySpendMoney(int amount)
	{
		if (money < amount) return false;

		money -= amount;
		fieldReport.RefreshAfterSpend(amount);
		return true;
	}

	private string[,] CaptureBoardSnapshot()
	{
		int w = gridManager.Width;
		int h = gridManager.Height;

		string[,] snapshot = new string[w, h];

		for (int x = 0; x < w; x++)
		{
			for (int y = 0; y < h; y++)
			{
				var room = gridManager.Rooms[x, y];
				snapshot[x, y] = room.Occupant != null
					? room.Occupant.Definition.crewID
					: ".";
			}
		}

		return snapshot;
	}

	public void ExportRunToFile()
	{
#if UNITY_EDITOR
		StringBuilder output = new StringBuilder();

		output.AppendLine("=== RUN SUMMARY ===");
		output.AppendLine("Seed: " + currentRunLog.seed);
		output.AppendLine("");
		output.AppendLine("Final Money: " + currentRunLog.finalMoney);
		output.AppendLine("");

		int weekCount = 1;
		foreach (var week in currentRunLog.weeks)
		{
			output.AppendLine("Week: " + weekCount);
			output.AppendLine("");
			weekCount++;
			output.AppendLine("Weekly Money: " + week.finalMoney);
			output.AppendLine("");
			output.AppendLine("Peak: " + week.peak);
			output.AppendLine("");
			output.AppendLine("Peak Night: " + week.peakNight);
			output.AppendLine("");
			output.AppendLine("Solved Night: " + week.solvedNight);
			output.AppendLine("");

			foreach (var night in week.nights)
			{
				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Day  " + night.nightNumber);

				output.AppendLine("-Arrivals:");
				foreach (var a in night.arrivals)
					output.AppendLine("- " + a);
				output.AppendLine();

				output.AppendLine("-Placements:");
				foreach (var p in night.placements)
					output.AppendLine("- " + p);
				output.AppendLine();

				output.AppendLine("-Extends:");
				foreach (var e in night.extends)
					output.AppendLine("- " + e);
				output.AppendLine();

				output.AppendLine("Night " + night.nightNumber);

				// output.AppendLine("-Events:");
				// foreach (var ev in night.events)
				// 	output.AppendLine("- " + ev);

				output.AppendLine("-Events:");
				foreach (var ev in night.typedEvents)
					output.AppendLine($"- [{ev.type}] {ev.label} {ev.value}");

				output.AppendLine("");

				output.AppendLine("Engine Type: " + night.engineType);

				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Base Income: " + night.baseIncome);
				output.AppendLine("Bonus Income: " + night.bonusIncome);
				output.AppendLine("Kill Bonus: " + night.killBonus);
				output.AppendLine("Multiplier: x" + night.multiplier);
				output.AppendLine("Total Income: " + night.totalIncome);

				output.AppendLine("");
				output.AppendLine("Current Money: " + night.currentMoney);

				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Checkouts:");
				foreach (var ev in night.checkouts)
					output.AppendLine("- " + ev);
				output.AppendLine();

				output.AppendLine("Board States:");
				output.AppendLine("Before     | AfterPlace | AfterSumm  | AfterKills | End");
				output.AppendLine("----------------------------------------------------------------");

				int width = gridManager.Width;
				int height = gridManager.Height;

				for (int y = height - 1; y >= 0; y--)
				{
					string row = "";

					row += FormatRow(night.beforePlacement, y, width) + " | ";
					row += FormatRow(night.afterPlacement, y, width) + " | ";
					row += FormatRow(night.afterCreations, y, width) + " | ";
					row += FormatRow(night.afterKills, y, width) + " | ";
					row += FormatRow(night.endOfNight, y, width);

					output.AppendLine(row);
				}

				output.AppendLine();
			}

			output.AppendLine("Final Board:");
			for (int y = gridManager.Height - 1; y >= 0; y--)
			{
				string row = "";
				row += FormatRow(CaptureBoardSnapshot(), y, gridManager.Width);
				output.AppendLine(row);
			}

			output.AppendLine("");

			output.AppendLine("Current Weights:");
			foreach (CrewDefinition definition in curatedPool)
				output.AppendLine("- " + definition.displayName + ": " + definition.weight);
			output.AppendLine("");

			output.AppendLine("All Arrivals:");
			for (int i = 0; i < bagIndex; i++)
				output.AppendLine("- " + week.crewBag[i].displayName);
			// foreach (var a in currentRunLog.crewBag)
			// 	output.AppendLine("- " + a.displayName);
			output.AppendLine("");

			output.AppendLine("All Extensions:");
			foreach (var a in week.crewsExtended)
				output.AppendLine("- " + a);
			output.AppendLine("");
		}


		string folderPath = Path.Combine(Application.dataPath, "../RunLogs");

		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}

		// Create filename with timestamp
		string fileName = "Run_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
		string fullPath = Path.Combine(folderPath, fileName);

		File.WriteAllText(fullPath, output.ToString());


		UnityEditor.EditorUtility.RevealInFinder(fullPath);

		Debug.Log("Run exported to: " + fullPath);
#endif
	}

	private string FormatRow(string[,] board, int y, int width)
	{
		StringBuilder row = new StringBuilder();

		for (int x = 0; x < width; x++)
		{
			row.Append("[");
			row.Append(board[x, y]);
			row.Append("]");
		}

		return row.ToString();
	}

	string DetectEngine(NightLog currentNight)
	{
		var board = currentNight.afterPlacement;

		Dictionary<string, int> counts = new();

		for (int y = 0; y < 3; y++)
		for (int x = 0; x < 3; x++)
		{
			string m = board[x, y];

			if (m == "." || m == "M") continue;

			if (!counts.ContainsKey(m))
				counts[m] = 0;

			counts[m]++;
		}

		bool hasStrategist = counts.ContainsKey(crewIds.Strategist);
		bool hasGhost = counts.ContainsKey(crewIds.Ghost);
		bool hasConArtist = counts.ContainsKey(crewIds.ConArtist);
		bool hasEnforcer = counts.ContainsKey(crewIds.Enforcer);
		bool hasGunslinger = counts.ContainsKey(crewIds.Gunslinger);
		bool hasHandler = counts.ContainsKey(crewIds.Handler);
		bool hasPawn = counts.ContainsKey(crewIds.Pawn);
		bool hasAnchor = counts.ContainsKey(crewIds.Anchor);
		bool hasScavenger = counts.ContainsKey(crewIds.Scavenger);
		bool hasDetonator = counts.ContainsKey(crewIds.Detonator);
		bool hasLoner = counts.ContainsKey(crewIds.Loner);

		if (hasGhost && hasStrategist)
			return "Ghost + Strategist";

		if (hasConArtist && hasEnforcer)
			return "Con Artist + Enforcer";

		if (hasGunslinger && hasStrategist)
			return "Gunslinger + Strategist";

		if (hasHandler && hasStrategist)
			return "Handler + Strategist";

		if (hasGhost)
			return "Ghost";

		if (hasLoner)
			return "Loner";

		if (hasPawn)
			return "Pawn";

		if (hasAnchor)
			return "Anchor";

		if (hasScavenger)
			return "Scavenger";

		if (hasDetonator)
			return "Detonator";

		if (hasConArtist)
			return "Con Artist";

		if (hasStrategist)
			return "Strategist";

		return "Mixed";
	}
}

public static class crewIds
{
	public const string Rookie = "R";
	public const string Ghost = "G";
	public const string Handler = "H";
	public const string Strategist = "T";
	public const string Enforcer = "E";
	public const string ConArtist = "C";
	public const string Gunslinger = "U";
	public const string Pawn = "P";
	public const string Anchor = "A";
	public const string Scavenger = "S";
	public const string Detonator = "D";
	public const string Loner = "L";
}