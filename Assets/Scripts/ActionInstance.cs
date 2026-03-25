using UnityEngine;

public class ActionInstance
{
	public ActionDefinition Definition { get; }
	public bool isUsed;

	public ActionInstance(ActionDefinition definition)
	{
		Definition = definition;
		isUsed = false;
	}
}