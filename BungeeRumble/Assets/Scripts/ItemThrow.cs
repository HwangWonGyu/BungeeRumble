using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemThrow : MonoBehaviour {

    public LineRenderer lineRenderer;
    public Transform rayStart;
    public Transform markerobject;

    public string itemName;

    Vector3 center = Vector3.zero;
    Vector3 theArc = Vector3.zero;

    RaycastHit hitInfo;

    private PhotonView pv = null;

	private void Awake()
    {
        pv = GetComponent<PhotonView>();

        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 25;
	}

    void Start()
    {
        lineRenderer.enabled = false;
        this.enabled = false;
    }

    void Update()
    {
        if (!pv.isMine)
            return;

		// NullReference를 방지하기 위한 조건문
		if (GetComponentInChildren<Camera>() != null)
		{
			print("?");
			//카메라 씬 중간에서 레이를 쏴라
			Ray ray = GetComponentInChildren<Camera>().ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0.0f));

			Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red);

			//타겟 포인트 초기화
			Vector3 targetPoint = Vector3.zero;

			//플레이어를 제외한 다른 물체가 맞으면 
			if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~LayerMask.GetMask("Player")))
			{

				//레이 시작 지점과 맞은 지점의 거리 
				targetPoint = ray.GetPoint(Vector3.Distance(ray.origin, hitInfo.point));

				center = (rayStart.position + targetPoint) * 0.5f;
				center.y -= 70.0f;

				RaycastHit hitInfoLine;

				if (Physics.Linecast(rayStart.position, targetPoint, out hitInfoLine))
				{
					targetPoint = hitInfoLine.point;
				}
			}
			else
			{
				targetPoint = rayStart.position;
			}

			if (Input.GetMouseButtonDown(0))
			{
				pv.RPC("ItemconstructorRPC", PhotonTargets.All, itemName, targetPoint);

				this.enabled = false;
				lineRenderer.enabled = false;
			}

			Vector3 RelCenter = rayStart.position - center;
			Vector3 aimRelCenter = targetPoint - center;

			for (float index = 0.0f, interval = -0.0417f; interval < 1.0f;)
			{
				theArc = Vector3.Slerp(RelCenter, aimRelCenter, interval += 0.0417f);
				lineRenderer.SetPosition((int)index++, theArc + center);
			}
		}
    }

}
