using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // 프리팹 연결
    public Transform player;          // 플레이어 위치
    public float spawnDistance = 20f; // 장애물 간격
    public float destroyDelay = 15f;  // 삭제될 시간 (초)

    private float nextSpawnX;         // 다음 생성 좌표

    void Start()
    {
        // 첫 장애물 위치 설정
        nextSpawnX = player.position.x + spawnDistance;
    }

    void Update()
    {
        // 플레이어가 생성 지점 근처(30f 앞)까지 오면 생성
        if (player.position.x > nextSpawnX - 30f)
        {
            SpawnObstacle();
            nextSpawnX += spawnDistance;
        }
    }

    void SpawnObstacle()
    {
        // Y값은 네 바닥 높이인 -2.45f로 고정!
        Vector3 spawnPos = new Vector3(nextSpawnX, -2.45f, 0);

        // 1. 장애물을 생성하고 'tempObstacle'이라는 변수에 잠시 담아둬
        GameObject tempObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

        // 2. 생성된 장애물한테 15초 뒤에 스스로 파괴되라고 명령해!
        Destroy(tempObstacle, destroyDelay);
    }
}