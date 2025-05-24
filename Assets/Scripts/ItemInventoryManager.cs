using System.Collections.Generic;
using UnityEngine;

public class ItemInventoryManager : MonoBehaviour
{
    [Header("Item Database (ScriptableObject)")]
    [SerializeField] private ItemDatabaseObject itemDatabase;

    // Public 프로퍼티로 읽기 전용 접근 제공
    public ItemDatabaseObject ItemDatabase => itemDatabase;

    // 실제 보유중인 아이템 목록 (최대 5개)
    private List<ItemData> ownedItems = new List<ItemData>();

    [Header("인벤토리 최대 크기")]
    [SerializeField] private int maxInventorySize = 5;

    private const string PLAYER_PREFS_OWNED_KEY = "OwnedItemJsonV2"; 
    // (만약 아이템을 영구 저장하려면 PlayerPrefs나 JSON등으로 저장 가능. 
    //  여기서는 예시 키만 설정)

    private void Awake()
    {
        Debug.Log("[ItemInventoryManager] 아이템 인벤토리는 게임 종료 시 유지되지 않을 수도 있습니다. (샘플)");
        // 필요하다면 LoadItems() 등 호출
    }

    /// <summary>
    /// 아이템을 인벤토리에 추가한다.
    /// (이미 5개면 새 아이템 버림)
    /// </summary>
    public void AddItem(ItemData newItem)
    {
        if (newItem == null) return;

        // 5개 초과 방지
        if (ownedItems.Count >= maxInventorySize)
        {
            Debug.Log($"[ItemInventoryManager] '{newItem.itemName}' 획득 시도 -> 이미 {maxInventorySize}개 초과이므로 버림");
            return; 
        }

        ownedItems.Add(newItem);
        Debug.Log($"[ItemInventoryManager] 아이템 획득: {newItem.itemName}");
    }

    /// <summary>
    /// 아이템을 인벤토리에서 제거한다.
    /// </summary>
    public void RemoveItem(ItemData item)
    {
        if (item != null && ownedItems.Contains(item))
        {
            ownedItems.Remove(item);
            Debug.Log($"[ItemInventoryManager] 아이템 제거: {item.itemName}");
        }
    }

    /// <summary>
    /// 현재 소유한 아이템 목록(사본)을 반환
    /// </summary>
    public List<ItemData> GetOwnedItems()
    {
        return new List<ItemData>(ownedItems);
    }

    // ===================================================================
    // == (추가) 아이템을 "게임" 전체에 적용하는 메서드 (몬스터나 유닛 소환 등)
    // ===================================================================
    /// <summary>
    /// (새로 추가) 아이템 효과를 "게임에" 적용
    /// (ex: 점프해온 몬스터를 찾아 랜덤 위치 이동, 범위 데미지, 2~3성 유닛 소환 등)
    /// 
    /// ※ 실제로 UI에서 "아이템 클릭 → 이 메서드 호출" 식으로 연결해주세요.
    /// </summary>
    public bool ApplyItemToGame(ItemData item)
    {
        if (item == null) return false;

        switch (item.effectType)
        {
            case ItemEffectType.IncreaseAttack:
            case ItemEffectType.IncreaseHP:
            case ItemEffectType.IncreaseRange:
                // 기존처럼 캐릭터 1명을 타겟팅해서 ApplyEffectToCharacter() 쓰는 경우
                // 여기서는 아무 캐릭터도 지정 안 했다고 가정 → 실패 처리
                Debug.LogWarning($"[ItemInventoryManager] 이 아이템은 개별 캐릭터에 써야 합니다: {item.itemName}");
                return false;

            case ItemEffectType.TeleportJumpedEnemies:
                {
                    // 1) 점프해온 적 몬스터들 검색
                    //    (Character에서 hasCrossedRegion == true && tag=="Character"? 등
                    //     여기서는 Monster 중 "areaIndex != 1" 검색 예시)
                    Monster[] allMobs = FindObjectsByType<Monster>(FindObjectsSortMode.None);
                    List<Monster> jumpedEnemies = new List<Monster>();
                    foreach (var mob in allMobs)
                    {
                        if (mob != null && mob.areaIndex != 1)
                        {
                            // "점프해온" 조건을 구분해야 한다면,
                            // 별도 플래그가 필요할 수 있음(현재 Monster에는 hasCrossedRegion 없음)
                            jumpedEnemies.Add(mob);
                        }
                    }

                    if (jumpedEnemies.Count == 0)
                    {
                        Debug.Log("[ItemInventoryManager] 점프해온 적 몬스터가 없음 -> 효과 무효");
                        return false;
                    }

                    // 2) "우리 지역" 타일들 중 랜덤 위치로 재배치
                    //    (areaIndex=1인 Tile 중 placable? walkable? 등)
                    //    여기서는 간단히 Monster 위치만 랜덤으로 보정
                    for (int i = 0; i < jumpedEnemies.Count; i++)
                    {
                        Monster m = jumpedEnemies[i];
                        // 랜덤 좌표 예시 (카메라 내부, 혹은 특정범위)
                        Vector3 randomPos = new Vector3(Random.Range(-3f, 3f), Random.Range(-2f, 2f), 0f);
                        m.transform.position = randomPos;
                    }

                    Debug.Log($"[ItemInventoryManager] 점프해온 {jumpedEnemies.Count}마리 몬스터를 랜덤 위치로 이동 완료");

                    // 아이템 사용 후 제거
                    RemoveItem(item);
                    return true;
                }

            case ItemEffectType.DamageJumpedEnemies:
                {
                    // 예) 점프해온 적 몬스터들에게 item.effectValue만큼 데미지
                    Monster[] allMobs = FindObjectsByType<Monster>(FindObjectsSortMode.None);
                    int hitCount = 0;
                    foreach (var mob in allMobs)
                    {
                        if (mob != null && mob.areaIndex != 1)
                        {
                            mob.TakeDamage(item.effectValue);
                            hitCount++;
                        }
                    }
                    Debug.Log($"[ItemInventoryManager] 점프해온 몬스터 {hitCount}마리에 데미지 {item.effectValue} 적용");

                    RemoveItem(item);
                    return true;
                }

            case ItemEffectType.SummonRandom2Or3Star:
                {
                    // 2~3성 유닛 랜덤 소환
                    // (PlacementManager 소환 함수를 호출하거나, 직접 Instantiate)
                    // 여기서는 간단히 로그만 찍음

                    int star = Random.value < 0.5f ? 2 : 3; 
                    Debug.Log($"[ItemInventoryManager] 랜덤으로 {star}성 유닛을 소환!");

                    // 실제 소환 로직 예시:
                    // if (PlacementManager.Instance != null)
                    // {
                    //     PlacementManager.Instance.SummonRandomStarUnit(star);
                    // }

                    RemoveItem(item);
                    return true;
                }

            default:
                Debug.LogWarning($"[ItemInventoryManager] 알 수 없는 효과타입: {item.effectType}");
                return false;
        }
    }

    // ===================================================================
    // (기존) charData에 적용하는 경우
    // ===================================================================
    public void UseItemOnCharacter(ItemData item, Character target)
    {
        if (item == null || target == null)
        {
            Debug.LogWarning("[ItemInventoryManager] 아이템 혹은 타겟이 null");
            return;
        }

        bool success = item.ApplyEffectToCharacter(target);
        if (success)
        {
            RemoveItem(item);
        }
    }

    // ======================================
    // 필요하면 여기서 Save/Load 로직 추가
    // ======================================
    // public void SaveItems() { ... }
    // public void LoadItems() { ... }
}
