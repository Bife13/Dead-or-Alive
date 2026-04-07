using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Crew Pool", menuName = "Dead or Alive/Crew Pool")]
public class CrewPool : ScriptableObject
{
	public List<CrewDefinition> pool;
}