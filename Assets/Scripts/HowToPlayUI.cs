using UnityEngine;

public class HowToPlayUI : MonoBehaviour
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