using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Crew", menuName = "Dead or Alive/Crew")]
public class CrewDefinition : ScriptableObject
{
	public string crewID;
	public CrewType crewType;
	public string displayName;
	public int weight;

	public int baseIncome;

	[FormerlySerializedAs("stayDuration")]
	public int contractDuration;

	public int effectValue;
	public int effectRequirement;

	[FormerlySerializedAs("summonDefinition")]
	public CrewDefinition creationDefinition;

	public bool isTemporary;
	public bool canCopy;

	public GameObject crewPrefab;

	[Header("UI STUFF")]
	public string contractType;

	public string codename;

	[TextArea(10, 5)]
	public string incomeText;

	[TextArea(10, 5)]
	public string descriptionText;

	public string statLabel1;
	public string statValue1;
	public string statLabel2;
	public string statValue2;
	public string statLabel3;
	public string statValue3;
}

public enum CrewType
{
	Rookie,
	Ghost,
	Handler,
	Strategist,
	Enforcer,
	ConArtist,
	Gunslinger,
	Drone,
	Pawn,
	Detonator,
	Anchor,
	Scavenger,
	Loner
}