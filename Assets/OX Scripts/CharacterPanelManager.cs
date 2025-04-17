using UnityEngine;

public class CharacterPanelManager : MonoBehaviour
{
    [Header("덱 패널 (기본 활성)")]
    [SerializeField] private GameObject deckPanel;

    [Header("업그레이드 패널")]
    [SerializeField] private GameObject upgradePanel;

    /// <summary>
    /// [방법1] OnEnable()로도 할 수 있지만, 
    /// '캐릭터 창이 열릴 때'를 더 확실히 제어하기 위해 
    /// 별도의 OpenCharacterPanel() 메서드를 제공.
    /// </summary>
    public void OpenCharacterPanel()
    {
        // 이 오브젝트(캐릭터 패널) 자체를 활성화
        this.gameObject.SetActive(true);

        // 열릴 때마다 덱 패널 활성, 업그레이드 패널 비활성
        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    private void OnEnable()
    {
        // 혹시나 씬에서 수동 활성화될 때도 덱 패널이 자동으로 켜지도록 처리
        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    /// <summary>
    /// 덱 버튼을 눌렀을 때
    /// </summary>
    public void OnClickOpenDeckPanel()
    {
        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    /// <summary>
    /// 업그레이드 버튼을 눌렀을 때
    /// </summary>
    public void OnClickOpenUpgradePanel()
    {
        if (deckPanel) deckPanel.SetActive(false);
        if (upgradePanel) upgradePanel.SetActive(true);
    }
}
