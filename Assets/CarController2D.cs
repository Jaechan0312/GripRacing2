using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

public class CarController2D : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 9f;
    public float fallMultiplier = 2.5f;

    [Header("활공(글라이딩) 설정")]
    public float glideSpeed = -1.0f;

    [Header("더블 점프 설정")]
    public int maxJumps = 2;             // 최대 점프 가능 횟수 (2단 점프)
    private int jumpCount = 0;           // 현재 점프한 횟수를 세는 변수

    // 💡 [3단계 핵심 변경] 이제 최대 악력 대비 % 비율을 지정합니다. (0.5면 50%, 0.7면 70%)
    [Header("맞춤형 센서 점프 비율 설정")]
    [Range(0.1f, 1.0f)]
    [Tooltip("0.5면 내 최대 악력의 50%, 0.7면 70%를 넘어야 점프가 작동합니다.")]
    public float jumpPercentageThreshold = 0.5f;

    [Tooltip("최대 악력 데이터가 없을 때 작동할 기본 최소 kg 기준치입니다.")]
    public float defaultMinimumJumpKg = 15f;

    private Rigidbody2D rb;

    private bool exactJumpPressed = false;
    private bool isHolding = false;
    private float lastGripForce = 0f;
    private bool isRestarting = false;
    private bool isDead = false;

    [SerializeField] private float currentBonusSpeed = 0f;

    // 💡 계산된 이번 게임의 커스텀 점프 타깃 무게를 유니티 인스펙터창에서 실시간 확인 가능합니다!
    [SerializeField] private float calculatedJumpTargetKg = 0f;

    private FieldInfo scoreFieldInfo;
    private float lastScoredObstacleX = -9999f;
    private ObstacleSpawner spawnerCache;

    // ⭐ 속도 증가를 위한 변수 (중복 정의 제거 완료)
    private float currentScore = 0f;           // 현재 점수를 기억할 변수
    private float currentSpeedMultiplier = 1f; // 현재 속도 배율 (기본 1배)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Time.timeScale = 1f;
        isRestarting = false;
        isDead = false;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (rb.sharedMaterial != null) rb.sharedMaterial.friction = 0f;

        scoreFieldInfo = typeof(ScoreManager).GetField("score", BindingFlags.NonPublic | BindingFlags.Instance);
        spawnerCache = Object.FindAnyObjectByType<ObstacleSpawner>();

        // 💡 [체크포인트] 그래프 씬에서 측정한 최대 기록이 넘어왔다면 지정 백분율을 곱해 커트라인을 발동합니다.
        if (GripReceiver.Instance != null && GripReceiver.Instance.maxGripRecordKg > 0f)
        {
            calculatedJumpTargetKg = GripReceiver.Instance.maxGripRecordKg * jumpPercentageThreshold;
            Debug.Log($"🎯 [사용자 근력 맞춤 완료] 최고 악력: {GripReceiver.Instance.maxGripRecordKg:F1}kg -> 점프 타깃 수치({jumpPercentageThreshold * 100}%): {calculatedJumpTargetKg:F1}kg");
        }
        else
        {
            // 테스트를 위해 바로 레이싱 씬을 틀었다면 에러 방지용 기본 수치 적용
            calculatedJumpTargetKg = defaultMinimumJumpKg;
            Debug.LogWarning($"⚠️ 최대 악력 기록이 감지되지 않아 기본 값 {calculatedJumpTargetKg}kg 기반으로 작동합니다.");
        }
    }

    void Update()
    {
        if (ScoreManager.Instance != null && scoreFieldInfo != null)
        {
            int totalScore = (int)scoreFieldInfo.GetValue(ScoreManager.Instance);
            currentBonusSpeed = (totalScore / 10) * 0.2f;
        }

        // 사망 상태 재시작 처리
        if (Time.timeScale == 0f && !isRestarting)
        {
            if (Input.GetKeyDown(KeyCode.R)) { RestartGame(); return; }
            if (GripReceiver.Instance != null)
            {
                float currentForce = GripReceiver.Instance.ConvertedGripKg;
                if (currentForce >= calculatedJumpTargetKg && lastGripForce < calculatedJumpTargetKg) { RestartGame(); return; }
                lastGripForce = currentForce;
            }
            return;
        }

        if (!isDead && Time.timeScale > 0f && spawnerCache != null)
        {
            float targetObstacleX = spawnerCache.precedingObstacleX;

            if (targetObstacleX > -900f && transform.position.x > targetObstacleX && lastScoredObstacleX != targetObstacleX)
            {
                lastScoredObstacleX = targetObstacleX;

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(1);
                }
            }
        }

        bool isKeyTap = Input.GetKeyDown(KeyCode.UpArrow);
        bool isKeyHold = Input.GetKey(KeyCode.UpArrow);
        bool isGripTap = false;
        bool isGripHold = false;

        // --- 파이썬 중계기 악력값 체크 및 충돌 병합 ---
        if (GripReceiver.Instance != null)
        {
            float currentForce = GripReceiver.Instance.ConvertedGripKg;

            // 1. 실시간 계산된 타깃 무게(calculatedJumpTargetKg) 기반으로 꾹 누르고 있는지(Hold) 판단
            isGripHold = (currentForce >= calculatedJumpTargetKg);

            // 2. 이전 프레임에는 기준치 미만이었다가 이번에 기준치를 넘은 순간을 Tap(점프 입력)으로 트리거
            if (currentForce >= calculatedJumpTargetKg && lastGripForce < calculatedJumpTargetKg)
            {
                isGripTap = true;
                GripReceiver.Instance.isNewDataArrived = false;
            }
            lastGripForce = currentForce;
        }

        // --- 2단 점프 제한 로직 ---
        if (isKeyTap || isGripTap)
        {
            if (jumpCount < maxJumps)
            {
                exactJumpPressed = true;
                jumpCount++;
            }
        }
        isHolding = isKeyHold || isGripHold;
    }

    void FixedUpdate()
    {
        if (Time.timeScale > 0f && !isDead)
        {
            // ⭐ 점수별 속도 배율 계산 (10점마다 10%씩 증가)
            // ex) 0~9점 = 1.0배 / 10~19점 = 1.1배 / 20~29점 = 1.2배 ...
            int scoreInterval = Mathf.FloorToInt(currentScore / 10f);
            currentSpeedMultiplier = 1f + (scoreInterval * 0.1f);

            // 최종 속도 = 기본 속도 * 배율 (상대방 기획 및 수정사항 반영)
            float finalMoveSpeed = moveSpeed * currentSpeedMultiplier;

            // 앞으로 달리기 (최종 속도 적용)
            rb.linearVelocity = new Vector2(finalMoveSpeed, rb.linearVelocity.y);

            if (exactJumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                exactJumpPressed = false;
            }

            if (rb.linearVelocity.y < 0)
            {
                if (isHolding) rb.linearVelocity = new Vector2(rb.linearVelocity.x, glideSpeed);
                else rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
        else if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void RestartGame()
    {
        isRestarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ⭐ 외부(점수 매니저 등)에서 점수가 오를 때마다 이 함수를 찔러주면 실시간으로 속도에 반영돼!
    public void UpdateScore(float score)
    {
        currentScore = score;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        string objName = collision.gameObject.name;

        if (objName.Contains("Ground"))
        {
            jumpCount = 0;
            return;
        }

        if (isDead) return;

        if (objName.Contains("Obstacle"))
        {
            if (collision.transform.localScale.x >= 5f)
            {
                jumpCount = 0;
            }
            else
            {
                isDead = true;
                Debug.Log("순수 장애물 충돌! 게임 오버.");
                Time.timeScale = 0f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        string objName = collision.gameObject.name;

        if (objName.Contains("Tunnel"))
        {
            isDead = true;
            Debug.Log("터널 벽(트리거) 충돌! 게임 오버.");
            Time.timeScale = 0f;
        }
    }
}
