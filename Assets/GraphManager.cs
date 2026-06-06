using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GraphManager : MonoBehaviour
{
    [Header("UI & Object References")]
    public TextMeshProUGUI weightText;

    // 💡 [수정] 단순 글씨 표시용 Text 대신, 사용자가 직접 타이핑할 수 있는 인풋 필드로 변경합니다.
    public TMP_InputField maxWeightInputField;

    public LineRenderer lineRenderer;

    [Header("Graph Settings")]
    [Tooltip("화면에 채울 점의 최대 개수입니다.")]
    public int maxPoints = 100;

    [Tooltip("그래프 점이 추가되는 주기(초)입니다. 이 값을 늘리면 그래프가 느려집니다.")]
    public float graphUpdateInterval = 0.1f;

    [Header("3D World Boundary Settings")]
    public float leftEdgeX = -7.5f;
    public float rightEdgeX = 7.5f;
    public float bottomEdgeY = -4.2f;
    public float topEdgeY = 4.5f;

    private List<float> dataPoints = new List<float>();
    private float timeSinceLastUpdate = 0f;

    private float peakGripKg = 0f;

    void OnEnable()
    {
        dataPoints.Clear();
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        if (weightText != null) weightText.text = "Grip Strength : 0.0 kg";

        timeSinceLastUpdate = 0f;

        // 다른 씬에 갔다 왔을 때 전역 안전 금고에서 최고 기록을 복구합니다.
        if (GripReceiver.Instance != null && GripReceiver.Instance.maxGripRecordKg > 0f)
        {
            peakGripKg = GripReceiver.Instance.maxGripRecordKg;
        }
        else
        {
            peakGripKg = 0f;
        }

        UpdateMaxWeightText();
    }

    void Start()
    {
        if (lineRenderer != null) lineRenderer.positionCount = 0;

        // 💡 [새로 추가] 인스펙터에 연결된 인풋 필드가 있다면, 사용자가 키보드로 타이핑을 칠 때마다 실시간 감시하는 이벤트 연결
        if (maxWeightInputField != null)
        {
            maxWeightInputField.onValueChanged.AddListener(OnMaxWeightInputChanged);
        }
    }

    void Update()
    {
        if (GripReceiver.Instance == null) return;
        if (!GripReceiver.Instance.isMeasuring) return;
        if (GripReceiver.Instance.isPaused) return;

        // 텍스트 UI 업데이트 (실시간 반영)
        float currentKg = GripReceiver.Instance.ConvertedGripKg;
        if (weightText != null)
        {
            weightText.text = $"Grip Strength : {currentKg:F1} kg";
        }

        // [실시간 최고치 검사] 현재 악력이 기존 최고치보다 높으면 실시간 갱신
        if (currentKg > peakGripKg)
        {
            peakGripKg = currentKg;
            UpdateMaxWeightText();

            // 측정한 최고 악력 데이터를 전역 수신기에 실시간으로 백업 보관합니다.
            GripReceiver.Instance.maxGripRecordKg = peakGripKg;
        }

        bool hasNewData = false;
        if (GripReceiver.Instance.isNewDataArrived)
        {
            GripReceiver.Instance.isNewDataArrived = false;
            hasNewData = true;
        }

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= graphUpdateInterval)
        {
            timeSinceLastUpdate = 0f;

            if (hasNewData || currentKg > 0f)
            {
                UpdateGraph(currentKg);
            }
        }
    }

    // 💡 [새로 추가된 핵심 60 제한 수동 입력 공식] 사용자가 키보드로 텍스트를 입력할 때 실행됩니다.
    private void OnMaxWeightInputChanged(string text)
    {
        if (float.TryParse(text, out float inputVal))
        {
            // 🚨 60 제한 예외 처리 공식 적용
            if (inputVal > 60f)
            {
                inputVal = 60f; // 60을 초과하면 60으로 가둡니다.

                // 인풋 필드 글씨 창도 강제로 "60" 혹은 "60.0"으로 정정해 줍니다.
                maxWeightInputField.text = "60.0";
            }

            peakGripKg = inputVal;

            // 수동 입력한 값을 자동차 씬에서도 적용할 수 있게 전역 저장소에 실시간 주입합니다.
            if (GripReceiver.Instance != null)
            {
                GripReceiver.Instance.maxGripRecordKg = peakGripKg;
            }

            Debug.Log($"⌨️ [수동 입력 변경] 최대 악력이 사용자에 의해 {peakGripKg:F1}kg으로 세팅되었습니다.");
        }
    }

    // 리셋 버튼을 마우스로 직접 누를 때 모든 기록이 0으로 밀립니다.
    public void ResetPeakGrip()
    {
        peakGripKg = 0f;
        UpdateMaxWeightText();

        if (GripReceiver.Instance != null)
        {
            GripReceiver.Instance.maxGripRecordKg = 0f;
        }

        Debug.Log("🧹 [GraphManager] 사용자의 리셋 요청으로 최고 악력 기록을 초기화 완료했습니다!");
    }

    // 텍스트 가독성을 위한 인풋 필드 내용 갱신 함수
    private void UpdateMaxWeightText()
    {
        if (maxWeightInputField != null)
        {
            // 인풋 필드 글자 칸에 최고 악력을 소수점 첫째 자리까지 표기합니다.
            maxWeightInputField.text = peakGripKg.ToString("F1");
        }
    }

    void UpdateGraph(float newValue)
    {
        if (lineRenderer == null) return;

        dataPoints.Insert(0, newValue);

        if (dataPoints.Count > maxPoints)
        {
            dataPoints.RemoveAt(dataPoints.Count - 1);
        }

        lineRenderer.positionCount = dataPoints.Count;

        float totalWidth = rightEdgeX - leftEdgeX;
        float dynamicSpacing = totalWidth / (maxPoints - 1);
        float startX = leftEdgeX;
        float totalHeight = topEdgeY - bottomEdgeY;
        float maxGripKg = 60f;

        for (int i = 0; i < dataPoints.Count; i++)
        {
            float rawXPos = startX + (i * dynamicSpacing);
            float clampedXPos = Mathf.Clamp(rawXPos, leftEdgeX, rightEdgeX);

            float kgRatio = Mathf.Clamp01(dataPoints[i] / maxGripKg);
            float rawYPos = bottomEdgeY + (kgRatio * totalHeight);
            float clampedYPos = Mathf.Clamp(rawYPos, bottomEdgeY, topEdgeY);

            lineRenderer.SetPosition(i, new Vector3(clampedXPos, clampedYPos, 0));
        }
    }
}
