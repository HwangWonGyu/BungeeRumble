using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeadPlayerCamera : MonoBehaviour {

    PhotonView pv = null;

    //죽었는지 확인
    public bool confirmSurvival;

    //플레이어 정보를 얻기 위해서
    public GameObject[] players;

    public GameObject[] cameras;

	private void Start()
    {
        pv = GetComponent<PhotonView>();
        cameras = new GameObject[PhotonNetwork.room.MaxPlayers];
    }

    // Update is called once per frame
    void Update () {

        if (!pv.isMine)
            return;

        if(confirmSurvival && Input.GetMouseButtonDown(0))
        {
            //현재 게임안에 있는 오브젝트를 넣어라
            players = GameObject.FindGameObjectsWithTag("Player");

            int j = 0;

            for (int i = 0; i < players.Length; i++)
            {
                //죽지 않으면 리스트에 해당 오브젝트정보를 넣음
                if (!players[i].GetComponent<DeadPlayerCamera>().confirmSurvival)
                {
                    cameras[j++] = players[i];
                }
            }

            //살아있는 플레이어들 중에서 랜덤하게 관전해라
            int r = Random.Range(0, j);
			if(cameras[r] != null)
				Camera.main.gameObject.GetComponent<SmoothFollow>().target = cameras[r].transform;
        }
    }    

    public void PlayerSearch()
    {
		if (!pv.isMine)
            return;

		//print("설마?");
		AudioManager.instance.PlayDeathSound();
		Camera.main.GetComponent<AudioListener>().enabled = true;
        players = GameObject.FindGameObjectsWithTag("Player");

		// 방에 혼자 남아있었는데 죽으면
		if(PhotonNetwork.room.PlayerCount == 1)
		{
			// Exit버튼 활성화
			UIManager.instance.exitButton.SetActive(true);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			//print("방에 혼자 남아있었는데 죽으면 마우스 보임");
		}
	
        for (int i = 0, j = 0; i < players.Length; i++)
        {
            //죽지 않으면 리스트에 해당 오브젝트정보를 넣음
            if (!players[i].GetComponent<DeadPlayerCamera>().confirmSurvival)
            {
                cameras[j++] = players[i];
			}
        }

        //첫번째 카메라가 있다면 첫번째 카메라를 관전하고 
		if(cameras[0] != null)
			Camera.main.gameObject.GetComponent<SmoothFollow>().target = cameras[0].transform;
        //자기 카메라를 꺼라
        this.gameObject.GetComponent<Controller>().cam.gameObject.SetActive(false);
        //자기 controller 스크립트를 꺼라
        this.gameObject.GetComponent<Controller>().enabled = false;

		// 생존현황에서 자기가 자기것만 흑백처리
		UIManager.instance.ChangeDeadPlayerImage();

		// 내가 흑백처리 됐다는 정보를 다른 플레이어들에게 알려줌
		PhotonManager.instance.SyncMyDeadCharacterImage(PhotonNetwork.player.ID);

		// 게임이 끝났는지 검사하기
		PhotonManager.instance.CheckGameOver();
	}
}