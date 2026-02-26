using TMPro;
using UnityEngine;

public class MonsterView : MonoBehaviour
{
	private MonsterInstance instance;

	[SerializeField]
	private TMP_Text nightsRemText;

	public void Initialize(MonsterInstance newInstance)
	{
		instance = newInstance;
		UpdateNightsRemaining();
	}

	public void UpdateNightsRemaining()
	{
		nightsRemText.SetText(instance.nightsRemaining.ToString());
		if (instance.nightsRemaining == 1)
			nightsRemText.color = Color.red;
		else
			nightsRemText.color = Color.black;
	}
}