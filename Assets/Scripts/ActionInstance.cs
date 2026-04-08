using UnityEngine;

public class ActionInstance
{
	public ActionDefinition Definition { get; }
	public bool IsUsed;

	public ActionInstance(ActionDefinition definition)
	{
		Definition = definition;
		IsUsed = false;
	}
}