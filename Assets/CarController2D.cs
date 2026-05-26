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
    public int maxJumps = 2;             // ⭐ 최대 점프 가능 횟수 (2단 점프)
    private int jumpCount = 0;           // ⭐ 현재 점프한 횟수를 세는 변수

    [Header("센서 데이터 설정")]
    public float jumpThreshold = 70f;

    private Rigidbody2D rb;
    private GripInput grip;
    private bool isGrounded = true;

    // 입력 감지용 내부 플래그
    private bool exactJumpPressed = false;
    private bool isHolding = false;
    private float lastGripForce = 0f;

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

            // ⭐ [버그 해결 핵심] 테스트 모드일 때는 키보드 꾹 누를 때 발생하는 자동 점프를 막기 위해 악력기 탭을 무시해!
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
        // 키보드를 새로 눌렀거나, 진짜 악력기를 콱 쥐었을 때
        if (isKeyTap || isGripTap)
        {
            // 아직 최대 점프 횟수(2번)에 도달하지 않았다면 점프 허용!
            if (jumpCount < maxJumps)
            {
                exactJumpPressed = true;
                jumpCount++; // 점프 횟수 증가
            }
        }

        isHolding = isKeyHold || isGripHold;
    }

    void FixedUpdate()
    {
        if (Time.timeScale > 0f)
        {
            // 1. 앞으로 달리기 (X축 속도 고정)
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);

            // 2. 점프 처리
            if (exactJumpPressed)
            {
                // ⭐ 2단 점프할 때 기존에 떨어지던 속도가 있으면 점프가 씹히는 느낌이 나므로, 
                // Y축 속도를 순간적으로 0으로 초기화한 뒤 점프력을 줘야 2단 점프가 아주 청량하게 잘 뛰어져!
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ⭐ 바닥(Ground)에 닿으면 점프 횟수를 다시 0으로 초기화!
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