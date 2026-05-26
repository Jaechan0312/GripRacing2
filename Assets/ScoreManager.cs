using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 꼭 필요!

public class ScoreManager : MonoBehaviour
{
    // 어디서든 "ScoreManager.Instance"로 접근할 수 있게 해주는 마법의 코드
    public static ScoreManager Instance;

    public TextMeshProUGUI scoreText; // 연결할 UI 텍스트
    private int score = 0;           // 실제 점수 저장

    void Awake()
    {
        // 게임 시작하자마자 나 자신을 Instance에 등록 (강제 활성화)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 혹시라도 두 개 있으면 하나 삭제
        }
    }

    void Start()
    {
        UpdateScoreUI();
    }

    // 점수를 올리는 기능
    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
        Debug.Log("점수 획득! 현재 점수: " + score);

        // ⭐ [추가] 점수가 올라갈 때마다 맵에 있는 ObstacleSpawner를 찾아서 점수를 배달합니다.
        ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.UpdateScoreFromServer(score);
        }
    }

    // UI 글자를 업데이트하는 기능
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}