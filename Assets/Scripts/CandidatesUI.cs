using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CandidatesUI : MonoBehaviour
{
	[SerializeField]
	private Transform arrivalButtonContainer;

	[SerializeField]
	private GameObject arrivalButtonPrefab;

	[SerializeField]
	private TMP_Text header;

	public void Start()
	{
		header.text = $"Candidates · Night 01";
	}

	public void UpdateArrivalUI(List<CrewDefinition> dailyArrivals)
	{
		foreach (Transform child in arrivalButtonContainer)
		{
			Destroy(child.gameObject);
		}

		foreach (var crew in dailyArrivals)
		{
			GameObject buttonGO = Instantiate(
				arrivalButtonPrefab,
				arrivalButtonContainer
			);

			var view = buttonGO.GetComponent<CrewCard>();
			view.Initialize(crew);
		}

		header.text = $"Candidates · Night 0{GameManager.Instance.currentNight}";
	}
}