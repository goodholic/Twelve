using UnityEngine;

public class CharacterPanelManager : MonoBehaviour
{
    [Header("덱 패널 (기본 활성)")]
    [SerializeField] private GameObject deckPanel;

    [Header("업그레이드 패널")]
    [SerializeField] private GameObject upgradePanel;

    /// <summary>
    /// 캐릭터 창 열기
    /// </summary>
    public void OpenCharacterPanel()
    {
        this.gameObject.SetActive(true);

        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    public void OnClickOpenDeckPanel()
    {
        if (deckPanel) deckPanel.SetActive(true);
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    public void OnClickOpenUpgradePanel()
    {
        if (deckPanel) deckPanel.SetActive(false);
        if (upgradePanel) upgradePanel.SetActive(true);
    }
}
