using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // 프리팹 연결
    public Transform player;          // 플레이어 위치

    [Header("장애물 생성 간격 설정")]
    public float minSpawnDistance = 13f; // 최소 장애물 간격 (너무 좁으면 피하기 불가능)
    public float maxSpawnDistance = 23f; // 최대 장애물 간격 (너무 멀면 지루함)

    [Header("장애물 생성 높이 설정")]
    [Tooltip("기존 바닥 높이인 -2.45f를 기준으로 삼으세요.")]
    public float minSpawnHeight = -2.45f; // 최저 높이
    public float maxSpawnHeight = -1.0f;  // 최고 높이 (캐릭터 점프 높이에 따라 조절)

    public float destroyDelay = 15f;  // 삭제될 시간 (초)

    private float nextSpawnX;         // 다음 생성 좌표

    void Start()
    {
        // 첫 장애물 위치 설정 (첫 번째 간격도 랜덤으로 부여)
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;
    }

    void Update()
    {
        // 플레이어가 생성 지점 근처(30f 앞)까지 오면 생성
        if (player.position.x > nextSpawnX - 30f)
        {
            SpawnObstacle();

            // 핵심 수정 부분: 다음 장애물까지의 간격을 무작위로 뽑아서 더해줍니다.
            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            nextSpawnX += randomDistance;
        }
    }

    void SpawnObstacle()
    {
        // Y값도 네 바닥 높이 사이에서 랜덤으로 결정!
        float randomY = Random.Range(minSpawnHeight, maxSpawnHeight);
        Vector3 spawnPos = new Vector3(nextSpawnX, randomY, 0);
        // -----------------------------------------------------------------

        // 1. 장애물을 생성하고 'tempObstacle'이라는 변수에 잠시 담아둬
        GameObject tempObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

        // 2. 생성된 장애물한테 15초 뒤에 스스로 파괴되라고 명령해!
        Destroy(tempObstacle, destroyDelay);
    }
}