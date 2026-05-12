using UnityEngine;
using UnityEngine.SceneManagement; // ★ 재시작 기능을 위해 꼭 필요해!

public class CarController2D : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float fallMultiplier = 2.5f;

    [Header("센서 데이터 설정")]
    public float jumpThreshold = 70f;

    private Rigidbody2D rb;
    private GripInput grip;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        grip = GetComponent<GripInput>();

        // 시작할 때 시간 흐름을 정상(1)으로 초기화
        Time.timeScale = 1f;
    }

    void Update()
    {
        // [추가] 사망해서 게임이 멈췄을 때(Time.timeScale == 0) R키를 누르면 재시작!
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // 게임이 실행 중일 때만 이동/점프 로직 작동
        if (Time.timeScale > 0f)
        {
            // 1. 앞으로 달리기
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);

            // 2. 점프 조건 (악력 70 이상 또는 위쪽 방향키)
            bool isGripJumping = (grip != null && grip.dummyForce >= jumpThreshold);
            bool isKeyJumping = Input.GetKeyDown(KeyCode.UpArrow);

            if ((isGripJumping || isKeyJumping) && isGrounded)
            {
                Jump();
            }

            // 3. 낙하 가속 (묵직한 낙하)
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            }
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    // [추가] 재시작 함수
    void RestartGame()
    {
        // 현재 씬을 다시 불러오기
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Ground")) isGrounded = true;

        if (collision.gameObject.name.Contains("Obstacle"))
        {
            Debug.Log("사망! R키를 눌러 재시작하세요.");
            Time.timeScale = 0f; // 게임 멈춤
        }
    }
}