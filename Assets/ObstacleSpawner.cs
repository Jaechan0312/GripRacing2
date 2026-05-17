using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // 프리팹 연결
    public Transform player;          // 플레이어 위치

    [Header("장애물 생성 간격 설정")]
    public float minSpawnDistance = 13f; // 최소 장애물 간격
    public float maxSpawnDistance = 23f; // 최대 장애물 간격

    [Header("장애물 크기(높이) 설정")]
    [Tooltip("장애물이 가질 수 있는 최소, 최대 높이(길이)를 설정하세요.")]
    public float minHeight = 1.0f; // 최소 기둥 높이
    public float maxHeight = 4.0f; // 최대 기둥 높이

    [Header("바닥 기준 좌표")]
    [Tooltip("장애물이 위치할 실제 바닥의 Y 좌표입니다.")]
    public float groundY = -2.45f; // 기존 바닥 높이 (-2.45f)를 기준으로 고정

    public float destroyDelay = 15f;  // 삭제될 시간 (초)

    private float nextSpawnX;         // 다음 생성 좌표

    void Start()
    {
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;
    }

    void Update()
    {
        if (player.position.x > nextSpawnX - 30f)
        {
            SpawnObstacle();

            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            nextSpawnX += randomDistance;
        }
    }

    void SpawnObstacle()
    {
        // 1. 장애물의 높이(Y축 Scale)를 랜덤으로 결정
        float randomHeight = Random.Range(minHeight, maxHeight);

        // 2. 바닥(-2.45f)에 딱 붙도록 Y축 위치(Position)를 계산
        // 공식: 바닥 위치 + (높이 / 2)
        float spawnY = groundY + (randomHeight / 2f);

        // 3. 최종 생성 위치 설정
        Vector3 spawnPos = new Vector3(nextSpawnX, spawnY, 0);

        // 4. 장애물 생성
        GameObject tempObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

        // 5. 생성된 장애물의 Y축 크기(Scale)를 변경하여 직사각형 기둥으로 만듦
        // X와 Z 크기는 프리팹의 원본 크기를 그대로 유지해!
        Vector3 currentScale = tempObstacle.transform.localScale;
        tempObstacle.transform.localScale = new Vector3(currentScale.x, randomHeight, currentScale.z);

        // 6. 지정된 시간 뒤 파괴
        Destroy(tempObstacle, destroyDelay);
    }
}