using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardFloater1 : MonoBehaviour
{

	public float moveSpeed;
	public float moveLength;

	private PhotonView pv;

	private bool onlyOnceRPC = true;

	private void Start()
	{
		pv = GetComponent<PhotonView>();
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		// 현재 방 안에 플레이어가 2명이 될때
		if (PhotonNetwork.player.IsMasterClient && onlyOnceRPC == true)
		{
			//print("RPC 한번만 실행시키자!");
			pv.RPC("UpAndDownPingPongRPC", PhotonTargets.AllBuffered);
			onlyOnceRPC = false;
		}
	}

	[PunRPC]
	private void UpAndDownPingPongRPC()
	{
		//print("코루틴도 한번만 실행!");
		StartCoroutine(UpAndDownCoroutine());
	}

	IEnumerator UpAndDownCoroutine()
	{
		while (true)
		{
			//print("FixedUpdate 프레임 만큼 계속 돌겠지");
			transform.position = new Vector3(transform.position.x,
											Mathf.PingPong(Time.fixedTime * moveSpeed, moveLength / moveSpeed),
											transform.position.z);
			yield return new WaitForFixedUpdate();
		}
	}
}