using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public GameObject startCanvas;
	public GameObject lobbyCanvas;
	public GameObject gameCanvas;
    public GameObject resultCanvas;

    // StartScene
    public GameObject[] characterImages;
	public GameObject titleImage;
	public GameObject startSceneButtons;
	public Sprite[] startCharacterSprites;
	[SerializeField]
	private float characterImageFadeInWaitTime;
	[SerializeField]
	private float titleImageFadeInWaitTime;

	public GameObject starParticle;

    // LobbyScene
    public GameObject startButton;
	public GameObject readyButton;
	public GameObject[] readyStateImages;
	public Sprite[] readyStateSprites;
	public Sprite[] unReadyCharacterSprites;
	public Sprite emptyPlayerSprite;
	public Sprite[] readyButtonSprites;
	public Sprite hostSprites;
	public GameObject[] characterModelings;
	public GameObject[] kickMessageBoxs;
	public GameObject masterKickedCountDownText;
	
	// LobbyScene && GameScene
	public GameObject myCharacterImages;
	public GameObject[] otherCharacterImages;
	public Sprite[] characterSprites;

	// GameScene
	//public bool[] isPlayerDeads;
	public GameObject[] characterSurvivalImages;
	public Sprite[] survivalSprites;
	public GameObject[] characterColorImages;
    public GameObject[] itemslot;
	public Sprite[] ItemBackGrounds;
	public GameObject timerImage;
	public GameObject gameStartCountDownText;
	public GameObject winPanel;
	public GameObject losePanel;
	public GameObject winImage;
	public GameObject loseImage;
	public GameObject[] winCharacterImages;
	public GameObject[] loseCharacterImages;
	public GameObject optionsPanel;
	[HideInInspector]
	public bool isOptionsPopup;
	public GameObject exitButton;


    public List<int> initialAllPlayerActorIDList = null;
	public Dictionary<int, bool> initialAllPlayerConfirmSurvivalList = null;

	public static UIManager instance = null;


	// Use this for initialization
	void Awake()
	{
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);

		DontDestroyOnLoad(gameObject);
	}

	void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "LogoScene")
		{
			startCanvas.SetActive(false);
			lobbyCanvas.SetActive(false);
			gameCanvas.SetActive(false);
			// resultCanvas가 gameCanvas의 자식이 아닐때는 꺼줘야됨
			resultCanvas.SetActive(false);
		}
		else if (scene.name == "StartScene")
		{
			// GameScene에서 설정했던 Cursor를 StartScene에 맞게 변경
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			startCanvas.SetActive(true);
			lobbyCanvas.SetActive(false);
			gameCanvas.SetActive(false);
			// resultCanvas가 gameCanvas의 자식이 아닐때는 꺼줘야됨
			resultCanvas.SetActive(false);

			// 추후 FadeIn될 이미지를 랜덤하게 넣을 예정
			StartCoroutine(StartSceneUIAnimation());

			//Invoke("StarParticleInstantiate", 5.0f);
        }
		else if (scene.name == "LobbyScene")
			// OnJoinedRoom() 에서 할까? 씬이 로드 될때 해주는게 맞으니까
		{
			startCanvas.SetActive(false);
			lobbyCanvas.SetActive(true);
			gameCanvas.SetActive(false);
			// resultCanvas가 gameCanvas의 자식이 아닐때는 꺼줘야됨
			resultCanvas.SetActive(false);

            // 방장 표시는 모든 플레이어에게 항상 일정한 위치로 보여야 함
			// 필요한 코드인가?
            readyStateImages[0].GetComponent<Image>().sprite = hostSprites;
			
			// 다만 방장이 이미 있고 다른 사람이 들어온다면 
			if (PhotonNetwork.room.PlayerCount > 1)
			{
				// 그 플레이어의 레디현황은 레디 하지 않은 상태
				readyStateImages[PhotonNetwork.room.PlayerCount - 1].GetComponent<Image>().sprite = readyStateSprites[1];
				// 흑백처리
				//otherCharacterImages[PhotonNetwork.room.PlayerCount - 1].GetComponent<Image>().color = Color.gray;
			}

			// 마스터클라이언트는 스타트 버튼을 켬
			if (PhotonNetwork.isMasterClient)
			{
				readyButton.SetActive(false);
				startButton.SetActive(true);
			}
			// 일반클라이언트는 레디 버튼을 켬
			else
			{
				readyButton.SetActive(true);
				startButton.SetActive(false);
			}

		}
		else if (scene.name == "GameScene")
		{
			// 게임씬으로 넘어가려고 할때 같은 방안에 있는 플레이어들의 actorID들을 List에 저장해두고 오름차순 정렬
			// 이는 게임 도중 나가는 사람의 생존현황을 변경하는 용도로 사용
			initialAllPlayerActorIDList = new List<int>();
			// 모든 플레이어의 actorID와 생존 상태를 저장해둠
			// 이는 결과창 조건문에 쓰임
			initialAllPlayerConfirmSurvivalList = new Dictionary<int, bool>();
			for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			{
				
				initialAllPlayerActorIDList.Add(PhotonNetwork.playerList[i].ID);
				initialAllPlayerConfirmSurvivalList.Add(PhotonNetwork.playerList[i].ID, false);
			}
			initialAllPlayerActorIDList.Sort();
			
			startCanvas.SetActive(false);
			lobbyCanvas.SetActive(false);
			gameCanvas.SetActive(true);

			// 게임시작시 생존현황 빈자리는 틀만 남은 이미지로? UI 회의해보기
			for (int i = PhotonNetwork.room.MaxPlayers; i > PhotonNetwork.room.PlayerCount; i--)
			{
				// 우선은 알파값 0으로 투명처리
				characterSurvivalImages[i - 1].GetComponent<Image>().color = new Color(1, 1, 1, 0);
				characterColorImages[i - 1].SetActive(false);
			}

			StartCoroutine(CheckOptionsPopup());

			// 마우스 중앙에 고정시키고 안보이게
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
        }
	}

	IEnumerator CheckOptionsPopup()
	{
		while (SceneManager.GetActiveScene().name == "GameScene")
		{
			if (Input.GetKeyDown(KeyCode.Escape) && winPanel.activeSelf == false && losePanel.activeSelf == false)
			{
				if (isOptionsPopup == false)
				{
					isOptionsPopup = true;
					optionsPanel.SetActive(true);
					// 환경설정 할수있게 마우스 보여줘야됨
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			}
			yield return null;
		}
	}

	// 게임 끌때 호출
	void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public void OnExitGame()
	{
		Application.Quit();
	}

	void StarParticleInstantiate()
	{
		Instantiate(starParticle);
	}

	// 캐릭터 선택창에서 캐릭터 선택하면 그 캐릭터를 현재 선택한 캐릭터 보여주는곳에 띄워줌
	public void OnSelectCharacter(Sprite character)
	{
		// 레디 안하고 있으면 캐릭터 선택 가능
		if (PhotonManager.instance.isReady == false)
		{
			myCharacterImages.GetComponent<Image>().sprite = character;
			// 내가 선택한 캐릭터에 맞는 모델링을 보여줌
			for (int i = 0; i < characterModelings.Length; i++)
			{
				if (i.ToString() == character.name)
				{
					characterModelings[Convert.ToInt32(character.name)].SetActive(true);
				}
				else
				{
					characterModelings[i].SetActive(false);
				}
			}
			// 다른 플레이어들도 내가 이 캐릭터를 선택했다는 정보를 알아야 하기 때문에 RPC가 필요함
			// 캐릭터 Sprite의 이름을 정수로 만들어두고 이를 인덱스로 활용
			//print("character Number : " + Convert.ToInt32(character.name));
			PhotonManager.instance.SyncMySelectedCharacterImage(Convert.ToInt32(character.name));
		}
	}

	public void OnChangeReadyButtonSprite()
	{   
		// 캐릭터 선택했다면
		if(myCharacterImages.GetComponent<Image>().sprite != null)
		{
			// 방에 있는 플레이어들의 actorID값을 List로 저장
			List<int> actorIDList = new List<int>();
			for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			{
				actorIDList.Add(PhotonNetwork.playerList[i].ID);
			}
			// 오름차순 정렬
			actorIDList.Sort();

			if (PhotonManager.instance.isReady == false)
			{
				//readyButton.GetComponent<Image>().sprite = readyStateSprites[1];
				readyButton.GetComponent<Image>().sprite = readyButtonSprites[1];

				for (int i = 0; i < actorIDList.Count; i++)
				{
					// 캐릭터 선택창에서 내 위치 찾고 이미지 변경
					if (PhotonNetwork.player.ID == actorIDList[i])
					{
						readyStateImages[i].GetComponent<Image>().sprite = readyStateSprites[1];

						int characterSpriteName = 0;
						for (int j = 0; j < characterSprites.Length; j++)
						{
							if (otherCharacterImages[i].GetComponent<Image>().sprite.name == j.ToString())
							{
								otherCharacterImages[i].GetComponent<Image>().sprite = unReadyCharacterSprites[j];
								characterSpriteName = j;
							}
						}
						// 흑백처리
						//otherCharacterImages[i].GetComponent<Image>().color = Color.gray;
						PhotonManager.instance.SyncMyReadyStateImage(PhotonManager.instance.isReady, characterSpriteName);
					}
				}
			}
			else
			{
				// 캐릭터 선택돼있어야 readySprite로 교체 가능
				if (myCharacterImages.GetComponent<Image>().sprite != null)
				{
					//readyButton.GetComponent<Image>().sprite = readyStateSprites[0];
					readyButton.GetComponent<Image>().sprite = readyButtonSprites[0];

					for (int i = 0; i < actorIDList.Count; i++)
					{
						// 캐릭터 선택창에서 내 위치 찾고 이미지 변경
						if (PhotonNetwork.player.ID == actorIDList[i])
						{
							readyStateImages[i].GetComponent<Image>().sprite = readyStateSprites[0];

							int characterSpriteName = 0;
							for (int j = 0; j < characterSprites.Length; j++)
							{
								if(otherCharacterImages[i].GetComponent<Image>().sprite.name == j.ToString())
								{
									otherCharacterImages[i].GetComponent<Image>().sprite = characterSprites[j];
									characterSpriteName = j;
								}
							}
							// 흑백처리 초기화
							//otherCharacterImages[i].GetComponent<Image>().color = Color.white;
							PhotonManager.instance.SyncMyReadyStateImage(PhotonManager.instance.isReady, characterSpriteName);
						}
					}
				}
			}
		}
	}

	IEnumerator StartSceneUIAnimation()
	{
		StartCoroutine(FadeIn(titleImage, titleImageFadeInWaitTime));

		int r = UnityEngine.Random.Range(0, startCharacterSprites.Length);
		yield return FadeIn(characterImages[r], characterImageFadeInWaitTime);
		// 바로 위의 FadeIn이 끝나면 startSceneButtons 활성화
		startSceneButtons.SetActive(true);
	}

	IEnumerator FadeIn(GameObject image, float waitTime)
	{
		Color tempColor = new Color(1, 1, 1, 0);
		float fadeInAlpha = 0.0f;

		yield return new WaitForSeconds(waitTime);

		while (true)
		{
			if (fadeInAlpha >= 1.0f)
			{
				break;
			}

			fadeInAlpha += 0.005f;
			tempColor.a = fadeInAlpha;
			image.GetComponent<Image>().color = tempColor;

			yield return null;
		}

		fadeInAlpha = 0.0f;
	}

	public void ChangeDeadPlayerImage()
	{
		for (int i = 0; i < initialAllPlayerActorIDList.Count; i++)
		{
			if(PhotonNetwork.player.ID == initialAllPlayerActorIDList[i])
			{
				// 흑백처리
				characterSurvivalImages[i].GetComponent<Image>().color = Color.gray;
			}
		}
	}

	// 왠지 더 좋은 방법이 있을듯
	public void SetActiveTrue(GameObject panel)
	{
		if (panel != null)
		{
			panel.SetActive(true);
		}
		AudioManager.instance.PlayMenuSelectSound();
	}

	public void SetActiveFalse(GameObject panel)
	{
		if(panel != null)
		{ 
			panel.SetActive(false);
		}
		AudioManager.instance.PlayMenuSelectSound();

		if (SceneManager.GetActiveScene().name == "GameScene")
		{
			isOptionsPopup = false;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void PlayGameSceneResultUIAnimation(GameObject resultImage)
	{
		resultImage.SetActive(true);
		StartCoroutine(GameSceneResultUIAnimation(resultImage));
	}

	IEnumerator GameSceneResultUIAnimation(GameObject resultImage)
	{
		yield return null;
		StartCoroutine(FadeIn(resultImage, titleImageFadeInWaitTime));
	}

	public void PlayGameSceneResultCharacterUIAnimation(GameObject resultCharacterImage)
	{
		resultCharacterImage.SetActive(true);
		StartCoroutine(GameSceneResultCharacterUIAnimation(resultCharacterImage));
	}

	IEnumerator GameSceneResultCharacterUIAnimation(GameObject resultCharacterImage)
	{
		yield return StartCoroutine(FadeIn(resultCharacterImage, titleImageFadeInWaitTime));

		exitButton.SetActive(true);
	}
}
