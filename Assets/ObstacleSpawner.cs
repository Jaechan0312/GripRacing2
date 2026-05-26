using UnityEngine;
using UnityEngine.SceneManagement;
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
    public float feverStartScore = 15f; // 레거시 유지
    public float feverDuration = 10f;   // 레거시 유지

    [Header("⭐ 터널 설정 (가운데 통로 공간)")]
    public float tunnelBottomY = 1.0f;  // 통로 바닥
    public float tunnelTopY = 4.5f;     // 통로 천장
    public float tunnelLength = 40f;    // 터널 가로 길이

    [Range(0f, 1f)]
    public float tunnelChance = 0.25f;  // 터널이 등장할 기본 확률 (25%)

    private float nextSpawnX;
    private bool isFeverTime = false;
    private float feverTimer = 0f;            // 레거시 유지
    private float currentScore = 0f;          // 레거시 유지
    private bool hasSpawnedFeverWall = false; // 레거시 유지

    // 일반 장애물 연속 생성 카운트 변수
    private int normalObstacleCount = 0;

    // 터널 위치 체크용 변수
    private float tunnelStartX = 0f;      // 터널이 시작되는 실제 X 좌표
    private float tunnelEndX = 0f;        // 터널이 끝나는 실제 X 좌표
    private bool isWarningActive = false;  // 현재 워닝 문구가 켜져 있는지 여부

    private bool isNextObstacleTunnel = false; // "다음번에 터널을 깔아라"는 미리 결정된 예약 신호
    private float precedingObstacleX = -999f;  // 터널 바로 전 장애물의 X 좌표

    // ⭐ 타이밍 버그 해결을 위한 경고 대기 플래그
    private bool safeToTriggerWarning = false;

    void Start()
    {
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;

        if (warningText != null) warningText.gameObject.SetActive(false);

        DecideNextObstacleType();
    }

    void DecideNextObstacleType()
    {
        if (normalObstacleCount >= 7 || Random.value < tunnelChance)
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
        // ⭐ 1. 워닝 경고문 타이밍 실시간 체크 (구조 개선)
        if (safeToTriggerWarning)
        {
            // [켜기 조건] 아직 경고문이 안 켜졌고, 플레이어가 '전 장애물 X 위치'를 통과하는 바로 그 순간!
            if (!isWarningActive && player.position.x >= precedingObstacleX)
            {
                TurnOnWarning();
            }

            // [끄기 조건] 경고문이 켜져 있고, 플레이어가 '터널 입구 X 위치'에 도달한 순간!
            if (isWarningActive && player.position.x >= tunnelStartX)
            {
                TurnOffWarning();
            }
        }

        // 2. 피버 타임 종료 체크
        if (isFeverTime && player.position.x >= tunnelEndX)
        {
            isFeverTime = false;
        }

        // 3. 장애물 생성 루프
        if (player.position.x > nextSpawnX - 30f)
        {
            if (isNextObstacleTunnel)
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

        normalObstacleCount++;

        // 직전 장애물의 위치를 기억해 둠
        precedingObstacleX = nextSpawnX;

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX += randomDistance;

        DecideNextObstacleType();
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

    void SpawnLongTunnelWall()
    {
        StartFeverTime();

        isNextObstacleTunnel = false;
        normalObstacleCount = 0;

        float spawnXPosition = nextSpawnX;
        tunnelStartX = spawnXPosition;
        tunnelEndX = spawnXPosition + tunnelLength;

        // ⭐ 터널이 미리 스폰되었으니, 이제 플레이어가 전 장애물을 지나갈 때 경고를 띄우라고 신호를 보냄!
        safeToTriggerWarning = true;

        // 1. 아래쪽 연속 장벽
        float bottomWallHeight = Mathf.Abs(tunnelBottomY - groundY);
        float bottomWallCenterY = groundY + (bottomWallHeight / 2f);
        Vector3 bottomPos = new Vector3(spawnXPosition + (tunnelLength / 2f), bottomWallCenterY, 0);

        GameObject bottomWall = Instantiate(obstaclePrefab, bottomPos, Quaternion.identity);
        bottomWall.transform.localScale = new Vector3(tunnelLength, bottomWallHeight, bottomWall.transform.localScale.z);
        Destroy(bottomWall, destroyDelay);

        // 2. 위쪽 연속 장벽
        float topWallHeight = 10f;
        float topWallCenterY = tunnelTopY + (topWallHeight / 2f);
        Vector3 topPos = new Vector3(spawnXPosition + (tunnelLength / 2f), topWallCenterY, 0);

        GameObject topWall = Instantiate(obstaclePrefab, topPos, Quaternion.identity);
        topWall.transform.localScale = new Vector3(tunnelLength, topWallHeight, topWall.transform.localScale.z);
        Destroy(topWall, destroyDelay);

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = tunnelEndX + randomDistance;

        DecideNextObstacleType();
    }

    void StartFeverTime()
    {
        isFeverTime = true;
        feverTimer = feverDuration;
        hasSpawnedFeverWall = false;
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
        safeToTriggerWarning = false; // 이번 터널 경고 임무 끝났으니 비활성화
        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    void EndFeverTime() { }

    public void UpdateScoreFromServer(float score)
    {
        currentScore = score;
    }
}