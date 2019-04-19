using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float XSensitivity = 2f;
    public float YSensitivity = 2f;
    public bool clampVerticalRotation = true;
    public float MinimumX = -90F;
    public float MaximumX = 90F;
    public bool smooth;
    public float smoothTime = 5f;
    public bool lockCursor = true;

    private Quaternion m_CharacterTargetRot;
    private Quaternion m_CameraTargetRot;
    private bool m_cursorIsLocked = true;

    public void Init(Transform character, Transform camera)
    {
        m_CharacterTargetRot = character.localRotation;
        m_CameraTargetRot = camera.localRotation;
    }

    public void LookRotation(Transform character, Transform camera)
    {
       
            float yRot = Input.GetAxis("Mouse X") * XSensitivity;
            float xRot = Input.GetAxis("Mouse Y") * YSensitivity;

            m_CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);

            if (smooth)
            {
                character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot,
                    smoothTime * Time.fixedDeltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot,
                    smoothTime * Time.fixedDeltaTime);
            }
            else
            {
                character.localRotation = m_CharacterTargetRot;
                camera.localRotation = m_CameraTargetRot;
            }

            UpdateCursorLock();
    }

    public void SetCursorLock(bool value)
    {
        lockCursor = value;
        if (!lockCursor)
        {//we force unlock the cursor if the user disable the cursor locking helper
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void UpdateCursorLock()
    {
        //if the user set "lockCursor" we check & properly lock the cursos
        if (lockCursor)
            InternalLockUpdate();
    }

    private void InternalLockUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            m_cursorIsLocked = false;
			//print("ESC 눌렀다가 뗐으니 m_cursorIsLocked False로");
		}
		else if (Input.GetMouseButtonUp(0))
        {
            m_cursorIsLocked = true;
			//print("왼쪽 마우스 버튼 눌렀다가 뗐으니 m_cursorIsLocked True로 바꿈");
        }

        if (m_cursorIsLocked)
        {
			//print("m_cursorIsLocked가 true니까 마우스 안보이게");
			//if (UIManager.instance.isOptionsPopup == false)
			//{
			//	Cursor.lockState = CursorLockMode.Locked;
			//	Cursor.visible = false;
			//	print("설마?");
			//}

			// 결과창 떠있으면 마우스 보이게
			if (UIManager.instance.winPanel.activeSelf || UIManager.instance.losePanel.activeSelf)
			{
				if (Cursor.visible == false)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					//print("마우스 보임");
				}
			}
			else // 결과창 안떠있으면
			{
				// 팝업창 안떠있으면 마우스 안보이게
				if (Cursor.visible == true && UIManager.instance.isOptionsPopup == false)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = false;
					//print("마우스 안보임");
				}
			}
		}
        else if (!m_cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
			//print("m_cursorIsLocked가 false니까 마우스 보이게");
        }
    }

    Quaternion ClampRotationAroundXAxis(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

        angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }

}