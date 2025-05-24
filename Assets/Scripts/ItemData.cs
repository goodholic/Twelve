using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 아이템 효과 종류
/// </summary>
public enum ItemEffectType
{
    IncreaseAttack,   // 공격력 증가
    IncreaseHP,       // 체력 증가
    IncreaseRange,    // 사거리 증가

    // ▼▼ [신규 추가] 아이템 효과 3종 ▼▼
    TeleportJumpedEnemies, // (1) 점프해온 일정 범위 적 몬스터를 무작위 위치로 이동
    DamageJumpedEnemies,   // (2) 점프해온 일정 범위 적 몬스터에게 데미지
    SummonRandom2Or3Star   // (3) 2~3성 유닛 중 랜덤으로 소환
    // ▲▲ [신규 추가 끝] ▲▲
}

[System.Serializable]
public class ItemData
{
    [Header("기본 정보")]
    public string itemName;      // 아이템 이름
    public Sprite itemIcon;      // 인벤토리/카드 등에 표시할 아이콘
    public ItemEffectType effectType;
    public float effectValue;    // 효과값 (예: 공격력+10, 체력+50, 사거리+1.0 등)
    
    [TextArea]
    public string description;   // 아이템 설명

    // =============================================
    // ▼▼ [신규 추가] 광역 범위 / 데미지값 / 기타용 필드 ▼▼
    // (필요에 따라 추가 사용)
    [Header("추가 필드 (옵션)")]
    [Tooltip("범위(반경) 값. '점프해온 몬스터' 찾을 때 사용")]
    public float areaRadius = 3f;

    [Tooltip("소환할 캐릭터의 별(2성/3성) 범위를 구분하거나, 데미지용으로도 활용 가능")]
    public int starMin = 2;
    public int starMax = 3;

    [Tooltip("데미지 줄 때 사용할 값. (effectValue를 데미지가 아닌, 범위나 다른 용도로 쓰는 경우 대비)")]
    public float damageValue = 30f;
    // ▲▲ [신규 추가 끝] ▲▲
    // =============================================

    /// <summary>
    /// 아이템을 캐릭터에게 적용하는 메서드.
    /// (주의) 'TeleportJumpedEnemies', 'DamageJumpedEnemies'와 같이
    ///  다수 대상(점프해온 적)을 처리할 때는 target을 중심으로 범위 검색하거나,
    ///  혹은 areaIndex 등을 이용해 적을 찾을 수 있음.
    /// 
    /// effectValue가 0 이하인 경우 기본적으로 적용하지 않는다.
    /// 적용 성공 시 true 반환.
    /// </summary>
    /// <param name="target">아이템을 사용할 때 클릭된 캐릭터 (없을 수도 있음)</param>
    /// <returns>효과 적용 성공 여부</returns>
    public bool ApplyEffectToCharacter(Character target)
    {
        // (기존) 기본 유효성 체크
        if (target == null)
        {
            Debug.LogWarning($"[ItemData] {itemName} 은(는) 대상 캐릭터가 null입니다.");
            return false;
        }

        // (기존) 효과값 <= 0이면 적용 안 함
        if (effectValue <= 0f &&
            (effectType == ItemEffectType.IncreaseAttack 
          || effectType == ItemEffectType.IncreaseHP
          || effectType == ItemEffectType.IncreaseRange))
        {
            Debug.LogWarning($"[ItemData] {itemName} 은(는) 효과값이 0 이하이므로 적용 불가.");
            return false;
        }

        // ================ 기존 효과 (스위치문) ================
        switch (effectType)
        {
            case ItemEffectType.IncreaseAttack:
                target.attackPower += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 공격력 +{effectValue}, 결과={target.attackPower}");
                break;

            case ItemEffectType.IncreaseHP:
                // 체력(현재 HP)을 추가로 회복할 수도 있고, maxHP를 올릴 수도 있음.
                // 여기서는 단순히 '현재 체력' 증가라고 가정:
                float oldHP = target.currentHP;
                target.currentHP = Mathf.Min(target.currentHP + effectValue, target.currentHP + 9999); // 예: 과도한 회복 제한
                Debug.Log($"[ItemData] {itemName} 사용 -> HP +{effectValue}, (이전={oldHP}, 현재={target.currentHP})");
                break;

            case ItemEffectType.IncreaseRange:
                float oldRange = target.attackRange;
                target.attackRange += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 사거리 +{effectValue}, (이전={oldRange}, 현재={target.attackRange})");
                break;


            // ▼▼ [신규 추가] 3가지 효과 ▼▼
            case ItemEffectType.TeleportJumpedEnemies:
            {
                // (1) 점프해온 일정 범위 적 몬스터(또는 캐릭터)를 찾아
                //     우리 지역(=target.areaIndex) 중 무작위 타일로 이동
                //     * areaRadius 사용

                // 1) 대상 지역(=target.areaIndex)과 반대쪽에서 점프해온 캐릭터를 찾는다
                //    "hasCrossedRegion==true && areaIndex != target.areaIndex" 조건
                List<Character> jumpedEnemies = FindJumpedEnemiesInRange(target.transform.position, target.areaIndex, areaRadius);

                if (jumpedEnemies.Count == 0)
                {
                    Debug.Log($"[ItemData] {itemName} 사용 -> 범위 내 점프해온 적이 없음");
                    return false;
                }

                // 2) 점프해온 적들을 '우리 지역'의 랜덤 타일로 강제 이동
                //    여기서는 'PlacementManager'의 tile 중 "target.areaIndex"에 해당하는 Walkable/Placable 등을 찾는다
                MoveJumpedEnemiesToRandomTile(jumpedEnemies, target.areaIndex);

                Debug.Log($"[ItemData] {itemName} 사용 -> 점프해온 적 {jumpedEnemies.Count}명 위치를 우리 지역으로 랜덤 텔레포트 완료");
                break;
            }

            case ItemEffectType.DamageJumpedEnemies:
            {
                // (2) 점프해온 일정 범위 적 몬스터에게 데미지 주기
                //    damageValue 사용 (기본 30f 등)

                List<Character> jumpedEnemies = FindJumpedEnemiesInRange(target.transform.position, target.areaIndex, areaRadius);
                if (jumpedEnemies.Count == 0)
                {
                    Debug.Log($"[ItemData] {itemName} 사용 -> 범위 내 점프해온 적이 없음");
                    return false;
                }

                foreach (Character c in jumpedEnemies)
                {
                    c.TakeDamage(damageValue);
                }
                Debug.Log($"[ItemData] {itemName} 사용 -> {jumpedEnemies.Count}명의 점프해온 적에게 {damageValue}씩 데미지 부여");
                break;
            }

            case ItemEffectType.SummonRandom2Or3Star:
            {
                // (3) 2~3성 유닛 중에서 랜덤으로 소환
                //     => "PlacementManager"를 이용해, 해당 캐릭터(target) 근처 타일 등에 소환하거나,
                //        혹은 임의의 '우리 지역' 타일에 소환 가능
                // 여기서는 단순히: starMin~starMax 범위(2~3)의 임의 스타를 골라
                // starMergeDatabase에서 랜덤 캐릭터 하나 뽑고 -> target.areaIndex에 배치

                int randomStar = Random.Range(starMin, starMax + 1);
                if (randomStar < 2) randomStar = 2;
                if (randomStar > 3) randomStar = 3;

                // PlacementManager에서 starMergeDatabase 참조
                StarMergeDatabaseObject starDB = (target.areaIndex == 2 && PlacementManager.Instance.starMergeDatabaseRegion2 != null)
                    ? PlacementManager.Instance.starMergeDatabaseRegion2
                    : PlacementManager.Instance.starMergeDatabase;

                if (starDB == null)
                {
                    Debug.LogWarning($"[ItemData] {itemName} 사용 -> starMergeDatabase(지역{target.areaIndex})가 null!");
                    return false;
                }

                // 종족은 target.race로 통일한다고 가정(혹은 아무나 뽑을 수도 있음)
                RaceType raceType = (RaceType)target.race;
                CharacterData newCD = null;

                if (randomStar == 2)
                {
                    newCD = starDB.GetRandom2Star(raceType);
                }
                else // 3성
                {
                    newCD = starDB.GetRandom3Star(raceType);
                }

                if (newCD == null)
                {
                    Debug.LogWarning($"[ItemData] {itemName} 사용 -> {randomStar}성 {raceType} 캐릭터를 뽑지 못함");
                    return false;
                }

                // 실제 소환
                // PlacementManager에 'SummonCharacterOnTile' 호출 or 'PlaceCharacterOnTile' 호출
                // 여기서는 임의로 '우리 지역'의 placable 타일 중 하나 찾아 소환
                bool success = SummonRandomUnitInArea(newCD, target.areaIndex);
                if (!success)
                {
                    Debug.LogWarning($"[ItemData] {itemName} 사용 -> 소환 실패(타일없음?)");
                    return false;
                }

                Debug.Log($"[ItemData] {itemName} 사용 -> {randomStar}성 {raceType} 캐릭터 '{newCD.characterName}' 소환 완료");
                break;
            }
            // ▲▲ [신규 추가 끝] ▲▲

            default:
                Debug.LogWarning($"[ItemData] {itemName} 은(는) 알 수 없는 ItemEffectType: {effectType}");
                return false;
        }

        return true; // 성공 적용
    }

    // =========================================================================
    // ▼▼ [신규 추가] 내부 유틸 함수: '점프해온 적' 찾기, 이동, 소환 로직 등 ▼▼
    // =========================================================================

    /// <summary>
    /// 해당 위치(pos)를 중심으로, 일정 반경(radius) 안에 있는 "점프해온 적" 캐릭터를 찾는다.
    /// - '점프해온 적' = hasCrossedRegion == true && areaIndex != myArea
    /// </summary>
    private List<Character> FindJumpedEnemiesInRange(Vector3 pos, int myArea, float radius)
    {
        List<Character> result = new List<Character>();

        // 모든 Character 컴포넌트 찾기 (deprecated 메서드 수정)
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);

        foreach (Character c in allChars)
        {
            if (c == null) continue;

            // 점프해온 캐릭터인지 확인 (hasCrossedRegion 대신 다른 방법 사용)
            // 우선 areaIndex가 다른 캐릭터를 점프해온 것으로 간주
            if (c.areaIndex != myArea)
            {
                // 범위 체크
                float distance = Vector3.Distance(pos, c.transform.position);
                if (distance <= radius)
                {
                    result.Add(c);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// jumpedEnemies 목록을 '우리 지역'의 랜덤 타일로 이동시키는 함수
    /// </summary>
    private void MoveJumpedEnemiesToRandomTile(List<Character> jumpedEnemies, int myArea)
    {
        if (jumpedEnemies == null || jumpedEnemies.Count == 0) return;

        // 대상 지역(myArea)에 있는 배치 가능한 타일들 찾기
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> availableTiles = new List<Tile>();

        foreach (Tile t in allTiles)
        {
            if (t == null) continue;

            // 내 지역의 Walkable/Placable 타일만 가져오기
            bool isMyRegion = (myArea == 1) ? !t.isRegion2 : t.isRegion2;
            if (isMyRegion && (t.IsWalkable() || t.IsPlacable()))
            {
                availableTiles.Add(t);
            }
        }

        if (availableTiles.Count == 0)
        {
            Debug.LogWarning($"[ItemData] 지역 {myArea}에 이동 가능한 타일이 없습니다!");
            return;
        }

        // 점프해온 적들을 랜덤 타일로 이동
        foreach (Character c in jumpedEnemies)
        {
            if (c != null)
            {
                Tile randomTile = availableTiles[Random.Range(0, availableTiles.Count)];
                
                // 이전 타일에서 제거
                if (c.currentTile != null && PlacementManager.Instance != null)
                {
                    PlacementManager.Instance.RemovePlaceTileChild(c.currentTile);
                }
                
                // 새 위치로 이동
                c.transform.position = randomTile.transform.position;
                c.currentTile = randomTile;

                // PlacementManager의 OnDropCharacter 호출로 올바른 배치
                if (PlacementManager.Instance != null)
                {
                    PlacementManager.Instance.OnDropCharacter(c, randomTile);
                }
            }
        }

        Debug.Log($"[ItemData] {jumpedEnemies.Count}명의 점프해온 적을 지역 {myArea}의 랜덤 타일로 이동 완료");
    }

    /// <summary>
    /// 우리 지역(myArea)에 'CharacterData cd'를 임의 타일에 소환
    /// => PlacementManager 사용 (SummonCharacterOnTile or PlaceCharacterOnTile)
    /// 
    /// 여기서는 SummonCharacterOnTile에 맞춰서 DB 인덱스를 구한 뒤 호출하거나,
    /// 아니면 직접 Instantiate...
    /// </summary>
    private bool SummonRandomUnitInArea(CharacterData cd, int myArea)
    {
        if (cd == null) return false;

        // 해당 지역의 placable 타일 찾기
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> placableTiles = new List<Tile>();

        foreach (Tile t in allTiles)
        {
            if (t == null) continue;

            bool isMyRegion = (myArea == 1) ? !t.isRegion2 : t.isRegion2;
            if (isMyRegion && t.IsPlacable() && t.transform.childCount == 0)
            {
                placableTiles.Add(t);
            }
        }

        if (placableTiles.Count == 0)
        {
            Debug.LogWarning($"[ItemData] 지역 {myArea}에 소환 가능한 빈 타일이 없습니다!");
            return false;
        }

        // 랜덤 타일 선택
        Tile selectedTile = placableTiles[Random.Range(0, placableTiles.Count)];

        // PlacementManager를 통한 소환
        if (PlacementManager.Instance != null)
        {
            return PlacementManager.Instance.SummonCharacterOnTile(0, selectedTile, false); // 임시 인덱스 사용
        }

        return false;
    }
    // =========================================================================
    // ▲▲ [신규 추가 끝] 내부 유틸 함수 ▲▲
    // =========================================================================
}
