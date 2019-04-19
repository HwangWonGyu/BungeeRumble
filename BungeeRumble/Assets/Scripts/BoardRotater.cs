using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardRotater : MonoBehaviour {

	public float rotateAngle;
	public float moveSpeed;

	private PhotonTransformView ptv;

	private void Start()
	{
		ptv = GetComponent<PhotonTransformView>();
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (PhotonNetwork.isMasterClient)
		{
			transform.Rotate(Vector3.up, rotateAngle * Time.fixedDeltaTime);
			transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime);

			ptv.SetSynchronizedValues(Vector3.forward * moveSpeed, 0);
		}
	}
}
