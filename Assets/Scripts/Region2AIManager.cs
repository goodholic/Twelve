using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 지역2 AI 전용 매니저(캐릭터 소환):
/// - 지역2용 미네랄 바를 소모
/// - opponentCharacterDatabase.characters[0..9] 중에서 골라 (인덱스 9는 히어로)
/// - walkable2Tiles / placable2Tiles 아무 데나 배치
/// - '몬스터'가 아니라 'Character'를 소환
/// - 10번째 캐릭터(인덱스=9)는 Hero Panel(opponentOurMonsterPanel)에 직접 생성(예시).
/// - 그 외는 PlacementManager.SummonCharacterOnTile(..., forceEnemyArea2=true)
/// </summary>
public class Region2AIManager : MonoBehaviour
{
    [Header("상대 캐릭터 데이터베이스 (ScriptableObject)")]
    public CharacterDatabaseObject opponentCharacterDatabase;

    [Header("캐릭터 자동 소환 간격(초)")]
    public float spawnInterval = 1f;

    [Header("소환 실패 시 재시도 대기 시간")]
    public float retryInterval = 0.3f;

    [Header("지역2 타일들 (Walkable2) - 3개 루트")]
    [Tooltip("좌측 루트 walkable2 타일들")]
    public List<Tile> walkable2LeftTiles = new List<Tile>();
    [Tooltip("중앙 루트 walkable2 타일들")]
    public List<Tile> walkable2CenterTiles = new List<Tile>();
    [Tooltip("우측 루트 walkable2 타일들")]
    public List<Tile> walkable2RightTiles = new List<Tile>();

    [Header("지역2 타일들 (Placable2)")]
    public List<Tile> placable2Tiles = new List<Tile>();

    [Header("Opponent Hero Panel (10번째 캐릭터 전용)")]
    public RectTransform opponentHeroPanel;

    [Header("AI 합성 설정")]
    [Tooltip("AI가 자동으로 합성을 시도할 확률 (0~1)")]
    public float autoMergeChance = 0.3f;
    [Tooltip("AI 합성 시도 간격(초)")]
    public float mergeCheckInterval = 5f;

    private bool isRunning = false;

    private void Start()
    {
        // (A) PlacementManager 연결 확인
        if (PlacementManager.Instance != null)
        {
            Debug.Log("[Region2AIManager] PlacementManager 연결 확인 완료.");
        }
        else
        {
            Debug.LogError("[Region2AIManager] PlacementManager.Instance가 null입니다!");
        }

        // (B) opponentCharacterDatabase 유효성 체크
        if (opponentCharacterDatabase == null)
        {
            Debug.LogError("[Region2AIManager] opponentCharacterDatabase가 null입니다!");
            return;
        }
        if (opponentCharacterDatabase.characters == null || opponentCharacterDatabase.characters.Length == 0)
        {
            Debug.LogError("[Region2AIManager] opponentCharacterDatabase.characters가 비어있습니다!");
            return;
        }

        // (C) 타일 자동 검색 (Inspector에서 설정하지 않은 경우)
        if (walkable2LeftTiles.Count == 0 && walkable2CenterTiles.Count == 0 && walkable2RightTiles.Count == 0)
        {
            CollectWalkable2Tiles();
        }
        if (placable2Tiles.Count == 0)
        {
            CollectPlacable2Tiles();
        }

        // (D) AI 루틴 시작
        StartCoroutine(AIRoutine());
        StartCoroutine(AIMergeRoutine());

        // (E) 지역2 히어로 캐릭터 즉시 생성
        CreateRegion2HeroCharacter();
    }

    /// <summary>
    /// Walkable2 타일들을 자동으로 수집
    /// </summary>
    private void CollectWalkable2Tiles()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            if (tile.IsWalkable2Left())
                walkable2LeftTiles.Add(tile);
            else if (tile.IsWalkable2Center())
                walkable2CenterTiles.Add(tile);
            else if (tile.IsWalkable2Right())
                walkable2RightTiles.Add(tile);
        }
        
        Debug.Log($"[Region2AIManager] Walkable2 타일 수집 완료 - 좌:{walkable2LeftTiles.Count}, 중앙:{walkable2CenterTiles.Count}, 우:{walkable2RightTiles.Count}");
    }

    /// <summary>
    /// Placable2 타일들을 자동으로 수집
    /// </summary>
    private void CollectPlacable2Tiles()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            if (tile.IsPlacable2() || tile.IsPlaced2())
            {
                placable2Tiles.Add(tile);
            }
        }
        
        Debug.Log($"[Region2AIManager] Placable2 타일 수집 완료: {placable2Tiles.Count}개");
    }

    /// <summary>
    /// 지역2 히어로 캐릭터 생성
    /// </summary>
    private void CreateRegion2HeroCharacter()
    {
        // 1) 패널 확인
        if (opponentHeroPanel == null)
        {
            Debug.LogWarning("[Region2AIManager] opponentHeroPanel이 null => 지역2 히어로 생성 불가");
            
            // 자동으로 찾기 시도
            GameObject heroPanel = GameObject.Find("OpponentHeroPanel");
            if (heroPanel != null)
            {
                opponentHeroPanel = heroPanel.GetComponent<RectTransform>();
                if (opponentHeroPanel == null)
                {
                    opponentHeroPanel = heroPanel.transform as RectTransform;
                }
            }
            
            if (opponentHeroPanel == null)
            {
                Debug.LogError("[Region2AIManager] OpponentHeroPanel을 찾을 수 없습니다! (지역2 히어로 생성 불가)");
                return;
            }
        }
        if (opponentCharacterDatabase.characters.Length < 10)
        {
            Debug.LogWarning("[Region2AIManager] DB에 10개 미만 캐릭터 => 인덱스 9 히어로가 존재하지 않습니다.");
            return;
        }

        // 2) 인덱스=9(히어로) 확인
        CharacterData heroCandidate = opponentCharacterDatabase.characters[9];
        if (heroCandidate == null)
        {
            Debug.Log("[Region2AIManager] 9번 캐릭터가 null => 지역2 히어로가 설정되지 않았거나 이미 소환됨.");
            return;
        }
        if (heroCandidate.spawnPrefab == null)
        {
            Debug.LogWarning("[Region2AIManager] 9번 히어로 CharacterData의 spawnPrefab이 null => 생성 불가");
            return;
        }

        // 3) 실제 생성
        GameObject heroObj = Instantiate(heroCandidate.spawnPrefab, opponentHeroPanel);

        RectTransform rt = heroObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            heroObj.transform.position = Vector3.zero;
            heroObj.transform.localRotation = Quaternion.identity;
        }

        // 4) 캐릭터 설정
        Character heroComp = heroObj.GetComponent<Character>();
        if (heroComp != null)
        {
            heroComp.isHero = true;
            heroComp.areaIndex = 2;

            heroComp.attackPower = heroCandidate.attackPower;
            heroComp.attackSpeed = heroCandidate.attackSpeed;
            heroComp.attackRange = heroCandidate.attackRange;
            heroComp.currentHP = heroCandidate.maxHP;
            heroComp.star = heroCandidate.initialStar;
            
            // visual 컴포넌트가 초기화될 때까지 잠시 대기 후 ApplyStarVisual 호출
            StartCoroutine(ApplyStarVisualDelayed(heroComp));

            // 지역2 히어로 탄환 패널 설정
            if (PlacementManager.Instance != null && PlacementManager.Instance.opponentBulletPanel != null)
            {
                heroComp.opponentBulletPanel = PlacementManager.Instance.opponentBulletPanel;
            }
        }

        // 히어로도 AI처럼 자동 이동/공격하려면 HeroAutoMover 추가
        heroObj.AddComponent<HeroAutoMover>();

        Debug.Log("[Region2AIManager] 지역2 히어로 캐릭터(인덱스=9) 즉시 생성 완료!");
    }

    private IEnumerator AIRoutine()
    {
        isRunning = true;
        
        // 게임 시작 직후 약간의 지연 (1초)만 주고 바로 소환 시작
        yield return new WaitForSeconds(1f);
        
        while (isRunning)
        {
            CharacterData chosen = SelectRandomUnit();
            if (chosen != null)
            {
                // 미네랄 체크 먼저
                CoreDataManager coreData = CoreDataManager.Instance;
                if (coreData != null && coreData.region2MineralBar != null)
                {
                    if (coreData.region2MineralBar.GetCurrentMinerals() >= chosen.cost)
                    {
                        Tile targetTile = PickRandomRegion2Tile();
                        if (targetTile != null)
                        {
                            bool summonSuccess = SummonCharacterInRegion2(chosen, targetTile);
                            
                            // 소환 실패 시 짧은 재시도 대기
                            if (!summonSuccess)
                            {
                                Debug.Log($"[Region2AIManager] 소환 실패, {retryInterval}초 후 재시도");
                                yield return new WaitForSeconds(retryInterval);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[Region2AIManager] 미네랄 부족 (현재: {coreData.region2MineralBar.GetCurrentMinerals()}, 필요: {chosen.cost})");
                    }
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// AI 자동 합성 루틴
    /// 기획서: 1성×3 → 2성, 2성×3 → 3성
    /// </summary>
    private IEnumerator AIMergeRoutine()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(mergeCheckInterval);
            
            if (Random.value < autoMergeChance)
            {
                TryAutoMergeArea2Characters();
            }
        }
    }

    /// <summary>
    /// 지역2의 캐릭터들을 자동으로 합성 시도
    /// </summary>
    private void TryAutoMergeArea2Characters()
    {
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // 지역2 캐릭터만 필터링
        List<Character> area2Characters = new List<Character>();
        foreach (var character in allCharacters)
        {
            if (character != null && character.areaIndex == 2 && !character.isHero)
            {
                area2Characters.Add(character);
            }
        }
        
        // 1성 캐릭터 우선 합성 시도
        if (TryMergeByStarLevel(area2Characters, CharacterStar.OneStar))
        {
            Debug.Log("[Region2AIManager] AI가 1성 캐릭터 3개를 합성했습니다!");
            return;
        }
        
        // 2성 캐릭터 합성 시도
        if (TryMergeByStarLevel(area2Characters, CharacterStar.TwoStar))
        {
            Debug.Log("[Region2AIManager] AI가 2성 캐릭터 3개를 합성했습니다!");
            return;
        }
    }

    /// <summary>
    /// 특정 별 등급의 캐릭터들을 합성 시도
    /// </summary>
    private bool TryMergeByStarLevel(List<Character> characters, CharacterStar starLevel)
    {
        // 같은 별 등급의 캐릭터들을 이름별로 그룹화
        Dictionary<string, List<Character>> sameTypeCharacters = new Dictionary<string, List<Character>>();
        
        foreach (var character in characters)
        {
            if (character.star == starLevel)
            {
                if (!sameTypeCharacters.ContainsKey(character.characterName))
                {
                    sameTypeCharacters[character.characterName] = new List<Character>();
                }
                sameTypeCharacters[character.characterName].Add(character);
            }
        }
        
        // 3개 이상인 그룹 찾기
        foreach (var kvp in sameTypeCharacters)
        {
            if (kvp.Value.Count >= 3)
            {
                // 합성할 3개 선택
                List<Character> toMerge = kvp.Value.GetRange(0, 3);
                
                // 가장 위쪽(Y값이 큰) 캐릭터의 타일 찾기
                Character topMostChar = null;
                float highestY = float.MinValue;
                
                foreach (var character in toMerge)
                {
                    if (character.currentTile != null)
                    {
                        RectTransform rect = character.GetComponent<RectTransform>();
                        float y = rect != null ? rect.anchoredPosition.y : character.transform.position.y;
                        
                        if (y > highestY)
                        {
                            highestY = y;
                            topMostChar = character;
                        }
                    }
                }
                
                if (topMostChar != null && topMostChar.currentTile != null)
                {
                    // MergeManager를 통해 합성 실행
                    ExecuteAIMerge(toMerge, topMostChar.currentTile);
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// AI 합성 실행
    /// </summary>
    private void ExecuteAIMerge(List<Character> charactersToMerge, Tile targetTile)
    {
        // MergeManager의 로직을 재사용
        CharacterStar currentStar = charactersToMerge[0].star;
        CharacterStar targetStar = CharacterStar.OneStar;
        
        switch (currentStar)
        {
            case CharacterStar.OneStar:
                targetStar = CharacterStar.TwoStar;
                break;
            case CharacterStar.TwoStar:
                targetStar = CharacterStar.ThreeStar;
                break;
            default:
                return;
        }
        
        // 첫 번째 캐릭터를 업그레이드
        Character baseChar = charactersToMerge[0];
        baseChar.star = targetStar;
        
        // 스탯 업그레이드
        switch (targetStar)
        {
            case CharacterStar.TwoStar:
                baseChar.attackPower *= 1.3f;
                baseChar.attackSpeed *= 1.1f;
                baseChar.attackRange *= 1.1f;
                baseChar.currentHP *= 1.2f;
                break;
            case CharacterStar.ThreeStar:
                baseChar.attackPower *= 1.6f;
                baseChar.attackSpeed *= 1.2f;
                baseChar.attackRange *= 1.2f;
                baseChar.currentHP *= 1.4f;
                break;
        }
        
        // visual 컴포넌트가 초기화될 때까지 잠시 대기 후 ApplyStarVisual 호출
        StartCoroutine(ApplyStarVisualDelayed(baseChar));
        
        // 나머지 캐릭터 제거
        for (int i = 1; i < charactersToMerge.Count; i++)
        {
            if (charactersToMerge[i] != null && charactersToMerge[i].gameObject != null)
            {
                Destroy(charactersToMerge[i].gameObject);
            }
        }
        
        Debug.Log($"[Region2AIManager] AI 합성 완료! {baseChar.characterName}이(가) {targetStar}로 업그레이드됨");
    }

    /// <summary>
    /// DB에서 [0..8] 범위 중 null 아닌 캐릭터들을 골라 무작위 반환
    /// </summary>
    private CharacterData SelectRandomUnit()
    {
        if (opponentCharacterDatabase == null || opponentCharacterDatabase.characters == null)
        {
            Debug.LogWarning("[Region2AIManager] opponentCharacterDatabase 또는 .characters가 null => 소환 불가");
            return null;
        }

        List<CharacterData> validList = new List<CharacterData>();
        for (int i = 0; i < 9; i++)
        {
            if (i < opponentCharacterDatabase.characters.Length)
            {
                CharacterData c = opponentCharacterDatabase.characters[i];
                if (c != null)
                {
                    // 프리팹 유효성 검사
                    if (c.spawnPrefab != null)
                    {
                        // Character 컴포넌트 확인
                        Character charComp = c.spawnPrefab.GetComponent<Character>();
                        if (charComp != null)
                        {
                            validList.Add(c);
                        }
                        else
                        {
                            Debug.LogWarning($"[Region2AIManager] 캐릭터({c.characterName})의 spawnPrefab에 Character 컴포넌트가 없음 => 소환 목록에서 제외");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Region2AIManager] 캐릭터({c.characterName})의 spawnPrefab이 null => 소환 목록에서 제외");
                    }
                }
            }
        }

        if (validList.Count == 0)
        {
            Debug.Log("[Region2AIManager] 소환 가능 캐릭터(0..8)가 모두 null/invalid => 더 이상 소환할 유닛 없음");
            return null;
        }

        int randIdx = Random.Range(0, validList.Count);
        CharacterData selected = validList[randIdx];
        Debug.Log($"[Region2AIManager] 선택된 랜덤 캐릭터: {selected.characterName}, spawnPrefab={(selected.spawnPrefab != null ? selected.spawnPrefab.name : "null")}, "
                 + $"cost={selected.cost}, attackPower={selected.attackPower}");
        return selected;
    }

    /// <summary>
    /// walkable2 3개 루트 중 하나 선택 후 타일 / placable2Tiles 중 랜덤
    /// </summary>
    private Tile PickRandomRegion2Tile()
    {
        // walkable2 타일들을 하나의 리스트로 합침
        List<Tile> allWalkable2Tiles = new List<Tile>();
        if (walkable2LeftTiles != null) allWalkable2Tiles.AddRange(walkable2LeftTiles);
        if (walkable2CenterTiles != null) allWalkable2Tiles.AddRange(walkable2CenterTiles);
        if (walkable2RightTiles != null) allWalkable2Tiles.AddRange(walkable2RightTiles);
        
        bool hasWalkable2 = allWalkable2Tiles.Count > 0;
        bool hasPlacable2 = (placable2Tiles != null && placable2Tiles.Count > 0);

        if (!hasWalkable2 && !hasPlacable2)
        {
            Debug.LogError("[Region2AIManager] 소환 가능한 타일이 없습니다. walkable2 루트 타일들과 placable2Tiles를 Inspector에서 설정해주세요.");
            return null;
        }

        // 빈 타일 찾기
        List<Tile> emptyTiles = new List<Tile>();
        
        // walkable2 타일 중 빈 타일 추가
        foreach (var tile in allWalkable2Tiles)
        {
            if (tile != null && tile.CanPlaceCharacter() && !IsOccupiedByCharacter(tile))
            {
                emptyTiles.Add(tile);
            }
        }
        
        // placable2 타일 중 빈 타일 추가
        foreach (var tile in placable2Tiles)
        {
            if (tile != null && tile.CanPlaceCharacter() && !IsOccupiedByCharacter(tile))
            {
                emptyTiles.Add(tile);
            }
        }
        
        if (emptyTiles.Count > 0)
        {
            return emptyTiles[Random.Range(0, emptyTiles.Count)];
        }
        
        Debug.Log("[Region2AIManager] 모든 타일이 점유됨. 첫 번째 사용 가능한 타일 반환");
        
        // 모든 타일이 점유된 경우, 첫 번째 사용 가능한 타일 반환
        if (allWalkable2Tiles.Count > 0) return allWalkable2Tiles[0];
        if (placable2Tiles.Count > 0) return placable2Tiles[0];
        
        return null;
    }

    /// <summary>
    /// chosen == AI가 골라낸 CharacterData
    /// => DB의 characters[] 중에서 인덱스 찾기 → PlacementManager 소환
    /// 
    /// [수정] PlacementManager.SummonCharacterOnTile의 반환값을 체크하고,
    /// forceEnemyArea2=true를 전달하도록 수정
    /// </summary>
    private bool SummonCharacterInRegion2(CharacterData chosen, Tile tile)
    {
        if (chosen == null || tile == null)
        {
            return false;
        }

        Debug.Log($"[Region2AIManager] SummonCharacterInRegion2() => chosen={chosen.characterName}, tile={tile.name}");

        // DB에서 인덱스 찾기
        int foundIndex = System.Array.IndexOf(opponentCharacterDatabase.characters, chosen);
        if (foundIndex < 0)
        {
            Debug.LogWarning($"[Region2AIManager] DB에서 {chosen.characterName} 인덱스를 못찾음 => 소환 실패");
            return false;
        }

        // PlacementManager를 통해 소환 시도
        // forceEnemyArea2=true를 전달하여 지역2에 강제 소환
        if (PlacementManager.Instance != null)
        {
            // 미네랄 소비
            CoreDataManager coreData = CoreDataManager.Instance;
            if (coreData != null && coreData.region2MineralBar != null)
            {
                if (!coreData.region2MineralBar.TrySpend(chosen.cost))
                {
                    Debug.LogWarning($"[Region2AIManager] 미네랄 부족으로 소환 실패");
                    return false;
                }
            }
            
            bool success = PlacementManager.Instance.SummonCharacterOnTile(foundIndex, tile, true);
            
            if (success)
            {
                Debug.Log($"[Region2AIManager] <color=magenta>AI 소환 성공</color> => {chosen.characterName}, tile={tile.name}, cost={chosen.cost}");
            }
            else
            {
                Debug.Log($"[Region2AIManager] 소환 실패 => {chosen.characterName}, tile={tile.name}");
                
                // 소환 실패 시 미네랄 환불
                if (coreData != null && coreData.region2MineralBar != null)
                {
                    coreData.region2MineralBar.Refund(chosen.cost);
                }
            }
            
            return success;
        }
        else
        {
            Debug.LogError("[Region2AIManager] PlacementManager.Instance가 null입니다!");
            return false;
        }
    }

    /// <summary>
    /// 코루틴 중단 등
    /// </summary>
    private void OnDisable()
    {
        isRunning = false;
    }

    private IEnumerator ApplyStarVisualDelayed(Character character)
    {
        yield return new WaitForSeconds(0.1f); // 잠시 대기
        character.ApplyStarVisual();
    }

    /// <summary>
    /// 타일이 이미 다른 캐릭터에 의해 점유되었는지 확인
    /// </summary>
    private bool IsOccupiedByCharacter(Tile tile)
    {
        if (tile == null) return false;
        
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character c in allCharacters)
        {
            if (c != null && c.currentTile == tile)
            {
                return true;
            }
        }
        return false;
    }
}