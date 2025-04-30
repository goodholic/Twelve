// Assets\Scripts\HeroAutoMover.cs

using UnityEngine;

/// <summary>
/// "10번째 덱"으로 등록된 주인공 캐릭터가
///   1) 다른 캐릭터와 동일하게 공격 가능
///   2) 가까운 '적' 몬스터를 자동으로 추적(이동)하도록 만들어주는 예시 스크립트.
///
/// [수정 사항]
///   - 아군 몬스터는 추적하지 않도록 로직 추가( isAlly == true 무시 ).
/// 
/// [주의]
///  - 이 HeroAutoMover가 활성화되어 있으면, 매 프레임 "가장 가까운 적 몬스터"를 찾아 이동합니다.
///  - 만약 유저가 드래그로 Hero를 옮기려 할 때, 이 스크립트의 이동과 충돌할 수 있습니다.
///    필요하면 드래그 중엔 이 스크립트를 임시로 끄거나, 드래그가 끝난 후 다시 켜주는 식으로 관리하세요.
/// </summary>
public class HeroAutoMover : MonoBehaviour
{
    [Header("주인공 이동 속도(초당)")]
    public float moveSpeed = 2f;

    [Header("몬스터와 얼마나 가까워져야 멈출지 (가까이 다가가기 범위)")]
    public float stopDistance = 1.0f;

    // Hero 자신의 Character 스크립트(공격/스탯 관리)를 가져올 수 있음(선택 사항)
    private Character heroCharacter;

    private void Start()
    {
        // Hero 본인 Character 컴포넌트 참조(선택 사항)
        heroCharacter = GetComponent<Character>();
    }

    private void Update()
    {
        // 1) 가장 가까운 '적' 몬스터 찾기
        Monster nearest = FindNearestMonster();
        if (nearest == null)
        {
            // 주변에 적 몬스터가 없으면 이동 중단(그냥 제자리에 있음)
            return;
        }

        // 2) 몬스터와의 거리 측정
        float dist = Vector2.Distance(transform.position, nearest.transform.position);

        // 3) 너무 가까우면(예: stopDistance 이하) 더 안 움직임
        if (dist <= stopDistance)
        {
            return;
        }

        // 4) 그렇지 않다면, 몬스터 방향으로 이동
        Vector2 dir = (nearest.transform.position - transform.position).normalized;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 씬 내 모든 Monster를 찾아, '아군이 아닌( isAlly==false )' 것들 중 가장 가까운 한 마리를 반환.
    /// 없으면 null.
    /// </summary>
    private Monster FindNearestMonster()
    {
        Monster[] allMonsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            // ---------------------- 추가된 부분 ----------------------
            // 아군(isAlly == true)은 무시하여, 적 몬스터만 추적
            if (m.isAlly) continue;
            // -------------------------------------------------------

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        return nearest;
    }
}
