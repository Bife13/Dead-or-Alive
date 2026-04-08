using UnityEngine;

public class TitleCardUI : MonoBehaviour
{
	public void Show()
	{
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}