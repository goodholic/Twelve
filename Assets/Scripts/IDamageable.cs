using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 객체들의 인터페이스
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 데미지를 받는 메서드
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    void TakeDamage(float damage);
}