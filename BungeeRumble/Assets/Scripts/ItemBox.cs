using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Itemlist
{
   public Sprite jumpItem, 
            pngItem, 
            shieldItem, 
            stairDestroyItem, 
            ExplosionItem;
}

public class ItemBox : MonoBehaviour {

    public Itemlist itemlist;

    private int item;

    private ItemManager itemManager;

    public GameObject particle;

    private void Awake()
    {
		item = Random.Range(0, 5);

		// 가짜계단 아이템이라면 다시 랜덤
		if (item == 2)
		{
			item = Random.Range(0, 5);
		}
	}

    private void Update()
    {
        if (itemManager != null)
        {
            switch (item)
            {
                case 0:
                    itemlist.jumpItem.name = "jumpItem";
                    itemManager.ItemSlot(itemlist.jumpItem);
                    break;
                case 1:
                    itemlist.shieldItem.name = "shieldItem";
                    itemManager.ItemSlot(itemlist.shieldItem);
                    break;
                case 2:
                    itemlist.stairDestroyItem.name = "stairDestroyItem";
                    itemManager.ItemSlot(itemlist.stairDestroyItem);
                    break;
                case 3:
                    itemlist.pngItem.name = "pngItem";
                    itemManager.ItemSlot(itemlist.pngItem);
                    break;
                case 4:
                    itemlist.ExplosionItem.name = "ExplosionItem";
                    itemManager.ItemSlot(itemlist.ExplosionItem);
                    break;
            }

            itemManager.itemboxName = this.gameObject.name;

            itemManager.ItemBoxExecution();

            itemManager = null;

            GameObject ticle = Instantiate(particle, this.transform.position, Quaternion.identity);

			// 아이템 박스 안보이게
			GetComponent<MeshRenderer>().enabled = false;
			// 아이템 박스 못먹게
			BoxCollider[] boxColliders = GetComponents<BoxCollider>();
			for (int i = 0; i < boxColliders.Length; i++)
			{
				boxColliders[i].enabled = false;
			}

            Destroy(this.gameObject, 1);

            Destroy(ticle, 2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 해당 플레이어의 아이템 매니저를 가져 온다.
            itemManager = other.gameObject.GetComponent<ItemManager>();
        }
    } 
}
