using UnityEngine;

public class MonsterView : MonoBehaviour
{
	private MonsterInstance instance;

	public void Initialize(MonsterInstance newInstance)
	{
		instance = newInstance;
	}
}
