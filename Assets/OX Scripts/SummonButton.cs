using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SummonButton : MonoBehaviour
{
    [Header("소환 버튼에 배정된 캐릭터 (oxCharacter)")]
    [SerializeField] private oxCharacter assignedPrefab;

    [SerializeField] private SummonUIManager summonManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClickSummonButton);
    }

    private void OnClickSummonButton()
    {
        if (summonManager == null)
        {
            Debug.LogWarning("[SummonButton] SummonUIManager가 연결되지 않음!");
            return;
        }
        if (assignedPrefab == null)
        {
            Debug.LogWarning("[SummonButton] assignedPrefab(캐릭터)가 없음!");
            return;
        }

        summonManager.SetSelectedPrefab(assignedPrefab);
        Debug.Log($"[SummonButton] '{assignedPrefab.name}' 소환 모드");
    }

    public void SetAssignedPrefab(oxCharacter newChar)
    {
        assignedPrefab = newChar;
    }
}
