// Assets\OX UI Scripts\BookPanelManager.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    /// <summary>
    /// 작은 슬롯 정보를 저장하기 위한 구조체
    /// </summary>
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

    /// <summary>
    /// 현재 인스턴스된 '모션 프리팹' 참조(기존 것 있으면 지우고 새로 인스턴스)
    /// </summary>
    private GameObject currentMotionInstance = null;

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
        CharacterData[] dbChars = characterDatabaseObject.characters;
        List<CharacterData> ownedList = characterInventory.GetAllCharactersWithDuplicates();
        int dbCount = (dbChars != null) ? dbChars.Length : 0;

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

            // 보유 여부
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
            if (slot.nameText)
            {
                slot.nameText.text = "???";
            }
            return;
        }

        // 실제 캐릭터
        CharacterData cData = slot.charData;
        if (slot.iconImage)
        {
            Sprite iconSpr = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            slot.iconImage.sprite = iconSpr;
            slot.iconImage.color  = isOwned ? ownedColor : notOwnedColor;
        }
        if (slot.nameText)
        {
            slot.nameText.text = isOwned ? cData.characterName : "???";
        }
    }

    /// <summary>
    /// 슬롯 클릭 -> '모션 프리팹'을 표시 + 이름 텍스트
    /// (여기서 모션 프리팹의 위치/크기를 원하는대로 조정 가능)
    /// </summary>
    private void OnClickBookSlot(BookSlot slot)
    {
        // ??? 슬롯이면 무시
        if (slot == null || !slot.isActual || slot.charData == null)
            return;

        Debug.Log($"[BookPanelManager] 작은 슬롯 클릭: {slot.charData.characterName}");

        // 1) 기존에 표시 중인 프리팹이 있다면 파괴
        if (currentMotionInstance != null)
        {
            Destroy(currentMotionInstance);
            currentMotionInstance = null;
        }

        // 2) 새로 표시할 모션 프리팹 가져오기
        GameObject motionPrefab = slot.charData.motionPrefab;
        if (motionPrefab != null && motionPrefabParent != null)
        {
            currentMotionInstance = Instantiate(motionPrefab, motionPrefabParent);

            // --------------------------
            // 위치/회전/크기 조정
            // --------------------------
            currentMotionInstance.transform.localPosition = motionPrefabPositionOffset;
            currentMotionInstance.transform.localRotation = Quaternion.identity;
            currentMotionInstance.transform.localScale    = motionPrefabScale;
        }

        // 3) 캐릭터 이름 표시
        if (motionNameText != null)
        {
            motionNameText.text = slot.charData.characterName;
        }
    }

    // =========================================================
    // (2) 초상화 이미지(portraitImages) 갱신
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

            if (i >= maxCount || dbChars[i] == null)
            {
                img.sprite = null;
                img.color  = notOwnedColor;
                continue;
            }

            // 실제 캐릭터
            CharacterData cData = dbChars[i];
            bool isOwned = CheckIfOwned(ownedList, cData.characterName);

            Sprite portraitSpr = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
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
