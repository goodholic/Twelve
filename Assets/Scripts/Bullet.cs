using UnityEngine;

/// <summary>
/// 캐릭터가 발사하는 총알(Projectile) 예시.
/// 몬스터에게 날아가 닿으면 데미지를 주고 사라진다.
/// 만약 광역 공격(isAreaAttack=true)이라면, 해당 지점에서 주변 몬스터에게도 동시에 데미지를 준다.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 5f;         // 총알 이동 속도
    public float maxLifeTime = 3f;   // 총알이 사라지기까지의 최대 시간(초)

    // =============================
    // (추가) 광역 공격을 위한 필드
    // =============================
    private bool isAreaAttack = false;
    private float areaRadius = 0f;

    private Monster target;          // 총알이 추적할 몬스터
    private float damage;            // 총알이 줄 데미지
    private float aliveTime = 0f;    // 총알이 살아있는 시간 누적

    /// <summary>
    /// 캐릭터가 발사할 때 초기값을 세팅하는 메서드
    /// </summary>
    public void Init(Monster targetMonster, float damageAmount, float bulletSpeed,
                     bool isAreaAtk, float areaAtkRadius)
    {
        target = targetMonster;
        damage = damageAmount;
        speed = bulletSpeed;

        isAreaAttack = isAreaAtk;
        areaRadius = areaAtkRadius;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;
        if (aliveTime >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            // 타겟 몬스터가 죽었거나 사라졌다면, 총알도 제거
            Destroy(gameObject);
            return;
        }

        // 타겟을 향해 이동
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 만약 충분히 가까워지면 타격
        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist < 0.2f)
        {
            // ============================
            // (1) 광역 공격 여부 체크
            // ============================
            if (isAreaAttack)
            {
                // target 주위 areaRadius 안의 모든 몬스터에게 데미지
                ApplyAreaDamage(target.transform.position);
            }
            else
            {
                // 단일 대상 데미지
                target.TakeDamage(damage);
            }

            // 총알 제거
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 광역 데미지 처리
    /// </summary>
    private void ApplyAreaDamage(Vector3 centerPos)
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (var mo in monsters)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            float dist = Vector2.Distance(centerPos, m.transform.position);
            if (dist <= areaRadius)
            {
                m.TakeDamage(damage);
            }
        }

        Debug.Log($"[Bullet] 광역 공격 발동! 중심={centerPos}, 범위={areaRadius}, damage={damage}");
    }
}
