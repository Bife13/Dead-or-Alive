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

	private int weeklyDeathCount = 0;

	private List<CrewInstance> deadCrew;

	[SerializeField]
	private ResolutionDelays resolutionDelays;

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
			if (room.IsOccupied())
				room.Occupant.isResident = true;
		}

		PlacementManager.Instance.ClearSelection();

		StartCoroutine(ResolveNight());
	}

	private IEnumerator ResolveNight()
	{
		deathsThisNight = 0;
		multiplier = 1;
		deadCrew = new List<CrewInstance>();

		NightReport report = new NightReport();
		currentNightLog.afterPlacement = CaptureBoardSnapshot();
		currentNightLog.engineType = DetectEngine(currentNightLog);

		ResetIncome();

		// Adjacency Buffs
		yield return StartCoroutine(ResolveAdjacencyBuffs(report));

		// Creations
		yield return StartCoroutine(ResolveCreations(report));

		currentNightLog.afterCreations = CaptureBoardSnapshot();

		// Detonator
		yield return StartCoroutine(ResolveDetonator(report));

		// Kills
		yield return StartCoroutine(ResolveKillEffects(report));

		yield return StartCoroutine(CleanupDead());
		currentNightLog.afterKills = CaptureBoardSnapshot();

		// Pawns
		yield return StartCoroutine(ResolvePawnDeaths(report));

		// Income
		yield return StartCoroutine(ResolveIncome(report));

		// Finalize
		money += report.finalIncome;

		yield return StartCoroutine(ResolveKillBounty(report));

		if ((money >= weeklyTarget - 5000 || report.finalIncome >= weeklyTarget - 5000) &&
		    currentWeekLog.solvedNight == 0)
			currentWeekLog.solvedNight = currentNight;

		yield return StartCoroutine(CleanupTemporary());

		yield return StartCoroutine(DecreaseAndFinishContract(report));

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

		ShuffleCrewBag();
	}

	public void ShuffleCrewBag()
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
			if (room.IsOccupied())
			{
				room.Occupant.currentIncome =
					room.Occupant.Definition.baseIncome;
			}
		}
	}

	private IEnumerator CleanupDead()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied() || room.Occupant.isAlive) continue;

			yield return StartCoroutine(room.view.FadeOutSlate(resolutionDelays.fade));
			deadCrew.Add(room.Occupant);
			room.ClearOccupant();
		}
	}

	private IEnumerator CleanupTemporary()
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied() || !room.Occupant.isTemporary) continue;

			// yield return
			StartCoroutine(room.view.FadeOutSlate(resolutionDelays.fade));
			room.ClearOccupant();
		}

		yield return new WaitForSeconds(resolutionDelays.fade);
	}

	private IEnumerator DecreaseAndFinishContract(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;

			room.Occupant.DecreaseStay();

			yield return new WaitForSeconds(resolutionDelays.expiryDelay);

			if (room.Occupant.contractDurationRemaining > 0) continue;

			report.checkouts.Add(room.Occupant.Definition.displayName);

			room.view.Flash(DoAPalette.Instance.wineBright, resolutionDelays.expiryDelay);
			yield return new WaitForSeconds(resolutionDelays.expiryDelay);

			yield return StartCoroutine(room.view.FadeOutSlate(resolutionDelays.fade));
			room.ClearOccupant();
		}
	}

	private IEnumerator ResolveAdjacencyBuffs(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.Handler:
					foreach (var adjRoom in gridManager.GetAdjacentRooms(room))
					{
						if (!adjRoom.IsOccupied()) continue;

						adjRoom.Occupant.currentIncome += crew.Definition.effectValue;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.Buff,
							label = "{0} buffs {1}",
							sourceCrew = CrewType.Handler,
							targetCrew = adjRoom.Occupant.Definition.crewType,
							value = crew.Definition.effectValue,
							sourcePosition = room.Position,
							targetPosition = adjRoom.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
						adjRoom.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
						yield return new WaitForSeconds(resolutionDelays.buffDelay);
					}

					break;

				// FUTURE BUFF PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolveCreations(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.ConArtist:
					List<(Room room, CrewDefinition definition)> creations = new();

					foreach (var adjRoom in gridManager.GetAdjacentRooms(room))
					{
						if (adjRoom.IsOccupied()) continue;
						if (creations.Any(c => c.room == adjRoom)) continue;

						creations.Add((adjRoom, crew.Definition.creationDefinition));
						crew.currentIncome += crew.Definition.effectValue;
					}

					if (creations.Count <= 0) continue;

					report.creationBonus += creations.Count;
					report.typedEvents.Add(new NightReportEvent
					{
						type = ReportEventType.Creation,
						label = $"{{0}} creates {creations.Count} {crew.Definition.creationDefinition.displayName}",
						value = creations.Count * crew.Definition.effectValue,
						sourceCrew = CrewType.ConArtist,
						sourcePosition = room.Position,
					});

					room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
					yield return new WaitForSeconds(resolutionDelays.creationDelay);

					foreach (var creation in creations)
					{
						SpawnCrewInRoom(creation.room, creation.definition);
						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
						creation.room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
					}

					yield return new WaitForSeconds(resolutionDelays.creationDelay);
					break;

				// FUTURE CREATION PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolveKillEffects(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.Enforcer:
					int kills = 0;

					foreach (Room adjRoom in gridManager.GetAdjacentRooms(room))
					{
						if (!adjRoom.IsOccupied()) continue;
						if (!adjRoom.Occupant.isAlive) continue;
						if (adjRoom.Occupant.Definition == crew.Definition) continue;

						adjRoom.Occupant.isAlive = false;

						yield return StartCoroutine(TryAnchorSave(adjRoom.Occupant, report));
						if (adjRoom.Occupant.isAlive) continue;

						adjRoom.Occupant.eliminatedBySource = true;
						kills++;
						weeklyDeathCount++;
						deathsThisNight++;

						adjRoom.view.Flash(DoAPalette.Instance.wine, resolutionDelays.killDelay);
					}

					if (kills <= 0) continue;

					int killIncome = kills * crew.Definition.effectValue;
					report.killBonus += killIncome;

					report.typedEvents.Add(new NightReportEvent
					{
						type = ReportEventType.Kill,
						label = $"{{0}} eliminates {kills} crew",
						value = 0,
						sourceCrew = CrewType.Enforcer,
						sourcePosition = room.Position,
					});
					report.typedEvents.Add(new NightReportEvent
					{
						type = ReportEventType.KillBonus,
						label = "{0} kill bonus",
						value = killIncome,
						sourceCrew = crew.Definition.crewType,
						sourcePosition = room.Position,
					});

					yield return new WaitForSeconds(resolutionDelays.killDelay);
					break;

				// FUTURE KILL PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolvePawnDeaths(NightReport report)
	{
		if (deadCrew.Count <= 0) yield break;

		List<CrewInstance> deadPawns = deadCrew
			.Where(c => c.Definition.crewType == CrewType.Pawn && c.eliminatedBySource)
			.ToList();

		if (deadPawns.Count <= 0) yield break;

		foreach (CrewInstance pawn in deadPawns)
		{
			int payout = pawn.Definition.effectValue;

			foreach (Room room in gridManager.GetAllRooms())
			{
				if (!room.IsOccupied()) continue;

				room.Occupant.currentIncome += payout;

				report.typedEvents.Add(new NightReportEvent
				{
					type = ReportEventType.BuffedIncome,
					label = "{0} death payout",
					sourceCrew = CrewType.Pawn,
					targetCrew = room.Occupant.Definition.crewType,
					value = payout,
				});

				room.view.Flash(DoAPalette.Instance.ochre, resolutionDelays.incomeDelay);
				yield return new WaitForSeconds(resolutionDelays.incomeDelay);
			}
		}
	}

	private IEnumerator ResolveIncome(NightReport report)
	{
		int income = report.killBonus;
		int baseIncome = 0;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;
			int finalIncome = crew.currentIncome;
			baseIncome += crew.currentIncome;

			if (crew.Definition.baseIncome > 0)
			{
				report.typedEvents.Add(new NightReportEvent
				{
					type = ReportEventType.BaseIncome,
					label = "{0} base income",
					value = crew.Definition.baseIncome,
					sourceCrew = crew.Definition.crewType,
					sourcePosition = room.Position,
				});

				room.view.Flash(DoAPalette.Instance.ochre, resolutionDelays.incomeDelay);
				yield return new WaitForSeconds(resolutionDelays.incomeDelay);
			}

			switch (crew.Definition.crewType)
			{
				case CrewType.Ghost:
					int emptyAdj = CountEmptyAdjacent(room);
					int ghostBonus = Mathf.Min(
						emptyAdj * crew.Definition.effectValue,
						crew.Definition.effectRequirement);

					if (emptyAdj > 0)
					{
						finalIncome += ghostBonus;
						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = $"{{0}} {emptyAdj} empty adj zones",
							value = ghostBonus,
							sourceCrew = CrewType.Ghost,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				case CrewType.Gunslinger:
					if (CountFilledAdjacent(room) == crew.Definition.effectRequirement)
					{
						finalIncome += crew.Definition.effectValue;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = "{0} adj bonus",
							value = crew.Definition.effectValue,
							sourceCrew = CrewType.Gunslinger,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}


					break;
				case CrewType.Loner:
					int emptyZones = CountEmpty();
					if (emptyZones > 0)
					{
						int cappedZones = Mathf.Min(emptyZones, crew.Definition.effectRequirement);
						int lonerBonus = crew.Definition.effectValue * cappedZones;
						finalIncome += lonerBonus;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = $"{{0}} {cappedZones} empty zones",
							value = lonerBonus,
							sourceCrew = CrewType.Loner,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				case CrewType.Scavenger:
					int scavBonus = crew.Definition.effectValue * weeklyDeathCount;

					if (scavBonus > 0)
					{
						finalIncome += scavBonus;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.BuffedIncome,
							label = $"{{0}} scavenged {weeklyDeathCount} deaths",
							value = scavBonus,
							sourceCrew = CrewType.Scavenger,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				// Future income-phase crew added here as new cases
				// Rookie and other flat-income crew need no case —
				// their baseIncome is already handled above.
			}

			income += finalIncome;
		}


		report.baseIncome = baseIncome;
		report.bonusIncome = income - baseIncome - report.killBonus;

		yield return StartCoroutine(ApplyMultipliers(report));

		income = Mathf.RoundToInt(income * multiplier);

		// BOUNTY DRAIN
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
			yield return new WaitForSeconds(resolutionDelays.bountyDelay);
		}

		report.finalIncome = income;

		if (income <= currentWeekLog.peak) yield break;

		currentWeekLog.peak = income;
		currentWeekLog.peakNight = currentNight;
	}

	private IEnumerator ApplyMultipliers(NightReport report)
	{
		int aliveCount = 0;
		foreach (Room room in gridManager.GetAllRooms())
			if (room.IsOccupied() && !room.Occupant.isTemporary)
				aliveCount++;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;


			switch (crew.Definition.crewType)
			{
				case CrewType.Strategist:
					if (deathsThisNight == 0
					    && aliveCount >= crew.Definition.effectRequirement)
					{
						multiplier += crew.Definition.effectValue;

						report.typedEvents.Add(new NightReportEvent
						{
							type = ReportEventType.Multiplier,
							label = "{0} conditions met",
							value = multiplier,
							sourceCrew = CrewType.Strategist,
							sourcePosition = room.Position,
						});

						room.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.multiplierDelay);
						yield return new WaitForSeconds(resolutionDelays.multiplierDelay);
					}

					break;

				// Future multiplier-phase crew added here as new cases
			}

			report.multiplier = multiplier;
		}
	}

	private IEnumerator ResolveKillBounty(NightReport report)
	{
		List<Room> allRooms = gridManager.GetAllRooms().Where(r => r.Occupant != null && !r.Occupant.isTemporary)
			.ToList();
		int threatIndex = bountyManager.ResolveCrewThreat(allRooms.Count);

		if (threatIndex >= 0)
		{
			Room targetRoom = allRooms[threatIndex];
			CrewInstance target = targetRoom.Occupant;
			target.isAlive = false;

			yield return StartCoroutine(TryAnchorSave(target, report));
			if (target.isAlive) yield break;

			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Kill,
				label = "Bounty targets {0}",
				value = 0,
				sourceCrew = target.Definition.crewType,
				sourcePosition = targetRoom.Position
			});

			targetRoom.view.Flash(DoAPalette.Instance.wine, resolutionDelays.killDelay);
			yield return new WaitForSeconds(resolutionDelays.killDelay);

			weeklyDeathCount++;
			target.eliminatedBySource = true;
			yield return StartCoroutine(CleanupDead());
		}
	}

	private IEnumerator TryAnchorSave(CrewInstance targetCrew, NightReport report)
	{
		List<Room> adjacentRooms = gridManager.GetAdjacentRooms(targetCrew.currentRoom);

		foreach (Room adjRoom in adjacentRooms)
		{
			if (!adjRoom.IsOccupied()) continue;
			var adjCrew = adjRoom.Occupant;

			if (adjCrew.Definition.crewType != CrewType.Anchor) continue;
			if (adjCrew.anchorSaveUsed) continue;

			adjCrew.SetAnchorUse(true);
			targetCrew.contractDurationRemaining = 2;
			targetCrew.isAlive = true;

			report.typedEvents.Add(new NightReportEvent
			{
				type = ReportEventType.Buff,
				label = "{0} shields {1}",
				value = 0,
				sourceCrew = CrewType.Anchor,
				targetCrew = targetCrew.Definition.crewType,
			});

			targetCrew.currentRoom.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
			adjCrew.currentRoom.view.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
			yield return new WaitForSeconds(resolutionDelays.buffDelay);
			yield break;
		}
	}

	public void TriggerDetonator(Room room)
	{
		if (currentPhase != GamePhase.PlanningPhase) return;
		if (!room.IsOccupied()) return;

		CrewInstance crew = room.Occupant;
		if (crew.Definition.crewType != CrewType.Detonator) return;
		if (crew.detonatorUsed) return;

		crew.detonatorUsed = true;
		room.view.GetSlate().RefreshDetonatorButton(crew);
	}

	private IEnumerator ResolveDetonator(NightReport report)
	{
		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied()) continue;
			CrewInstance crew = room.Occupant;

			if (crew.Definition.crewType != CrewType.Detonator) continue;
			if (!crew.detonatorUsed) continue;

			int burst = crew.Definition.effectValue;

			foreach (var adjRoom in gridManager.GetAdjacentRooms(room))
			{
				if (!adjRoom.IsOccupied()) continue;

				adjRoom.Occupant.currentIncome += burst;

				report.typedEvents.Add(new NightReportEvent
				{
					type = ReportEventType.BuffedIncome,
					label = "{0} buffs {1} ",
					sourceCrew = CrewType.Detonator,
					targetCrew = adjRoom.Occupant.Definition.crewType,
					value = burst,
					sourcePosition = room.Position,
					targetPosition = adjRoom.Position,
				});

				adjRoom.view.Flash(DoAPalette.Instance.ochre, resolutionDelays.buffDelay);
				yield return new WaitForSeconds(resolutionDelays.buffDelay);
			}

			// Flash the Detonator cell then remove — not a kill
			room.view.Flash(DoAPalette.Instance.wineBright, resolutionDelays.killDelay);
			yield return new WaitForSeconds(resolutionDelays.killDelay);
			yield return StartCoroutine(room.view.FadeOutSlate(resolutionDelays.fade));
			room.ClearOccupant();
		}
	}

	private int CountEmptyAdjacent(Room room)
	{
		int total = 0;

		foreach (Room adjRoom in gridManager.GetAdjacentRooms(room))
		{
			if (!adjRoom.IsOccupied() && !IsZoneLocked(room.Index))
				total++;
		}

		return total;
	}

	private int CountEmpty()
	{
		int total = 0;

		foreach (Room room in gridManager.GetAllRooms())
		{
			if (!room.IsOccupied() && !IsZoneLocked(room.Index))
				total++;
		}

		return total;
	}

	private int CountFilledAdjacent(Room room)
	{
		int total = 0;

		List<Room> adjacentRooms = gridManager.GetAdjacentRooms(room);

		foreach (Room adjRoom in adjacentRooms)
		{
			if (adjRoom.IsOccupied())
				total++;
		}

		return total;
	}

	private void EndWeek()
	{
		runActive = false;

		currentWeekLog.finalMoney = money;
		currentRunLog.weeks.Add(currentWeekLog);

		weeklyDeathCount = 0;

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

	private void EndRun()
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

		oldRoom.ClearHideOccupant();

		targetRoom.SetOccupant(selected);

		// currentNightLog.placements.Add("Moved " + selected.Definition.displayName + " to " + targetRoom.Position);

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
			if (room.IsOccupied())
			{
				room.ClearHideOccupant();
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
				snapshot[x, y] = room.IsOccupied()
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


				output.AppendLine("-Events:");
				foreach (var ev in night.typedEvents)
					output.AppendLine(BuildLabel(ev.label, ev.sourceCrew, ev.targetCrew));

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

	private string BuildLabel(string template, CrewType? source, CrewType? target)
	{
		string result = template;

		if (source.HasValue)
		{
			string name = GetDisplayName(source.Value);
			result = result.Replace("{0}", $"{name}");
		}

		if (target.HasValue)
		{
			string name = GetDisplayName(target.Value);
			result = result.Replace("{1}", $"{name}");
		}

		return result;
	}

	private string GetDisplayName(CrewType t) => t switch
	{
		CrewType.ConArtist => "Con Artist",
		_ => t.ToString()
	};

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