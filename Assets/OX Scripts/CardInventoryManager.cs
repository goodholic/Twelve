using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 카드 인벤토리 및 뽑기(gacha) 기능 관리.
/// (1) CardDatabase를 넣으면, Awake()에서 자동으로 gachaPool이 채워짐.
/// (2) ownedCards: 뽑기(중복 포함)한 카드 목록. (이 리스트가 실제 소유 카드)
/// (3) DrawRandomCard() 호출 시 cardData를 복제해서 ownedCards에 추가.
/// (4) 뽑은 후 세이브/로드를 위해 SaveOwnedCards/LoadOwnedCards 추가.
/// </summary>
public class CardInventoryManager : MonoBehaviour
{
    [Header("CardDatabase 참조 (자동 등록)")]
    [SerializeField] private CardDatabase cardDatabase;

    [Header("현재 소지 중인 카드(중복 포함)")]
    [SerializeField] private List<CardData> ownedCards = new List<CardData>();

    // 외부에서 안 보이게 private 처리
    private List<CardData> gachaPool = new List<CardData>();

    // PlayerPrefs 저장용 키
    private const string PLAYER_PREFS_OWNED_KEY = "OwnedCardsJson";

    private void Awake()
    {
        // 1) CardDatabase가 연결되어 있으면 gachaPool을 자동 구성
        if (cardDatabase == null)
        {
            Debug.LogError("[CardInventoryManager] CardDatabase가 연결되지 않음 -> 뽑기 후보를 구성할 수 없습니다.");
            return;
        }

        gachaPool.Clear();
        foreach (var card in cardDatabase.cardList)
        {
            gachaPool.Add(card);
        }
        Debug.Log($"[CardInventoryManager] CardDatabase로부터 {gachaPool.Count}개 카드 로딩 -> gachaPool 구성 완료");

        // 2) 이전에 저장된 카드 목록 로드
        LoadOwnedCards();
    }

    /// <summary>
    /// gachaPool 중 임의 CardData를 뽑아 ownedCards에 추가 후 저장
    /// </summary>
    public CardData DrawRandomCard()
    {
        if (gachaPool.Count == 0)
        {
            Debug.LogWarning("[CardInventoryManager] gachaPool이 비어있음 -> 뽑기 불가");
            return null;
        }

        int randIdx = Random.Range(0, gachaPool.Count);
        CardData template = gachaPool[randIdx];
        CardData newCard = CreateNewCard(template);

        ownedCards.Add(newCard);
        Debug.Log($"[CardInventoryManager] 뽑기 결과: {newCard.cardName} (Lv.{newCard.level})");

        return newCard;
    }

    /// <summary>
    /// CardData 템플릿을 복제 -> 새 CardData
    /// </summary>
    private CardData CreateNewCard(CardData template)
    {
        CardData copy = new CardData
        {
            cardName       = template.cardName,
            level          = template.level,
            currentExp     = template.currentExp,
            cardSprite     = template.cardSprite,
            characterPrefab= template.characterPrefab,
            gamePrefab     = template.gamePrefab
        };
        return copy;
    }

    /// <summary>
    /// (옵션) 중복 제거한 카드 목록
    /// </summary>
    public List<CardData> GetUniqueCardList()
    {
        Dictionary<string, CardData> dict = new Dictionary<string, CardData>();
        foreach (var card in ownedCards)
        {
            if (!dict.ContainsKey(card.cardName))
                dict.Add(card.cardName, card);
        }
        return new List<CardData>(dict.Values);
    }

    /// <summary>
    /// (옵션) 중복 포함 전체 카드
    /// </summary>
    public List<CardData> GetAllCardsWithDuplicates()
    {
        return new List<CardData>(ownedCards);
    }

    /// <summary>
    /// (옵션) 업그레이드 등 카드 소모 -> ownedCards에서 제거
    /// </summary>
    public void ConsumeCardsForUpgrade(List<CardData> cardsToConsume)
    {
        foreach (var card in cardsToConsume)
        {
            if (ownedCards.Contains(card))
            {
                ownedCards.Remove(card);
            }
            else
            {
                Debug.LogWarning($"[CardInventoryManager] 인벤토리에 없는 카드: {card.cardName}");
            }
        }
    }

    // =========================================================================
    // ===========   아래부터 세이브/로드 로직 (새로 추가/수정)   ===============
    // =========================================================================

    /// <summary>
    /// (중요) ownedCards를 PlayerPrefs에 JSON 형태로 저장
    /// </summary>
    public void SaveOwnedCards()
    {
        // CardData는 Sprite나 Prefab 참조 등이 있으므로 직접 JSON화가 복잡합니다.
        // 여기서는 cardName, level, currentExp 정도만 저장(단순 예시).
        // 만약 sprite나 prefab도 복원하려면, spritePath나 prefabName 등을 별도로 저장 후
        // 로드 시 DataBase에서 찾는 방식으로 구현해야 합니다.
        //
        // 아래는 간단히 Name/Level/Exp만 저장하는 예시.

        List<OwnedCardRecord> recordList = new List<OwnedCardRecord>();
        foreach (var c in ownedCards)
        {
            OwnedCardRecord rec = new OwnedCardRecord
            {
                cardName   = c.cardName,
                level      = c.level,
                currentExp = c.currentExp
            };
            recordList.Add(rec);
        }

        OwnedCardRecordWrapper wrapper = new OwnedCardRecordWrapper
        {
            records = recordList
        };

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PLAYER_PREFS_OWNED_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[CardInventoryManager] SaveOwnedCards() 완료. count={recordList.Count}");
    }

    /// <summary>
    /// PlayerPrefs에서 OwnedCardsJson을 불러와 ownedCards로 복원
    /// </summary>
    public void LoadOwnedCards()
    {
        string json = PlayerPrefs.GetString(PLAYER_PREFS_OWNED_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[CardInventoryManager] 저장된 소유 카드 데이터 없음 -> 초기 상태");
            return;
        }

        OwnedCardRecordWrapper wrapper
            = JsonUtility.FromJson<OwnedCardRecordWrapper>(json);

        if (wrapper == null || wrapper.records == null)
        {
            Debug.LogWarning("[CardInventoryManager] LoadOwnedCards() 파싱 실패. 초기화 처리.");
            return;
        }

        List<OwnedCardRecord> recordList = wrapper.records;
        ownedCards.Clear();

        // 로드 시, cardDatabase(템플릿)에서 동일 cardName을 찾아서 복제
        foreach (var rec in recordList)
        {
            CardData template = FindTemplateByName(rec.cardName);
            if (template == null)
            {
                // 데이터베이스에 없는 카드명 -> 무시
                Debug.LogWarning($"[CardInventoryManager] DB에 없는 카드({rec.cardName}) -> 무시");
                continue;
            }

            CardData newCard = CreateNewCard(template);
            newCard.level      = rec.level;
            newCard.currentExp = rec.currentExp;

            ownedCards.Add(newCard);
        }

        Debug.Log($"[CardInventoryManager] LoadOwnedCards() 완료. 복원된 카드 수={ownedCards.Count}");
    }

    private CardData FindTemplateByName(string cardName)
    {
        if (cardDatabase == null) return null;
        foreach (var c in cardDatabase.cardList)
        {
            if (c.cardName == cardName)
                return c;
        }
        return null;
    }
}

/// <summary>
/// JSON에 담을 용도: cardName, level, currentExp만 간단히 기록
/// </summary>
[System.Serializable]
public class OwnedCardRecord
{
    public string cardName;
    public int level;
    public int currentExp;
}

/// <summary>
/// JSON 직렬화 편의를 위해 리스트를 감싸는 Wrapper 클래스
/// </summary>
[System.Serializable]
public class OwnedCardRecordWrapper
{
    public List<OwnedCardRecord> records;
}
