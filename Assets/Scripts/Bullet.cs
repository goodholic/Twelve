// Assets\Scripts\Bullet.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// Bullet Pattern Enum (Inspector에서 10가지 선택 가능)
/// </summary>
public enum BulletPattern
{
    StraightShot = 1,   // (1) 직선
    CurvedShot,         // (2) 곡선
    SpreadShot,         // (3) 확산(샷건 느낌)
    HomingShot,         // (4) 추적(유도)
    SpiralShot,         // (5) 회전(나선)
    ZigzagShot,         // (6) 지그재그
    SplitShot,          // (7) 충돌 후 분열
    AccelerationShot,   // (8) 처음 느리다가 가속
    RadialBurst,        // (9) 원형 방사
    LaserBeam           // (10) 지속형 레이저
}

/// <summary>
/// 캐릭터가 발사하는 총알(Projectile) 예시.
/// Inspector창에서 bulletPattern(10가지) 선택.
/// 각 패턴마다 이동/동작 로직이 조금씩 다름.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Pattern Type")]
    public BulletPattern bulletPattern = BulletPattern.StraightShot;

    // 전역적으로 사용할 VFX 패널 참조
    private static RectTransform _vfxPanel;

    /// <summary>
    /// 모든 Bullet이 사용할 VFX Panel 설정 (정적 메서드)
    /// </summary>
    public static void SetVfxPanel(RectTransform vfxPanel)
    {
        _vfxPanel = vfxPanel;
    }

    [Header("Common Bullet Settings")]
    public float speed = 5f;         // 총알 이동 속도(기본)
    public float maxLifeTime = 3f;   // 총알이 사라지기까지의 최대 시간(초)
    public bool isAreaAttack = false; // 광역 공격 여부
    public float areaRadius = 0f;     // 광역 공격 범위

    // ============== 추가 ==============
    [Header("Impact VFX (optional)")]
    [Tooltip("몬스터에 닿거나 광역 공격 시 생성될 VFX 프리팹 (선택)")]
    public GameObject impactEffectPrefab;
    // =================================

    // (공격 대상) - HomingShot 등 추적형에서 사용
    private Monster target;
    // 데미지
    private float damage;
    // 살아있는 시간 누적
    private float aliveTime = 0f;

    // (SplitShot에서 분열할 작은 탄환 프리팹(옵션))
    [Header("Split Shot SubBullet (옵션)")]
    public GameObject subBulletPrefab;
    public int subBulletCount = 3;  // 분열 시 몇 개 생성?

    // 내부적으로 사용할 추가 필드들
    private Vector3 startPosition;    // 발사 시점 위치(커브/지그재그/스파이럴 등에 사용)
    private Vector3 initialDirection; // 스프레드샷 등에서 초기에 정해진 방향
    private float spiralAngle = 0f;   // 스파이럴 각도
    private float accelRate = 2f;     // 가속 탄에서의 가속도(임의 예시)

    /// <summary>
    /// 캐릭터가 발사할 때 초기값을 세팅
    /// (bulletPattern은 Inspector에서 설정)
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

    private void Start()
    {
        // 발사 시점 위치, 초기 방향 등 기록
        startPosition = transform.position;

        // SpreadShot(확산)인 경우, "초기 방향"을 조금 랜덤 각도로 부채꼴 생성
        if (bulletPattern == BulletPattern.SpreadShot)
        {
            float angle = Random.Range(-15f, 15f);
            initialDirection = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
        }
        else
        {
            // StraightShot 등 기본은 target이 있으면 그쪽, 없으면 우측
            if (target != null)
            {
                initialDirection = (target.transform.position - transform.position).normalized;
            }
            else
            {
                initialDirection = Vector3.right;
            }
        }

        // RadialBurst(원형 방사)라면, 생성되자마자 여러 발을 뿌리고 제거
        if (bulletPattern == BulletPattern.RadialBurst)
        {
            DoRadialBurst();
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        // (LaserBeam) → 직선 이동 + maxLifeTime 후 파괴(충돌로는 안 사라짐)
        if (bulletPattern == BulletPattern.LaserBeam)
        {
            MoveStraightIgnoreCollision();

            if (aliveTime >= maxLifeTime)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 레이저 외 패턴 -> 일정 시간 지나면 소멸
        if (aliveTime >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 패턴별 이동 로직
        switch (bulletPattern)
        {
            case BulletPattern.StraightShot:
                MoveStraight();
                break;

            case BulletPattern.CurvedShot:
                MoveCurved();
                break;

            case BulletPattern.SpreadShot:
                MoveSpread();
                break;

            case BulletPattern.HomingShot:
                MoveHoming();
                break;

            case BulletPattern.SpiralShot:
                MoveSpiral();
                break;

            case BulletPattern.ZigzagShot:
                MoveZigzag();
                break;

            case BulletPattern.SplitShot:
                MoveStraight();
                CheckCollisionAndSplit();
                break;

            case BulletPattern.AccelerationShot:
                MoveAcceleration();
                break;

            // RadialBurst는 Start()에서 처리
        }
    }

    // =========================
    //   각 패턴별 이동 메서드
    // =========================

    private void MoveStraight()
    {
        transform.position += initialDirection * speed * Time.deltaTime;

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    /// <summary>
    /// LaserBeam 전용 (몬스터와 충돌하더라도 사라지지 않는 직선 이동)
    /// </summary>
    private void MoveStraightIgnoreCollision()
    {
        transform.position += initialDirection * speed * Time.deltaTime;
        // 별도의 충돌 판정 없음
    }

    private void MoveCurved()
    {
        float moveX = speed * Time.deltaTime;
        float moveY = Mathf.Sin(aliveTime * 3f) * 0.02f * speed;

        transform.position += new Vector3(moveX, moveY, 0f);

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    private void MoveSpread()
    {
        transform.position += initialDirection * speed * Time.deltaTime;

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    private void MoveHoming()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist < 0.2f)
        {
            HitTarget();
        }
    }

    private void MoveSpiral()
    {
        spiralAngle += 5f * Time.deltaTime;
        float radius = 0.5f + 0.5f * aliveTime;
        float offsetX = Mathf.Cos(spiralAngle * 2f * Mathf.PI) * radius;
        float offsetY = Mathf.Sin(spiralAngle * 2f * Mathf.PI) * radius;

        transform.position = startPosition + new Vector3(offsetX, offsetY, 0f);

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    private void MoveZigzag()
    {
        float zigzagFrequency = 4f;
        float zigzagAmplitude = 0.5f;
        float x = speed * aliveTime;
        float y = Mathf.Sin(aliveTime * zigzagFrequency) * zigzagAmplitude;

        Vector3 offset = new Vector3(x, y, 0f);
        transform.position = startPosition + offset;

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    private void MoveAcceleration()
    {
        speed += accelRate * Time.deltaTime;
        transform.position += initialDirection * speed * Time.deltaTime;

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                HitTarget();
            }
        }
    }

    private void CheckCollisionAndSplit()
    {
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist < 0.2f)
        {
            DoSplit();
            Destroy(gameObject);
        }
    }

    private void DoSplit()
    {
        if (subBulletPrefab == null) return;

        for (int i = 0; i < subBulletCount; i++)
        {
            float angle = 360f * i / subBulletCount;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 dir = rot * Vector3.right;

            GameObject sub = Instantiate(subBulletPrefab, transform.position, Quaternion.identity);
            Bullet subB = sub.GetComponent<Bullet>();
            if (subB != null)
            {
                subB.bulletPattern = BulletPattern.StraightShot;
                subB.Init(null, damage * 0.5f, speed * 0.5f, false, 0f);
            }

            if (sub.GetComponent<Rigidbody2D>() != null)
            {
                sub.GetComponent<Rigidbody2D>().linearVelocity = dir * (speed * 0.5f);
            }
        }
    }

    private void DoRadialBurst()
    {
        if (subBulletPrefab == null) return;

        int bulletCount = 12;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = 360f * i / bulletCount;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;

            GameObject sub = Instantiate(subBulletPrefab, transform.position, Quaternion.identity);
            Bullet subB = sub.GetComponent<Bullet>();
            if (subB != null)
            {
                subB.bulletPattern = BulletPattern.StraightShot;
                subB.Init(null, damage, speed, false, 0f);
            }

            if (sub.GetComponent<Rigidbody2D>() != null)
            {
                sub.GetComponent<Rigidbody2D>().linearVelocity = dir * speed;
            }
        }
    }

    // ===========================
    //   타격 시 처리 + VFX
    // ===========================
    private void HitTarget()
    {
        if (isAreaAttack)
        {
            // 광역 공격
            ApplyAreaDamage(target.transform.position);
        }
        else
        {
            // ** 아군 몬스터인지 체크 후 데미지 적용 **
            if (!target.isAlly)  // 아군이면 데미지를 주지 않는다
            {
                target.TakeDamage(damage);

                // 단일 충돌 지점에 VFX
                if (impactEffectPrefab != null)
                {
                    Instantiate(impactEffectPrefab, target.transform.position, Quaternion.identity);
                }
            }
        }

        Destroy(gameObject);
    }

    private void ApplyAreaDamage(Vector3 centerPos)
    {
        // 범위 내 몬스터 전부 데미지
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (var mo in monsters)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            // 아군 몬스터면 스킵 (데미지 X)
            if (m.isAlly) 
                continue;

            float dist = Vector2.Distance(centerPos, m.transform.position);
            if (dist <= areaRadius)
            {
                m.TakeDamage(damage);

                // 광역에도 각각 VFX 표시(옵션)
                if (impactEffectPrefab != null)
                {
                    Instantiate(impactEffectPrefab, m.transform.position, Quaternion.identity);
                }
            }
        }

        Debug.Log($"[Bullet] 광역 공격 발동! 중심={centerPos}, 범위={areaRadius}, damage={damage}");
    }
}
