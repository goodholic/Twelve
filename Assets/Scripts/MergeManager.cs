using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 캐릭터 합성, 별 등급 관리
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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
        Debug.Log($"[MergeManager] TryMergeCharacter: movingChar={movingChar.characterName}, star={movingChar.star}, tile={newTile.name}");

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> sameCharacters = new List<Character>();
        
        // 타일에서 같은 캐릭터 찾기
        foreach (var otherChar in allChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;

            if (otherChar.currentTile == newTile)
            {
                // 같은 캐릭터명과 같은 별 등급인지 확인
                if (otherChar.star == movingChar.star && otherChar.characterName == movingChar.characterName)
                {
                    sameCharacters.Add(otherChar);
                }
                else
                {
                    Debug.Log("[MergeManager] 별이 다르거나 다른 캐릭터명 => 합성 불가");
                    return false;
                }
            }
        }
        
        // 기획서: 3개가 모여야 합성 가능
        if (sameCharacters.Count >= 2) // movingChar + 2개 = 총 3개
        {
            sameCharacters.Add(movingChar);
            return ExecuteMerge(sameCharacters, newTile);
        }
        else if (sameCharacters.Count == 1) // movingChar + 1개 = 총 2개 (아직 부족)
        {
            // 2개만 있으면 movingChar를 그 타일로 이동만 시킴
            return false; // 합성은 하지 않고 단순 이동만
        }
        
        return false;
    }

    private bool ExecuteMerge(List<Character> charactersToMerge, Tile targetTile)
    {
        if (charactersToMerge.Count < 3)
        {
            Debug.LogWarning("[MergeManager] 합성에는 3개의 캐릭터가 필요합니다.");
            return false;
        }
        
        // 첫 번째 캐릭터 기준으로 정보 저장
        Character baseChar = charactersToMerge[0];
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
        
        // 실제 프리팹 교체로 합성 실행
        return ReplaceCharacterWithNewStar(charactersToMerge, targetTile, targetStar);
    }

    private bool ReplaceCharacterWithNewStar(List<Character> charactersToMerge, Tile targetTile, CharacterStar newStar)
    {
        var coreData = CoreDataManager.Instance;
        
        // 첫 번째 캐릭터를 기준으로 정보 저장
        Character baseChar = charactersToMerge[0];
        int areaIndex = baseChar.areaIndex;
        Vector3 position = targetTile.transform.position;
        Transform parent = baseChar.transform.parent;
        
        Debug.Log($"[MergeManager] 프리팹 교체 시작: {baseChar.characterName} -> {newStar}");
        
        StarMergeDatabaseObject targetDB = (areaIndex == 2 && coreData.starMergeDatabaseRegion2 != null) 
            ? coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;
            
        if (targetDB == null)
        {
            Debug.LogWarning("[MergeManager] StarMergeDatabase가 null입니다.");
            SimpleUpgrade(baseChar, newStar);
            RemoveOtherCharacters(charactersToMerge, baseChar);
            return true;
        }
        
        // 합성할 3개 중 랜덤 종족 선택 (UnityEngine.Random으로 명시)
        RaceType selectedRace = (RaceType)charactersToMerge[UnityEngine.Random.Range(0, charactersToMerge.Count)].race;
        CharacterData newCharData = null;
        
        if (newStar == CharacterStar.TwoStar)
        {
            newCharData = targetDB.GetRandom2Star(selectedRace);
        }
        else if (newStar == CharacterStar.ThreeStar)
        {
            newCharData = targetDB.GetRandom3Star(selectedRace);
        }
        
        if (newCharData == null || newCharData.spawnPrefab == null)
        {
            Debug.LogWarning($"[MergeManager] {newStar} 프리팹을 찾을 수 없습니다. 기존 방식으로 처리");
            SimpleUpgrade(baseChar, newStar);
            RemoveOtherCharacters(charactersToMerge, baseChar);
            return true;
        }
        
        // 새로운 캐릭터 생성
        GameObject newCharObj = Instantiate(newCharData.spawnPrefab, parent);
        if (newCharObj == null)
        {
            Debug.LogError("[MergeManager] 새 프리팹 생성 실패");
            return false;
        }
        
        // 위치 설정
        RectTransform newCharRect = newCharObj.GetComponent<RectTransform>();
        if (newCharRect != null)
        {
            RectTransform oldRect = baseChar.GetComponent<RectTransform>();
            if (oldRect != null)
            {
                newCharRect.anchoredPosition = oldRect.anchoredPosition;
                newCharRect.localRotation = oldRect.localRotation;
            }
        }
        else
        {
            newCharObj.transform.position = position;
            newCharObj.transform.localRotation = baseChar.transform.localRotation;
        }
        
        // Character 컴포넌트 설정
        Character newCharacter = newCharObj.GetComponent<Character>();
        if (newCharacter != null)
        {
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
            newCharacter.race = newCharData.race;
            newCharacter.star = newStar;
            
            // 스탯 설정 (별 등급에 따른 배율 적용)
            float statMultiplier = 1.0f;
            switch (newStar)
            {
                case CharacterStar.TwoStar:
                    statMultiplier = 1.3f;
                    break;
                case CharacterStar.ThreeStar:
                    statMultiplier = 1.6f;
                    break;
            }
            
            newCharacter.attackPower = newCharData.attackPower * statMultiplier;
            newCharacter.attackSpeed = newCharData.attackSpeed * 1.1f;
            newCharacter.attackRange = newCharData.attackRange * 1.1f;
            newCharacter.currentHP = newCharData.maxHP * statMultiplier;
            newCharacter.moveSpeed = newCharData.moveSpeed;
            
            // 별 비주얼 적용
            newCharacter.ApplyStarVisual();
            
            // 탄환 패널 설정
            if (areaIndex == 2 && coreData.opponentBulletPanel != null)
            {
                newCharacter.opponentBulletPanel = coreData.opponentBulletPanel;
            }
            else
            {
                newCharacter.SetBulletPanel(coreData.bulletPanel);
            }
            
            // 앞뒤 이미지 적용
            if (newCharData.frontSprite != null || newCharData.backSprite != null)
            {
                ApplyFrontBackImages(newCharacter, newCharData);
            }
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

    private void SimpleUpgrade(Character character, CharacterStar newStar)
    {
        character.star = newStar;
        
        // 스탯 업그레이드
        switch (newStar)
        {
            case CharacterStar.TwoStar:
                character.attackPower *= 1.3f;
                character.attackSpeed *= 1.1f;
                character.attackRange *= 1.1f;
                character.currentHP *= 1.2f;
                break;
            case CharacterStar.ThreeStar:
                character.attackPower *= 1.6f;
                character.attackSpeed *= 1.2f;
                character.attackRange *= 1.2f;
                character.currentHP *= 1.4f;
                break;
        }
        
        character.ApplyStarVisual();
    }

    private void RemoveOtherCharacters(List<Character> allCharacters, Character keepCharacter)
    {
        foreach (var character in allCharacters)
        {
            if (character != null && character != keepCharacter && character.gameObject != null)
            {
                Destroy(character.gameObject);
            }
        }
    }

    private void ApplyFrontBackImages(Character character, CharacterData data)
    {
        Transform frontImageObj = character.transform.Find("FrontImage");
        Transform backImageObj = character.transform.Find("BackImage");
        
        if (data.frontSprite != null)
        {
            if (frontImageObj == null)
            {
                GameObject frontGO = new GameObject("FrontImage");
                frontGO.transform.SetParent(character.transform, false);
                frontImageObj = frontGO.transform;
                
                UnityEngine.UI.Image frontImg = frontGO.AddComponent<UnityEngine.UI.Image>();
                RectTransform frontRect = frontGO.GetComponent<RectTransform>();
                frontRect.anchorMin = new Vector2(0.5f, 0.5f);
                frontRect.anchorMax = new Vector2(0.5f, 0.5f);
                frontRect.pivot = new Vector2(0.5f, 0.5f);
                frontRect.sizeDelta = new Vector2(100, 100);
                frontRect.anchoredPosition = Vector2.zero;
            }
            
            UnityEngine.UI.Image frontImage = frontImageObj.GetComponent<UnityEngine.UI.Image>();
            if (frontImage != null)
            {
                frontImage.sprite = data.frontSprite;
                frontImage.preserveAspect = true;
            }
            
            frontImageObj.gameObject.SetActive(false);
        }
        
        if (data.backSprite != null)
        {
            if (backImageObj == null)
            {
                GameObject backGO = new GameObject("BackImage");
                backGO.transform.SetParent(character.transform, false);
                backImageObj = backGO.transform;
                
                UnityEngine.UI.Image backImg = backGO.AddComponent<UnityEngine.UI.Image>();
                RectTransform backRect = backGO.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0.5f, 0.5f);
                backRect.anchorMax = new Vector2(0.5f, 0.5f);
                backRect.pivot = new Vector2(0.5f, 0.5f);
                backRect.sizeDelta = new Vector2(100, 100);
                backRect.anchoredPosition = Vector2.zero;
            }
            
            UnityEngine.UI.Image backImage = backImageObj.GetComponent<UnityEngine.UI.Image>();
            if (backImage != null)
            {
                backImage.sprite = data.backSprite;
                backImage.preserveAspect = true;
            }
            
            backImageObj.gameObject.SetActive(true);
        }
        
        Debug.Log($"[MergeManager] {character.characterName}에 앞뒤 이미지 적용 완료");
    }

    // 기존 메서드들 제거 (RandomizeAppearanceByStarAndRace, UpgradeStats 등)
    // SimpleUpgrade로 통합됨
}