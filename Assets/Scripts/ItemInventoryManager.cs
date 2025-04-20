// Assets/Scripts/ItemInventoryManager.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 인벤토리 매니저 (이 게임에서는 "9칸"만 유지)
///  1) 이전 게임과 연결되어 저장될 필요가 없으므로,
///     Save/Load 로직 제거(즉시 반영 없이, 메모리상만 유지).
///  2) 만약 아이템이 이미 9개면, 새로 추가되는 아이템은 모두 버림.
/// </summary>
public class ItemInventoryManager : MonoBehaviour
{
    [Header("Item Database (ScriptableObject)")]
    [SerializeField] private ItemDatabaseObject itemDatabase;

    // 실제 보유중인 아이템 목록 (최대 9개)
    private List<ItemData> ownedItems = new List<ItemData>();

    // ★ 수정됨: PlayerPrefs 관련 키값, SaveItems(), LoadItems() 전부 제거했음

    private void Awake()
    {
        // (과거에 LoadItems()를 하던 부분 제거)
        Debug.Log("[ItemInventoryManager] 아이템 인벤토리는 게임 종료 시 유지되지 않습니다 (비휘발 저장 X).");
    }

    /// <summary>
    /// 아이템을 인벤토리에 추가한다.
    /// 단, 이미 9개를 보유중이면 새 아이템은 버린다.
    /// </summary>
    public void AddItem(ItemData newItem)
    {
        if (newItem == null) return;

        // ★ 수정됨: 9개 초과 방지
        if (ownedItems.Count >= 9)
        {
            Debug.Log($"[ItemInventoryManager] 아이템 '{newItem.itemName}' 획득 시도 -> 이미 9개 초과이므로 버림");
            return; // 그냥 미추가 (바로 버림)
        }

        ownedItems.Add(newItem);
        Debug.Log($"[ItemInventoryManager] 아이템 획득: {newItem.itemName}");

        // (저장 로직 없음)
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
        // 리스트 사본을 반환
        return new List<ItemData>(ownedItems);
    }

    // =================================================================
    //  아래부터 Save/Load 관련 로직은 전부 제거함
    // =================================================================

    /// <summary>
    /// DB에서 itemName으로 ItemData를 찾기 (필요하다면).
    /// </summary>
    private ItemData FindItemByName(string name)
    {
        if (itemDatabase == null || itemDatabase.items == null) return null;

        foreach (var i in itemDatabase.items)
        {
            if (i != null && i.itemName == name)
                return i;
        }
        return null;
    }
}
