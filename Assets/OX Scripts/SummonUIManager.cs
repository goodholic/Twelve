using UnityEngine;

public class SummonUIManager : MonoBehaviour
{
    // 원래 private Character currentSelectedPrefab -> oxCharacter로 변경
    private oxCharacter currentSelectedPrefab;

    public oxCharacter GetCurrentSelectedPrefab()
    {
        return currentSelectedPrefab;
    }

    public void SetSelectedPrefab(oxCharacter prefab)
    {
        currentSelectedPrefab = prefab;
        Debug.Log($"[SummonUIManager] 소환 프리팹 설정: {prefab?.name}");
    }

    public void ClearSelection()
    {
        currentSelectedPrefab = null;
        Debug.Log("[SummonUIManager] 소환모드 해제");
    }
}
