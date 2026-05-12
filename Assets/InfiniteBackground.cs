using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    public Transform target; // 카메라나 플레이어
    public Transform[] grounds; // 바닥들 (Ground1, Ground2)
    public float groundLength = 100f; // 바닥 길이

    void Update()
    {
        foreach (Transform ground in grounds)
        {
            // 플레이어가 바닥의 끝 지점을 지나가면?
            if (target.position.x - ground.position.x > groundLength)
            {
                // 그 바닥을 현재 위치에서 (길이 * 바닥 개수)만큼 앞으로 이동!
                Vector3 newPos = ground.position;
                newPos.x += groundLength * grounds.Length;
                ground.position = newPos;
            }
        }
    }
}