using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using GuildMaster.Data;
using GuildMaster.Game;

public class BookPanelManager : MonoBehaviour
{
    [Header("Character Database (ScriptableObject)")]
    [SerializeField] private CharacterDatabaseSO characterDatabaseObject;

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
    // (C) 작은 슬롯 클릭 시 보여줄 '모션 프리팹' + 이름
    // ----------------------------------------------------
    [Header("슬롯 클릭 시 크게 표시할 '모션 프리팹' + 이름 텍스트")]
    [Tooltip("여기에 모션 프리팹을 인스턴스하여 표시할 부모 Transform(또는 GameObject)을 연결하세요.")]
    [SerializeField] private Transform motionPrefabParent;
    [SerializeField] private TextMeshProUGUI motionNameText;

    // ============================
    // ** 추가: 크기/위치 조절용
    // ============================
    [Header("도감용 모션 프리팹 - 스케일/위치 오프셋 설정")]
    [SerializeField] private Vector3 motionPrefabPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 motionPrefabScale = Vector3.one;

    private class BookSlot
    {
        public GameObject root;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public Button button;
        public CharacterData charData;
        public bool isActual;
    }

    private List<BookSlot> bookSlotInfos = new List<BookSlot>();

    private GameObject currentMotionInstance = null;

    private void OnEnable()
    {
        RefreshBookPanel();
        RefreshPortraitPanel();
    }

    public void RefreshBookPanel()
    {
        if (!CheckCommonReferences()) return;
        if (bookSlots == null || bookSlots.Count == 0)
        {
            Debug.LogWarning("[BookPanelManager] bookSlots가 비어있습니다.");
            return;
        }

        bookSlotInfos.Clear();

        // 1) bookSlots 각각에 대해 BookSlot 구조체 생성
        foreach (GameObject slotObj in bookSlots)
        {
            if (slotObj == null) continue;

            BookSlot slot = new BookSlot
            {
                root      = slotObj,
                iconImage = slotObj.transform.Find("IconImage")?.GetComponent<Image>(),
                nameText  = slotObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                button    = slotObj.GetComponent<Button>(),
                charData  = null,
                isActual  = false
            };

            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
            }
            bookSlotInfos.Add(slot);
        }

        // 2) DB 캐릭터 목록 + 인벤토리(덱 포함)에서 보유한 목록
        List<CharacterData> dbChars = characterDatabaseObject.characters;
        List<CharacterData> ownedList = characterInventory.GetAllCharactersWithDuplicates();
        int dbCount = (dbChars != null) ? dbChars.Count : 0;

        // 3) 실제 표시할 개수
        int slotCount = bookSlotInfos.Count;
        int displayCount = Mathf.Min(slotCount, dbCount);

        // 4) 슬롯별로 캐릭터 할당
        for (int i = 0; i < slotCount; i++)
        {
            BookSlot slot = bookSlotInfos[i];
            if (slot.root) slot.root.SetActive(true);

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

            bool isOwned = false;
            if (slot.isActual && slot.charData != null)
            {
                isOwned = CheckIfOwned(ownedList, slot.charData.characterName);
            }

            UpdateBookSlotVisual(slot, isOwned);

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

        if (!slot.isActual || slot.charData == null)
        {
            if (slot.iconImage)
            {
                slot.iconImage.sprite = null;
                slot.iconImage.color  = notOwnedColor;
            }
            if (slot.nameText)
            {
                slot.nameText.text = "???";
            }
            return;
        }

        CharacterData cData = slot.charData;
        if (slot.iconImage)
        {
            Sprite iconSpr = (cData.buttonIcon != null) ? cData.buttonIcon : null;
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
        if (slot == null || !slot.isActual || slot.charData == null)
            return;

        Debug.Log($"[BookPanelManager] 작은 슬롯 클릭: {slot.charData.characterName}");

        if (currentMotionInstance != null)
        {
            Destroy(currentMotionInstance);
            currentMotionInstance = null;
        }

        GameObject motionPrefab = slot.charData.motionPrefab;
        if (motionPrefab != null && motionPrefabParent != null)
        {
            currentMotionInstance = Instantiate(motionPrefab, motionPrefabParent);

            currentMotionInstance.transform.localPosition = motionPrefabPositionOffset;
            currentMotionInstance.transform.localRotation = Quaternion.identity;
            currentMotionInstance.transform.localScale    = motionPrefabScale;
        }

        // ▼▼ [수정] motionNameText가 null이면 그냥 경고 후 리턴 ▼▼
        if (motionNameText == null)
        {
            Debug.LogWarning("[BookPanelManager] motionNameText가 null이라 이름 표시 불가.");
            return;
        }
        // ▲▲ [수정끝] ▲▲

        motionNameText.text = slot.charData.characterName;
    }

    public void RefreshPortraitPanel()
    {
        if (!CheckCommonReferences()) return;
        if (portraitImages == null || portraitImages.Count == 0)
        {
            Debug.Log("[BookPanelManager] portraitImages가 비어있어, 초상화 표시 생략.");
            return;
        }

        List<CharacterData> dbChars = characterDatabaseObject.characters;
        List<CharacterData> ownedList = characterInventory.GetAllCharactersWithDuplicates();
        int dbCount = (dbChars != null) ? dbChars.Count : 0;

        int maxCount = Mathf.Min(portraitImages.Count, dbCount);
        for (int i = 0; i < portraitImages.Count; i++)
        {
            Image img = portraitImages[i];
            if (img == null) continue;

            if (i >= maxCount || dbChars[i] == null)
            {
                img.sprite = null;
                img.color  = notOwnedColor;
                continue;
            }

            CharacterData cData = dbChars[i];
            bool isOwned = CheckIfOwned(ownedList, cData.characterName);

            Sprite portraitSpr = (cData.buttonIcon != null) ? cData.buttonIcon : null;
            img.sprite = portraitSpr;
            img.color  = isOwned ? ownedColor : notOwnedColor;
        }
    }

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
