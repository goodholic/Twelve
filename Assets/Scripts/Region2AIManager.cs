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

    [Header("지역2 타일들 (Walkable2)")]
    public List<Tile> walkable2Tiles = new List<Tile>();

    [Header("지역2 타일들 (Placable2)")]
    public List<Tile> placable2Tiles = new List<Tile>();

    [Header("Opponent Hero Panel (10번째 캐릭터 전용)")]
    public RectTransform opponentHeroPanel;

    private bool isRunning = false;

    private void Start()
    {
        // (A) PlacementManager에 적 DB 연결(있다면)
        if (PlacementManager.Instance != null && opponentCharacterDatabase != null)
        {
            PlacementManager.Instance.enemyDatabase = opponentCharacterDatabase;
            Debug.Log("[Region2AIManager] PlacementManager.enemyDatabase에 opponentCharacterDatabase를 연결했습니다.");
            
            // UI 패널 상태 확인
            RectTransform opponentCharPanel = PlacementManager.Instance.opponentCharacterPanel;
            RectTransform opponentBulletPanel = PlacementManager.Instance.opponentBulletPanel;
            RectTransform opponentMonsterPanel = PlacementManager.Instance.opponentOurMonsterPanel;
            
            Debug.Log($"[Region2AIManager] UI 패널 상태: opponentCharPanel={opponentCharPanel != null}, " +
                    $"opponentBulletPanel={opponentBulletPanel != null}, opponentMonsterPanel={opponentMonsterPanel != null}");
            
            // 씬의 모든 Canvas 확인
            Canvas[] allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"[Region2AIManager] 씬에 존재하는 Canvas 개수: {allCanvases.Length}");
            for (int i = 0; i < allCanvases.Length; i++)
            {
                Canvas c = allCanvases[i];
                Debug.Log($"[Region2AIManager] Canvas[{i}]: {c.name}, RenderMode={c.renderMode}, " +
                        $"OverrideSorting={c.overrideSorting}, SortingOrder={c.sortingOrder}");
            }
        }

        // (B) 지역2 히어로(인덱스=9) 즉시 생성 시도
        SpawnRegion2HeroImmediately();

        // (C) 일반 AI 루틴(0~8번) 시작
        StartCoroutine(AIRoutine());
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
                    
                    // 미네랄이 부족하여 소환이 실패했다면 더 짧은 시간 후에 재시도
                    if (!summonSuccess)
                    {
                        yield return new WaitForSeconds(retryInterval);
                        continue; // 다음 루프 반복으로 바로 이동
                    }
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
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
                // 프리팹 유효성 검사 추가
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
        Debug.Log($"[Region2AIManager] 선택된 랜덤 캐릭터: {selected.characterName}, spawnPrefab={selected.spawnPrefab.name}, " + 
                 $"cost={selected.cost}, attackPower={selected.attackPower}");
        return selected;
    }

    /// <summary>
    /// walkable2Tiles / placable2Tiles 중 랜덤
    /// </summary>
    private Tile PickRandomRegion2Tile()
    {
        bool hasWalkable2 = (walkable2Tiles != null && walkable2Tiles.Count > 0);
        bool hasPlacable2 = (placable2Tiles != null && placable2Tiles.Count > 0);

        if (!hasWalkable2 && !hasPlacable2)
        {
            Debug.LogError("[Region2AIManager] 소환 가능한 타일이 없습니다. walkable2Tiles와 placable2Tiles를 Inspector에서 설정해주세요.");
            return null;
        }

        // 타일 선택 - 랜덤 선택 전에 유효성 확인
        Tile selectedTile = null;
        
        // 최대 5번 시도
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (hasWalkable2 && hasPlacable2)
            {
                if (Random.value < 0.5f)
                    selectedTile = walkable2Tiles[Random.Range(0, walkable2Tiles.Count)];
                else
                    selectedTile = placable2Tiles[Random.Range(0, placable2Tiles.Count)];
            }
            else if (hasWalkable2)
            {
                selectedTile = walkable2Tiles[Random.Range(0, walkable2Tiles.Count)];
            }
            else
            {
                selectedTile = placable2Tiles[Random.Range(0, placable2Tiles.Count)];
            }
            
            // 선택된 타일이 유효한지 확인
            if (selectedTile != null && selectedTile.CanPlaceCharacter())
            {
                // 타일이 이미 다른 캐릭터에 의해 점유되었는지 확인
                bool isOccupied = IsOccupiedByCharacter(selectedTile);
                if (!isOccupied)
                {
                    Debug.Log($"[Region2AIManager] 유효한 타일 찾음: {selectedTile.name} (시도 {attempt+1}/5)");
                    return selectedTile;
                }
            }
        }
        
        // 모든 시도 후에도 적합한 타일을 찾지 못했으면 첫 번째 타일 반환
        if (hasWalkable2)
        {
            Debug.LogWarning("[Region2AIManager] 적합한 타일을 찾지 못해 첫 번째 walkable2 타일 사용");
            return walkable2Tiles[0];
        }
        else
        {
            Debug.LogWarning("[Region2AIManager] 적합한 타일을 찾지 못해 첫 번째 placable2 타일 사용");
            return placable2Tiles[0];
        }
    }
    
    /// <summary>
    /// 타일이 이미 다른 캐릭터에 의해 점유되었는지 확인
    /// </summary>
    private bool IsOccupiedByCharacter(Tile tile)
    {
        if (tile == null) return false;
        
        Character[] allCharacters = GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None);
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
    /// => DB의 characters[] 중에서 인덱스 찾기 → PlacementManager 소환
    /// </summary>
    private bool SummonCharacterInRegion2(CharacterData chosen, Tile tile)
    {
        Debug.Log($"[Region2AIManager] SummonCharacterInRegion2() => chosen={chosen?.characterName}, tile={tile?.name}");

        if (chosen == null)
        {
            Debug.LogWarning("[Region2AIManager] 소환할 캐릭터가 null");
            return false;
        }
        if (tile == null)
        {
            Debug.LogWarning("[Region2AIManager] 소환할 타일이 null");
            return false;
        }

        // 추가 디버그 정보
        Debug.Log($"[Region2AIManager] 캐릭터 상세정보: spawnPrefab={(chosen.spawnPrefab != null ? "존재함" : "null")}, " +
                 $"attackPower={chosen.attackPower}, attackRange={chosen.attackRange}, cost={chosen.cost}");

        // PlacementManager 유효성 검증
        if (PlacementManager.Instance == null)
        {
            Debug.LogError("[Region2AIManager] PlacementManager.Instance가 null입니다.");
            return false;
        }
        
        // 미네랄 소비 체크
        if (PlacementManager.Instance.region2MineralBar != null)
        {
            bool canSpend = PlacementManager.Instance.region2MineralBar.TrySpend(chosen.cost);
            if (!canSpend)
            {
                Debug.LogWarning($"[Region2AIManager] 미네랄 부족 => {chosen.characterName} 소환 불가");
                return false;
            }
        }
        else
        {
            Debug.LogWarning("[Region2AIManager] region2MineralBar가 존재하지 않습니다");
            return false;
        }

        int foundIndex = System.Array.IndexOf(opponentCharacterDatabase.characters, chosen);
        if (foundIndex < 0)
        {
            Debug.LogWarning($"[Region2AIManager] DB에서 {chosen.characterName} 인덱스를 찾지 못함 => 소환 실패");
            return false;
        }

        // 타일 위 다른 캐릭터(occupant) 있는지 검사 -> 합성
        Character occupantChar = null;
        Character[] allCharacters = GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character c in allCharacters)
        {
            if (c != null && c.currentTile == tile)
            {
                occupantChar = c;
                break;
            }
        }

        if (occupantChar != null)
        {
            // 이미 존재하는 캐릭터와 합성 시도
            if (!occupantChar.isHero
                && (int)occupantChar.star < 3
                && (int)occupantChar.star == (int)chosen.initialStar)
            {
                occupantChar.star++;
                occupantChar.currentHP = chosen.maxHP;
                occupantChar.ApplyStarVisual();
                Debug.Log($"[Region2AIManager] 타일({tile.name}) 위 캐릭터와 합성 완료 => 현재 스타: {occupantChar.star}");
            }
            else
            {
                Debug.Log($"[Region2AIManager] 타일({tile.name})에 다른 캐릭터가 있어 소환/합성 불가 => {occupantChar.characterName} (star={occupantChar.star})");
            }
            return true;
        }

        // 비었으면 정상 소환 시도
        try
        {
            if (tile.CanPlaceCharacter())
            {
                // AI는 지역2에만 소환 가능
                if (tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2())
                {
                    // UI 패널 검증
                    bool hasOpponentCharPanel = PlacementManager.Instance.opponentCharacterPanel != null;
                    bool hasOpponentBulletPanel = PlacementManager.Instance.opponentBulletPanel != null;
                    bool hasOpponentMonsterPanel = PlacementManager.Instance.opponentOurMonsterPanel != null;
                    Debug.Log($"[Region2AIManager] 소환 전 UI 패널 상태: opponentCharacterPanel={hasOpponentCharPanel}, " +
                             $"opponentBulletPanel={hasOpponentBulletPanel}, opponentOurMonsterPanel={hasOpponentMonsterPanel}");
                    
                    // 실제 소환 시도
                    PlacementManager.Instance.SummonCharacterOnTile(foundIndex, tile, forceEnemyArea2: true);
                    Debug.Log($"[Region2AIManager] <color=magenta>AI 소환 성공</color> => {chosen.characterName}, tile={tile.name}, cost={chosen.cost}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[Region2AIManager] 타일({tile.name})은 지역2 타일이 아닙니다! IsWalkable2={tile.IsWalkable2()}, IsPlacable2={tile.IsPlacable2()}, IsPlaced2={tile.IsPlaced2()}");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[Region2AIManager] 타일({tile.name})에 캐릭터를 배치할 수 없습니다. CanPlaceCharacter() = false");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Region2AIManager] 캐릭터 소환 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}
