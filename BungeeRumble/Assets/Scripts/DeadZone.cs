using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeadZone : MonoBehaviour
{
    public float waterUpTime;

    private float curTime;
    private Vector3 previousLocation;
    private float compareLocation;

    private DeadPlayerCamera deadPlayer;

	private float gameStartTime;
	[SerializeField]
	private float gameRunningTime;
	private Image timer;
	private Text timerText;

	private void Start()
	{
		// 게임 시작 시간은 데드존이 활성화 되는시점의 PhotonNetwork.time으로 설정
		gameStartTime = (float)PhotonNetwork.time;
		timer = UIManager.instance.timerImage.GetComponent<Image>();
		timerText = UIManager.instance.timerImage.GetComponentInChildren<Text>();
		timerText.text = " ";
	}

	// Update is called once per frame
	void FixedUpdate()
    {
		// 경과 시간에서 게임 시작 시간을 뺐는데 게임 러닝타임이면
		if ((float)PhotonNetwork.time - gameStartTime <= gameRunningTime)
		{
			// 타이머 UI 차오름
			timer.fillAmount = ((float)PhotonNetwork.time - gameStartTime) / gameRunningTime;

			// 시간이 10초도 안남았다면
			if (gameRunningTime - ((float)PhotonNetwork.time - gameStartTime) <= 9.9f)
			{
				timerText.text = (gameRunningTime - ((float)PhotonNetwork.time - gameStartTime)).ToString("0");
			}
		}
		else
		{
			// 타이머 UI 꽉 찬 상태로
			timer.fillAmount = 1.0f;
		}

		// 마스터만 데드존을 움직이게 하고 그 데드존의 위치는 PhotonTransformView에 의해 동기화
		if (PhotonNetwork.isMasterClient)
		{
			if (curTime <= waterUpTime)
			{
				previousLocation = transform.position;
			}
			else
			{
				compareLocation = transform.position.y - previousLocation.y;

				if (compareLocation < 8.0f)
				{
					transform.position += Vector3.up * Time.fixedDeltaTime;
				}
				else
				{
					curTime = 0;
				}
			}
		}
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            deadPlayer = other.gameObject.GetComponent<DeadPlayerCamera>();
			// 죽었다는 의미로 confirmSurvival을 true로
			deadPlayer.confirmSurvival = true;
			// 살아있는 플레이어 관전 및 생존현황 갱신
            deadPlayer.PlayerSearch();
        }
    }


    private void OnGUI()
    {
		curTime += Time.fixedDeltaTime; // OnGUI 안에 넣어도 되는 코드인지 알아보기

		// 플레이어별 시간 비교
		//GUI.Label(new Rect(10, 50, 200, 200), "Time.fixedTime : " + Time.fixedTime);
		//GUI.Label(new Rect(10, 70, 400, 200), "PhotonNetwork.time : " + PhotonNetwork.time.ToString("0"));

		//if (curTime <= waterUpTime)
  //      {
		//	GUI.Label(new Rect(10, 130, 200, 200), "남은시간 : " + ((waterUpTime - curTime)).ToString("0"));
		//	GUI.Label(new Rect(10, 150, 200, 200), "curTime : " + curTime.ToString());

		//}
		//else
  //      {
		//	GUI.Label(new Rect(10, 130, 200, 200), "남은시간 : 이 없음");
		//	GUI.Label(new Rect(10, 150, 200, 200), "curTime : " + curTime.ToString());
		//}
    }
}
