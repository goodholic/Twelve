// Assets\Scripts\ItemRewardPanelManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ItemRewardPanelManager : MonoBehaviour
{
    [Header("Item Database")]
    [SerializeField] private ItemDatabaseObject itemDatabase;

    [Header("ItemInventoryManager")]
    [SerializeField] private ItemInventoryManager itemInventoryManager;

    [Header("아이템 패널(웨이브 보상용)")]
    [Tooltip("WaveSpawner에서 [SerializeField] private GameObject itemPanel; 와 동일 GameObject를 연결")]
    [SerializeField] private GameObject itemPanel;

    [Header("아이템 카드 슬롯들(3개)")]
    [SerializeField] private List<Button> itemCardButtons;      // 3개 버튼
    [SerializeField] private List<Image> itemCardImages;        // 3개 이미지

    // -----------------------------------------------------------------------------------
    // ★ 변경됨: 이름 대신 "description"을 표시할 텍스트 (itemCardDescTexts) - 총 3개
    // -----------------------------------------------------------------------------------
    [Header("아이템 설명 텍스트들(3개) - 기존 itemCardNameTexts 제거 후 교체")]
    [SerializeField] private List<TextMeshProUGUI> itemCardDescTexts; // 3개 description

    // 뽑힌 3개의 아이템 보관용
    private ItemData[] chosenItems = new ItemData[3];

    private void Awake()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[ItemRewardPanelManager] itemPanel이 연결되지 않음!");
        }
    }

    /// <summary>
    /// WaveSpawner.OnWaveClear() 시점에 호출 → 아이템 패널 활성화 + 3개 랜덤 아이템 세팅
    /// </summary>
    public void ShowRewardPanel()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[ItemRewardPanelManager] itemPanel이 null이라 열 수 없음!");
        }

        chosenItems = PickRandomThreeItems();

        for (int i = 0; i < 3; i++)
        {
            if (chosenItems[i] != null)
            {
                if (itemCardImages != null && i < itemCardImages.Count && itemCardImages[i] != null)
                {
                    itemCardImages[i].sprite = chosenItems[i].itemIcon;
                }
                if (itemCardDescTexts != null && i < itemCardDescTexts.Count && itemCardDescTexts[i] != null)
                {
                    itemCardDescTexts[i].text = chosenItems[i].description;
                }
                if (itemCardButtons != null && i < itemCardButtons.Count && itemCardButtons[i] != null)
                {
                    int copyIndex = i;
                    itemCardButtons[i].onClick.RemoveAllListeners();
                    itemCardButtons[i].onClick.AddListener(() => OnClickSelectItem(copyIndex));
                    itemCardButtons[i].interactable = true;
                }
            }
            else
            {
                if (itemCardImages != null && i < itemCardImages.Count && itemCardImages[i] != null)
                {
                    itemCardImages[i].sprite = null;
                }
                if (itemCardDescTexts != null && i < itemCardDescTexts.Count && itemCardDescTexts[i] != null)
                {
                    itemCardDescTexts[i].text = "없음";
                }
                if (itemCardButtons != null && i < itemCardButtons.Count && itemCardButtons[i] != null)
                {
                    itemCardButtons[i].onClick.RemoveAllListeners();
                    itemCardButtons[i].interactable = false;
                }
            }
        }
    }

    private ItemData[] PickRandomThreeItems()
    {
        ItemData[] result = new ItemData[3];

        if (itemDatabase == null || itemDatabase.items == null || itemDatabase.items.Length == 0)
        {
            Debug.LogWarning("[ItemRewardPanelManager] 아이템DB가 비어있습니다!");
            return result;
        }

        List<ItemData> pool = new List<ItemData>(itemDatabase.items);
        for (int i = 0; i < 3; i++)
        {
            if (pool.Count == 0) break;
            int rand = Random.Range(0, pool.Count);
            result[i] = pool[rand];
            pool.RemoveAt(rand);
        }
        return result;
    }

    private void OnClickSelectItem(int index)
    {
        if (index < 0 || index >= chosenItems.Length) return;
        ItemData selected = chosenItems[index];
        if (selected == null) return;

        if (itemInventoryManager != null)
        {
            itemInventoryManager.AddItem(selected);
        }

        var itemPanelMgr = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelMgr != null)
        {
            itemPanelMgr.RefreshItemPanel();
        }

        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        Debug.Log($"[ItemRewardPanelManager] 아이템 '{selected.itemName}' 획득 후 아이템 패널 닫힘");
    }
}
