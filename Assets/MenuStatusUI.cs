using UnityEngine;
using TMPro;

public class MenuStatusUI : MonoBehaviour
{
    [Header("UI 오브젝트 연결")]
    [SerializeField] private TextMeshProUGUI statusText;     // 우측 하단 "bluetooth connected" 텍스트
    [SerializeField] private TextMeshProUGUI disconnectText; // 화면 중앙 "⚠️ 블루투스 연결 대기 중..." 텍스트

    void Start()
    {
        // 💡 [안전장치 1] 시작 시 무조건 텍스트들을 초기화합니다.
        if (statusText != null) statusText.text = "";
        if (disconnectText != null) disconnectText.text = "";

        // 💡 [안전장치 2] 이미 마스터 수신기가 연결된 상태라면 즉시 중앙 글씨를 지우고 시작합니다.
        if (GripReceiver.Instance != null && GripReceiver.Instance.isBleConnected)
        {
            ShowConnectedText(true);
        }
        else
        {
            ShowConnectedText(false);
        }
    }

    // 💡 GripReceiver에서 블루투스 신호가 바뀔 때 매 프레임/이벤트마다 강제로 실행되는 무적의 동기화 함수
    public void ShowConnectedText(bool isConnected)
    {
        if (isConnected)
        {
            // 1. 블루투스가 연결되었을 때의 화면 구성
            if (statusText != null) statusText.text = "bluetooth connected";

            // 🚨 [여기가 핵심!] 오브젝트를 끄는 대신, 글자 내용을 완전히 빈칸("")으로 만들어 강제로 화면에서 지워버립니다.
            if (disconnectText != null) disconnectText.text = "";

            Debug.Log("🎯 [UI 완벽 성공] 중앙 대기 문구를 강제로 지우고, 우측 하단에 연결 문구를 띄웠습니다!");
        }
        else
        {
            // 2. 블루투스가 연결되지 않았거나 끊겼을 때 화면에 띄울 문구
            if (statusText != null) statusText.text = "";
            if (disconnectText != null) disconnectText.text = "disconnected and retry..."; // 💡 영어로 매칭 완료!

            Debug.Log("⚠️ [UI 알림] 블루투스가 끊어져 재연결 안내 문구를 표시합니다.");
        }
    }
}
