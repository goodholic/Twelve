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
            
            // UI 패널 상태 확인
            RectTransform opponentCharPanel = PlacementManager.Instance.opponentCharacterPanel;
            RectTransform opponentBulletPanel = PlacementManager.Instance.opponentBulletPanel;
            RectTransform opponentMonsterPanel = PlacementManager.Instance.opponentOurMonsterPanel;
            
            Debug.Log(""
                + "[Region2AIManager] UI 패널 상태: "
                + $"opponentCharPanel={(opponentCharPanel != null)}, "
                + $"opponentBulletPanel={(opponentBulletPanel != null)}, "
                + $"opponentMonsterPanel={(opponentMonsterPanel != null)}"
            );
            
            // 씬의 모든 Canvas 확인(디버그)
            Canvas[] allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"[Region2AIManager] 씬에 존재하는 Canvas 개수: {allCanvases.Length}");
            for (int i = 0; i < allCanvases.Length; i++)
            {
                Canvas c = allCanvases[i];
                Debug.Log($"[Region2AIManager] Canvas[{i}]: {c.name}, RenderMode={c.renderMode}, "
                         + $"OverrideSorting={c.overrideSorting}, SortingOrder={c.sortingOrder}");
            }
        }

        // (B) 지역2 히어로(인덱스=9) 즉시 생성 시도
        SpawnRegion2HeroImmediately();

        // (C) 일반 AI 루틴(0~8번) 시작
        StartCoroutine(AIRoutine());
        
        // (D) AI 합성 루틴 시작
        StartCoroutine(AIMergeRoutine());
    }

    /// <summary>
    /// 인덱스 0~8은 일반 유닛 / 9번은 히어로
    /// 여기서 9번 캐릭터(히어로)가 null이 아니면 즉시 생성 후 계속 유지
    /// </summary>
    private void SpawnRegion2HeroImmediately()
    {
        // 1) DB 및 배열 길이 확인
        if (opponentCharacterDatabase == null || opponentCharacterDatabase.characters == null)
        {
            Debug.LogWarning("[Region2AIManager] opponentCharacterDatabase 또는 .characters가 null입니다. (지역2 히어로 생성 불가)");
            return;
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

        // 3) opponentHeroPanel 체크
        if (opponentHeroPanel == null)
        {
            Debug.LogWarning("[Region2AIManager] opponentHeroPanel이 null => 지역2 히어로 생성 불가");
            return;
        }

        // 4) 실제 생성
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

        // 5) 캐릭터 설정
        Character heroComp = heroObj.GetComponent<Character>();
        if (heroComp != null)
        {
            heroComp.isHero = true;
            heroComp.areaIndex = 2;

            heroComp.attackPower = heroCandidate.attackPower;
            heroComp.attackSpeed = heroCandidate.attackSpeed;
            heroComp.attackRange = heroCandidate.attackRange;
            heroComp.currentHP   = heroCandidate.maxHP;
            heroComp.star        = heroCandidate.initialStar;
            heroComp.ApplyStarVisual();

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
                Tile targetTile = PickRandomRegion2Tile();
                if (targetTile != null)
                {
                    bool summonSuccess = SummonCharacterInRegion2(chosen, targetTile);
                    
                    // 미네랄이 부족하거나 합성 실패 등으로 소환이 불발되면 -> 잠시 후 재시도
                    if (!summonSuccess)
                    {
                        yield return new WaitForSeconds(retryInterval);
                        continue;
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
    /// 특정 별 등급의 캐릭터 3개를 찾아서 합성 시도
    /// </summary>
    private bool TryMergeByStarLevel(List<Character> characters, CharacterStar starLevel)
    {
        // 같은 별 등급의 캐릭터들을 그룹핑
        Dictionary<string, List<Character>> characterGroups = new Dictionary<string, List<Character>>();
        
        foreach (var character in characters)
        {
            if (character.star == starLevel)
            {
                string key = character.characterName;
                if (!characterGroups.ContainsKey(key))
                {
                    characterGroups[key] = new List<Character>();
                }
                characterGroups[key].Add(character);
            }
        }
        
        // 3개 이상인 그룹 찾기
        foreach (var group in characterGroups)
        {
            if (group.Value.Count >= 3)
            {
                // 3개만 선택
                List<Character> toMerge = group.Value.GetRange(0, 3);
                
                // 가장 뒤쪽(Y좌표가 높은) 캐릭터 위치에 합성
                Character topMostChar = null;
                float highestY = float.MinValue;
                
                foreach (var character in toMerge)
                {
                    RectTransform rect = character.GetComponent<RectTransform>();
                    float y = rect != null ? rect.anchoredPosition.y : character.transform.position.y;
                    
                    if (y > highestY)
                    {
                        highestY = y;
                        topMostChar = character;
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
        
        baseChar.ApplyStarVisual();
        
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

        Tile selectedTile = null;
        
        // 최대 5번 시도
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (hasWalkable2 && hasPlacable2)
            {
                if (Random.value < 0.5f)
                {
                    // walkable2에서 선택할 때는 루트를 먼저 선택
                    RouteType selectedRoute = GetRandomRoute();
                    selectedTile = GetWalkable2TileForRoute(selectedRoute);
                }
                else
                {
                    selectedTile = placable2Tiles[Random.Range(0, placable2Tiles.Count)];
                }
            }
            else if (hasWalkable2)
            {
                // walkable2에서 선택할 때는 루트를 먼저 선택
                RouteType selectedRoute = GetRandomRoute();
                selectedTile = GetWalkable2TileForRoute(selectedRoute);
            }
            else
            {
                selectedTile = placable2Tiles[Random.Range(0, placable2Tiles.Count)];
            }
            
            if (selectedTile != null && selectedTile.CanPlaceCharacter())
            {
                // 이미 다른 캐릭터가 점유 중인지 검사
                if (!IsOccupiedByCharacter(selectedTile))
                {
                    Debug.Log($"[Region2AIManager] 유효한 타일 찾음: {selectedTile.name} (시도 {attempt+1}/5)");
                    return selectedTile;
                }
            }
        }
        
        // 결국 적합한 타일을 못 찾으면 랜덤 루트의 첫 번째를 사용
        if (hasWalkable2)
        {
            RouteType fallbackRoute = GetRandomRoute();
            Debug.LogWarning($"[Region2AIManager] 적합한 타일을 찾지 못해 {fallbackRoute} 루트의 첫 번째 walkable2 타일 사용");
            return GetWalkable2TileForRoute(fallbackRoute);
        }
        else
        {
            Debug.LogWarning("[Region2AIManager] 적합한 타일을 찾지 못해 첫 번째 placable2 타일 사용");
            return placable2Tiles[0];
        }
    }
    
    /// <summary>
    /// 랜덤하게 루트(좌/중/우)를 선택합니다.
    /// </summary>
    private RouteType GetRandomRoute()
    {
        int randomValue = Random.Range(0, 3);
        return (RouteType)randomValue;
    }
    
    /// <summary>
    /// 선택된 루트에 해당하는 walkable2 타일을 반환합니다.
    /// </summary>
    private Tile GetWalkable2TileForRoute(RouteType route)
    {
        List<Tile> routeTiles = null;
        
        switch (route)
        {
            case RouteType.Left:
                routeTiles = walkable2LeftTiles;
                break;
            case RouteType.Center:
                routeTiles = walkable2CenterTiles;
                break;
            case RouteType.Right:
                routeTiles = walkable2RightTiles;
                break;
        }
        
        if (routeTiles != null && routeTiles.Count > 0)
        {
            return routeTiles[Random.Range(0, routeTiles.Count)];
        }
        
        // 해당 루트에 타일이 없으면 다른 루트에서 찾기
        if (walkable2LeftTiles != null && walkable2LeftTiles.Count > 0)
            return walkable2LeftTiles[Random.Range(0, walkable2LeftTiles.Count)];
        if (walkable2CenterTiles != null && walkable2CenterTiles.Count > 0)
            return walkable2CenterTiles[Random.Range(0, walkable2CenterTiles.Count)];
        if (walkable2RightTiles != null && walkable2RightTiles.Count > 0)
            return walkable2RightTiles[Random.Range(0, walkable2RightTiles.Count)];
            
        return null;
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

    /// <summary>
    /// chosen == AI가 골라낸 CharacterData
    /// => DB의 characters[] 중에서 인덱스 찾기 → PlacementManager 소환 OR 합성
    /// 
    /// [수정] 실제로 합성 혹은 소환이 **확정**되기 전에 미네랄을 소모하지 않도록 수정함.
    /// </summary>
    private bool SummonCharacterInRegion2(CharacterData chosen, Tile tile)
    {
        if (chosen == null)
        {
            return false;
        }
        if (tile == null)
        {
            return false;
        }

        Debug.Log($"[Region2AIManager] SummonCharacterInRegion2() => chosen={chosen.characterName}, tile={tile.name}");

        // 1) 타일 위에 이미 캐릭터가 있는지(occupant) 검사
        Character occupantChar = null;
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c == null) continue;
            if (c.currentTile == tile)
            {
                occupantChar = c;
                break;
            }
        }

        // 2) occupant가 있으면 합성 시도
        if (occupantChar != null)
        {
            // (a) 별이 같고 3성 미만이면 합성 가능
            if (!occupantChar.isHero
                && (int)occupantChar.star < 3
                && occupantChar.star == chosen.initialStar)
            {
                // 합성 처리 (미네랄 체크는 별도 시스템에서 처리)
                occupantChar.star++;
                occupantChar.currentHP = chosen.maxHP;
                occupantChar.ApplyStarVisual();
                Debug.Log($"[Region2AIManager] 타일({tile.name}) 위 캐릭터와 합성 완료 => 현재 스타: {occupantChar.star}");
                return true;
            }
            else
            {
                Debug.Log($"[Region2AIManager] 타일({tile.name})에 있는 캐릭터와 합성 불가 => 소환 취소");
                return false;
            }
        }
        else
        {
            // occupant가 없는 타일 => 새로 소환
            if (!tile.CanPlaceCharacter())
            {
                Debug.LogWarning($"[Region2AIManager] {tile.name}에 배치 불가 => 소환 취소");
                return false;
            }

            // 소환 직전 타일 상태 체크
            Debug.Log($"[Region2AIManager] 소환 전 타일 상태: IsWalkable2={tile.IsWalkable2()}, IsPlacable2={tile.IsPlacable2()}, IsPlaced2={tile.IsPlaced2()}");

            // DB에서 인덱스 찾기
            int foundIndex = System.Array.IndexOf(opponentCharacterDatabase.characters, chosen);
            if (foundIndex < 0)
            {
                Debug.LogWarning($"[Region2AIManager] DB에서 {chosen.characterName} 인덱스를 못찾음 => 소환 실패");
                return false;
            }

            // 소환 직전에 재확인
            bool isValidTile = tile.CanPlaceCharacter() && 
                              (tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2());
                              
            if (!isValidTile)
            {
                Debug.LogWarning($"[Region2AIManager] {tile.name}에 최종 배치 불가 판정 => 소환 취소");
                return false;
            }

            // 이제 실제 소환
            Debug.Log($"[Region2AIManager] (소환) => PlacementManager.SummonCharacterOnTile({chosen.characterName}), tile={tile.name}");
            
            PlacementManager.Instance.SummonCharacterOnTile(foundIndex, tile);
            Debug.Log($"[Region2AIManager] <color=magenta>AI 소환 성공</color> => {chosen.characterName}, tile={tile.name}, cost={chosen.cost}");
            return true;
        }
    }

    /// <summary>
    /// 코루틴 중단 등
    /// </summary>
    private void OnDisable()
    {
        isRunning = false;
    }
}