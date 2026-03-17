using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "crew Hotel/Crew Definition")]
public class CrewDefinition : ScriptableObject
{
	public string crewID;
	public string displayName;
	public int weight;

	public int baseIncome;

	[FormerlySerializedAs("stayDuration")]
	public int contractDuration;

	public EffectType effectType;
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
	[TextArea(10,15)]
	public string incomeText;
	[TextArea(10,15)]
	public string descriptionText;

	public string statLabel1;
	public string statValue1;
	public string statLabel2;
	public string statValue2;
	public string statLabel3;
	public string statValue3;
	public Color crewColor;
}

public enum EffectType
{
	None,
	BuffAdjacentFlat,
	CreateAdjacent,
	KillAdjacent,
	MoveTowards,
	ConditionalMultNoDeaths,
	FlatMultAlways,
	EmptyAdjacent,
	ExactAdjacency
}