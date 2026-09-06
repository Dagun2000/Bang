using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("References")]
    public Transform headBone;          // 머리 본 (root/pelvis/.../Head)
    public Camera playerCamera;
    
    [Header("Settings")]
    public float sensitivity = 2f;
    public float maxYAngle = 60f;       // 상하 제한
    public float maxXAngle = 80f;       // 좌우 제한
    
    private bool isLookMode = false;    // 우클릭 토글 상태
    private float rotX = 0f;            // 좌우
    private float rotY = 0f;            // 상하
    
    void Update()
    {
        // 우클릭 토글
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isLookMode = !isLookMode;
            
            if (isLookMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        // 시점 회전 모드
        if (isLookMode)
        {
            float mouseX = Mouse.current.delta.x.ReadValue() * sensitivity * 0.1f;
            float mouseY = Mouse.current.delta.y.ReadValue() * sensitivity * 0.1f;
            
            rotX += mouseX;
            rotY -= mouseY;
            
            rotX = Mathf.Clamp(rotX, -maxXAngle, maxXAngle);
            rotY = Mathf.Clamp(rotY, -maxYAngle, maxYAngle);
        }
    }
    
    void LateUpdate()
    {
        // 카메라 회전
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotY, rotX, 0);
        }
        
        // 머리 본 회전 (카메라와 동기화)
        if (headBone != null)
        {
            headBone.localRotation = Quaternion.Euler(rotY * 0.5f, rotX, 0);  // 머리는 절반만
        }
    }
    
    public bool IsLookMode()
    {
        return isLookMode;
    }
}