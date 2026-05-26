using UnityEngine;
using TMPro;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // 프리팹 연결
    public Transform player;          // 플레이어 위치
    public TextMeshProUGUI warningText; // "주의" 문구 UI 연결 칸

    [Header("장애물 생성 간격 설정")]
    public float minSpawnDistance = 13f;
    public float maxSpawnDistance = 23f;

    [Header("장애물 크기(높이) 설정")]
    public float minHeight = 1.0f;
    public float maxHeight = 4.0f;

    [Header("바닥 기준 좌표")]
    public float groundY = -2.45f;
    public float destroyDelay = 15f;

    [Header("등척성 피버 타임 설정")]
    public float feverStartScore = 15f; // 15점 진입 고정
    public float feverDuration = 10f;   // 피버 타임 지속 시간 (10초)

    [Header("⭐ 터널 출구 높이 (가운데 통로 공간)")]
    public float tunnelBottomY = 1.0f;  // 통로 바닥 (이 아래는 장애물벽)
    public float tunnelTopY = 4.5f;     // 통로 천장 (이 위는 장애물벽) -> 틈새를 널널하게 3.5 확보

    private float nextSpawnX;
    private bool isFeverTime = false;
    private float feverTimer = 0f;
    private float currentScore = 0f;
    private bool hasSpawnedFeverWall = false; // 피버 장벽 중복 생성 방지

    void Start()
    {
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;

        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    void Update()
    {
        // 1. 피버 타임 조건 체크
        if (!isFeverTime && currentScore >= feverStartScore)
        {
            StartFeverTime();
        }

        // 2. 피버 타임 타이머 작동
        if (isFeverTime)
        {
            feverTimer -= Time.deltaTime;
            if (feverTimer <= 0)
            {
                EndFeverTime();
            }
        }

        // 3. 장애물 생성 루프
        if (player.position.x > nextSpawnX - 30f)
        {
            if (isFeverTime)
            {
                // ⭐ 피버 타임일 때는 개별 기둥 스폰을 멈추고, 긴 터널 장벽을 딱 '한 번' 길게 깝니다.
                if (!hasSpawnedFeverWall)
                {
                    SpawnLongTunnelWall();
                }
            }
            else
            {
                // 평소에는 기존 랜덤 장애물 생성
                SpawnObstacle();
                float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
                nextSpawnX += randomDistance;
            }
        }
    }

    void SpawnObstacle()
    {
        float randomHeight = Random.Range(minHeight, maxHeight);
        float spawnY = groundY + (randomHeight / 2f);
        Vector3 spawnPos = new Vector3(nextSpawnX, spawnY, 0);

        GameObject tempObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        Vector3 currentScale = tempObstacle.transform.localScale;
        tempObstacle.transform.localScale = new Vector3(currentScale.x, randomHeight, currentScale.z);

        Destroy(tempObstacle, destroyDelay);
    }

    // ⭐ 핵심 수정: y=3, y=10 그래프 그리듯 완벽하게 이어지는 가로 롱 장벽 생성
    void SpawnLongTunnelWall()
    {
        hasSpawnedFeverWall = true;

        // 플레이어 속도나 피버 지속 시간(10초)을 감안하여 엄청나게 길게 장벽 가로 크기를 잡습니다.
        float wallLength = 150f;
        float spawnXPosition = player.position.x + 15f; // 플레이어 조금 앞쪽부터 장벽 시작

        // 1. 아래쪽을 꽉 채우는 연속 장벽 (y = tunnelBottomY 이하를 전부 채움)
        float bottomWallHeight = Mathf.Abs(tunnelBottomY - groundY);
        float bottomWallCenterY = groundY + (bottomWallHeight / 2f);
        Vector3 bottomPos = new Vector3(spawnXPosition + (wallLength / 2f), bottomWallCenterY, 0);

        GameObject bottomWall = Instantiate(obstaclePrefab, bottomPos, Quaternion.identity);
        bottomWall.transform.localScale = new Vector3(wallLength, bottomWallHeight, bottomWall.transform.localScale.z);
        Destroy(bottomWall, feverDuration + 2f); // 피버타임 끝나면 파괴

        // 2. 위쪽을 꽉 채우는 연속 장벽 (y = tunnelTopY 이상을 전부 채움)
        float topWallHeight = 10f; // 천장을 넉넉히 가릴 높이
        float topWallCenterY = tunnelTopY + (topWallHeight / 2f);
        Vector3 topPos = new Vector3(spawnXPosition + (wallLength / 2f), topWallCenterY, 0);

        GameObject topWall = Instantiate(obstaclePrefab, topPos, Quaternion.identity);
        topWall.transform.localScale = new Vector3(wallLength, topWallHeight, topWall.transform.localScale.z);
        Destroy(topWall, feverDuration + 2f); // 피버타임 끝나면 파괴
    }

    void StartFeverTime()
    {
        isFeverTime = true;
        feverTimer = feverDuration;
        hasSpawnedFeverWall = false; // 플래그 초기화

        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "WARNING";
        }
    }

    void EndFeverTime()
    {
        isFeverTime = false;
        if (warningText != null) warningText.gameObject.SetActive(false);

        // 피버타임 끝난 장벽 너머로 다음 일반 장애물 배치 시작 좌표 설정
        nextSpawnX = player.position.x + 30f;
    }

    public void UpdateScoreFromServer(float score)
    {
        currentScore = score;
    }
}