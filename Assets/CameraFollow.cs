using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 따라갈 대상 (큐브)
    public float offset_x = 0f; // 카메라와 캐릭터 사이의 가로 간격

    void LateUpdate()
    {
        if (target != null)
        {
            // 카메라의 현재 위치를 가져와서 x좌표만 타겟의 x좌표로 바꿔줌
            Vector3 newPos = transform.position;
            newPos.x = target.position.x + offset_x;

            // 변경된 위치를 카메라에 적용
            transform.position = newPos;
        }
    }
}