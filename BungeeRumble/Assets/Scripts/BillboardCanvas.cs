using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
	private Transform tr;
	private Transform cameraTransform;

	// Use this for initialization
	void Start()
	{
		tr = GetComponent<Transform>();
		// 상대 플레이어의 자식에 있는 카메라의 Transform 컴포넌트를 가져옴
		cameraTransform = GameObject.Find("ChildCamera").transform;
	}

	// Update is called once per frame
	void Update()
	{
		if(cameraTransform.gameObject.activeSelf == true)
			tr.LookAt(cameraTransform);
		else
			tr.LookAt(Camera.main.transform);
	}
}

