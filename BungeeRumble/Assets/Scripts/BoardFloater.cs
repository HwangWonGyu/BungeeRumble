using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardFloater : MonoBehaviour
{
	public float moveSpeed;
	public float moveLength;

	// Update is called once per frame
	void FixedUpdate()
	{
		if (PhotonNetwork.isMasterClient)
		{
			transform.position = new Vector3(transform.position.x,
											Mathf.PingPong(Time.fixedTime * moveSpeed, moveLength / moveSpeed),
											transform.position.z);
		}
	}

}