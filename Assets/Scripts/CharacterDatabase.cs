using UnityEngine;

/// <summary>
/// GameScene에 존재하는 CharacterDatabase (MonoBehaviour).
/// 기존의 'characters[]'를 없애고,
/// 'currentRegisteredCharacters' 에 GameManager의 1~9 + 10(주인공)을 받아옴.
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    [Header("현재 등록된 캐릭터(1~9 + 10)")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];

    private void Start()
    {
        // 1) GameManager 인스턴스 확인
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CharacterDatabase] GameManager.Instance가 없습니다!");
            return;
        }

        // 2) GameManager의 currentRegisteredCharacters(10) 를 가져와 동기화
        for (int i = 0; i < 10; i++)
        {
            currentRegisteredCharacters[i] = gm.currentRegisteredCharacters[i];
        }

        Debug.Log("[CharacterDatabase] 현재 등록된 캐릭터 10개를 GameManager에서 받아왔습니다.");
    }
}
