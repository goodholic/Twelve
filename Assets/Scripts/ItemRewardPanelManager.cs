// Assets/Scripts/ItemRewardPanelManager.cs

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
    [SerializeField] private List<Button> itemCardButtons;         // 3개 버튼
    [SerializeField] private List<Image> itemCardImages;           // 3개 이미지
    [SerializeField] private List<TextMeshProUGUI> itemCardNameTexts; // 3개 이름

    // 뽑힌 3개의 아이템 보관용
    private ItemData[] chosenItems = new ItemData[3];

    private void Awake()
    {
        // 초기엔 '보상 패널'을 꺼둠
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

        // 3개 랜덤 아이템 뽑기
        chosenItems = PickRandomThreeItems();

        // 슬롯 UI 적용
        for (int i = 0; i < 3; i++)
        {
            if (chosenItems[i] != null)
            {
                // 이미지 표시
                if (itemCardImages != null && i < itemCardImages.Count && itemCardImages[i] != null)
                {
                    itemCardImages[i].sprite = chosenItems[i].itemIcon;
                }
                // 이름 표시
                if (itemCardNameTexts != null && i < itemCardNameTexts.Count && itemCardNameTexts[i] != null)
                {
                    itemCardNameTexts[i].text = chosenItems[i].itemName;
                }
                // 버튼 설정
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
                // null 아이템이면, 해당 슬롯을 비워둠(필요하다면 추가 처리 가능)
                if (itemCardImages != null && i < itemCardImages.Count && itemCardImages[i] != null)
                {
                    itemCardImages[i].sprite = null;
                }
                if (itemCardNameTexts != null && i < itemCardNameTexts.Count && itemCardNameTexts[i] != null)
                {
                    itemCardNameTexts[i].text = "없음";
                }
                if (itemCardButtons != null && i < itemCardButtons.Count && itemCardButtons[i] != null)
                {
                    itemCardButtons[i].onClick.RemoveAllListeners();
                    itemCardButtons[i].interactable = false;
                }
            }
        }
    }

    /// <summary>
    /// 3개 랜덤 아이템 뽑기
    /// </summary>
    private ItemData[] PickRandomThreeItems()
    {
        ItemData[] result = new ItemData[3];

        if (itemDatabase == null || itemDatabase.items == null || itemDatabase.items.Length == 0)
        {
            Debug.LogWarning("[ItemRewardPanelManager] 아이템DB가 비어있습니다!");
            return result; // 모두 null
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

    /// <summary>
    /// 보상 카드 중 하나를 클릭했을 때(획득)
    /// 선택된 아이템을 인벤토리에 추가 + 아이템 패널 닫음 + 인벤토리 패널 갱신
    /// </summary>
    private void OnClickSelectItem(int index)
    {
        if (index < 0 || index >= chosenItems.Length) return;
        ItemData selected = chosenItems[index];
        if (selected == null) return;

        // 1) 인벤토리에 추가
        if (itemInventoryManager != null)
        {
            itemInventoryManager.AddItem(selected);
        }

        // 2) 아이템 인벤토리(9칸)도 즉시 갱신
        var itemPanelMgr = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelMgr != null)
        {
            itemPanelMgr.RefreshItemPanel();
        }

        // 3) "보상 패널" 닫기
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[ItemRewardPanelManager] itemPanel이 null이라 닫을 수 없음!");
        }

        Debug.Log($"[ItemRewardPanelManager] 아이템 '{selected.itemName}' 획득 후 아이템 패널 닫힘");
    }
}
