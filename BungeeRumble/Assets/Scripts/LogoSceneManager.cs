using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogoSceneManager : MonoBehaviour {

	public GameObject panel;

	private Color initialColor;
	private bool isLogoAnimation;

	private void Start()
	{
		initialColor = panel.GetComponent<Image>().color;
	}

	void Update()
	{
		if (isLogoAnimation == false)
		{
			StartCoroutine(LogoAnimation());
		}
	}

	IEnumerator LogoAnimation()
	{
		isLogoAnimation = true;

		while (true)
		{
			if (panel.GetComponent<Image>().color.a <= 0.0f)
			{
				break;
			}

			initialColor.a -= 0.01f;
			panel.GetComponent<Image>().color = initialColor;
			yield return null;
		}

		while (true)
		{
			if (panel.GetComponent<Image>().color.a >= 1.0f)
			{
				break;
			}

			initialColor.a += 0.01f;
			panel.GetComponent<Image>().color = initialColor;
			yield return null;
		}


		yield return new WaitForSeconds(1.0f);

		SceneManager.LoadScene("StartScene");
	}
}
