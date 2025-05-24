using UnityEngine;

[CreateAssetMenu(fileName = "NewItemDatabase", menuName = "MyGame/Item Database (ScriptableObject)")]
public class ItemDatabaseObject : ScriptableObject
{
    [Header("Item Data List (.asset)")]
    public ItemData[] items;

    // ================= [수정] 여기서부터 끝까지 전체 코드 =================

    // (추가) ScriptableObject 에서 에디터로 아이템을 직접 추가/수정하는 기능은 
    // Editor 스크립트로 가능하지만, 여기서는 단순 배열만 사용.
    // 
    // 아래는 예시로 3가지 아이템을 하드코딩해놓은 상태로, 
    // 실제 프로젝트에서는 인스펙터에서 추가하는 방식을 권장.

    // (참고) 만약 프로젝트상 인스펙터에서 직접 편집 중이라면, 
    // 아래 예시처럼 "items" 배열 원소를 3개 더 추가해두면 됩니다.
    // ------------------------------------------------------------
    // 예시:
    //  items[<기존개수>] = new ItemData { ... };
    //  items[<기존개수+1>] = ...
    //  items[<기존개수+2>] = ...
    //
    // 이미 인스펙터에서 아이템이 여러 개 등록되어 있다면, 
    // 그 뒤에 3개를 추가하는 식으로 해주시면 됩니다.
    // ------------------------------------------------------------
}
