using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
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

	private RunLog _currentRunLog = new();
	private WeekLog _currentWeekLog = new();
	private NightLog _currentNightLog = new();
	private int _currentSeed;
	private int _seedValue = 0;

	[SerializeField]
	private GridManager gridManager;

	[SerializeField]
	private FieldReport fieldReport;

	[SerializeField]
	private GameObject startButton;

	[SerializeField]
	private CrewPool crewPool;

	public List<CrewDefinition> crewBag;
	private int _bagIndex;

	[SerializeField]
	private int crewBagSize;

	public int extendContractCost;

	public int arrivalsPerDay = 2;


	public int money;
	public int currentNight = 1;
	public int totalNights = 1;

	public int currentWeek;
	public int totalWeeks = 3;
	public int weeklyTarget = 25;
	public List<int> weeklyTargets;
	public bool runActive = true;

	private int _deathsThisNight;
	private int _multiplier;
	private readonly List<CrewDefinition> _dailyArrivals = new();

	private GamePhase _currentPhase;
	public GamePhase CurrentPhase => _currentPhase;

	[Header("Actions")]
	[SerializeField]
	private List<ActionDefinition> availableActions;

	private readonly List<ActionInstance> _currentActions = new();

	[Header("Bounty")]
	[SerializeField]
	private BountyBar bountyBar;

	[SerializeField]
	private BountyManager bountyManager;


	private int _lastPopulatedWeek = -1;

	[SerializeField]
	private CandidatesUI candidatesUI;

	public int GridWidth => gridManager.Width;

	private int _weeklyDeathCount;

	public int WeeklyDeathCount => _weeklyDeathCount;


	private List<CrewInstance> _deadCrew;

	[SerializeField]
	private ResolutionDelays resolutionDelays;

	[Header("Weekly UI")]
	[SerializeField]
	private WeekCompleteUI weekCompleteUI;

	[SerializeField]
	private WeekDossierUI weekDossierUI;

	[SerializeField]
	private TitleCardUI titleCardUI;

	[SerializeField]
	private RunEndUI runEndUI;

	private List<int> _weeklyEarnings = new();

	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(gameObject);
		else
			_instance = this;
	}

	public void Start()
	{
		titleCardUI.Show();
	}

	public void StartNight()
	{
		if (!runActive) return;

		startButton.SetActive(false);
		_currentPhase = GamePhase.ResolutionPhase;

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (zone.IsOccupied())
			{
				zone.Occupant.IsResident = true;
				zone.Occupant.CanReposition = false;
			}
		}

		PlacementManager.Instance.ClearSelection();

		StartCoroutine(ResolveNight());
	}

	private IEnumerator ResolveNight()
	{
		_deathsThisNight = 0;
		_multiplier = 1;
		_deadCrew = new List<CrewInstance>();

		NightReport report = new NightReport();
		_currentNightLog.AfterPlacement = CaptureBoardSnapshot();
		_currentNightLog.EngineType = DetectEngine(_currentNightLog);

		ResetIncome();

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;

			if (crew.Definition.crewType == CrewType.Detonator
			    && crew.IsArmedForDetonation)
			{
				crew.DetonatorUsed = true;
				crew.IsArmedForDetonation = false;
				TriggerDetonator(crew.CurrentZone);
			}
		}

		// Adjacency Buffs
		yield return StartCoroutine(ResolveAdjacencyBuffs(report));

		// Creations
		yield return StartCoroutine(ResolveCreations(report));

		_currentNightLog.AfterCreations = CaptureBoardSnapshot();

		// Detonator
		yield return StartCoroutine(ResolveDetonator(report));

		// Kills
		yield return StartCoroutine(ResolveKillEffects(report));

		yield return StartCoroutine(CleanupDead());
		_currentNightLog.AfterKills = CaptureBoardSnapshot();

		// Pawns
		yield return StartCoroutine(ResolvePawnDeaths(report));

		// Income
		yield return StartCoroutine(ResolveIncome(report));

		// Finalize
		money += report.FinalIncome;

		yield return StartCoroutine(ResolveKillBounty(report));

		if ((money >= weeklyTarget - 5000 || report.FinalIncome >= weeklyTarget - 5000) &&
		    _currentWeekLog.SolvedNight == 0)
			_currentWeekLog.SolvedNight = currentNight;

		yield return StartCoroutine(CleanupTemporary());

		yield return StartCoroutine(DecreaseAndFinishContract(report));

		_currentNightLog.EndOfNight = CaptureBoardSnapshot();

		fieldReport.ShowSummary(report, currentNight);

		_currentNightLog.CurrentMoney = money;
		_currentNightLog.Checkouts.AddRange(report.Checkouts);
		_currentNightLog.TypedEvents.AddRange(report.TypedEvents);
		_currentNightLog.BaseIncome = report.BaseIncome;
		_currentNightLog.BonusIncome = report.BonusIncome;
		_currentNightLog.KillBonus = report.KillBonus;
		_currentNightLog.Multiplier = report.Multiplier;
		_currentNightLog.TotalIncome = report.FinalIncome;
		_currentWeekLog.Nights.Add(_currentNightLog);

		currentNight++;
		_currentPhase = GamePhase.ScorePhase;
		_currentNightLog = new NightLog();
		_currentNightLog.NightNumber = currentNight;
	}

	public void NextDay()
	{
		InitializePowerUps();
		_dailyArrivals.Clear();
		_currentNightLog.BeforePlacement = CaptureBoardSnapshot();

		if (currentNight > totalNights)
			EndWeek();
		else
		{
			GenerateDailyArrivals();
			_currentPhase = GamePhase.PlanningPhase;
			startButton.SetActive(true);
		}
	}

	private void GenerateDailyArrivals()
	{
		_dailyArrivals.Clear();

		for (int i = 0; i < arrivalsPerDay; i++)
		{
			CrewDefinition crew = crewBag[_bagIndex];

			_dailyArrivals.Add(crew);
			_currentNightLog.Arrivals.Add(crew.displayName);
			_bagIndex++;

			foreach (CrewLog log in _currentWeekLog.CrewLogs)
			{
				if (log.Definition == crew)
					log.TimesOffered++;
			}
		}

		candidatesUI.UpdateArrivalUI(_dailyArrivals);
	}

	private void GenerateWeeklyBag()
	{
		crewBag.Clear();
		for (int i = 0; i < crewBagSize; i++)
		{
			CrewDefinition crew = GetRandomcrew(crewPool.pool);
			crewBag.Add(crew);
		}

		ShuffleCrewBag();
	}

	public void ShuffleCrewBag()
	{
		System.Random rng = new System.Random(_currentSeed);
		Shuffle(crewBag, rng);
		_bagIndex = 0;
		_currentWeekLog.CrewBag = crewBag;
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
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (zone.IsOccupied())
			{
				zone.Occupant.CurrentIncome =
					zone.Occupant.Definition.baseIncome;
			}
		}
	}

	private IEnumerator CleanupDead()
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied() || zone.Occupant.IsAlive) continue;

			yield return StartCoroutine(zone.View.FadeOutSlate(resolutionDelays.fade));
			_deadCrew.Add(zone.Occupant);
			zone.ClearOccupant();
		}
	}

	private IEnumerator CleanupTemporary()
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied() || !zone.Occupant.IsTemporary) continue;

			// yield return
			StartCoroutine(zone.View.FadeOutSlate(resolutionDelays.fade));
			zone.ClearOccupant();
		}

		yield return new WaitForSeconds(resolutionDelays.fade);
	}

	private IEnumerator DecreaseAndFinishContract(NightReport report)
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;

			zone.Occupant.DecreaseStay();

			yield return new WaitForSeconds(resolutionDelays.expiryDelay);

			if (zone.Occupant.ContractDurationRemaining > 0) continue;

			report.Checkouts.Add(zone.Occupant.Definition.displayName);

			zone.View.Flash(DoAPalette.Instance.wineBright, resolutionDelays.expiryDelay);
			yield return new WaitForSeconds(resolutionDelays.expiryDelay);
			yield return StartCoroutine(zone.View.FadeOutSlate(resolutionDelays.fade));
			zone.ClearOccupant();
		}
	}

	private IEnumerator ResolveAdjacencyBuffs(NightReport report)
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.Handler:
					foreach (var adjZone in gridManager.GetAdjacentZones(zone))
					{
						if (!adjZone.IsOccupied()) continue;
						if (adjZone.Occupant.DetonatorUsed) continue;

						adjZone.Occupant.CurrentIncome += crew.Definition.effectValue;

						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.Buff,
							Label = "{0} buffs {1}",
							SourceCrew = CrewType.Handler,
							TargetCrew = adjZone.Occupant.Definition.crewType,
							Value = crew.Definition.effectValue,
							SourcePosition = zone.Position,
							TargetPosition = adjZone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
						adjZone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
						yield return new WaitForSeconds(resolutionDelays.buffDelay);
					}

					break;

				// FUTURE BUFF PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolveCreations(NightReport report)
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.ConArtist:
					List<(Zone zone, CrewDefinition definition)> creations = new();

					foreach (var adjZone in gridManager.GetAdjacentZones(zone))
					{
						if (adjZone.IsOccupied()) continue;
						if (creations.Any(c => c.zone == adjZone)) continue;

						creations.Add((adjZone, crew.Definition.creationDefinition));
						crew.CurrentIncome += crew.Definition.effectValue;
					}

					if (creations.Count <= 0) continue;

					report.CreationBonus += creations.Count;
					report.TypedEvents.Add(new NightReportEvent
					{
						Type = ReportEventType.Creation,
						Label = $"{{0}} creates {creations.Count} {crew.Definition.creationDefinition.displayName}",
						Value = creations.Count * crew.Definition.effectValue,
						SourceCrew = CrewType.ConArtist,
						SourcePosition = zone.Position,
					});

					zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
					yield return new WaitForSeconds(resolutionDelays.creationDelay);

					foreach (var creation in creations)
					{
						SpawnCrewInZone(creation.zone, creation.definition);
						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
						creation.zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.creationDelay);
					}

					yield return new WaitForSeconds(resolutionDelays.creationDelay);
					break;

				// FUTURE CREATION PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolveKillEffects(NightReport report)
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;

			switch (crew.Definition.crewType)
			{
				case CrewType.Enforcer:
					int kills = 0;

					foreach (Zone adjZone in gridManager.GetAdjacentZones(zone))
					{
						if (!adjZone.IsOccupied()) continue;
						if (!adjZone.Occupant.IsAlive) continue;
						if (adjZone.Occupant.Definition == crew.Definition) continue;

						adjZone.Occupant.IsAlive = false;

						yield return StartCoroutine(TryAnchorSave(adjZone.Occupant, report));
						if (adjZone.Occupant.IsAlive) continue;

						kills++;
						adjZone.Occupant.EliminatedBySource = true;
						_weeklyDeathCount++;
						_deathsThisNight++;
						UpdateScavengers();

						adjZone.View.Flash(DoAPalette.Instance.wine, resolutionDelays.killDelay);
					}

					if (kills <= 0) continue;

					int killIncome = kills * crew.Definition.effectValue;
					report.KillBonus += killIncome;

					report.TypedEvents.Add(new NightReportEvent
					{
						Type = ReportEventType.Kill,
						Label = $"{{0}} eliminates {kills} crew",
						Value = 0,
						SourceCrew = CrewType.Enforcer,
						SourcePosition = zone.Position,
					});
					report.TypedEvents.Add(new NightReportEvent
					{
						Type = ReportEventType.KillBonus,
						Label = "{0} kill bonus",
						Value = killIncome,
						SourceCrew = crew.Definition.crewType,
						SourcePosition = zone.Position,
					});

					yield return new WaitForSeconds(resolutionDelays.killDelay);
					break;

				// FUTURE KILL PHASE CREW ADDED HERE AS NEW CASES
			}
		}
	}

	private IEnumerator ResolvePawnDeaths(NightReport report)
	{
		if (_deadCrew.Count <= 0) yield break;

		List<CrewInstance> deadPawns = _deadCrew
			.Where(c => c.Definition.crewType == CrewType.Pawn && c.EliminatedBySource)
			.ToList();

		if (deadPawns.Count <= 0) yield break;

		foreach (CrewInstance pawn in deadPawns)
		{
			yield return StartCoroutine(SinglePawnDeath(pawn, report));
		}
	}

	private IEnumerator SinglePawnDeath(CrewInstance pawn, NightReport report)
	{
		int payout = pawn.Definition.effectValue;

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			if (zone.Occupant.IsTemporary) continue;

			zone.Occupant.CurrentIncome += payout;

			report.TypedEvents.Add(new NightReportEvent
			{
				Type = ReportEventType.BuffedIncome,
				Label = "{0} death payout",
				SourceCrew = CrewType.Pawn,
				TargetCrew = zone.Occupant.Definition.crewType,
				Value = payout,
			});

			zone.View.Flash(DoAPalette.Instance.ochre, resolutionDelays.incomeDelay);
			yield return new WaitForSeconds(resolutionDelays.incomeDelay);
		}
	}

	private IEnumerator ResolveIncome(NightReport report)
	{
		int income = report.KillBonus;
		int baseIncome = 0;

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;
			int finalIncome = crew.CurrentIncome;
			baseIncome += crew.CurrentIncome;

			if (crew.Definition.baseIncome > 0)
			{
				report.TypedEvents.Add(new NightReportEvent
				{
					Type = ReportEventType.BaseIncome,
					Label = "{0} base income",
					Value = crew.Definition.baseIncome,
					SourceCrew = crew.Definition.crewType,
					SourcePosition = zone.Position,
				});

				zone.View.Flash(DoAPalette.Instance.ochre, resolutionDelays.incomeDelay);
				yield return new WaitForSeconds(resolutionDelays.incomeDelay);
			}

			switch (crew.Definition.crewType)
			{
				case CrewType.Ghost:
					int emptyAdj = CountEmptyAdjacent(zone);
					int ghostBonus = Mathf.Min(
						emptyAdj * crew.Definition.effectValue,
						crew.Definition.effectRequirement);

					if (emptyAdj > 0)
					{
						finalIncome += ghostBonus;
						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.BuffedIncome,
							Label = $"{{0}} {emptyAdj} empty adj zones",
							Value = ghostBonus,
							SourceCrew = CrewType.Ghost,
							SourcePosition = zone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				case CrewType.Gunslinger:
					if (CountFilledAdjacent(zone) == crew.Definition.effectRequirement)
					{
						finalIncome += crew.Definition.effectValue;

						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.BuffedIncome,
							Label = "{0} adj bonus",
							Value = crew.Definition.effectValue,
							SourceCrew = CrewType.Gunslinger,
							SourcePosition = zone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
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

						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.BuffedIncome,
							Label = $"{{0}} {cappedZones} empty zones",
							Value = lonerBonus,
							SourceCrew = CrewType.Loner,
							SourcePosition = zone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				case CrewType.Scavenger:
					int scavBonus = crew.Definition.effectValue * _weeklyDeathCount;

					if (scavBonus > 0)
					{
						finalIncome += scavBonus;

						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.BuffedIncome,
							Label = $"{{0}} scavenged {_weeklyDeathCount} deaths",
							Value = scavBonus,
							SourceCrew = CrewType.Scavenger,
							SourcePosition = zone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.incomeDelay);
						yield return new WaitForSeconds(resolutionDelays.incomeDelay);
					}

					break;

				// Future income-phase crew added here as new cases
				// Rookie and other flat-income crew need no case —
				// their baseIncome is already handled above.
			}

			income += finalIncome;
		}


		report.BaseIncome = baseIncome;
		report.BonusIncome = income - baseIncome - report.KillBonus;

		yield return StartCoroutine(ApplyMultipliers(report));

		income = Mathf.RoundToInt(income * _multiplier);

		// BOUNTY DRAIN
		int drain = bountyManager.ApplyIncomeDrain();
		if (drain > 0)
		{
			income -= drain;
			report.TypedEvents.Add(new NightReportEvent
			{
				Type = ReportEventType.Drain,
				Label = "Protection drain applies",
				Value = -drain
			});
			yield return new WaitForSeconds(resolutionDelays.bountyDelay);
		}

		report.FinalIncome = income;

		if (income <= _currentWeekLog.Peak) yield break;

		_currentWeekLog.Peak = income;
		_currentWeekLog.PeakNight = currentNight;
	}

	private IEnumerator ApplyMultipliers(NightReport report)
	{
		int aliveCount = 0;
		foreach (Zone zone in gridManager.GetAllZones())
			if (zone.IsOccupied() && !zone.Occupant.IsTemporary)
				aliveCount++;

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;


			switch (crew.Definition.crewType)
			{
				case CrewType.Strategist:
					if (_deathsThisNight == 0
					    && aliveCount >= crew.Definition.effectRequirement)
					{
						_multiplier += crew.Definition.effectValue;

						report.TypedEvents.Add(new NightReportEvent
						{
							Type = ReportEventType.Multiplier,
							Label = "{0} conditions met",
							Value = _multiplier,
							SourceCrew = CrewType.Strategist,
							SourcePosition = zone.Position,
						});

						zone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.multiplierDelay);
						yield return new WaitForSeconds(resolutionDelays.multiplierDelay);
					}

					break;

				// Future multiplier-phase crew added here as new cases
			}

			report.Multiplier = _multiplier;
		}
	}

	private IEnumerator ResolveKillBounty(NightReport report)
	{
		List<Zone> allZones = gridManager.GetAllZones().Where(r => r.Occupant != null && !r.Occupant.IsTemporary)
			.ToList();
		int threatIndex = bountyManager.ResolveCrewThreat(allZones.Count);

		if (threatIndex >= 0)
		{
			Zone targetZone = allZones[threatIndex];
			CrewInstance target = targetZone.Occupant;
			target.IsAlive = false;

			yield return StartCoroutine(TryAnchorSave(target, report));
			if (target.IsAlive) yield break;

			report.TypedEvents.Add(new NightReportEvent
			{
				Type = ReportEventType.Kill,
				Label = "Bounty targets {0}",
				Value = 0,
				SourceCrew = target.Definition.crewType,
				SourcePosition = targetZone.Position
			});

			targetZone.View.Flash(DoAPalette.Instance.wine, resolutionDelays.killDelay);
			yield return new WaitForSeconds(resolutionDelays.killDelay);

			_weeklyDeathCount++;
			_deathsThisNight++;
			target.EliminatedBySource = true;
			UpdateScavengers();
			if (target.Definition.crewType == CrewType.Pawn)
				yield return StartCoroutine(SinglePawnDeath(target, report));

			yield return StartCoroutine(CleanupDead());
		}
	}

	private IEnumerator TryAnchorSave(CrewInstance targetCrew, NightReport report)
	{
		if (targetCrew.IsTemporary) yield break;

		List<Zone> adjacentZones = gridManager.GetAdjacentZones(targetCrew.CurrentZone);

		foreach (Zone adjZone in adjacentZones)
		{
			if (!adjZone.IsOccupied()) continue;
			var adjCrew = adjZone.Occupant;

			if (adjCrew.Definition.crewType != CrewType.Anchor) continue;
			if (adjCrew.AnchorSaveUsed) continue;

			adjCrew.SetAnchorUse(true);
			targetCrew.ContractDurationRemaining = 2;
			targetCrew.IsAlive = true;

			report.TypedEvents.Add(new NightReportEvent
			{
				Type = ReportEventType.Buff,
				Label = "{0} shields {1}",
				Value = 0,
				SourceCrew = CrewType.Anchor,
				TargetCrew = targetCrew.Definition.crewType,
			});

			targetCrew.CurrentZone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
			adjCrew.CurrentZone.View.Flash(DoAPalette.Instance.verdigris, resolutionDelays.buffDelay);
			yield return new WaitForSeconds(resolutionDelays.buffDelay);
			yield break;
		}
	}

	public void TriggerDetonator(Zone zone)
	{
		CrewInstance crew = zone.Occupant;
		zone.View.GetSlate().RefreshDetonatorButton(crew);
	}

	private IEnumerator ResolveDetonator(NightReport report)
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied()) continue;
			CrewInstance crew = zone.Occupant;

			if (crew.Definition.crewType != CrewType.Detonator) continue;
			if (!crew.DetonatorUsed) continue;

			int burst = crew.Definition.effectValue;

			foreach (var adjZone in gridManager.GetAdjacentZones(zone))
			{
				if (!adjZone.IsOccupied()) continue;
				if (adjZone.Occupant.IsTemporary) continue;

				adjZone.Occupant.CurrentIncome += burst;

				report.TypedEvents.Add(new NightReportEvent
				{
					Type = ReportEventType.BuffedIncome,
					Label = "{0} buffs {1} ",
					SourceCrew = CrewType.Detonator,
					TargetCrew = adjZone.Occupant.Definition.crewType,
					Value = burst,
					SourcePosition = zone.Position,
					TargetPosition = adjZone.Position,
				});

				adjZone.View.Flash(DoAPalette.Instance.ochre, resolutionDelays.buffDelay);
				yield return new WaitForSeconds(resolutionDelays.buffDelay);
			}

			// Flash the Detonator cell then remove — not a kill
			zone.View.Flash(DoAPalette.Instance.wineBright, resolutionDelays.killDelay);
			yield return new WaitForSeconds(resolutionDelays.killDelay);
			yield return StartCoroutine(zone.View.FadeOutSlate(resolutionDelays.fade));
			zone.ClearOccupant();
		}
	}

	private int CountEmptyAdjacent(Zone zone)
	{
		int total = 0;

		foreach (Zone adjZone in gridManager.GetAdjacentZones(zone))
		{
			if (!adjZone.IsOccupied() && !IsZoneLocked(zone.Index))
				total++;
		}

		return total;
	}

	private int CountEmpty()
	{
		int total = 0;

		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (!zone.IsOccupied() && !IsZoneLocked(zone.Index))
				total++;
		}

		return total;
	}

	private int CountFilledAdjacent(Zone zone)
	{
		int total = 0;

		List<Zone> adjacentZones = gridManager.GetAdjacentZones(zone);

		foreach (Zone adjZone in adjacentZones)
		{
			if (adjZone.IsOccupied())
				total++;
		}

		return total;
	}

	public void UpdateScavengers()
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (zone.IsOccupied() && zone.Occupant.Definition.crewType == CrewType.Scavenger)
				zone.View.GetSlate().ScavengerCounterUpdate();
		}
	}

	private void EndWeek()
	{
		runActive = false;

		_weeklyEarnings.Add(money);
		_currentWeekLog.FinalMoney = money;
		_currentRunLog.Weeks.Add(_currentWeekLog);

		_weeklyDeathCount = 0;

		if (money >= weeklyTarget)
		{
			Debug.Log($"YOU COMPLETED WEEK {currentWeek} Money:" + money);
			if (currentWeek < totalWeeks)
			{
				weekCompleteUI.Show(currentWeek, bountyManager.CurrentBounty, money, true);
				fieldReport.Hide();
				weeklyTarget = weeklyTargets[currentWeek];
			}
			else
			{
				EndRun(true);
				return;
			}

			currentWeek++;
			currentNight = 1;
			runActive = true;

			_currentWeekLog = new WeekLog();

			foreach (var crew in crewPool.pool)
			{
				_currentWeekLog.CrewLogs.Add(new CrewLog(crew));
			}

			ClearBoard();

			if (currentWeek != _lastPopulatedWeek)
			{
				bountyManager.LoadBountyForWeek(currentWeek);
				bountyBar.Populate(bountyManager.CurrentBounty, weeklyTargets[currentWeek - 1]);
				gridManager.InitializeLocationNames();
				_lastPopulatedWeek = currentWeek;
			}

			_currentNightLog = new NightLog();
			_currentNightLog.NightNumber = currentNight;
			_currentNightLog.BeforePlacement = CaptureBoardSnapshot();

			GenerateWeeklyBag();
			GenerateDailyArrivals();
			fieldReport.UpdateTarget(money, weeklyTarget);
		}
		else
			EndRun(false);
	}

	private void EndRun(bool winState)
	{
		runActive = false;
		_currentRunLog.FinalMoney = money;

		if (winState)
			runEndUI.ShowWin(_weeklyEarnings, bountyManager);
		else
			runEndUI.ShowLoss(_weeklyEarnings, currentWeek, weeklyTarget, bountyManager);
		ExportRunToFile();
	}

	public void AdvanceWeekCompleteScreen()
	{
		weekCompleteUI.Hide();
		weekDossierUI.Show(currentWeek, bountyManager.CurrentBounty, weeklyTarget);
	}

	public void AdvanceWeekDossierScreen()
	{
		weekDossierUI.Hide();
		_currentPhase = GamePhase.PlanningPhase;
		startButton.SetActive(true);
		runActive = true;
	}

	public bool CanPlaceSelected()
	{
		return _dailyArrivals.Contains(PlacementManager.Instance.selectedCrew);
	}

	public void TryExtendContract(Zone zone)
	{
		if (!zone.IsOccupied()) return;
		CrewInstance crew = zone.Occupant;
		crew.ExtendContract(1);
		_currentNightLog.Extends.Add(crew.Definition.displayName + " extended to " + crew.ContractDurationRemaining);
		_currentWeekLog.CrewsExtended.Add(crew.Definition.displayName);
	}

	public void MakeCrewRepositionable(Zone zone)
	{
		if (!zone.IsOccupied()) return;
		CrewInstance crew = zone.Occupant;
		crew.CanReposition = true;
	}

	public void PlaceSelectedCrew(Zone zone)
	{
		var selected = PlacementManager.Instance.selectedCrew;

		if (!_dailyArrivals.Contains(selected))
			return;

		CrewInstance instance = new CrewInstance(selected);
		zone.SetOccupant(instance);

		_currentNightLog.Placements.Add("Placed " + selected.displayName + " at " + zone.Position);

		foreach (CrewLog log in _currentWeekLog.CrewLogs)
		{
			if (log.Definition == selected)
				log.TimesPlaced++;
		}

		_dailyArrivals.Remove(selected);

		PlacementManager.Instance.ClearSelection();
		candidatesUI.UpdateArrivalUI(_dailyArrivals);
	}

	public void ReturnSelectedCrew(Zone zone)
	{
		CrewInstance instance = zone.Occupant;
		if (instance.IsResident) return;
		CrewDefinition selected = zone.Occupant.Definition;

		zone.ClearHideOccupant();
		_currentNightLog.Placements.Remove("Placed " + selected.displayName + " at " + zone.Position);

		foreach (CrewLog log in _currentWeekLog.CrewLogs)
		{
			if (log.Definition == selected)
				log.TimesPlaced--;
		}

		_dailyArrivals.Add(selected);

		PlacementManager.Instance.ClearSelection();
		candidatesUI.UpdateArrivalUI(_dailyArrivals);
	}

	public void MoveSelectedCrewTo(Zone targetZone)
	{
		var selected = PlacementManager.Instance.SelectedInstance;

		if (selected == null || (selected.IsResident && !selected.CanReposition))
			return;

		Zone oldZone = selected.CurrentZone;

		oldZone.ClearHideOccupant();

		targetZone.SetOccupant(selected);

		// currentNightLog.placements.Add("Moved " + selected.Definition.displayName + " to " + targetZone.Position);

		// PlacementManager.Instance.selectedInstance = null;
	}

	public void SpawnCrewInZone(Zone zone, CrewDefinition definition)
	{
		CrewInstance instance = new CrewInstance(definition);
		zone.SetOccupant(instance);
	}

	public void ResetRun()
	{
		titleCardUI.Hide();
		InitializePowerUps();
		gridManager.Initialize();
		weeklyTarget = weeklyTargets[0];
		money = 0;
		currentNight = 1;
		currentWeek = 1;
		_weeklyEarnings = new List<int>();
		runActive = true;

		_currentRunLog = new RunLog();
		_currentSeed = Random.Range(int.MinValue, int.MaxValue);
		int finalSeed = _seedValue != 0 ? _seedValue : _currentSeed;
		_currentRunLog.Seed = finalSeed;
		Random.InitState(finalSeed);

		_currentWeekLog = new WeekLog();
		foreach (var crew in crewPool.pool)
		{
			_currentWeekLog.CrewLogs.Add(new CrewLog(crew));
		}

		ClearBoard();

		if (currentWeek != _lastPopulatedWeek)
		{
			bountyManager.LoadBountyForWeek(currentWeek);
			bountyBar.Populate(bountyManager.CurrentBounty, weeklyTargets[currentWeek - 1]);
			gridManager.InitializeLocationNames();
			_lastPopulatedWeek = currentWeek;
		}

		_currentNightLog = new NightLog();
		_currentNightLog.NightNumber = currentNight;
		_currentPhase = GamePhase.PlanningPhase;
		startButton.SetActive(true);
		_currentNightLog.BeforePlacement = CaptureBoardSnapshot();

		GenerateWeeklyBag();
		GenerateDailyArrivals();
		_currentNightLog.BeforePlacement = CaptureBoardSnapshot();

		weekDossierUI.Show(currentWeek, bountyManager.CurrentBounty, weeklyTarget);
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
		_currentActions.Clear();
		foreach (var def in availableActions)
			_currentActions.Add(new ActionInstance(def));

		ActionBarUI.Instance.PopulateHand(_currentActions);
	}

	private void ClearBoard()
	{
		foreach (Zone zone in gridManager.GetAllZones())
		{
			if (zone.IsOccupied())
			{
				zone.ClearHideOccupant();
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
				var zone = gridManager.Zones[x, y];
				snapshot[x, y] = zone.IsOccupied()
					? zone.Occupant.Definition.crewID
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
		output.AppendLine("Seed: " + _currentRunLog.Seed);
		output.AppendLine("");
		output.AppendLine("Final Money: " + _currentRunLog.FinalMoney);
		output.AppendLine("");

		int weekCount = 1;
		foreach (var week in _currentRunLog.Weeks)
		{
			output.AppendLine("Week: " + weekCount);
			output.AppendLine("");
			weekCount++;
			output.AppendLine("Weekly Money: " + week.FinalMoney);
			output.AppendLine("");
			output.AppendLine("Peak: " + week.Peak);
			output.AppendLine("");
			output.AppendLine("Peak Night: " + week.PeakNight);
			output.AppendLine("");
			output.AppendLine("Solved Night: " + week.SolvedNight);
			output.AppendLine("");

			foreach (var night in week.Nights)
			{
				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Day  " + night.NightNumber);

				output.AppendLine("-Arrivals:");
				foreach (var a in night.Arrivals)
					output.AppendLine("- " + a);
				output.AppendLine();

				output.AppendLine("-Placements:");
				foreach (var p in night.Placements)
					output.AppendLine("- " + p);
				output.AppendLine();

				output.AppendLine("-Extends:");
				foreach (var e in night.Extends)
					output.AppendLine("- " + e);
				output.AppendLine();

				output.AppendLine("Night " + night.NightNumber);


				output.AppendLine("-Events:");
				foreach (var ev in night.TypedEvents)
					output.AppendLine(BuildLabel(ev.Label, ev.SourceCrew, ev.TargetCrew));

				output.AppendLine("");

				output.AppendLine("Engine Type: " + night.EngineType);

				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Base Income: " + night.BaseIncome);
				output.AppendLine("Bonus Income: " + night.BonusIncome);
				output.AppendLine("Kill Bonus: " + night.KillBonus);
				output.AppendLine("Multiplier: x" + night.Multiplier);
				output.AppendLine("Total Income: " + night.TotalIncome);

				output.AppendLine("");
				output.AppendLine("Current Money: " + night.CurrentMoney);

				output.AppendLine("");
				output.AppendLine("----------------------------");
				output.AppendLine("");

				output.AppendLine("Checkouts:");
				foreach (var ev in night.Checkouts)
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

					row += FormatRow(night.BeforePlacement, y, width) + " | ";
					row += FormatRow(night.AfterPlacement, y, width) + " | ";
					row += FormatRow(night.AfterCreations, y, width) + " | ";
					row += FormatRow(night.AfterKills, y, width) + " | ";
					row += FormatRow(night.EndOfNight, y, width);

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
			foreach (CrewDefinition definition in crewPool.pool)
				output.AppendLine("- " + definition.displayName + ": " + definition.weight);
			output.AppendLine("");

			output.AppendLine("All Arrivals:");
			for (int i = 0; i < _bagIndex; i++)
				output.AppendLine("- " + week.CrewBag[i].displayName);
			// foreach (var a in currentRunLog.crewBag)
			// 	output.AppendLine("- " + a.displayName);
			output.AppendLine("");

			output.AppendLine("All Extensions:");
			foreach (var a in week.CrewsExtended)
				output.AppendLine("- " + a);
			output.AppendLine("");
		}


		string folderPath = Path.Combine(Application.dataPath, "../RunLogs");

		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}

		// Create filename with timestamp
		string fileName = "Run_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
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
			string displayName = GetDisplayName(source.Value);
			result = result.Replace("{0}", $"{displayName}");
		}

		if (target.HasValue)
		{
			string displayName = GetDisplayName(target.Value);
			result = result.Replace("{1}", $"{displayName}");
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

	string DetectEngine(NightLog night)
	{
		var board = night.AfterPlacement;

		Dictionary<string, int> counts = new();

		for (int y = 0; y < 3; y++)
		for (int x = 0; x < 3; x++)
		{
			string m = board[x, y];

			if (m == "." || m == "M") continue;

			counts.TryAdd(m, 0);

			counts[m]++;
		}

		bool hasStrategist = counts.ContainsKey(CrewIds.Strategist);
		bool hasGhost = counts.ContainsKey(CrewIds.Ghost);
		bool hasConArtist = counts.ContainsKey(CrewIds.ConArtist);
		bool hasEnforcer = counts.ContainsKey(CrewIds.Enforcer);
		bool hasGunslinger = counts.ContainsKey(CrewIds.Gunslinger);
		bool hasHandler = counts.ContainsKey(CrewIds.Handler);
		bool hasPawn = counts.ContainsKey(CrewIds.Pawn);
		bool hasAnchor = counts.ContainsKey(CrewIds.Anchor);
		bool hasScavenger = counts.ContainsKey(CrewIds.Scavenger);
		bool hasDetonator = counts.ContainsKey(CrewIds.Detonator);
		bool hasLoner = counts.ContainsKey(CrewIds.Loner);

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

public static class CrewIds
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