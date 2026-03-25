using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "Dead or Alive/Action Definition")]
public class ActionDefinition : ScriptableObject
{
	public string displayName;
	public string description;
	public int cost;
	public Color accentColor;

	[Header("Behavior")]
	public ActionTargetType targetType;
}

public enum ActionTargetType
{
	TargetCrew, // player clicks a crew cell after selecting — Retain, Safehouse etc
	Instant // applies immediately on click — Intel etc
}