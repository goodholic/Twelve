using UnityEngine;

/// <summary>
/// "10번째 덱"으로 등록된 주인공 캐릭터가 
/// Hero Panel(혹은 해당 부모)의 RectTransform 영역 안에서
/// 자유롭게 이동하도록 만들어주는 예시 스크립트.
///
/// [사용 방식]
/// - 이 스크립트가 붙은 오브젝트(주인공)는 RectTransform이 있다면
///   해당 위치에서 랜덤하게 돌아다님(UI 상에서 방치형 이동).
/// - RectTransform이 없다면(3D Transform) 월드 좌표에서 움직이는 식.
/// - moveSpeed, roamRangeX/Y 등은 상황에 맞게 조절.
/// </summary>
public class HeroAutoMover : MonoBehaviour
{
    [Header("주인공 이동 속도")]
    public float moveSpeed = 100f;

    [Header("UI 모드에서 돌아다닐 범위 (사각형)")]
    public float roamRangeX = 400f;
    public float roamRangeY = 200f;

    // (월드 좌표 전용) 범위
    private float worldRangeX = 5f;
    private float worldRangeY = 3f;

    private RectTransform rect;
    private Vector2 targetPosUI;
    private Vector3 targetPosWorld;

    private void Start()
    {
        // 만약 RectTransform 컴포넌트를 찾으면(=UI 공간)
        rect = GetComponent<RectTransform>();
        PickNewDestination();
    }

    private void Update()
    {
        if (rect != null) // UI 모드
        {
            Vector2 current = rect.anchoredPosition;
            rect.anchoredPosition = Vector2.MoveTowards(
                current, 
                targetPosUI, 
                moveSpeed * Time.deltaTime
            );

            // 목적지 근처 도달 시 새로운 목적지
            if (Vector2.Distance(current, targetPosUI) < 1f)
            {
                PickNewDestination();
            }
        }
        else // 3D(또는 월드) 모드
        {
            Vector3 current = transform.position;
            float moveStep = (moveSpeed * 0.01f) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, targetPosWorld, moveStep);

            // 목적지 근처 도달 시 새로운 목적지
            if (Vector3.Distance(current, targetPosWorld) < 0.1f)
            {
                PickNewDestination();
            }
        }
    }

    private void PickNewDestination()
    {
        if (rect != null)
        {
            // UI 공간에서 랜덤 위치
            float x = Random.Range(-roamRangeX, roamRangeX);
            float y = Random.Range(-roamRangeY, roamRangeY);
            targetPosUI = new Vector2(x, y);
        }
        else
        {
            // 월드 좌표상 랜덤 위치
            float x = Random.Range(-worldRangeX, worldRangeX);
            float y = Random.Range(-worldRangeY, worldRangeY);
            targetPosWorld = new Vector3(x, y, 0f);
        }
    }
}
