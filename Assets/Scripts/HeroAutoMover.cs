/// <summary>
/// 히어로 자동 이동/공격 컴포넌트
/// </summary>
public class HeroAutoMover : MonoBehaviour
{
    private Character character;
    private float searchRadius = 10f;
    private float attackCheckInterval = 0.5f;
    private float lastAttackCheck = 0f;
    
    private void Start()
    {
        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("[HeroAutoMover] Character 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
        }
    }
    
    private void Update()
    {
        if (Time.time - lastAttackCheck > attackCheckInterval)
        {
            lastAttackCheck = Time.time;
            CheckForTargets();
        }
    }
    
    private void CheckForTargets()
    {
        // 공격 범위 내 적 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        
        foreach (var collider in colliders)
        {
            if (collider == null) continue;
            
            // 몬스터 확인
            Monster monster = collider.GetComponent<Monster>();
            if (monster != null && monster.areaIndex != character.areaIndex)
            {
                // 공격 가능 거리인지 확인
                float distance = Vector3.Distance(transform.position, monster.transform.position);
                if (distance <= character.attackRange)
                {
                    // 공격 처리
                    Attack(monster);
                    break;
                }
            }
            
            // 적 캐릭터 확인
            Character enemyChar = collider.GetComponent<Character>();
            if (enemyChar != null && enemyChar.areaIndex != character.areaIndex)
            {
                float distance = Vector3.Distance(transform.position, enemyChar.transform.position);
                if (distance <= character.attackRange)
                {
                    // 공격 처리
                    Attack(enemyChar);
                    break;
                }
            }
        }
    }
    
    private void Attack(IDamageable target)
    {
        if (target == null) return;
        
        // CharacterCombat 컴포넌트 사용
        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null)
        {
            // 공격은 CharacterCombat에서 처리
        }
        else
        {
            // 직접 데미지 처리
            target.TakeDamage(character.attackPower);
        }
    }
}

/// <summary>
/// 카메라를 바라보는 컴포넌트 (HP바, 텍스트 등에 사용)
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}