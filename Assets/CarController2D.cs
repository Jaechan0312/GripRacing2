using UnityEngine;
using UnityEngine.SceneManagement;

public class CarController2D : MonoBehaviour
{
    [Header("이동 및 비행(상승) 설정")]
    public float moveSpeed = 5f;
    public float flyForce = 15f;        // 꾹 누르고 있을 때 위로 밀어 올리는 힘 (기존 jumpForce 대용)
    public float maxUpwardVelocity = 8f; // 위로 너무 빠르게 치솟는 걸 방지하는 최대 속도
    public float fallMultiplier = 2.5f;

    [Header("센서 데이터 설정")]
    public float jumpThreshold = 70f;

    private Rigidbody2D rb;
    private GripInput grip;
    // 이제 바닥에 닿아있지 않아도 힘을 주면 올라가야 하므로이동 제어용으로만 씁니다.
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // [중요 수정] GripInput이 이 오브젝트가 아니라 다른 곳(싱글톤 Instance)에 있을 수 있으므로 안전하게 가져옴
        grip = GripInput.Instance != null ? GripInput.Instance : GetComponent<GripInput>();

        Time.timeScale = 1f;
    }

    void Update()
    {
        // 사망해서 게임이 멈췄을 때 R키를 누르면 재시작
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    // 물리 연산(AddForce, 속도 제어)은 FixedUpdate에서 처리하는 게 부드럽고 정확해!
    void FixedUpdate()
    {
        if (Time.timeScale > 0f)
        {
            // 1. 앞으로 달리기 (X축 속도 고정)
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);

            // 2. 상승 조건 체크 (악력이 기준치 이상이거나, 위쪽 방향키를 '꾹 누르고 있는 동안')
            bool isGripPushing = (grip != null && grip.dummyForce >= jumpThreshold);
            bool isKeyPushing = Input.GetKey(KeyCode.UpArrow); // GetKeyDown에서 GetKey로 변경!

            if (isGripPushing || isKeyPushing)
            {
                // 위로 밀어 올리는 힘을 지속적으로 가함
                rb.AddForce(Vector2.up * flyForce, ForceMode2D.Force);

                // 너무 무한정 빠르게 치솟지 않도록 위쪽 최고 속도를 제한해 줘
                if (rb.linearVelocity.y > maxUpwardVelocity)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxUpwardVelocity);
                }
            }

            // 3. 낙하 가속 (힘을 빼서 아래로 떨어질 때 묵직하게 가속)
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Ground")) isGrounded = true;

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