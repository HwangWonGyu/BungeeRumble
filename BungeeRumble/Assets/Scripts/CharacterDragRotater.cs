using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDragRotater : MonoBehaviour {

	public float rotateMultiplier;

	private IEnumerator OnMouseDown()
	{
		float initialMouseHorizontalPosition = 0.0f;
		float previousMouseHorizontalPosition;

		while (Input.GetMouseButton(0))
		{
			previousMouseHorizontalPosition = initialMouseHorizontalPosition;
			initialMouseHorizontalPosition = -Input.mousePosition.x;

			transform.Rotate(
				Vector3.up,
				new Vector2((initialMouseHorizontalPosition - previousMouseHorizontalPosition), 0.0f).normalized.x * rotateMultiplier
				);

			yield return null;
		}
	}
}
