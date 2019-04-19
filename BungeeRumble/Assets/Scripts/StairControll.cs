using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StairControll : MonoBehaviour {

    [HideInInspector]
    public bool stairItem;

    private ItemManager itemManager;

    private void Update()
    {
        if (stairItem)
        {
            print("사용중");
            StartCoroutine(this.StairExecution());
            //아이템 매니저에 박스 이름을 넣어줌;
            itemManager.stairName = this.gameObject.name;

            itemManager.StairExecution();

			//아이템을 사용했다고 변경해줌
			StartCoroutine(StairDestroyCheckToFalse());
            //itemManager.stairDestroyCheck = false;

            //업데이트에서 더이상 발동하지 않도록함 
            stairItem = false;
        }

    }

	IEnumerator StairDestroyCheckToFalse()
	{
		yield return null;
		itemManager.stairDestroyCheck = false;
	}


	private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 아이템 매니저를 가져 온다.
            itemManager = other.gameObject.GetComponent<ItemManager>();

            // 가져온곳에서 아이템을 사용했는지 체크
            stairItem = itemManager.stairDestroyCheck;

			//print("검색중");
        }
    }
    
    IEnumerator StairExecution()
    {
        yield return new WaitForSeconds(3.0f);

		//BoxCollider[] boxCollider = this.gameObject.GetComponents<BoxCollider>();

		//for (int i = 0; i < boxCollider.Length; i++)
		//{
		//    boxCollider[i].enabled = false;
		//}

		// 회전 계단은 다른 계단과 콜라이더 구조가 다르기 때문에 따로 처리
		if (this.gameObject.name == "Spiralstairs")
		{
			print("회전계단");
		}
		else
		{
			MeshCollider meshCollider = this.gameObject.GetComponent<MeshCollider>();

			meshCollider.enabled = false;
			print("일반계단");
            print("삭제중");
        }

	}
}
