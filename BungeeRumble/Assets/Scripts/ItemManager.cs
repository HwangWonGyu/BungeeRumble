using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemManager : MonoBehaviour
{
    private PhotonView pv = null;
    private ItemThrow itemline;

    [SerializeField]
    private float shieldTime;

    [SerializeField]
    private float jumpTime;

    [HideInInspector]
    public bool stairDestroyCheck;
    [HideInInspector]
    public string stairName;
    [HideInInspector]
    public string itemboxName;

    [SerializeField]
    private Sprite itemBasic;
	[SerializeField]
    private GameObject pngPrefab;
    [SerializeField]
    private GameObject ExplosionPrefab;

    [SerializeField]
    private GameObject shieldPrefab;
    [SerializeField]
    private GameObject jumpPrefab;
    [SerializeField]
    private GameObject stairDestroyPrefab;

    private Sprite[] invItems;

	private AudioSource itemAudioSource;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    private void Start(){

        itemline = this.GetComponent<ItemThrow>();

        invItems = new Sprite[2];

        stairName = null;
        itemBasic.name = "basic";

        //아이템 슬롯 초기화
        ItemSlot(itemBasic);

		itemAudioSource = GetComponents<AudioSource>()[4];
	}

    private void Update()
    {
        if (!pv.isMine)
            return;

        if (Input.GetKeyDown(KeyCode.E) && invItems[0].name != "basic")
        {
            switch (invItems[0].name)
            {
                case "jumpItem":
                    StartCoroutine(this.JumpExecution());
                    pv.RPC("ParticleRPC", PhotonTargets.All, "Jump", pv.viewID);
                    break;
                case "shieldItem":
                    StartCoroutine(this.ShieldExecution());
                    pv.RPC("ParticleRPC", PhotonTargets.All, "Shield", pv.viewID);
                    break;
                case "stairDestroyItem":
					Invoke("StairExecution", 3.0f);
                    Instantiate(stairDestroyPrefab, transform.position, Quaternion.identity);
                    stairDestroyCheck = true;
                    break;
                case "pngItem":
					itemline.enabled = true;
                    itemline.lineRenderer.enabled = true;
                    itemline.itemName = "png";
                    break;
                case "ExplosionItem":
					itemline.enabled = true;
                    itemline.lineRenderer.enabled = true;
                    itemline.itemName = "Explosion";
                    break;
            }
            ItemSlotRemove();
        }

        if(Input.GetKeyDown(KeyCode.LeftShift) && invItems[1].name != "basic")
        {
            Sprite temp = invItems[0];
            invItems[0] = invItems[1];
            invItems[1] = temp;

			for (int i = 0; i < UIManager.instance.itemslot.Length; i++)
			{
				UIManager.instance.itemslot[i].GetComponent<Image>().sprite = invItems[i];
			}

			//// 슬롯 별로 다른 배경 넣어줘야 해서 로직 수정
			//UIManager.instance.itemslot[0].GetComponent<Image>().sprite = invItems[0];
			//UIManager.instance.itemslot[1].GetComponent<Image>().sprite = UIManager.instance.ItemBackGrounds[1];
		}
    }

    [PunRPC]
    public void ItemconstructorRPC(string itemName, Vector3 location)
    {
        if(itemName == "png")
        {
			AudioManager.instance.PlaySmokeSound(itemAudioSource);
			GameObject temp = Instantiate(pngPrefab, location, Quaternion.identity);
			Destroy(temp, 15.0f);
        }
        else
        {
			AudioManager.instance.PlayFlashBangSound(itemAudioSource);
			GameObject temp = Instantiate(ExplosionPrefab, location, Quaternion.identity);
			Destroy(temp, 7.0f);
        }
    }

    //계단 삭제 동기화
    public void StairExecution()
    {
        pv.RPC("StairDestroyRPC", PhotonTargets.Others, stairName);
    }

    //아이템 박스 삭제 동기화
    public void ItemBoxExecution()
    {
		// 아이템 박스 획득 사운드 재생
		AudioManager.instance.PlayItemGetSound(itemAudioSource);
		pv.RPC("ItemBoxDestroyRPC", PhotonTargets.Others, itemboxName);
    }

    [PunRPC]
    public void ItemBoxDestroyRPC(string boxNameRPC)
    {
		GameObject temp;
        Destroy(temp = GameObject.Find(boxNameRPC).gameObject, 2);
		// 아이템 박스 안보이게
		temp.GetComponent<MeshRenderer>().enabled = false;
		// 아이템 박스 못먹게
		BoxCollider[] boxColliders = temp.GetComponents<BoxCollider>();
		for (int i = 0; i < boxColliders.Length; i++)
		{
			boxColliders[i].enabled = false;
		}

		//itemboxName = null;
		StartCoroutine(ItemboxNameToNull(2.0f));
    }

	IEnumerator ItemboxNameToNull(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		itemboxName = null;
	}

	//해당 계단의 이름을 넘겨 받아서 꺼버린다.
	[PunRPC]
    public void StairDestroyRPC(string stairNameRPC)
    {
		//BoxCollider[] boxCollider = GameObject.Find(stairNameRPC).GetComponents<BoxCollider>();

		//for (int i = 0; i < boxCollider.Length; i++)
		//{
		//    boxCollider[i].enabled = false;
		//}

		//MeshCollider meshCollider = GameObject.Find(stairNameRPC).GetComponent<MeshCollider>();

		//meshCollider.enabled = false;

		////stairName = null;
        
		StartCoroutine(Test(stairNameRPC));
    }

	IEnumerator Test(string stairNameRPC)
	{
		yield return new WaitForSeconds(3.0f);

		// 회전 계단은 다른 계단과 콜라이더 구조가 다르기 때문에 따로 처리
		if (this.gameObject.name == "Spiralstairs")
		{
			print("회전계단");
		}
		else
		{
			if (stairNameRPC != null)
			{
				MeshCollider meshCollider = GameObject.Find(stairNameRPC).GetComponent<MeshCollider>();

				meshCollider.enabled = false;
				print("일반계단");
			}
		}
		stairName = null;
	}

    //쉴드 : 레이블를 바꿔서 플레이어를 찾지 못하게 한다.
    [PunRPC]
    public void ShiledRPC(int i, int id)
    {
		if (id == pv.viewID)
		{
			//print("나만?");
			AudioManager.instance.PlayShieldSound(itemAudioSource);
		}

		if (i == 1)
        {
            PhotonView.Find(id).gameObject.GetComponent<Controller>().isShield = true;
        }
        if(i == 0)
        {
            PhotonView.Find(id).gameObject.GetComponent<Controller>().isShield = false;
        }
    }

    [PunRPC]
    public void ParticleRPC(string str, int id)
    {
        //2단 점프
       if(str == "Jump")
        {
            PhotonView.Find(id).GetComponent<Controller>().usedItem = Instantiate(jumpPrefab, PhotonView.Find(id).gameObject.transform.position , Quaternion.identity);
            PhotonView.Find(id).GetComponent<Controller>().usedItem.transform.parent = PhotonView.Find(id).gameObject.transform;
            Destroy(PhotonView.Find(id).GetComponent<Controller>().usedItem, jumpTime);
        }
       //쉴드
        else
        {
            PhotonView.Find(id).GetComponent<Controller>().usedItem = Instantiate(shieldPrefab, PhotonView.Find(id).gameObject.transform.position + Vector3.up * 0.8f, Quaternion.identity);
            PhotonView.Find(id).GetComponent<Controller>().usedItem.transform.parent = PhotonView.Find(id).gameObject.transform;
            Destroy(PhotonView.Find(id).GetComponent<Controller>().usedItem, shieldTime);
        }
    }

    [PunRPC]
    public void ParticleDestroyRPC(int id)
    {
        Destroy(PhotonView.Find(id).GetComponent<Controller>().usedItem);
    }

    IEnumerator ShieldExecution()
    {

        pv.RPC("ShiledRPC", PhotonTargets.All,1,pv.viewID);

        yield return new WaitForSeconds(shieldTime);

        pv.RPC("ShiledRPC", PhotonTargets.All,0,pv.viewID);
    }

    IEnumerator JumpExecution()
    {
        this.GetComponent<Controller>().jumpItem = true;
        
        yield return new WaitForSeconds(jumpTime);

        this.GetComponent<Controller>().jumpItem = false;
    }

    private void ItemSlotRemove()
    {
        invItems[0] = invItems[1];
        invItems[1] = itemBasic;

		//for (int i = 0; i < UIManager.instance.itemslot.Length; i++)
		//{
		//	UIManager.instance.itemslot[i].GetComponent<Image>().sprite = invItems[i];
		//}

		// 슬롯 별로 다른 배경 넣어줘야 해서 로직 수정
		UIManager.instance.itemslot[0].GetComponent<Image>().sprite = invItems[0];
		UIManager.instance.itemslot[1].GetComponent<Image>().sprite = UIManager.instance.ItemBackGrounds[1];

	}

	public void ItemSlot(Sprite itemImage)
    {
        if (!pv.isMine)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (invItems[i] == null)
            {
                invItems[i] = itemImage;
				// 아이템 슬롯별 배경 다르게 해야해서 조건 추가
				if (i == 0)
				{
					UIManager.instance.itemslot[i].GetComponent<Image>().sprite = itemImage;
				}
				else if (i == 1)
				{
					UIManager.instance.itemslot[i].GetComponent<Image>().sprite = UIManager.instance.ItemBackGrounds[i];
				}
            }
            else if (invItems[i].name == "basic")
            {
                invItems[i] = itemImage;
				UIManager.instance.itemslot[i].GetComponent<Image>().sprite = itemImage;
                break;
            }
		}
    }
}
