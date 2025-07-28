using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using GuildMaster.Data;

/// <summary>
/// 가챠 뽑기 결과를 표시하는 Draw Panel을 관리하는 매니저
/// </summary>
public class DrawPanelManager : MonoBehaviour
{
    [Header("Draw Panel UI")]
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject drawResultContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    
    [Header("결과 표시")]
    [SerializeField] private DrawResultIconUI[] resultIconSlots;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI totalGoldSpentText;
    
    [Header("애니메이션")]
    [SerializeField] private float iconAnimationDelay = 0.1f;
    [SerializeField] private GameObject celebrationEffect;
    
    private List<CharacterData> currentDrawResults = new List<CharacterData>();
    
    void Awake()
    {
        Debug.Log("[DrawPanelManager] Awake() 시작");
        
        // 버튼 이벤트 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDrawPanel);
            
        if (confirmButton != null)
            confirmButton.onClick.AddListener(CloseDrawPanel);
        
        // 패널 초기 비활성화
        if (drawPanel != null)
        {
            drawPanel.SetActive(false);
            Debug.Log("[DrawPanelManager] drawPanel 초기 비활성화");
        }
        
        Debug.Log("[DrawPanelManager] Awake() 완료");
    }
    
    /// <summary>
    /// 뽑기 결과를 표시합니다
    /// </summary>
    /// <param name="results">뽑기 결과 캐릭터 리스트</param>
    /// <param name="goldSpent">소모된 골드</param>
    public void ShowDrawResults(List<CharacterData> results, int goldSpent)
    {
        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[DrawPanelManager] 뽑기 결과가 없어 패널을 표시할 수 없습니다.");
            return;
        }
        
        Debug.Log($"[DrawPanelManager] 뽑기 결과 표시: {results.Count}개 캐릭터, {goldSpent} 골드 소모");
        
        currentDrawResults = new List<CharacterData>(results);
        
        // 패널 활성화
        if (drawPanel != null)
            drawPanel.SetActive(true);
        
        // 제목 설정
        if (resultTitleText != null)
        {
            string title = results.Count == 1 ? "단일 뽑기 결과" : $"{results.Count}연차 뽑기 결과";
            resultTitleText.text = title;
        }
        
        // 소모 골드 표시
        if (totalGoldSpentText != null)
            totalGoldSpentText.text = $"소모 골드: {goldSpent:N0}";
        
        // 결과 아이콘들 표시
        StartCoroutine(DisplayResultsAnimation());
    }
    
    /// <summary>
    /// 뽑기 결과를 애니메이션과 함께 표시
    /// </summary>
    private IEnumerator DisplayResultsAnimation()
    {
        if (resultIconSlots == null) yield break;
        
        // 모든 슬롯 초기화
        foreach (var slot in resultIconSlots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(false);
            }
        }
        
        // 결과를 순서대로 애니메이션과 함께 표시
        for (int i = 0; i < currentDrawResults.Count && i < resultIconSlots.Length; i++)
        {
            if (resultIconSlots[i] != null && currentDrawResults[i] != null)
            {
                resultIconSlots[i].gameObject.SetActive(true);
                resultIconSlots[i].SetCharacterInfo(currentDrawResults[i]);
                resultIconSlots[i].PlayAcquireAnimation();
                
                // 애니메이션 지연
                yield return new WaitForSeconds(iconAnimationDelay);
            }
        }
        
        // 고레어 캐릭터가 있으면 축하 효과
        bool hasRareCharacter = currentDrawResults.Exists(c => c.starLevel >= 3);
        if (hasRareCharacter && celebrationEffect != null)
        {
            celebrationEffect.SetActive(true);
            yield return new WaitForSeconds(1f);
            celebrationEffect.SetActive(false);
        }
    }
    
    /// <summary>
    /// Draw Panel을 닫습니다
    /// </summary>
    public void CloseDrawPanel()
    {
        Debug.Log("[DrawPanelManager] Draw Panel 닫기");
        
        if (drawPanel != null)
            drawPanel.SetActive(false);
        
        // 축하 효과 비활성화
        if (celebrationEffect != null)
            celebrationEffect.SetActive(false);
        
        currentDrawResults.Clear();
    }
    
    /// <summary>
    /// Draw Panel 열기 (LobbySceneManager에서 호출)
    /// </summary>
    public void OpenDrawPanel()
    {
        Debug.Log("[DrawPanelManager] Draw Panel 열기 요청");
        
        if (drawPanel != null)
        {
            drawPanel.SetActive(true);
            Debug.Log("[DrawPanelManager] Draw Panel 활성화됨");
        }
        else
        {
            Debug.LogWarning("[DrawPanelManager] drawPanel이 null이라 열 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 현재 뽑기 결과 반환
    /// </summary>
    public List<CharacterData> GetCurrentResults()
    {
        return new List<CharacterData>(currentDrawResults);
    }
    
    /// <summary>
    /// Panel이 현재 열려있는지 확인
    /// </summary>
    public bool IsOpen()
    {
        return drawPanel != null && drawPanel.activeInHierarchy;
    }
} 