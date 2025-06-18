using UnityEngine;

/// <summary>
/// 캐릭터 스탯 관리 컴포넌트
/// 게임 기획서: 별 등급에 따른 스탯 보정, 종족별 강화
/// </summary>
public class CharacterStats : MonoBehaviour
{
    private Character character;
    private CharacterVisual visual;
    private float attackCooldown;
    
    /// <summary>
    /// 스탯 초기화
    /// </summary>
    public void Initialize(Character character)
    {
        this.character = character;
        InitializeStats(character);
    }
    
    /// <summary>
    /// 캐릭터 스탯 초기화
    /// </summary>
    public void InitializeStats(Character character)
    {
        this.character = character;
        
        // 별에 따른 기초 스탯 보정 (게임 기획서: 1성 < 2성 < 3성)
        switch (character.star)
        {
            case CharacterStar.OneStar:
                // 1성 기본 스탯
                break;
            case CharacterStar.TwoStar:
                // 2성 스탯 보정
                character.attackPower *= 1.3f;
                character.attackRange *= 1.1f;
                character.attackSpeed *= 1.1f;
                character.currentHP *= 1.2f;
                break;
            case CharacterStar.ThreeStar:
                // 3성 스탯 보정
                character.attackPower *= 1.6f;
                character.attackRange *= 1.2f;
                character.attackSpeed *= 1.2f;
                character.currentHP *= 1.4f;
                break;
        }
        
        attackCooldown = 1f / character.attackSpeed;
        character.SetMaxHP(character.currentHP);
        
        Debug.Log($"[CharacterStats] {character.characterName} 스탯 초기화 완료 - " +
                  $"별: {character.star}, 공격력: {character.attackPower}, " +
                  $"사거리: {character.attackRange}, 공격속도: {character.attackSpeed}");
    }
    
    /// <summary>
    /// 공격 쿨다운 시간 반환
    /// </summary>
    public float GetAttackCooldown()
    {
        return attackCooldown;
    }
    
    /// <summary>
    /// 공격 속도 가져오기
    /// </summary>
    public float GetAttackSpeed()
    {
        return character.attackSpeed;
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 히어로는 무적 (게임 기획서: 주인공 캐릭터)
        if (character.isHero)
        {
            Debug.Log($"[Character] Hero {character.characterName} 무적 처리, 데미지 무시!");
            return;
        }
        
        character.currentHP -= damage;
        if (character.currentHP < 0f) character.currentHP = 0f;
        
        // Visual 컴포넌트 가져오기
        if (visual == null)
            visual = GetComponent<CharacterVisual>();
            
        if (visual != null)
            visual.UpdateHpBar();
        
        if (character.currentHP <= 0f)
        {
            Debug.Log($"[Character] {character.characterName} 사망 (HP=0)!");
            HandleDeath();
        }
    }
    
    /// <summary>
    /// 캐릭터 사망 처리 (게임 기획서: 타워형 캐릭터)
    /// </summary>
    private void HandleDeath()
    {
        // 타일 참조 정리
        if (character.currentTile != null)
        {
            Tile dyingTile = character.currentTile;
            
            if (TileManager.Instance != null && TileManager.Instance.gameObject.activeInHierarchy)
            {
                Debug.Log($"[Character] {character.characterName} 사망 - {dyingTile.name} 타일 참조 정리");
                
                if (dyingTile.IsPlaceTile() || dyingTile.IsPlaced2())
                {
                    dyingTile.RefreshTileVisual();
                    Debug.Log($"[Character] placed tile {dyingTile.name} 비주얼 업데이트");
                }
                else
                {
                    TileManager.Instance.RemovePlaceTileChild(dyingTile);
                }
                
                TileManager.Instance.OnCharacterRemovedFromTile(dyingTile);
            }
            else
            {
                character.currentTile = null;
            }
        }
        
        // 모든 참조 해제
        character.currentTarget = null;
        character.currentCharTarget = null;
        
        // 다음 프레임에 파괴
        Destroy(character.gameObject, 0.01f);
    }
    
    /// <summary>
    /// 종족별 강화 적용 (게임 기획서: 인게임 중 강화)
    /// </summary>
    public void ApplyRaceEnhancement(float attackBonus, float attackSpeedBonus)
    {
        character.attackPower *= (1f + attackBonus);
        character.attackSpeed *= (1f + attackSpeedBonus);
        attackCooldown = 1f / character.attackSpeed;
        
        Debug.Log($"[CharacterStats] {character.characterName} 종족 강화 적용 - " +
                  $"공격력 +{attackBonus * 100}%, 공격속도 +{attackSpeedBonus * 100}%");
    }
}