using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public Transform player;        // 인스펙터에서 Player 연결
    private float backgroundWidth;  // 배경의 실제 가로 길이를 자동 저장할 변수

    void Start()
    {
        // [자동 계산] SpriteRenderer의 크기를 바탕으로 배경의 정확한 가로 길이를 구합니다.
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 실제 크기보다 0.1만큼 작게 인식하도록 강제로 빼줍니다.
            // 이렇게 하면 배경이 서로 0.1만큼 살짝 포개어지면서 빈틈이 원천 차단됩니다!
            backgroundWidth = spriteRenderer.bounds.size.x - 0.1f;
        }
        else
        {
            backgroundWidth = 20f; // 예외 처리용 기본값
        }
    }

    void Update()
    {
        // 플레이어가 배경의 중심점보다 가로 길이만큼 앞으로 전진하면
        if (player.position.x - transform.position.x >= backgroundWidth)
        {
            // ⭐ [수정] 빈틈 없이 정확히 다음 칸(배경 길이의 2배 앞으로)으로 자동 순간이동!
            // 상대적인 거리를 더하는 방식으로 소수점 오차를 최소화합니다.
            Vector3 nextPos = transform.position;
            nextPos.x += backgroundWidth * 2f;
            transform.position = nextPos;
        }
    }
}