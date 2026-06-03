using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("장애물 프리팹 분리 설정")]
    public GameObject normalObstaclePrefab; // 일반 장애물 (꼬깔콘/나무 등)
    public GameObject tunnelObstaclePrefab; // 터널 장벽용 (가드레일/안전펜스 등)

    public Transform player;          // 플레이어 위치
    public TextMeshProUGUI warningText; // "주의" 문구 UI 연결 칸

    [Header("장애물 생성 간격 설정")]
    public float minSpawnDistance = 13f;
    public float maxSpawnDistance = 23f;

    [Header("⭐ 일반 장애물 크기(높이/너비) 설정")]
    public float minHeight = 1.0f;
    public float maxHeight = 4.0f;
    public float normalObstacleWidth = 1.0f;

    [Header("바닥 기준 좌표")]
    public float groundY = -2.45f;

    [Header("장애물 공중 부양 설정")]
    public float floatingOffset = 0.5f;

    public float destroyDelay = 15f;

    [Header("⭐ 터널 설정 (가운데 통로 공간)")]
    public float tunnelBottomY = 1.0f;  // 통로 바닥
    public float tunnelTopY = 4.5f;     // 통로 천장
    public float tunnelLength = 40f;    // 터널 가로 길이

    [Range(0f, 1f)]
    public float tunnelChance = 0.2f;   // 터널이 등장할 기본 확률

    private float nextSpawnX;
    private bool isFeverTime = false;
    private float currentScore = 0f;

    // 일반 장애물 연속 생성 카운트 변수
    private int obstaclesSpawnedSinceLastTunnel = 0;

    private float tunnelStartX = 0f;
    private float tunnelEndX = 0f;
    private bool isWarningActive = false;

    private bool isNextObstacleTunnel = false;
    private float precedingObstacleX = -999f;
    private bool safeToTriggerWarning = false;

    void Start()
    {
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;

        if (warningText != null) warningText.gameObject.SetActive(false);

        // 시작할 때는 일반 장애물 카운트를 0으로 초기화
        obstaclesSpawnedSinceLastTunnel = 0;
        DecideNextObstacleType();
    }

    void DecideNextObstacleType()
    {
        // ⭐ [3번 요구사항 천장 시스템 구현] 
        // 만약 일반 장애물이 연속으로 7번 이상 나왔다면, 주사위 안 굴리고 무조건 터널 확정!
        if (obstaclesSpawnedSinceLastTunnel >= 7)
        {
            isNextObstacleTunnel = true;
            return;
        }

        // 7번 연속 안 나왔을 때는 지정한 확률(20%)로 터널 생성 결정
        if (Random.value < tunnelChance)
        {
            isNextObstacleTunnel = true;
        }
        else
        {
            isNextObstacleTunnel = false;
        }
    }

    void Update()
    {
        if (safeToTriggerWarning)
        {
            if (!isWarningActive && player.position.x >= precedingObstacleX)
            {
                TurnOnWarning();
            }

            if (isWarningActive && player.position.x >= tunnelStartX)
            {
                TurnOffWarning();
            }
        }

        if (isFeverTime && player.position.x >= tunnelEndX)
        {
            isFeverTime = false;
        }

        if (player.position.x > nextSpawnX - 30f)
        {
            if (isNextObstacleTunnel && tunnelObstaclePrefab != null)
            {
                SpawnLongTunnelWall();
            }
            else
            {
                SpawnNormalObstacleAndUpdate();
            }
        }
    }

    void SpawnNormalObstacleAndUpdate()
    {
        SpawnObstacle();

        obstaclesSpawnedSinceLastTunnel++; // 일반 장애물 나왔으니 카운트 +1
        precedingObstacleX = nextSpawnX;

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX += randomDistance;

        DecideNextObstacleType();
    }

    void SpawnObstacle()
    {
        if (normalObstaclePrefab == null) return;

        float randomHeight = Random.Range(minHeight, maxHeight);
        float spawnY = groundY + (randomHeight / 2f) + floatingOffset;
        Vector3 spawnPos = new Vector3(nextSpawnX, spawnY, 0);

        GameObject tempObstacle = Instantiate(normalObstaclePrefab, spawnPos, Quaternion.identity);
        tempObstacle.transform.localScale = new Vector3(normalObstacleWidth, randomHeight, tempObstacle.transform.localScale.z);

        Destroy(tempObstacle, destroyDelay);
    }

    void SpawnLongTunnelWall()
    {
        isFeverTime = true;
        isNextObstacleTunnel = false;
        obstaclesSpawnedSinceLastTunnel = 0; // ⭐ 터널 소환했으니 연속 카운트 초기화!

        float spawnXPosition = nextSpawnX;
        tunnelStartX = spawnXPosition;
        tunnelEndX = spawnXPosition + tunnelLength;

        safeToTriggerWarning = true;

        float bottomWallHeight = Mathf.Abs(tunnelBottomY - groundY);
        float bottomWallCenterY = groundY + (bottomWallHeight / 2f);
        Vector3 bottomPos = new Vector3(spawnXPosition + (tunnelLength / 2f), bottomWallCenterY, 0);

        GameObject bottomWall = Instantiate(tunnelObstaclePrefab, bottomPos, Quaternion.identity);
        bottomWall.transform.localScale = new Vector3(tunnelLength, bottomWallHeight, bottomWall.transform.localScale.z);
        Destroy(bottomWall, destroyDelay);

        float topWallHeight = 10f;
        float topWallCenterY = tunnelTopY + (topWallHeight / 2f);
        Vector3 topPos = new Vector3(spawnXPosition + (tunnelLength / 2f), topWallCenterY, 0);

        GameObject topWall = Instantiate(tunnelObstaclePrefab, topPos, Quaternion.identity);
        topWall.transform.localScale = new Vector3(tunnelLength, topWallHeight, topWall.transform.localScale.z);
        Destroy(topWall, destroyDelay);

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = tunnelEndX + randomDistance;

        DecideNextObstacleType();
    }

    void TurnOnWarning()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "WARNING";
            isWarningActive = true;
        }
    }

    void TurnOffWarning()
    {
        isWarningActive = false;
        safeToTriggerWarning = false;
        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    public void UpdateScoreFromServer(float score)
    {
        currentScore = score;
    }
}