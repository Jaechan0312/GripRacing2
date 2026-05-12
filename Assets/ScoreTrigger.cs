using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    // 태그 검사도 빼고, 일단 뭐라도 닿으면 무조건 찍히게!
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("트리거 작동함! 닿은 물체: " + collision.name);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1);
        }
    }
}