using UnityEngine;
using System.Collections;

public class MouseCursor : MonoBehaviour
{
	public Texture2D basicCursorTexture;
	public Texture2D clickCursorTexture;
	//텍스처의 중심을 마우스 좌표로 할 것인지 입력받음
	public bool hotSpotIsCenter = false;
	//텍스처의 어느부분을 마우스의 좌표로 할 것인지 텍스처의 좌표를 입력받음
	public Vector2 adjustHotSpot = Vector2.zero;

	//public int width;
	//public int height;

	private Vector2 hotSpot;

	public void Start()
	{
		StartCoroutine("MyCursor");
	}

	IEnumerator MyCursor()
	{
		//모든 렌더링이 완료될 때까지 대기할테니 렌더링 완료되면 깨워달라고 부탁
		yield return new WaitForEndOfFrame();

		//텍스처의 중심을 마우스의 좌표로 사용하는 경우
		//텍스처의 폭과 높이의 1/2을 hotSpot 좌표로 입력
		if (hotSpotIsCenter)
		{
			hotSpot.x = basicCursorTexture.width / 2;
			hotSpot.y = basicCursorTexture.height / 2;
		}
		else
		{
			//중심을 사용하지 않을 경우 adjustHotSpot 사용
			hotSpot = adjustHotSpot;
		}
		//이제 새로운 마우스 커서를 화면에 표시
		//Cursor.SetCursor(basicCursorTexture, hotSpot, CursorMode.Auto);
		Cursor.SetCursor(basicCursorTexture, hotSpot, CursorMode.ForceSoftware);

	}

	private void OnMouseDown()
	{
		//Cursor.SetCursor(clickCursorTexture, hotSpot, CursorMode.Auto);
		Cursor.SetCursor(clickCursorTexture, hotSpot, CursorMode.ForceSoftware);

	}

	private void OnMouseUp()
	{
		//Cursor.SetCursor(basicCursorTexture, hotSpot, CursorMode.Auto);
		Cursor.SetCursor(basicCursorTexture, hotSpot, CursorMode.ForceSoftware);
	}
}
