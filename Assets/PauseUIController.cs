using UnityEngine;

public class PauseUIController : MonoBehaviour
{
    // 화면에 띄울 '일시정지 텍스트/패널' 오브젝트
    public GameObject pauseVisualObject;

    void Start()
    {
        // 게임 시작 시 일시정지 UI는 꺼두고, 시간은 정상(1)으로 설정
        if (pauseVisualObject != null) pauseVisualObject.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (GripReceiver.Instance == null || pauseVisualObject == null) return;

        // GripReceiver의 일시정지 상태(true/false)에 맞춰 연동
        if (GripReceiver.Instance.isPaused)
        {
            // 💡 일시정지 상태일 때
            if (!pauseVisualObject.activeSelf)
            {
                pauseVisualObject.SetActive(true); // 글자 켜기
                Time.timeScale = 0f;               // 게임 안의 물체 모두 멈추기!
                Debug.Log("[일시정지] 게임 월드의 시간이 멈췄습니다.");
            }
        }
        else
        {
            // 💡 일시정지가 해제되었을 때
            if (pauseVisualObject.activeSelf)
            {
                pauseVisualObject.SetActive(false); // 글자 끄기
                Time.timeScale = 1f;                // 게임 시간 다시 재생!
                Debug.Log("[다시시작] 게임 월드의 시간이 다시 흐릅니다.");
            }
        }
    }

    // ⚠️ 중요: 유니티에서 다른 씬으로 넘어갈 때 시간이 멈춰있으면 
    // 다음 화면도 멈추므로, 이 오브젝트가 파괴되거나 꺼질 때 시간을 원래대로 돌려놓습니다.
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
