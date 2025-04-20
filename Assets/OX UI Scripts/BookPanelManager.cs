using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감(Book Panel) 전용 매니저.
/// 
/// 1) bookSlots(List<GameObject>) : 아이콘 + 이름(또는 ???) 표시 (슬롯 클릭 시 '큰 이미지'에 표시)
/// 2) portraitImages(List<Image>) : 캐릭터 초상화를 나열 (DB의 buttonIcon.sprite 사용)
/// 3) 작은 슬롯 클릭 시, 'motionImage'에 크게 표시 + 이름 텍스트
/// </summary>
public class BookPanelManager : MonoBehaviour
{
    [Header("Character Database (ScriptableObject)")]
    [SerializeField] private CharacterDatabaseObject characterDatabaseObject;

    [Header("CharacterInventoryManager (인벤토리/덱 정보)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    // ----------------------------------------------------
    // (A) 작은 슬롯(bookSlots) : 아이콘 + ??? 표시
    // ----------------------------------------------------
    [Header("(A) 작은 BookSlot 오브젝트들 (갯수 자유)")]
    [SerializeField] private List<GameObject> bookSlots;

    // ----------------------------------------------------
    // (B) 초상화 이미지들 (DB의 buttonIcon을 그대로 표시)
    // ----------------------------------------------------
    [Header("(B) 초상화 이미지들 (갯수 자유)")]
    [Tooltip("캐릭터의 buttonIcon.sprite를 표시할 Image 컴포넌트들")]
    [SerializeField] private List<Image> portraitImages;

    // ----------------------------------------------------
    //  색상(보유/미보유)
    // ----------------------------------------------------
    [Header("소유/미소유 색상(또는 투명도)")]
    [SerializeField] private Color ownedColor = Color.white;
    [SerializeField] private Color notOwnedColor = Color.gray;

    // ----------------------------------------------------
    // (C) 작은 슬롯 클릭 시 보여줄 '모션 이미지' + 이름
    // ----------------------------------------------------
    [Header("슬롯 클릭 시 크게 표시할 '모션 이미지' + 이름 텍스트")]
    [SerializeField] private Image motionImage;       // 클릭 시 보여줄 큰 이미지
    [SerializeField] private TextMeshProUGUI motionNameText;  // 큰 이미지 아래 이름 표시

    // ----------------------------------------------------
    // 내부 구조체: 작은 슬롯 정보
    // ----------------------------------------------------
    private class BookSlot
    {
        public GameObject root;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public Button button;
        public CharacterData charData;
        public bool isActual; // true=실제 캐릭터, false=??? 슬롯
    }

    // 작은 슬롯들의 정보를 담을 리스트
    private List<BookSlot> bookSlotInfos = new List<BookSlot>();

    private void OnEnable()
    {
        RefreshBookPanel();
        RefreshPortraitPanel();
    }

    // =========================================================
    // (1) 작은 슬롯(bookSlots) 갱신
    // =========================================================
    public void RefreshBookPanel()
    {
        if (!CheckCommonReferences()) return;
        if (bookSlots == null || bookSlots.Count == 0)
        {
            Debug.LogWarning("[BookPanelManager] bookSlots가 비어있습니다.");
            return;
        }

        bookSlotInfos.Clear();

        // 1) bookSlots 각각에 대해 BookSlot 구조를 만든다
        foreach (GameObject slotObj in bookSlots)
        {
            if (slotObj == null) continue;

            BookSlot slot = new BookSlot();
            slot.root       = slotObj;
            slot.iconImage  = slotObj.transform.Find("IconImage")?.GetComponent<Image>();
            slot.nameText   = slotObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            slot.button     = slotObj.GetComponent<Button>();
            slot.charData   = null;
            slot.isActual   = false;

            // 버튼이 있다면 클릭 리스너 초기화
            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
            }

            bookSlotInfos.Add(slot);
        }

        // 2) DB 캐릭터, 그리고 인벤토리에서 보유한 목록
        CharacterData[] dbChars = characterDatabaseObject.characters;
        List<CharacterData> ownedList = characterInventory.GetAllCharactersWithDuplicates();
        int dbCount = (dbChars != null) ? dbChars.Length : 0;

        // 3) 실제 슬롯에 표시할 개수
        int slotCount = bookSlotInfos.Count;
        int displayCount = Mathf.Min(slotCount, dbCount);

        // 4) 슬롯별로 캐릭터 할당
        for (int i = 0; i < slotCount; i++)
        {
            BookSlot slot = bookSlotInfos[i];
            if (slot.root) slot.root.SetActive(true);

            // DB 범위 내면 캐릭터 할당, 아니면 ??? 슬롯
            if (i < displayCount && dbChars[i] != null)
            {
                slot.charData = dbChars[i];
                slot.isActual = true;
            }
            else
            {
                slot.charData = null;
                slot.isActual = false;
            }

            // 보유 여부 판단
            bool isOwned = false;
            if (slot.isActual && slot.charData != null)
            {
                isOwned = CheckIfOwned(ownedList, slot.charData.characterName);
            }

            // UI 반영
            UpdateBookSlotVisual(slot, isOwned);

            // 버튼 클릭 연결
            if (slot.button != null)
            {
                var copySlot = slot;
                slot.button.onClick.AddListener(() => OnClickBookSlot(copySlot));
            }
        }
    }

    private void UpdateBookSlotVisual(BookSlot slot, bool isOwned)
    {
        if (slot == null || slot.root == null) return;

        // ??? 처리
        if (!slot.isActual || slot.charData == null)
        {
            if (slot.iconImage)
            {
                slot.iconImage.sprite = null;
                slot.iconImage.color  = notOwnedColor;
            }
            if (slot.nameText) slot.nameText.text = "???";
            return;
        }

        // 실제 캐릭터 표시
        CharacterData cData = slot.charData;
        if (slot.iconImage)
        {
            // 여기서는 DB에 있는 buttonIcon(sprite)을 표시
            Sprite iconSpr = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            slot.iconImage.sprite = iconSpr;
            slot.iconImage.color  = isOwned ? ownedColor : notOwnedColor;
        }
        if (slot.nameText)
        {
            slot.nameText.text = isOwned ? cData.characterName : "???";
        }
    }

    private void OnClickBookSlot(BookSlot slot)
    {
        // ??? 슬롯이면 클릭 무시
        if (slot == null || !slot.isActual || slot.charData == null) return;

        Debug.Log($"[BookPanelManager] 작은 슬롯 클릭: {slot.charData.characterName}");

        // 모션이미지가 아니라, 이번에는 '큰 이미지'를 보여줄 때도
        // 원한다면 buttonIcon이든 motionSprite든 쓸 수 있지만,
        // 여기선 motionImage / motionNameText만 사용(예: motionSprite)
        // => 만약 "buttonIcon"을 크게 띄우려면 아래 sprite도 buttonIcon으로 변경 가능
        Sprite bigSpr = slot.charData.motionSprite; // 필요 시 buttonIcon으로 교체 가능

        if (motionImage != null)
        {
            motionImage.sprite = bigSpr;
            motionImage.color  = (bigSpr != null) ? Color.white : Color.clear;
        }
        if (motionNameText != null)
        {
            motionNameText.text = slot.charData.characterName;
        }
    }

    // =========================================================
    // (2) 초상화 이미지(portraitImages) 갱신
    //     -> DB 캐릭터 순서대로 buttonIcon.sprite 배치
    // =========================================================
    public void RefreshPortraitPanel()
    {
        if (!CheckCommonReferences()) return;
        if (portraitImages == null || portraitImages.Count == 0)
        {
            Debug.Log("[BookPanelManager] portraitImages가 비어있어, 초상화 표시 생략.");
            return;
        }

        // DB + 보유목록
        CharacterData[] dbChars = characterDatabaseObject.characters;
        List<CharacterData> ownedList = characterInventory.GetAllCharactersWithDuplicates();
        int dbCount = (dbChars != null) ? dbChars.Length : 0;

        int maxCount = Mathf.Min(portraitImages.Count, dbCount);

        for (int i = 0; i < portraitImages.Count; i++)
        {
            Image img = portraitImages[i];
            if (img == null) continue;

            // 범위 밖이면 ??? 처리
            if (i >= maxCount || dbChars[i] == null)
            {
                img.sprite = null;
                img.color  = notOwnedColor;
                continue;
            }

            // 실제 캐릭터
            CharacterData cData = dbChars[i];
            bool isOwned = CheckIfOwned(ownedList, cData.characterName);

            // 초상화 이미지는 DB의 buttonIcon.sprite를 사용
            Sprite portraitSpr = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            img.sprite = portraitSpr;
            img.color  = (isOwned) ? ownedColor : notOwnedColor;
        }
    }

    // =========================================================
    //  공통
    // =========================================================
    private bool CheckCommonReferences()
    {
        if (characterDatabaseObject == null)
        {
            Debug.LogWarning("[BookPanelManager] characterDatabaseObject가 null입니다!");
            return false;
        }
        if (characterInventory == null)
        {
            Debug.LogWarning("[BookPanelManager] characterInventory가 null입니다!");
            return false;
        }
        return true;
    }

    private bool CheckIfOwned(List<CharacterData> ownedList, string charName)
    {
        foreach (var c in ownedList)
        {
            if (c != null && c.characterName == charName)
                return true;
        }
        return false;
    }
}
