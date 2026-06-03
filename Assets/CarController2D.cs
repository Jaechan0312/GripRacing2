using UnityEngine;
using UnityEngine.SceneManagement;

public class CarController2D : MonoBehaviour
{
    [Header("이동 및 점프(상승) 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 9f;         // 점프했을 때 솟구치는 힘
    public float fallMultiplier = 2.5f;

    [Header("활공(글라이딩) 설정")]
    public float glideSpeed = -1.0f;     // 꾹 누르고 있을 때의 낙하 속도

    [Header("더블 점프 설정")]
    public int maxJumps = 2;             // 최대 점프 가능 횟수 (2단 점프)
    private int jumpCount = 0;           // 현재 점프한 횟수를 세는 변수

    [Header("센서 데이터 설정")]
    public float jumpThreshold = 70f;

    private Rigidbody2D rb;
    private GripInput grip;
    private bool isGrounded = true;

    // 입력 감지용 내부 플래그
    private bool exactJumpPressed = false;
    private bool isHolding = false;
    private float lastGripForce = 0f;

    // ⭐ 속도 증가를 위한 새로운 변수들
    private float currentScore = 0f;     // 현재 점수를 기억할 변수
    private float currentSpeedMultiplier = 1f; // 현재 속도 배율 (기본 1배)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        grip = GripInput.Instance != null ? GripInput.Instance : GetComponent<GripInput>();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // --- 1. 입력 감지 ---
        bool isKeyTap = Input.GetKeyDown(KeyCode.UpArrow);
        bool isKeyHold = Input.GetKey(KeyCode.UpArrow);

        bool isGripTap = false;
        bool isGripHold = false;

        if (grip != null)
        {
            isGripHold = (grip.CurrentForce >= jumpThreshold);

            if (!grip.isTestMode)
            {
                if (grip.CurrentForce >= jumpThreshold && lastGripForce < jumpThreshold)
                {
                    isGripTap = true;
                }
            }
            lastGripForce = grip.CurrentForce;
        }

        // --- 2. 2단 점프 제한 로직 ---
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
        if (Time.timeScale > 0f)
        {
            // ⭐ 1. 점수별 속도 배율 계산 (10점마다 10%씩 증가)
            // ex) 0~9점 = 1.0배 / 10~19점 = 1.1배 / 20~29점 = 1.2배 ...
            int scoreInterval = Mathf.FloorToInt(currentScore / 10f);
            currentSpeedMultiplier = 1f + (scoreInterval * 0.1f);

            // 최종 속도 = 기본 속도 * 배율
            float finalMoveSpeed = moveSpeed * currentSpeedMultiplier;

            // 앞으로 달리기 (최종 속도 적용)
            rb.linearVelocity = new Vector2(finalMoveSpeed, rb.linearVelocity.y);

            // 2. 점프 처리
            if (exactJumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                exactJumpPressed = false;
            }

            // 3. 낙하 및 활공(글라이딩) 제어
            if (rb.linearVelocity.y < 0)
            {
                if (isHolding)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, glideSpeed);
                }
                else
                {
                    rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
                }
            }
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ⭐ 외부(점수 매니저 등)에서 점수가 오를 때마다 이 함수를 찔러주면 실시간으로 속도에 반영돼!
    public void UpdateScore(float score)
    {
        currentScore = score;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Ground"))
        {
            isGrounded = true;
            jumpCount = 0;
        }

        if (collision.gameObject.name.Contains("Obstacle"))
        {
            Debug.Log("사망! R키를 눌러 재시작하세요.");
            Time.timeScale = 0f;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Ground")) isGrounded = false;
    }
}