using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public GameObject[] startingPoints;

	// Use this for initialization
	void Start()
	{
		// 각 플레이어가 선택한 캐릭터들의 시작 위치가 겹치지 않게 actorID별로 지정해준 위치에 생성
		for (int i = 0, j = PhotonManager.instance.playerPrefabTypeNumber; i < PhotonNetwork.room.PlayerCount; i++)
		{
			if (PhotonNetwork.player.ID == UIManager.instance.initialAllPlayerActorIDList[i])
			{
				PhotonNetwork.Instantiate("Player" + j.ToString(), startingPoints[i].transform.position, startingPoints[i].transform.rotation, 0);
			}
		}
	}
}
