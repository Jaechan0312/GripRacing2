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

    [Header("⭐ 장애물 이미지(스프라이트) 설정")]
    public Sprite normalObstacleSprite; // 일반 장애물에 들어갈 이미지
    public Sprite tunnelWallSprite;     // 터널 장벽(위/아래)에 들어갈 이미지

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

    [Tooltip("기존 15초에서 인스펙터 기본값을 4초로 하향 조정 (렉 방지)")]
    public float destroyDelay = 4f;

    [Header("⭐ 터널 설정 (가운데 통로 공간)")]
    public float tunnelBottomY = 1.0f;  // 통로 바닥
    public float tunnelTopY = 4.5f;     // 통로 천장
    public float tunnelLength = 40f;    // 터널 가로 길이

    [Range(0f, 1f)]
    public float tunnelChance = 0.2f;   // 터널이 등장할 기본 확률

    private float nextSpawnX;
    private bool isFeverTime = false;
    private float currentScore = 0f;

    private int obstaclesSpawnedSinceLastTunnel = 0;

    private float tunnelStartX = 0f;
    private float tunnelEndX = 0f;
    private bool isWarningActive = false;

    private bool isNextObstacleTunnel = false;

    // 💡 [중요] CarController2D가 실시간 점수 체크를 위해 접근할 수 있도록 public으로 복구 및 유지합니다.
    [HideInInspector] public float precedingObstacleX = -999f;

    private bool safeToTriggerWarning = false;
    private float warningStartX = -999f; // 💡 경고 문구를 켜기 시작할 정확한 플레이어 위치 기준점

    void Start()
    {
        float firstDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = player.position.x + firstDistance;

        if (warningText != null) warningText.gameObject.SetActive(false);

        obstaclesSpawnedSinceLastTunnel = 0;
        DecideNextObstacleType();
    }

    void DecideNextObstacleType()
    {
        if (obstaclesSpawnedSinceLastTunnel >= 7)
        {
            isNextObstacleTunnel = true;
            return;
        }

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
        // ⭐ [경고창 켜고 끄기 로직 완벽 보완]
        if (safeToTriggerWarning)
        {
            // 1. 플레이어가 경고 시작 지점(이전 장애물을 지난 시점)을 넘으면 WARNING 켜기
            if (!isWarningActive && player.position.x >= warningStartX)
            {
                TurnOnWarning();
            }

            // 2. 플레이어가 드디어 터널 입구(tunnelStartX)에 완전히 진입하면 WARNING 끄기
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

        obstaclesSpawnedSinceLastTunnel++;
        precedingObstacleX = nextSpawnX; // 일반 장애물의 통과 기준 x좌표 저장

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

        // 이미지(스프라이트) 적용 기능 유지
        if (normalObstacleSprite != null)
        {
            SpriteRenderer[] srs = tempObstacle.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in srs)
            {
                sr.sprite = normalObstacleSprite;
            }
        }

        // 정해진 딜레이 뒤에 파괴 (렉 방지)
        Destroy(tempObstacle, destroyDelay);
    }

    void SpawnLongTunnelWall()
    {
        isFeverTime = true;
        isNextObstacleTunnel = false;
        obstaclesSpawnedSinceLastTunnel = 0;

        // 💡 경고 문구를 켜기 시작할 정확한 위치를 "이전 장애물 통과 기준점"으로 강제 지정합니다.
        warningStartX = precedingObstacleX;

        float spawnXPosition = nextSpawnX;
        tunnelStartX = spawnXPosition;
        tunnelEndX = spawnXPosition + tunnelLength;

        // 터널이 시작되는 '입구 지점'을 플레이어 통과 및 점수 획득 기준으로 설정
        precedingObstacleX = tunnelStartX;

        // ⭐ 장벽 생성 처리가 완전히 끝났으므로 경고 추적 플래그 가동
        safeToTriggerWarning = true;

        // 사용할 프리팹 결정 (분리된 전용 프리팹이 없다면 일반용 백업 사용)
        GameObject wallPrefabToUse = (tunnelObstaclePrefab != null) ? tunnelObstaclePrefab : normalObstaclePrefab;

        // 1. 아래쪽 연속 터널 장벽 생성
        float bottomWallHeight = Mathf.Abs(tunnelBottomY - groundY);
        float bottomWallCenterY = groundY + (bottomWallHeight / 2f);
        Vector3 bottomPos = new Vector3(spawnXPosition + (tunnelLength / 2f), bottomWallCenterY, 0);

        GameObject bottomWall = Instantiate(wallPrefabToUse, bottomPos, Quaternion.identity);
        bottomWall.transform.localScale = new Vector3(tunnelLength, bottomWallHeight, bottomWall.transform.localScale.z);
        bottomWall.name = "TunnelWall_Bottom";

        // 스프라이트 적용 및 검은색 색상 처리
        SpriteRenderer[] bottomSRs = bottomWall.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in bottomSRs)
        {
            if (tunnelWallSprite != null) sr.sprite = tunnelWallSprite;
            sr.color = Color.black;
            sr.drawMode = SpriteDrawMode.Simple;
        }

        UpdateColliderSize(bottomWall);
        // ⭐ 터널 장벽은 길이가 길기 때문에 완전히 통과한 직후(렉 방지 하향 수치 + 여유분) 지워지도록 개별 세팅 보완
        Destroy(bottomWall, destroyDelay + 3f);

        // 2. 위쪽 연속 터널 장벽 생성
        float topWallHeight = 10f;
        float topWallCenterY = tunnelTopY + (topWallHeight / 2f);
        Vector3 topPos = new Vector3(spawnXPosition + (tunnelLength / 2f), topWallCenterY, 0);

        GameObject topWall = Instantiate(wallPrefabToUse, topPos, Quaternion.identity);
        topWall.transform.localScale = new Vector3(tunnelLength, topWallHeight, topWall.transform.localScale.z);
        topWall.name = "TunnelWall_Top";

        SpriteRenderer[] topSRs = topWall.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in topSRs)
        {
            if (tunnelWallSprite != null) sr.sprite = tunnelWallSprite;
            sr.color = Color.black;
            sr.drawMode = SpriteDrawMode.Simple;
        }

        UpdateColliderSize(topWall);
        // ⭐ 위쪽 벽도 동일하게 동시 파괴 처리
        Destroy(topWall, destroyDelay + 3f);

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        nextSpawnX = tunnelEndX + randomDistance;

        DecideNextObstacleType();
    }

    // 자식 컴포넌트의 BoxCollider2D 크기를 스케일에 맞춰 1로 초기화해주는 유틸 기능 복구
    void UpdateColliderSize(GameObject target)
    {
        BoxCollider2D boxCol = target.GetComponentInChildren<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.size = Vector2.one;
            boxCol.offset = Vector2.zero;
        }
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

    // 외부 연동용 점수 업데이트 메서드 명칭 통일
    public void UpdateScore(float score)
    {
        currentScore = score;
    }
}
