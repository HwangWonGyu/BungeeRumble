using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhotonManager : Photon.PunBehaviour
{

	#region Public Variables

	[Tooltip("The maximum number of players per room")]
	public byte maxPlayersPerRoom;

	[HideInInspector]
	public bool isReady;

	[HideInInspector]
	public int playerPrefabTypeNumber;

	public float mustStartTimeLimit;

	public static PhotonManager instance;

	#endregion

	#region Private Variables

	private PhotonView pv;

	private bool isConnecting;
	private bool isClickStartButton;

	private string _gameVersion = "1";

	private bool isAllReady;
	private int readyCount;
	private int deadPlayerCount;

	// RPC & 콜백 테스트용
	//private int isSyncOtherCharacterImagesRPC;
	//private int isOnPhotonPlayerConnected;
	//private bool isOnPhotonRandomJoinFailed;
	//private bool isCheckOthersIsReadyRPC;
	//private bool isOthersReadyState;
	//private bool isOnPhotonPlayerDisconnected;
	//private int isRequestOtherCharacterImagesRPC;
	//private int isSendSyncOtherCharacterImagesRPC;
	//private int isSendSyncOtherReadyStateImagesRPC;
	//private int isMineOnPhotonPlayerConnected;
	//private bool isGameOverRPC;

	#endregion

	#region MonoBehaviour CallBacks

	private void Awake()
	{
		pv = GetComponent<PhotonView>();

		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}

		PhotonNetwork.autoJoinLobby = false;

		PhotonNetwork.automaticallySyncScene = true;

		// 접속 끊길때(게임 끌때만?) 접속상태 확인 횟수 최솟값으로 설정?
		// 안건드려도 플레이어 나갈때 동기화 빨리 되는 기능이 있는지 알아보기
		PhotonNetwork.MaxResendsBeforeDisconnect = 3;

		DontDestroyOnLoad(gameObject);
	}

	#endregion

	public void OnSceneMovement()
	{
		isConnecting = true;

		// 누른 버튼이 Start 버튼이라면
		if (SceneManager.GetActiveScene().name == "StartScene")
		{
			isClickStartButton = true;
			AudioManager.instance.PlayMenuSelectSound();
		}

		// 마스터 서버에 접속중이지 않다면 다시 연결, 현재 위치 : StartScene
		if (PhotonNetwork.connected == false)
		{
			PhotonNetwork.ConnectUsingSettings(_gameVersion);
		}
		// 마스터 서버에 접속 중이라면, 현재 위치 : LobbyScene
		else
		{
			//print("마스터 서버에서 게임서버로 넘어가려고함");

			// 마스터 클라이언트라면
			if (PhotonNetwork.isMasterClient)
			{
				if (isAllReady == true)
				{
					if (UIManager.instance.myCharacterImages.GetComponent<Image>().sprite == null)
					{
						print("캐릭터를 선택하지 않았습니다. 선택해주세요.");
					}
					else
					{
						pv.RPC("PlayReadyStartSoundRPC", PhotonTargets.All);
						print("게임을 시작합니다.");
						PhotonNetwork.LoadLevel("GameScene"); // 2 = GameScene

						// 현재 룸에 있는 사람들이 모두 레디하고 마스터가 시작하면 현재 룸 난입 금지
						PhotonNetwork.room.IsOpen = false;
					}
				}
				else
				{
					if (PhotonNetwork.room.PlayerCount == 1)
					{
						if (UIManager.instance.myCharacterImages.GetComponent<Image>().sprite == null)
						{
							print("캐릭터를 선택하지 않았습니다. 선택해주세요.");
						}
						else
						{
							AudioManager.instance.PlayReadyStartSound();

							// 싱글모드
							PhotonNetwork.LoadLevel("GameScene"); // 2 = GameScene

							// 시작하면 현재 룸 난입 금지
							PhotonNetwork.room.IsOpen = false;
							//print("혼자서는 게임을 시작할 수 없습니다.");
						}
					}
					else if (PhotonNetwork.room.PlayerCount > 1)
					{
						// UI 띄울지 회의해보기
						print("아직 Ready하지 않은 사람이 있습니다.");
					}
				}
			}
			// 일반 클라이언트라면
			else
			{
				if (isReady == false)
				{
					// 캐릭터를 선택했다면 ReadyRPC를 마스터 클라이언트가 호출하게 함
					if (UIManager.instance.myCharacterImages.GetComponent<Image>().sprite != null)
					{
						AudioManager.instance.PlayReadyStartSound();
						pv.RPC("ReadyRPC", PhotonNetwork.masterClient);
						isReady = true;
					}
				}
				else
				{
					//UnReadyRPC를 마스터 클라이언트가 호출하게 함
					pv.RPC("UnReadyRPC", PhotonNetwork.masterClient);
					isReady = false;
				}
			}
		}
	}

	[PunRPC]
	private void PlayReadyStartSoundRPC()
	{
		AudioManager.instance.PlayReadyStartSound();
	}

	[PunRPC]
	private void ReadyRPC()
	{
		//// 방 최대인원까지 꽉 차야 게임 시작 가능한 로직
		//if (readyCount != maxPlayersPerRoom - 1)
		//{
		//	readyCount++;
		//	if (readyCount == maxPlayersPerRoom - 1)
		//	{
		//		print("최소 풀방 시작모드 : isAllReady true됨");
		//		isAllReady = true;
		//	}
		//}

		// 최소 2인만 있어도 게임 시작 가능한 로직
		if (readyCount < PhotonNetwork.room.PlayerCount - 1)
		{
			readyCount++;
			if (readyCount == PhotonNetwork.room.PlayerCount - 1)
			{
				//print("최소 2인 시작모드 : isAllReady true됨!!!");
				isAllReady = true;
				StartCoroutine(MasterKickedCountdown(mustStartTimeLimit));
			}
		}
	}

	IEnumerator MasterKickedCountdown(float mustStartTimeLimit)
	{
		UIManager.instance.masterKickedCountDownText.GetComponent<Text>().text = "10";

		print("GameStartCountdown 시작");
		float countdownStartTime = (float)PhotonNetwork.time;
		while(isAllReady)
		{
			print("while문 반복");
			if ((float)PhotonNetwork.time - countdownStartTime >= mustStartTimeLimit)
			{
				print("전부 레디 했는데 게임시작 안누르고 있으니 방장은 나가");
				PhotonNetwork.LeaveRoom();
				// UI 띄워주자
				yield break;
			}
			if(SceneManager.GetActiveScene().name == "GameScene")
			{
				print("다행히 게임시작 눌러서 게임씬 왔으니까 코루틴 종료하자");
				yield break;
			}
			yield return null;
			UIManager.instance.masterKickedCountDownText.GetComponent<Text>().text =
				(mustStartTimeLimit - ((float)PhotonNetwork.time - countdownStartTime)).ToString("0");
		}
		UIManager.instance.masterKickedCountDownText.GetComponent<Text>().text = " ";
	}

	[PunRPC]
	private void UnReadyRPC()
	{
		isAllReady = false;
		// readyCount는 한명이라도 Ready를 풀면 줄어듦
		// 0보다 작아지면 추후 다시 Ready할때 isAllReady 판정 기준에 문제가 생기므로 0까지만 낮춤
		if (readyCount > 0)
		{
			readyCount--;
		}
	}

	// 마스터 서버로 이동할때마다 호출됨
	// 예) 네임서버 -> 마스터서버, 게임서버 -> 마스터서버
	public override void OnConnectedToMaster()
	{
		base.OnConnectedToMaster();

		//Debug.Log("Region:" + PhotonNetwork.networkingPeer.CloudRegion);

		if (isConnecting)
		{
			//Debug.Log("Launcher: OnConnectedToMaster() was called by PUN." +
				//"Now this client is connected and could join a room.\n Calling: PhotonNetwork.JoinRandomRoom();" +
				//" Operation will fail if no room found");
			if (SceneManager.GetActiveScene().name == "StartScene" && isClickStartButton == true)
			{
				PhotonNetwork.JoinRandomRoom();

				//print("isClickStartButton false됨");
				isClickStartButton = false;
			}
		}
	}

	public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
	{
		base.OnPhotonRandomJoinFailed(codeAndMsg);

		//Debug.Log("Launcher:OnPhotonRandomJoinFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom(null, new () {MaxPlayers = maxPlayersPerRoom}, null);");

		//지정한 조건에 맞는 룸 생성 함수
		//RoomOptions roomOptions = new RoomOptions();
		//roomOptions.IsOpen = true;
		//roomOptions.IsVisible = false;
		//roomOptions.MaxPlayers = maxPlayersPerRoom;
		// CreateRoom에 roomOptions을 넣어줬더니 의도대로 안됨, 사용방법 제대로 숙지할 것

		// 굳이 위의 코드처럼 안해줘도 난입자가 생기진 않음
		// PhotonNetwork.JoinRandomRoom();을 실패하면 이 콜백이 호출되고
		// 이후 PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = maxPlayersPerRoom }, null); 을 해주기 때문에
		// 방 이름(GUID 기반?)이 겹치지 않나봄

		//isOnPhotonRandomJoinFailed = true;
		PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = maxPlayersPerRoom }, null);
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		//print("OnJoinedRoom 호출");
		PhotonNetwork.LoadLevel("LobbyScene"); // 1 = LobbyScene
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
	{
		// 현재 내가 접속 중인 룸에 새로운 플레이어가 들어오면 내게 호출
		base.OnPhotonPlayerConnected(newPlayer);

		//isOnPhotonPlayerConnected++;

		// 새 플레이어의 레디현황은 레디 하지 않은 상태
		UIManager.instance.readyStateImages[PhotonNetwork.room.PlayerCount - 1].GetComponent<Image>().sprite =
			UIManager.instance.readyStateSprites[1];
		// 흑백처리
		//UIManager.instance.otherCharacterImages[PhotonNetwork.room.PlayerCount - 1].GetComponent<Image>().color = Color.gray;

		// pv.isMine을 안해주면 나를 제외한 클라이언트 수만큼 호출되므로 pv.isMine 일때 호출함
		// 이 조건문이 맞나?

		// 마스터 클라이언트에게 현재 플레이어들 캐릭터 선택상황 보여달라고 부탁
		if (/*pv.isMine*/PhotonNetwork.isMasterClient)
		{
			//isMineOnPhotonPlayerConnected++;
			// 새 플레이어가 들어왔으니 마스터클라이언트의 isAllReady는 다시 false가 됨
			isAllReady = false;

			//pv.RPC("RequestOtherCharacterImagesRPC", PhotonNetwork.masterClient, newPlayer.ID);
			RequestOtherCharacterImages(newPlayer.ID);
			// 마스터 클라이언트에게 현재 플레이어들 레디 상황 보여달라고 부탁
			//pv.RPC("RequestOtherReadyStateImagesRPC", PhotonNetwork.masterClient, newPlayer.ID);
			RequestOtherReadyStateImages(newPlayer.ID);
			// 위 두 메소드 중복되는 부분 많으니 나중에 합쳐보기
		}
	}

	private void RequestOtherCharacterImages(int id)
	{
		//isRequestOtherCharacterImagesRPC++;
		// 마스터 클라이언트 자신의 캐릭터 선택 상태는
		// SyncMySelectedCharacterImage에 있는 SyncMySelectedCharacterImageRPC에 구현돼있으므로 재사용
		if (UIManager.instance.myCharacterImages.GetComponent<Image>().sprite != null)
		{
			//for (int i = 0; i < UIManager.instance.characterSprites.Length; i++)
			for (int i = 0; i < UIManager.instance.unReadyCharacterSprites.Length; i++)
			{
				// 마스터 클라이언트가 선택한 이미지의 이름과 일치하는 이미지를 싱크
				if (UIManager.instance.myCharacterImages.GetComponent<Image>().sprite.name == i.ToString())
				{
					SyncMySelectedCharacterImage(i);
				}
			}
		}

		// 자기자신과 방장 말고 다른 플레이어가 있다면 otherCharacterImageNumbers에 그 플레이어 이미지 이름(숫자)을 저장
		int[] otherCharacterImageNumbers;
		if (PhotonNetwork.room.PlayerCount > 2)
		{
			otherCharacterImageNumbers = new int[PhotonNetwork.room.PlayerCount - 2];

			for (int i = 0; i < otherCharacterImageNumbers.Length; i++)
			{
				//if (UIManager.instance.otherCharacterImages[i + 1].GetComponent<Image>().sprite != null)
				if(UIManager.instance.otherCharacterImages[i + 1].GetComponent<Image>().sprite.name != UIManager.instance.emptyPlayerSprite.name)
				{
					otherCharacterImageNumbers[i] = Convert.ToInt32(UIManager.instance.otherCharacterImages[i + 1].GetComponent<Image>().sprite.name);
					// 마스터한테 넘겨받을 이미지들의 이름, otherCharacterImages의 인덱스를 int로 넘겨받음
					pv.RPC("SyncOtherCharacterImagesRPC", PhotonPlayer.Find(id), otherCharacterImageNumbers[i], i + 1);
				}
			}
		}
	}

	private void RequestOtherReadyStateImages(int id)
	{
		// 새 플레이어 들어오자마자 맨 위 자리가 방장이라는것만 보여주면됨

		// 나머지 자리는 레디 상태 얻어와야함
		// 자기자신과 방장 말고 다른 플레이어가 있다면 otherReadyStateNumbers에 그 플레이어 이미지 이름(숫자)을 저장
		int[] otherReadyStateNumbers;
		if (PhotonNetwork.room.PlayerCount > 2)
		{
			otherReadyStateNumbers = new int[PhotonNetwork.room.PlayerCount - 2];

			for (int i = 0; i < otherReadyStateNumbers.Length; i++)
			{
				//if (UIManager.instance.readyStateImages[i + 1].GetComponent<Image>().sprite != null)
				if (UIManager.instance.readyStateImages[i + 1].GetComponent<Image>().sprite.name != UIManager.instance.emptyPlayerSprite.name)
				{
					otherReadyStateNumbers[i] = Convert.ToInt32(UIManager.instance.readyStateImages[i + 1].GetComponent<Image>().sprite.name);
					// 마스터가 알려주는 레디 이미지의 이름, 캐릭터 선택현황에서 레디 이미지의 위치, 캐릭터 이미지 이름을 int로 받음
					for (int j = 0; j < UIManager.instance.characterSprites.Length; j++)
					{

						if (UIManager.instance.otherCharacterImages[i + 1].GetComponent<Image>().sprite.name == j.ToString())
						{
							pv.RPC("SyncOtherReadyStateImagesRPC", PhotonPlayer.Find(id), otherReadyStateNumbers[i], i + 1, j);
						}
						// 나중에 RPC 전송횟수 줄일수 있는지 알아보기
						else if(UIManager.instance.otherCharacterImages[i + 1].GetComponent<Image>().sprite.name
								== UIManager.instance.emptyPlayerSprite.name)
						{
							pv.RPC("SyncOtherReadyStateImagesRPC", PhotonPlayer.Find(id), otherReadyStateNumbers[i], i + 1, -1);
						}
					}
				}
			}
		}
	}

	[PunRPC]
	private void SyncOtherCharacterImagesRPC(int imageName, int index)
	{
		//isSendSyncOtherCharacterImagesRPC++;
		// 캐릭터 선택 현황 갱신
		UIManager.instance.otherCharacterImages[index].GetComponent<Image>().sprite = UIManager.instance.characterSprites[imageName];
		// 생존현황 갱신
		//UIManager.instance.characterSurvivalImages[index].GetComponent<Image>().sprite = UIManager.instance.characterSprites[imageName];
		UIManager.instance.characterSurvivalImages[index].GetComponent<Image>().sprite = UIManager.instance.survivalSprites[imageName];
	}

	[PunRPC]
	private void SyncOtherReadyStateImagesRPC(int readyImageName, int index, int otherCharacterImageName)
	{
		//isSendSyncOtherReadyStateImagesRPC++;
		// 레디 현황 갱신
		UIManager.instance.readyStateImages[index].GetComponent<Image>().sprite = UIManager.instance.readyStateSprites[readyImageName];

		// 레디 중이라면
		if (readyImageName == 0)
		{
			for (int i = 0; i < UIManager.instance.characterSprites.Length; i++)
			{
				if (otherCharacterImageName == i)
				{
					UIManager.instance.otherCharacterImages[index].GetComponent<Image>().sprite = UIManager.instance.characterSprites[i];
				}
			}
			//UIManager.instance.otherCharacterImages[index].GetComponent<Image>().color = Color.white;
		}
		// 레디 중이지 않다면
		else if (readyImageName == 1)
		{
			for (int i = 0; i < UIManager.instance.unReadyCharacterSprites.Length; i++)
			{
				if (otherCharacterImageName == i)
				{
					UIManager.instance.otherCharacterImages[index].GetComponent<Image>().sprite = UIManager.instance.unReadyCharacterSprites[i];
				}
			}
			// 흑백처리
			//UIManager.instance.otherCharacterImages[index].GetComponent<Image>().color = Color.gray;
		}
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
	{
		// 현재 내가 접속 중인 룸에 특정 플레이어가 나가면 내게 호출
		base.OnPhotonPlayerDisconnected(otherPlayer);

		//isOnPhotonPlayerDisconnected = true;

		if (PhotonNetwork.isMasterClient)
		{
			// OnPhotonPlayerDisconnected 호출될때마다 마스터는
			// 남아있는 모든 플레이어들의 isReady상태 체크해서 true 개수를 readyCount로 만들어줌
			// readyCount를 초기화 하고 다시 계산
			readyCount = 0;
			pv.RPC("CheckOthersIsReadyRPC", PhotonTargets.Others);
		}

		// 게임씬으로 넘어가고나서 OnPhotonPlayerDisconnected이 호출된다면
		// GameManager에서 GameScene으로 올때 저장해둔 initialAllPlayerActorIDList을 사용
		if (SceneManager.GetActiveScene().name == "GameScene")
		{
			// 생존현황 UI에서 나간 플레이어칸 초기화
			for (int i = 0; i < UIManager.instance.initialAllPlayerActorIDList.Count; i++)
			{
				if (otherPlayer.ID == UIManager.instance.initialAllPlayerActorIDList[i])
				{
					// 흑백처리
					UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().color = Color.gray;
				}
			}

			// 결과창 조건문에 쓰일 리스트에 내 생존상태를 죽음으로 
			UIManager.instance.initialAllPlayerConfirmSurvivalList[otherPlayer.ID] = true;

			// 결과창이 안떠있으면
			if (UIManager.instance.winPanel.activeSelf == false && UIManager.instance.losePanel.activeSelf == false)
			{
				// 혼자라면 결과창에 승리 보여줌
				if (PhotonNetwork.room.PlayerCount == 1)
				{
					UIManager.instance.winPanel.SetActive(true);
					//StartCoroutine(ExitRoom());

					AudioManager.instance.PlayWinSound();
					UIManager.instance.PlayGameSceneResultUIAnimation(UIManager.instance.winImage);

					if (UIManager.instance.characterSurvivalImages
					[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
					GetComponent<Image>().sprite.name == "Charcter_Minibox1")
					{
						UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[0]);
					}
					else if (UIManager.instance.characterSurvivalImages
						[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
						GetComponent<Image>().sprite.name == "Charcter_Minibox2")
					{
						UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[1]);
					}
					else if (UIManager.instance.characterSurvivalImages
						[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
						GetComponent<Image>().sprite.name == "Charcter_Minibox3")
					{
						UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[2]);
					}
					else if (UIManager.instance.characterSurvivalImages
						[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
						GetComponent<Image>().sprite.name == "Charcter_Minibox3")
					{
						UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[3]);
					}

					//UIManager.instance.exitButton.SetActive(true);
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			}

			// OnPhotonPlayerDisconnected 콜백 받는 사람이 전부 죽은사람들이라면 결과창에 패배 보여줌
			// 이렇게 되면 결과창 이미 떠있는 상태에서 누군가가 나가면 전부 패배로 변하니까 더 생각해보기
			bool isAllDead = true;
			for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			{
				if(UIManager.instance.initialAllPlayerConfirmSurvivalList[PhotonNetwork.playerList[i].ID] == false)
				{
					isAllDead = false;
				}
			}
			// 다 죽었고 결과창 안떠있으면
			if(isAllDead && UIManager.instance.winPanel.activeSelf == false && UIManager.instance.losePanel.activeSelf == false)
			{
				UIManager.instance.losePanel.SetActive(true);

				AudioManager.instance.PlayLoseSound();
				UIManager.instance.PlayGameSceneResultUIAnimation(UIManager.instance.loseImage);

				if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox1")
				{
					UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[0]);
				}
				else if (UIManager.instance.characterSurvivalImages
					[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
					GetComponent<Image>().sprite.name == "Charcter_Minibox2")
				{
					UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[1]);
				}
				else if (UIManager.instance.characterSurvivalImages
					[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
					GetComponent<Image>().sprite.name == "Charcter_Minibox3")
				{
					UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[2]);
				}
				else if (UIManager.instance.characterSurvivalImages
					[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
					GetComponent<Image>().sprite.name == "Charcter_Minibox3")
				{
					UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[3]);
				}
				//StartCoroutine(ExitRoom());
				//UIManager.instance.exitButton.SetActive(true);
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}

		}
		else if (SceneManager.GetActiveScene().name == "LobbyScene")
		{
			// 로비씬에서 방에 있는 플레이어들의 actorID값을 List로 저장
			List<int> actorIDList = new List<int>();
			for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			{
				actorIDList.Add(PhotonNetwork.playerList[i].ID);
			}
			// 나간 플레이어 actorID값도 List로 저장
			actorIDList.Add(otherPlayer.ID);
			// 오름차순 정렬
			actorIDList.Sort();

			// 나간 플레이어가 방장이 아니었다면 현재 방장에게 나간 플레이어 위치에 해당되는 강퇴팝업창 끄라고 알려주기
			if(actorIDList[0] != otherPlayer.ID)
				pv.RPC("OffKickBoxRPC", PhotonNetwork.masterClient, actorIDList.IndexOf(otherPlayer.ID));

			for (int i = 0; i < actorIDList.Count; i++)
			{
				if (actorIDList[i] == otherPlayer.ID)
				{
					// 생존현황, 캐릭터 선택현황, 레디현황에서 나간 플레이어칸 아래의 이미지들을 위로 당김
					// 다만 맨 끝 사람이 나가면 당길 필요 없이 그 자리를 없애기만 하면 됨
					if (i == actorIDList.Count - 1)
					{
						UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().sprite = null;
						//UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite = null;
						UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
						// 흑백처리 초기화
						//UIManager.instance.otherCharacterImages[i].GetComponent<Image>().color = Color.white;
						//UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite = null;
						UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
					}
					else
					{
						int j = i;
						for (; j < actorIDList.Count - 1; j++)
						{
							UIManager.instance.characterSurvivalImages[j].GetComponent<Image>().sprite =
								UIManager.instance.characterSurvivalImages[j + 1].GetComponent<Image>().sprite;
							UIManager.instance.otherCharacterImages[j].GetComponent<Image>().sprite =
								UIManager.instance.otherCharacterImages[j + 1].GetComponent<Image>().sprite;
							UIManager.instance.otherCharacterImages[j].GetComponent<Image>().color =
								UIManager.instance.otherCharacterImages[j + 1].GetComponent<Image>().color;
							UIManager.instance.readyStateImages[j].GetComponent<Image>().sprite =
								UIManager.instance.readyStateImages[j + 1].GetComponent<Image>().sprite;
						}
						UIManager.instance.characterSurvivalImages[j].GetComponent<Image>().sprite = null;
						//UIManager.instance.otherCharacterImages[j].GetComponent<Image>().sprite = null;
						UIManager.instance.otherCharacterImages[j].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
						// 흑백처리 초기화
						//UIManager.instance.otherCharacterImages[j].GetComponent<Image>().color = Color.white;
						UIManager.instance.readyStateImages[j].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
						// 방장은 레디가 없으므로 항상 흑백처리 초기화
						//UIManager.instance.otherCharacterImages[0].GetComponent<Image>().color = Color.white;
						for (int k = 0; k < UIManager.instance.characterSprites.Length; k++)
						{
							if(UIManager.instance.otherCharacterImages[0].GetComponent<Image>().sprite.name == k.ToString())
							{
								UIManager.instance.otherCharacterImages[0].GetComponent<Image>().sprite =
									UIManager.instance.characterSprites[k];
							}
						}
						// 방장 표시는 항상 맨 위에 있게
						UIManager.instance.readyStateImages[0].GetComponent<Image>().sprite =
							UIManager.instance.hostSprites;
					}
				}
			}
		}
	}

	[PunRPC]
	private void CheckOthersIsReadyRPC()
	{
		//isCheckOthersIsReadyRPC = true;
		if (isReady)
		{
			//isOthersReadyState = true;
			pv.RPC("IncreaseReadyCountRPC", PhotonNetwork.masterClient);
		}
	}

	[PunRPC]
	private void IncreaseReadyCountRPC()
	{
		readyCount++;

		if (PhotonNetwork.room.PlayerCount - 1 == readyCount)
			isAllReady = true;
		else
			isAllReady = false;
	}

	[PunRPC]
	private void OffKickBoxRPC(int index)
	{
		UIManager.instance.kickMessageBoxs[index - 1].SetActive(false);
	}

	public void SyncMySelectedCharacterImage(int characterSpriteNumber)
	{
		// RPC를 뿌린 사람의 고유 ID, 캐릭터 Sprite를 저장해둔 배열의 인덱스가 될 숫자를 넘겨줌
		pv.RPC("SyncMySelectedCharacterImageRPC", PhotonTargets.All, PhotonNetwork.player.ID, characterSpriteNumber);
		// GameManager에서 어느 캐릭터를 생성할지 판별할 변수
		playerPrefabTypeNumber = characterSpriteNumber;
	}

	[PunRPC]
	private void SyncMySelectedCharacterImageRPC(int id, int selectedPlayerIndex)
	{
		// 방에 있는 플레이어들의 actorID값을 List로 저장
		List<int> actorIDList = new List<int>();
		for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
		{
			actorIDList.Add(PhotonNetwork.playerList[i].ID);
		}
		// 오름차순 정렬
		actorIDList.Sort();

		for (int i = 0; i < actorIDList.Count; i++)
		{
			// 아래 코드 한줄 한줄을 UIManager에서 메소드화 해주면 좋을것 같음
			if (id == actorIDList[i])
			{
				// 로비씬 우측의 캐릭터 선택현황 i + 1 번째 자리에 이 RPC를 뿌린 사람이 선택한 캐릭터 이미지를 넣어줌
				//UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite = UIManager.instance.characterSprites[selectedPlayerIndex];
				// 
				// 그게 마스터 클라이언트라면 무조건 컬러로
				if (id == actorIDList[0])
				{
					UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite =
						UIManager.instance.characterSprites[selectedPlayerIndex];
				}
				// 마스터 클라이언트가 아니라면 무조건 흑백으로
				else
				{
					UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite =
						UIManager.instance.unReadyCharacterSprites[selectedPlayerIndex];
				}
				
				// 게임씬 좌측의 플레이어 생존현황 i + 1 번째 자리에 이 RPC를 뿌린 사람이 선택한 캐릭터 이미지를 넣어줌
				//UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().sprite = UIManager.instance.characterSprites[selectedPlayerIndex];
				UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().sprite = UIManager.instance.survivalSprites[selectedPlayerIndex];
			}
		}
	}

	public void SyncMyDeadCharacterImage(int id)
	{
		// RPC를 뿌린 사람의 고유 ID를 넘겨줌
		pv.RPC("SyncMyDeathCharacterImageRPC", PhotonTargets.Others, id);
	}

	[PunRPC]
	private void SyncMyDeathCharacterImageRPC(int id)
	{
		for (int i = 0; i < UIManager.instance.initialAllPlayerActorIDList.Count; i++)
		{
			if (id == UIManager.instance.initialAllPlayerActorIDList[i])
			{
				UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().color = Color.gray;
			}
		}
	}

	// 추후 이 기능은 특정 키를 누르거나 마우스로 버튼을 누르면 작동하게 할 예정
	// 게임을 끌때와 위 메소드가 바인딩 돼있는 버튼을 눌러 게임을 나갈때는 다른 경우이므로 더 생각해보기
	public void OnClickExitRoom()
	{
		AudioManager.instance.PlayMenuSelectSound();
		//print("버튼 누름, OnClickExitRoom 호출");
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();
		//print("OnLeftRoom이 호출 되니?");

		// StartScene의 타이틀, 일러스트, 버튼 초기화
		InitializeStartSceneUI();

		PhotonNetwork.LoadLevel("StartScene");

		// PhotonNetwork.Disconnect()를 바로 해주면 에러 발생하므로 좀 기다렸다가 해줌
		// 다만 맞는 방법인지는 잘 모르겠음 
		StartCoroutine(DisconnectPhoton(0.5f));

		// OnSceneMoveMent()에서 관리하던 Ready관련변수들 초기화
		InitializeReadyStateVariable();

		// 레디버튼 초기화
		InitializeReadyButton();

		// 레디현황 초기화
		InitializeReadyStateImage();

		// 자기가 선택한 캐릭터 보여주는 칸 초기화
		InitializeMyCharacterModeling();

		// 자기가 선택했던 캐릭터, 상대가 선택했던 캐릭터 이미지정보 초기화
		InitializeCharacterImage();

		// 강퇴팝업창, 모두 레디시 방장 나가지는 기능 카운트다운 초기화
		InitializeKick();

		// 생존현황 초기화
		InitializeCharacterSurvivalImage();

		// 게임씬 환경설정 초기화
		InitializeOptionsPanel();

		// 결과창 초기화
		InitializeGameOverImage();

		// 테스트용 변수들 초기화
		InitializeGUITestVariable();
	}

	IEnumerator DisconnectPhoton(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		PhotonNetwork.Disconnect();
	}

	public override void OnDisconnectedFromPhoton()
	{
		base.OnDisconnectedFromPhoton();

		print("Launcher:Disconnected");

		isConnecting = false;
	}

	private void InitializeStartSceneUI()
	{
		// StartScene에서 FadeIn됐던 이미지들의 알파값을 0으로 다시 설정
		// 이미지 랜덤하다면 그에 맞게 다시 생각해보기
		UIManager.instance.titleImage.GetComponent<Image>().color = new Color(1, 1, 1, 0);
		for (int i = 0; i < UIManager.instance.characterImages.Length; i++)
		{
			UIManager.instance.characterImages[i].GetComponent<Image>().color = new Color(1, 1, 1, 0);
		}

		// StartScene에서 보였던 버튼들을 다시 끔
		UIManager.instance.startSceneButtons.SetActive(false);
	}

	private void InitializeReadyStateVariable()
	{
		isReady = false;
		isAllReady = false;
		readyCount = 0;
	}

	private void InitializeReadyButton()
	{
		//UIManager.instance.readyButton.GetComponent<Image>().sprite = UIManager.instance.readyStateSprites[1];
		UIManager.instance.readyButton.GetComponent<Image>().sprite = UIManager.instance.readyButtonSprites[1];
	}

	private void InitializeReadyStateImage()
	{
		for (int i = 0; i<UIManager.instance.readyStateImages.Length; i++)
		{
			//UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite = null;
			UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
		}
	}

	private void InitializeMyCharacterModeling()
	{
		for (int i = 0; i < UIManager.instance.characterModelings.Length; i++)
		{
			UIManager.instance.characterModelings[i].SetActive(false);
		}
	}
	private void InitializeCharacterImage()	
	{
		UIManager.instance.myCharacterImages.GetComponent<Image>().sprite = null;
		for (int i = 0; i<UIManager.instance.otherCharacterImages.Length; i++)
		{
			UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite = UIManager.instance.emptyPlayerSprite;
		}
	}

	private void InitializeKick()
	{
		for (int i = 0; i < UIManager.instance.kickMessageBoxs.Length; i++)
		{
			UIManager.instance.kickMessageBoxs[i].SetActive(false);
		}

		UIManager.instance.masterKickedCountDownText.GetComponent<Text>().text = " ";
	}

	private void InitializeCharacterSurvivalImage()
	{
		for (int i = 0; i < UIManager.instance.characterSurvivalImages.Length; i++)
		{
			UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().color = Color.white;
			UIManager.instance.characterSurvivalImages[i].GetComponent<Image>().sprite = null;
		}

		for (int i = 0; i < UIManager.instance.characterColorImages.Length; i++)
		{
			UIManager.instance.characterColorImages[i].SetActive(true);
		}
	}

	private void InitializeOptionsPanel()
	{
		UIManager.instance.optionsPanel.SetActive(false);
		UIManager.instance.isOptionsPopup = false;
	}

	private void InitializeGameOverImage()
	{
		UIManager.instance.winImage.GetComponent<Image>().color = new Color(1, 1, 1, 0);
		UIManager.instance.loseImage.GetComponent<Image>().color = new Color(1, 1, 1, 0);

		for (int i = 0; i < UIManager.instance.winCharacterImages.Length; i++)
		{
			UIManager.instance.winCharacterImages[i].GetComponent<Image>().color = new Color(1, 1, 1, 0);
		}

		for (int i = 0; i < UIManager.instance.loseCharacterImages.Length; i++)
		{
			UIManager.instance.loseCharacterImages[i].GetComponent<Image>().color = new Color(1, 1, 1, 0);
		}

		UIManager.instance.winPanel.SetActive(false);
		UIManager.instance.losePanel.SetActive(false);
		UIManager.instance.exitButton.SetActive(false);
	}

	public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
	{
		// 마스터 클라이언트가 나가면 현재 방에 남아있는 모든 클라이언트들에게 호출되는 콜백
		base.OnMasterClientSwitched(newMasterClient);

		// 이 콜백을 받은 클라이언트가 새로운 마스터 클라이언트라면
		if (PhotonNetwork.isMasterClient)
		{
			//레디 버튼을 끄고 스타트 버튼 활성화
			UIManager.instance.startButton.SetActive(true);
			UIManager.instance.readyButton.SetActive(false);
			//더이상 레디할 필요 없음
			//이는 레디 완료 후 마스터가 자신으로 바뀔때 캐릭터를 다시 자유롭게 선택 가능하게 해줌
			isReady = false;
		}
	}

	public void SyncMyReadyStateImage(bool readyState, int characterSpriteName)
	{
		// 레디 버튼 누른 사람의 actorID, 레디상태를 넘겨줌
		pv.RPC("SyncMyReadyStateImageRPC", PhotonTargets.Others, PhotonNetwork.player.ID, readyState, characterSpriteName);
	}

	[PunRPC]
	private void SyncMyReadyStateImageRPC(int id, bool readyState, int characterSpriteName)
	{
		// 방에 있는 플레이어들의 actorID값을 List로 저장
		List<int> actorIDList = new List<int>();
		for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
		{
			actorIDList.Add(PhotonNetwork.playerList[i].ID);
		}
		// 오름차순 정렬
		actorIDList.Sort();

		for (int i = 0; i < actorIDList.Count; i++)
		{
			if (actorIDList[i] == id)
			{
				if (readyState == true)
				{
					UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite =
						UIManager.instance.readyStateSprites[0];
					// 흑백처리 초기화
					//UIManager.instance.otherCharacterImages[i].GetComponent<Image>().color = Color.white;
					UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite = 
						UIManager.instance.characterSprites[characterSpriteName];
				}
				else
				{
					UIManager.instance.readyStateImages[i].GetComponent<Image>().sprite =
						UIManager.instance.readyStateSprites[1];
					// 흑백처리
					//UIManager.instance.otherCharacterImages[i].GetComponent<Image>().color = Color.gray;
					UIManager.instance.otherCharacterImages[i].GetComponent<Image>().sprite =
						UIManager.instance.unReadyCharacterSprites[characterSpriteName];
				}
			}
		}
	}

	// ReadyState의 자식에 있는 Button의 OnClick에 등록할 메소드, Inspector에서 index를 넣어줘야함
	public void ActivateKickPlayer(int index)
	{
		// 마스터라면 다른 플레이어 강퇴하는 RPC 호출 가능
		if (PhotonNetwork.isMasterClient && PhotonNetwork.room.PlayerCount > 1 && PhotonNetwork.playerList.Length > index)
		{
			UIManager.instance.kickMessageBoxs[index - 1].SetActive(true);

			//// 방에 있는 플레이어들의 actorID값을 List로 저장
			//List<int> actorIDList = new List<int>();
			//for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			//{
			//	actorIDList.Add(PhotonNetwork.playerList[i].ID);
			//}
			//// 오름차순 정렬
			//actorIDList.Sort();

			//// 파라미터로 넘겨받은 숫자를 actorIDList의 index로 활용
			//pv.RPC("KickPlayerRPC", PhotonPlayer.Find(actorIDList[index]));
		}
	}

	// ReadyState의 자식에 있는 Button의 OnClick에 등록할 메소드, Inspector에서 index를 넣어줘야함
	public void KickPlayer(int index)
	{
		// 마스터라면 다른 플레이어 강퇴하는 RPC 호출 가능
		if (PhotonNetwork.isMasterClient && PhotonNetwork.room.PlayerCount > 1 && PhotonNetwork.playerList.Length > index)
		{
			// 방에 있는 플레이어들의 actorID값을 List로 저장
			List<int> actorIDList = new List<int>();
			for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
			{
				actorIDList.Add(PhotonNetwork.playerList[i].ID);
			}
			// 오름차순 정렬
			actorIDList.Sort();

			// 파라미터로 넘겨받은 숫자를 actorIDList의 index로 활용
			pv.RPC("KickPlayerRPC", PhotonPlayer.Find(actorIDList[index]));
		}
	}

	[PunRPC]
	private void KickPlayerRPC()
	{
		PhotonNetwork.LeaveRoom();
	}

	//public void SyncOtherTimerImage(float fillRange)
	//{
	//	pv.RPC("SyncOtherTimerImageRPC", PhotonTargets.Others, fillRange);
	//}

	//[PunRPC]
	//private void SyncOtherTimerImageRPC(float fillRange)
	//{
	//	UIManager.instance.timerImage.GetComponent<Image>().fillAmount = fillRange;
	//}

	public void CheckGameOver()
	{
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		deadPlayerCount = 0;

		for (int i = 0; i < players.Length; i++)
		{
			// 죽은 플레이어수 계산
			if (players[i].GetComponent<DeadPlayerCamera>().confirmSurvival == true)
			{
				deadPlayerCount++;
			}
		}

		// 모든 플레이어의 결과창 조건문에 쓰일 리스트에 내 생존상태를 죽음으로 
		pv.RPC("SyncMyConfirmSurvivalRPC", PhotonTargets.All, PhotonNetwork.player.ID);

		// 죽은 사람수가 현재 방 인원보다 한명 적으면 == 생존자가 한명이라면
		if (deadPlayerCount == PhotonNetwork.room.PlayerCount - 1)
		{
			//isGameOverRPC = true;
			// 게임 종료 후 생존자에게 승리, 나머지에게 패배 보여줌
			pv.RPC("GameOverRPC", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void SyncMyConfirmSurvivalRPC(int id)
	{
		UIManager.instance.initialAllPlayerConfirmSurvivalList[id] = true;
	}

	[PunRPC]
	private void GameOverRPC()
	{
		// 내가 살아있으면
		if (UIManager.instance.initialAllPlayerConfirmSurvivalList[PhotonNetwork.player.ID] == false)
		{
			UIManager.instance.winPanel.SetActive(true);

			AudioManager.instance.PlayWinSound();
			UIManager.instance.PlayGameSceneResultUIAnimation(UIManager.instance.winImage);

			if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox1")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[0]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox2")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[1]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox3")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[2]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox3")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.winCharacterImages[3]);
			}
		}
		else
		{
			UIManager.instance.losePanel.SetActive(true);

			AudioManager.instance.PlayLoseSound();
			UIManager.instance.PlayGameSceneResultUIAnimation(UIManager.instance.loseImage);

			if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox1")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[0]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox2")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[1]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox3")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[2]);
			}
			else if (UIManager.instance.characterSurvivalImages
				[UIManager.instance.initialAllPlayerActorIDList.FindIndex(x => x == PhotonNetwork.player.ID)].
				GetComponent<Image>().sprite.name == "Charcter_Minibox3")
			{
				UIManager.instance.PlayGameSceneResultCharacterUIAnimation(UIManager.instance.loseCharacterImages[3]);
			}
		}

		//StartCoroutine(ExitRoom());
		//UIManager.instance.exitButton.SetActive(true);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		print("GameOverRPC로 마우스 보이게");
	}

	IEnumerator ExitRoom()
	{
		int anyKeyDownCount = 0;

		while(true)
		{
			if (Input.anyKeyDown)
			{
				anyKeyDownCount++;
				if (anyKeyDownCount >= 3)
				{
					break;
				}
			}
			yield return null;
		}
		PhotonNetwork.LeaveRoom();
	}


	//private void OnGUI()
	//{
	//	if (SceneManager.GetActiveScene().name != "LogoScene")
	//	{
	//		GUI.Label(new Rect(10, 10, 600, 200), PhotonNetwork.connectionStateDetailed.ToString());
	//		if (PhotonNetwork.isMasterClient)
	//			GUI.Label(new Rect(10, 30, 200, 200), "isAllReady : " + isAllReady.ToString() + ", readyCount : " + readyCount.ToString());

	//		for (int i = 0, j = 0; i < PhotonNetwork.playerList.Length; i++, j = j + 20)
	//		{
	//			GUI.Label(new Rect(10, 70 + j, 1900, 200),
	//				"현재 플레이어 리스트.ID : " + PhotonNetwork.playerList[i].ID +
	//				", 리스트.ToString() : " + PhotonNetwork.playerList[i].ToString());
	//		}

	//		GUI.Label(new Rect(10, 250, 400, 200), isOnPhotonPlayerConnected.ToString() + ": isOnPhotonPlayerConnected");
	//		GUI.Label(new Rect(10, 270, 400, 200), isMineOnPhotonPlayerConnected.ToString() + ": isMineOnPhotonPlayerConnected");
	//		GUI.Label(new Rect(10, 290, 400, 200), isRequestOtherCharacterImagesRPC.ToString() + ": (Only Master) isRequestOtherCharacterImagesRPC");
	//		GUI.Label(new Rect(10, 310, 400, 200), isSyncOtherCharacterImagesRPC.ToString() + " : (Only Master) isSyncOtherCharacterImagesRPC");
	//		GUI.Label(new Rect(10, 330, 400, 200), isSendSyncOtherCharacterImagesRPC.ToString() + ": (Only NewPlayer) isSendSyncOtherCharacterImagesRPC");
	//		GUI.Label(new Rect(10, 350, 400, 200), isSendSyncOtherReadyStateImagesRPC.ToString() + " : (Only NewPlayer) isSendSyncOtherReadyStateImagesRPC");
	//		GUI.Label(new Rect(10, 370, 400, 200), isOnPhotonRandomJoinFailed.ToString() + " : (Only Master) OnPhotonRandomJoinFailed: ");
	//		GUI.Label(new Rect(10, 390, 400, 200), isCheckOthersIsReadyRPC.ToString() + " : (Except Master) isCheckOthersIsReadyRPC");
	//		GUI.Label(new Rect(10, 410, 400, 200), isOthersReadyState.ToString() + " : (Except Master) isOthersReadyState");
	//		GUI.Label(new Rect(10, 430, 400, 200), isOnPhotonPlayerDisconnected.ToString() + " : isOnPhotonPlayerDisconnected");
	//	}
	//	if (SceneManager.GetActiveScene().name == "GameScene")
	//	{
	//		GUI.Label(new Rect(10, 20, 400, 200), deadPlayerCount.ToString());
	//		for (int i = 0, j = 0; i < PhotonNetwork.playerList.Length; i++, j = j + 20)
	//		{
	//			GUI.Label(new Rect(10, 70 + j, 1900, 200),
	//				"뭐지 : " + UIManager.instance.initialAllPlayerConfirmSurvivalList[PhotonNetwork.playerList[i].ID].ToString());
	//		}
	//	}
	//	GUI.Label(new Rect(10, 100, 400, 200), "과연" + isGameOverRPC.ToString());
	//	GUI.Label(new Rect(10, 30, 400, 20), "네트워크 시간 : " + PhotonNetwork.time.ToString());
	//	if (SceneManager.GetActiveScene().name == "GameScene")
	//	{
	//		GUI.Label(new Rect(10, 100, 400, 400), UIManager.instance.characterSurvivalImages[0].GetComponent<Image>().sprite.name);
	//	}
	//}

	private void InitializeGUITestVariable()
	{
		//isOnPhotonPlayerConnected = 0;
		//isMineOnPhotonPlayerConnected = 0;
		//isRequestOtherCharacterImagesRPC = 0;
		//isSyncOtherCharacterImagesRPC = 0;
		//isSendSyncOtherCharacterImagesRPC = 0;
		//isSendSyncOtherReadyStateImagesRPC = 0;
		//isOnPhotonRandomJoinFailed = false;
		//isCheckOthersIsReadyRPC = false;
		//isOthersReadyState = false;
		//isOnPhotonPlayerDisconnected = false;
		//isGameOverRPC = false;
	}
}
