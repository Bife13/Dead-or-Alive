using UnityEngine;

[CreateAssetMenu(fileName = "New Bounty", menuName = "Dead or Alive/Bounty")]
public class BountyData : ScriptableObject
{
	[Header("Identity")]
	public string bountyName;

	public string caseNumber;
	public string alias;
	public string location;

	public int week;

	[Range(1, 3)]
	public int threatLevel;

	[Header("Modifiers")]
	[Tooltip("Max 2.")]
	public BountyModifier[] modifiers;
}