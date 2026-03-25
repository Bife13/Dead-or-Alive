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
	
}