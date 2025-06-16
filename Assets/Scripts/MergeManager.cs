using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 월드 좌표 기반 캐릭터 합성 관리자
/// 기획서: 1성×3 → 2성, 2성×3 → 3성
/// </summary>
public class MergeManager : MonoBehaviour
{
    private static MergeManager instance;
    public static MergeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MergeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MergeManager");
                    instance = go.AddComponent<MergeManager>();
                }
            }
            return instance;
        }
    }

    [Header("합성 효과")]
    public GameObject mergeEffectPrefab;
    public float mergeEffectDuration = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// ★★★ 수정: 타일에서 3개의 캐릭터 합성 시도
    /// </summary>
    public bool TryMergeCharacters(List<Character> charactersToMerge, Tile targetTile)
    {
        if (charactersToMerge == null || charactersToMerge.Count < 3)
        {
            Debug.LogWarning("[MergeManager] 합성에는 3개의 캐릭터가 필요합니다.");
            return false;
        }

        // 첫 번째 캐릭터 기준으로 정보 저장
        Character baseChar = charactersToMerge[0];
        
        // 모든 캐릭터가 같은 종류인지 확인
        if (!charactersToMerge.All(c => c.characterName == baseChar.characterName && c.star == baseChar.star))
        {
            Debug.LogWarning("[MergeManager] 합성하려는 캐릭터들이 같은 종류가 아닙니다.");
            return false;
        }

        CharacterStar currentStar = baseChar.star;
        CharacterStar targetStar = CharacterStar.OneStar;
        
        // 기획서: 1성×3 → 2성, 2성×3 → 3성
        switch (currentStar)
        {
            case CharacterStar.OneStar:
                targetStar = CharacterStar.TwoStar;
                break;
            case CharacterStar.TwoStar:
                targetStar = CharacterStar.ThreeStar;
                break;
            case CharacterStar.ThreeStar:
                Debug.Log("[MergeManager] 3성은 더 이상 합성 불가");
                return false;
            default:
                Debug.Log("[MergeManager] 알 수 없는 별 등급");
                return false;
        }
        
        // 합성 실행
        return ExecuteMerge(charactersToMerge, targetTile, targetStar);
    }

    /// <summary>
    /// 합성 실행
    /// </summary>
    private bool ExecuteMerge(List<Character> charactersToMerge, Tile targetTile, CharacterStar newStar)
    {
        var coreData = CoreDataManager.Instance;
        
        // 첫 번째 캐릭터를 기준으로 정보 저장
        Character baseChar = charactersToMerge[0];
        int areaIndex = baseChar.areaIndex;
        Vector3 position = targetTile.transform.position;
        Transform parent = baseChar.transform.parent;
        
        Debug.Log($"[MergeManager] 합성 시작: {baseChar.characterName} ({baseChar.star}) × 3 → {newStar}");
        
        // 합성 효과 재생
        PlayMergeEffect(position);
        
        // StarMergeDatabase에서 새 캐릭터 데이터 가져오기
        StarMergeDatabaseObject targetDB = (areaIndex == 2 && coreData.starMergeDatabaseRegion2 != null) 
            ? coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;
            
        if (targetDB == null)
        {
            Debug.LogWarning("[MergeManager] StarMergeDatabase가 null입니다. 간단한 업그레이드로 처리합니다.");
            SimpleUpgrade(baseChar, newStar);
            RemoveOtherCharacters(charactersToMerge, baseChar);
            return true;
        }
        
        // 합성할 3개 중 랜덤 종족 선택
        CharacterRace selectedRace = charactersToMerge[UnityEngine.Random.Range(0, charactersToMerge.Count)].race;
        CharacterData newCharData = null;
        
        if (newStar == CharacterStar.TwoStar)
        {
            newCharData = targetDB.GetRandom2Star((RaceType)selectedRace);
        }
        else if (newStar == CharacterStar.ThreeStar)
        {
            newCharData = targetDB.GetRandom3Star((RaceType)selectedRace);
        }
        
        if (newCharData == null || newCharData.spawnPrefab == null)
        {
            Debug.LogWarning($"[MergeManager] {newStar} 프리팹을 찾을 수 없습니다. 간단한 업그레이드로 처리합니다.");
            SimpleUpgrade(baseChar, newStar);
            RemoveOtherCharacters(charactersToMerge, baseChar);
            return true;
        }
        
        // 타일에서 기존 캐릭터들 제거
        foreach (var character in charactersToMerge)
        {
            if (character.currentTile != null)
            {
                character.currentTile.RemoveOccupyingCharacter(character);
            }
        }
        
        // 새로운 캐릭터 생성
        GameObject newCharObj = Instantiate(newCharData.spawnPrefab, position, Quaternion.identity, parent);
        
        Character newCharacter = newCharObj.GetComponent<Character>();
        if (newCharacter == null)
        {
            newCharacter = newCharObj.AddComponent<Character>();
        }
        
        // 기존 정보 복사
        newCharacter.currentTile = targetTile;
        newCharacter.areaIndex = areaIndex;
        newCharacter.isHero = baseChar.isHero;
        newCharacter.isCharAttack = baseChar.isCharAttack;
        newCharacter.currentWaypointIndex = baseChar.currentWaypointIndex;
        newCharacter.maxWaypointIndex = baseChar.maxWaypointIndex;
        newCharacter.pathWaypoints = baseChar.pathWaypoints;
        
        // 새로운 데이터 적용
        newCharacter.characterName = newCharData.characterName;
        newCharacter.characterIndex = newCharData.characterIndex;
        newCharacter.race = newCharData.race;
        newCharacter.star = newStar;
        
        // 스탯 설정 (별 등급에 따른 배율 적용)
        float statMultiplier = GetStatMultiplier(newStar);
        
        newCharacter.attackPower = newCharData.attackPower * statMultiplier;
        newCharacter.attackSpeed = newCharData.attackSpeed * (newStar == CharacterStar.TwoStar ? 1.1f : 1.2f);
        newCharacter.attackRange = newCharData.attackRange * (newStar == CharacterStar.TwoStar ? 1.1f : 1.2f);
        newCharacter.currentHP = newCharData.health * statMultiplier;
        newCharacter.maxHP = newCharData.health * statMultiplier;
        newCharacter.level = baseChar.level; // 레벨 유지
        
        // 스프라이트 설정
        if (newCharData.characterSprite != null)
        {
            newCharacter.characterSprite = newCharData.characterSprite;
            newCharacter.SetSprite(newCharData.characterSprite);
        }
        
        // 앞뒤 이미지 설정
        if (newCharData.frontSprite != null || newCharData.backSprite != null)
        {
            newCharacter.frontSprite = newCharData.frontSprite;
            newCharacter.backSprite = newCharData.backSprite;
        }
        
        // 타일에 새 캐릭터 추가
        targetTile.AddOccupyingCharacter(newCharacter);
        
        // 별 비주얼 적용
        newCharacter.ApplyStarVisual();
        
        // DraggableCharacter 컴포넌트 추가 (플레이어 캐릭터만)
        if (areaIndex == 1 && newCharacter.GetComponent<DraggableCharacter>() == null)
        {
            newCharacter.gameObject.AddComponent<DraggableCharacter>();
        }
        
        // 기존 3개 캐릭터 모두 제거
        foreach (var character in charactersToMerge)
        {
            if (character != null && character.gameObject != null)
            {
                Destroy(character.gameObject);
            }
        }
        
        Debug.Log($"[MergeManager] 합성 성공! 새로운 {newStar} 캐릭터 '{newCharData.characterName}' 생성 완료");
        return true;
    }

    /// <summary>
    /// 간단한 업그레이드 (프리팹 교체 없이)
    /// </summary>
    private void SimpleUpgrade(Character character, CharacterStar newStar)
    {
        character.star = newStar;
        
        float statMultiplier = GetStatMultiplier(newStar);
        
        // 스탯 업그레이드
        switch (newStar)
        {
            case CharacterStar.TwoStar:
                character.attackPower *= 1.3f;
                character.attackSpeed *= 1.1f;
                character.attackRange *= 1.1f;
                character.currentHP *= 1.2f;
                character.maxHP *= 1.2f;
                break;
            case CharacterStar.ThreeStar:
                character.attackPower *= 1.6f;
                character.attackSpeed *= 1.2f;
                character.attackRange *= 1.2f;
                character.currentHP *= 1.4f;
                character.maxHP *= 1.4f;
                break;
        }
        
        character.ApplyStarVisual();
    }

    /// <summary>
    /// 별 등급에 따른 스탯 배율
    /// </summary>
    private float GetStatMultiplier(CharacterStar star)
    {
        switch (star)
        {
            case CharacterStar.OneStar:
                return 1.0f;
            case CharacterStar.TwoStar:
                return 1.3f;
            case CharacterStar.ThreeStar:
                return 1.6f;
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// 다른 캐릭터들 제거
    /// </summary>
    private void RemoveOtherCharacters(List<Character> allCharacters, Character keepCharacter)
    {
        foreach (var character in allCharacters)
        {
            if (character != null && character != keepCharacter && character.gameObject != null)
            {
                // 타일에서 제거
                if (character.currentTile != null)
                {
                    character.currentTile.RemoveOccupyingCharacter(character);
                }
                
                Destroy(character.gameObject);
            }
        }
    }

    /// <summary>
    /// 합성 효과 재생
    /// </summary>
    private void PlayMergeEffect(Vector3 position)
    {
        if (mergeEffectPrefab != null)
        {
            GameObject effect = Instantiate(mergeEffectPrefab, position, Quaternion.identity);
            Destroy(effect, mergeEffectDuration);
        }
        
        // 추가 시각 효과 (파티클, 사운드 등)
        // TODO: 필요시 추가
    }

    /// <summary>
    /// 디버그용: 특정 타일의 합성 가능 여부 확인
    /// </summary>
    public void DebugCheckMergeOnTile(Tile tile)
    {
        if (tile == null) return;
        
        List<Character> tileCharacters = tile.GetOccupyingCharacters();
        Debug.Log($"[MergeManager] {tile.name}의 캐릭터 수: {tileCharacters.Count}");
        
        var groups = tileCharacters.GroupBy(c => new { c.characterName, c.star });
        foreach (var group in groups)
        {
            Debug.Log($"  - {group.Key.characterName} ({group.Key.star}): {group.Count()}개");
        }
    }
}