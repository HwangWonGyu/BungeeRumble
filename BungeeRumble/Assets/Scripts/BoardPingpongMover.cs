using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardPingpongMover : MonoBehaviour {

	public float moveSpeed;
	public float moveLength;

	// Update is called once per frame
	void FixedUpdate ()
	{
		if (PhotonNetwork.isMasterClient)
		{
			transform.localPosition = new Vector3(Mathf.PingPong(Time.fixedTime * moveSpeed, moveLength / moveSpeed),
											transform.localPosition.y,
											transform.localPosition.z);
		}
	}
}
