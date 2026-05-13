using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위한 네임스페이스

public class SceneChanger : MonoBehaviour
{
    // 버튼 1: 게임 씬으로 이동
    public void GoToGame()
    {
        SceneManager.LoadScene("SampleScene"); // 실제 만든 씬 이름과 똑같아야 함!
    }

    // 버튼 2: 그래프 씬으로 이동
    public void GoToGraph()
    {
        SceneManager.LoadScene("GraphScene");
    }

    // 버튼 3: 홈(메인 메뉴) 화면으로 이동
    public void GoToHome()
    {
        // 실제 메인 화면 씬 이름이 "MenuScene"이 맞는지 확인하세요!
        SceneManager.LoadScene("MenuScene");
    }
}