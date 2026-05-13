using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    [Header("UI & Object References")]
    public TextMeshProUGUI weightText; // "Grip Strength" 텍스트
    public LineRenderer lineRenderer;  // 그래프를 그릴 라인 렌더러

    [Header("Graph Settings")]
    public int maxPoints = 50;         // 화면에 표시될 최대 점 개수
    public float pointSpacing = 10f;   // 점 사이의 가로 간격
    public float heightMultiplier = 5f;// 데이터 값에 따른 세로 높이 배율
    public float updateInterval = 0.2f;// 데이터 갱신 주기 (0.2초)

    private List<float> dataPoints = new List<float>();
    private float timer = 0f;

    void Start()
    {
        // 시작 시 라인 렌더러 초기화
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            // 팁: 여기서 Material이 없다면 코드로 기본 머티리얼을 할당할 수도 있습니다.
        }
    }

    void Update()
    {
        // 델타 타임을 더해 타이머 계산
        timer += Time.deltaTime;

        // 설정한 주기(0.2초)가 지나면 실행
        if (timer >= updateInterval)
        {
            float currentData = GenerateFakeData();

            // 1. 수치 텍스트 업데이트
            if (weightText != null)
            {
                weightText.text = $"Grip Strength : {currentData:F1} kg";
            }

            // 2. 그래프 선 업데이트
            UpdateGraph(currentData);

            // 타이머 초기화
            timer = 0f;
        }
    }

    // 테스트용 랜덤 데이터 생성 (나중에 아두이노 값으로 교체될 부분)
    float GenerateFakeData()
    {
        return Random.Range(5f, 45f);
    }

    void UpdateGraph(float newValue)
    {
        if (lineRenderer == null) return;

        dataPoints.Add(newValue);

        // 데이터가 최대 개수를 넘으면 가장 오래된 것 삭제
        if (dataPoints.Count > maxPoints)
        {
            dataPoints.RemoveAt(0);
        }

        lineRenderer.positionCount = dataPoints.Count;

        for (int i = 0; i < dataPoints.Count; i++)
        {
            // 좌표 계산 (X: 간격에 따른 위치, Y: 데이터 값 * 배율)
            float xPos = i * pointSpacing;
            float yPos = dataPoints[i] * heightMultiplier;

            lineRenderer.SetPosition(i, new Vector3(xPos, yPos, 0));
        }
    }
}