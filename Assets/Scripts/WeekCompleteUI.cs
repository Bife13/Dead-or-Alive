using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class WeekCompleteUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text weekLabel;

	[SerializeField]
	private TMP_Text locationText;

	[SerializeField]
	private TMP_Text bountyNameText;

	[SerializeField]
	private TMP_Text caseNumberText;

	[SerializeField]
	private TMP_Text earnedValueText;

	[SerializeField]
	private TMP_Text stampText;

	[SerializeField]
	private Image stampImage;

	public void Awake()
	{
		gameObject.SetActive(false);
	}

	public void Show(int week, BountyData bountyData, int weeklyMoney, bool complete)
	{
		weekLabel.text = $"WEEK {week:D2} -- COMPLETE";
		locationText.text = bountyData.location.ToUpper();
		bountyNameText.text = $"{bountyData.bountyName}";
		caseNumberText.text = bountyData.caseNumber;
		earnedValueText.text = $"¥{weeklyMoney:N0}";
		if (complete)
		{
			stampText.text = "COMPLETE";
			stampImage.color = DoAPalette.Instance.verdigris;
		}
		else
		{
			stampText.text = "FAIL";
			stampImage.color = DoAPalette.Instance.wine;
		}

		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}