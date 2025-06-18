using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타워 스태킹 시스템 - 같은 캐릭터를 최대 3개까지 스택
/// </summary>
public class TowerStackSystem : MonoBehaviour
{
    [Header("스택 설정")]
    [SerializeField] private int maxStackCount = 3;
    [SerializeField] private float stackScaleReduction = 0.8f; // 스택당 크기 감소율
    [SerializeField] private float stackOffsetY = 0.5f; // 스택 간 Y 오프셋
    
    [Header("스택 보너스")]
    [SerializeField] private float attackPowerBonus = 0.2f; // 스택당 공격력 20% 증가
    [SerializeField] private float attackSpeedBonus = 0.1f; // 스택당 공격속도 10% 증가
    
    private List<Character> stackedCharacters = new List<Character>();
    private Tile currentTile;
    
    /// <summary>
    /// 타일에 타워 스택 초기화
    /// </summary>
    public void Initialize(Tile tile)
    {
        currentTile = tile;
        transform.position = tile.transform.position;
    }
    
    /// <summary>
    /// 캐릭터를 스택에 추가
    /// </summary>
    public bool TryAddCharacter(Character character)
    {
        // 최대 스택 수 확인
        if (stackedCharacters.Count >= maxStackCount)
        {
            Debug.Log($"[TowerStackSystem] 최대 스택 수({maxStackCount})에 도달했습니다.");
            return false;
        }
        
        // 첫 번째 캐릭터이거나 같은 타입의 캐릭터인지 확인
        if (stackedCharacters.Count > 0)
        {
            Character baseCharacter = stackedCharacters[0];
            if (character.characterIndex != baseCharacter.characterIndex)
            {
                Debug.Log($"[TowerStackSystem] 다른 타입의 캐릭터는 스택할 수 없습니다.");
                return false;
            }
        }
        
        // 캐릭터 추가
        stackedCharacters.Add(character);
        character.transform.SetParent(transform);
        
        // 시각적 효과 적용
        ApplyStackVisuals();
        
        // 스택 보너스 적용
        ApplyStackBonuses();
        
        Debug.Log($"[TowerStackSystem] {character.characterName} 스택 추가 (현재 스택: {stackedCharacters.Count}/{maxStackCount})");
        
        return true;
    }
    
    /// <summary>
    /// 캐릭터를 스택에서 제거
    /// </summary>
    public void RemoveCharacter(Character character)
    {
        if (stackedCharacters.Remove(character))
        {
            character.transform.SetParent(null);
            
            // 남은 캐릭터가 있으면 시각 효과 재적용
            if (stackedCharacters.Count > 0)
            {
                ApplyStackVisuals();
                ApplyStackBonuses();
            }
            else
            {
                // 스택이 비었으면 시스템 제거
                Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// 스택 시각 효과 적용
    /// </summary>
    private void ApplyStackVisuals()
    {
        for (int i = 0; i < stackedCharacters.Count; i++)
        {
            Character character = stackedCharacters[i];
            
            // 위치 설정 (위로 쌓기)
            float yOffset = i * stackOffsetY;
            character.transform.localPosition = new Vector3(0, yOffset, -i * 0.01f); // Z값으로 렌더 순서 조정
            
            // 크기 조정 (위로 갈수록 작아짐)
            float scale = Mathf.Pow(stackScaleReduction, i);
            character.transform.localScale = Vector3.one * scale;
            
            // 정렬 순서 조정
            SpriteRenderer sr = character.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10 + i; // 위쪽이 더 앞에 렌더링
            }
        }
    }
    
    /// <summary>
    /// 스택 보너스 적용
    /// </summary>
    private void ApplyStackBonuses()
    {
        if (stackedCharacters.Count == 0) return;
        
        // 기본 캐릭터의 원본 스탯
        Character baseCharacter = stackedCharacters[0];
        CharacterData baseData = baseCharacter.GetCharacterData();
        
        // 스택 수에 따른 보너스 계산
        float stackMultiplier = 1f + (stackedCharacters.Count - 1) * attackPowerBonus;
        float speedMultiplier = 1f + (stackedCharacters.Count - 1) * attackSpeedBonus;
        
        // 모든 스택된 캐릭터에 보너스 적용
        foreach (Character character in stackedCharacters)
        {
            character.attackPower = baseData.attackPower * stackMultiplier;
            character.attackSpeed = baseData.attackSpeed * speedMultiplier;
            
            // HP는 스택 수만큼 증가
            character.SetMaxHP(baseData.maxHP * stackedCharacters.Count);
            character.currentHP = character.maxHP;
            character.UpdateHPBar();
        }
        
        Debug.Log($"[TowerStackSystem] 스택 보너스 적용 - 공격력: x{stackMultiplier:F1}, 공격속도: x{speedMultiplier:F1}");
    }
    
    /// <summary>
    /// 현재 스택 수 가져오기
    /// </summary>
    public int GetStackCount()
    {
        return stackedCharacters.Count;
    }
    
    /// <summary>
    /// 스택 가능 여부 확인
    /// </summary>
    public bool CanStack(Character character)
    {
        if (stackedCharacters.Count >= maxStackCount)
            return false;
            
        if (stackedCharacters.Count > 0 && character.characterIndex != stackedCharacters[0].characterIndex)
            return false;
            
        return true;
    }
    
    /// <summary>
    /// 대표 캐릭터 가져오기 (전투 등에서 사용)
    /// </summary>
    public Character GetRepresentativeCharacter()
    {
        return stackedCharacters.Count > 0 ? stackedCharacters[0] : null;
    }
    
    /// <summary>
    /// 모든 스택된 캐릭터 가져오기
    /// </summary>
    public List<Character> GetStackedCharacters()
    {
        return new List<Character>(stackedCharacters);
    }
    
    /// <summary>
    /// 스택 전체가 데미지를 받을 때
    /// </summary>
    public void TakeStackDamage(float damage)
    {
        // 데미지를 스택 수로 분산
        float damagePerCharacter = damage / stackedCharacters.Count;
        
        // 각 캐릭터에 데미지 적용
        for (int i = stackedCharacters.Count - 1; i >= 0; i--)
        {
            stackedCharacters[i].TakeDamage(damagePerCharacter);
        }
    }
    
    /// <summary>
    /// 타일 위치 가져오기
    /// </summary>
    public Tile GetTile()
    {
        return currentTile;
    }
    
    // 디버그용
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (stackedCharacters.Count > 0)
        {
            // 스택 범위 표시
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position + Vector3.up * (stackOffsetY * stackedCharacters.Count * 0.5f),
                           new Vector3(1, stackOffsetY * stackedCharacters.Count, 1));
        }
    }
    #endif
}