using UnityEngine;

/// <summary>
/// 씬 위의 캐릭터를 마우스로 클릭하면, oxGameManager에 자동 등록되는 예시.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))] // (추가) 2D Collider 필수
public class oxCharacter : MonoBehaviour
{
    [Header("공격 패턴 (5x5)")]
    [SerializeField] private CharacterAttackPattern attackPattern;

    [Header("전투 관련 파라미터")]
    [SerializeField] private float maxHP = 10f;
    [SerializeField] private float attackPower = 3f;

    private float currentHP;

    public CharacterAttackPattern AttackPattern => attackPattern;
    public float AttackPower => attackPower;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// (중요) 마우스로 이 캐릭터 오브젝트를 클릭하면 oxGameManager에 자신을 등록.
    /// </summary>
    private void OnMouseDown()
    {
        // oxGameManager 싱글톤 접근
        if (oxGameManager.Instance != null)
        {
            oxGameManager.Instance.SelectCharacter(this);
        }
        else
        {
            Debug.LogWarning("[oxCharacter] oxGameManager.Instance가 null입니다. 싱글톤 설정을 확인하세요.");
        }
    }

    /// <summary>
    /// 데미지 처리 후 체력이 0 이하이면 사망(파괴).
    /// 반환값: true=사망, false=생존
    /// </summary>
    public bool TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0f)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
