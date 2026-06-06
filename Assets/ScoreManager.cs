using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TextMeshProUGUI scoreText; // UI 텍스트 연결
    private int score = 0;           // 현재 점수

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
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

        // 1. 장애물 생성기(ObstacleSpawner)를 찾아서 실시간 점수를 전달합니다.
        ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
        if (spawner != null)
        {
            // 주의: ObstacleSpawner 클래스 내부에 UpdateScore 메서드가 구현되어 있어야 합니다.
            spawner.UpdateScore(score);
        }

        // 2. 자동차(CarController2D)를 찾아서 실시간 점수를 전달합니다.
        CarController2D car = FindFirstObjectByType<CarController2D>();
        if (car != null)
        {
            car.UpdateScore(score);
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
