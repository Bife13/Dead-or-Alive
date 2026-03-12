using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CrewView : MonoBehaviour
{
	private CrewInstance instance;

	[FormerlySerializedAs("contractRemText")]
	[FormerlySerializedAs("nightsRemText")]
	[SerializeField]
	private TMP_Text contractDurText;

	public void Initialize(CrewInstance newInstance)
	{
		instance = newInstance;
		UpdateContractDuration();
	}

	public void UpdateContractDuration()
	{
		contractDurText.SetText(instance.contractDurationRemaining.ToString());
		if (instance.contractDurationRemaining == 1)
			contractDurText.color = Color.red;
		else
			contractDurText.color = Color.black;
	}
}