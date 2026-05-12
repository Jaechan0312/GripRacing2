using UnityEngine;

public class GripInput : MonoBehaviour
{
    public static GripInput Instance;
    public int CurrentForce { get; private set; }

    [Header("테스트 설정")]
    public bool isTestMode = true;
    [Range(0, 100)] public int dummyForce = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (isTestMode)
        {
            if (Input.GetKey(KeyCode.UpArrow)) dummyForce = Mathf.Clamp(dummyForce + 1, 0, 100);
            else if (Input.GetKey(KeyCode.DownArrow)) dummyForce = Mathf.Clamp(dummyForce - 1, 0, 100);
            else dummyForce = Mathf.Clamp(dummyForce - 2, 0, 100);

            CurrentForce = dummyForce;
        }
    }
}