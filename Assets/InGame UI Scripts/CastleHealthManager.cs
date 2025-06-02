using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 내 "성(캐슬) 체력"을 관리하는 매니저.
/// - 중간성 3개 (각 라인별 체력 500)
/// - 최종성 1개 (체력 1000)
/// </summary>
public class CastleHealthManager : MonoBehaviour
{
    public static CastleHealthManager Instance { get; private set; }

    [Header("최종성 설정")]
    public int finalCastleMaxHealth = 1000;
    public int finalCastleCurrentHealth = 1000;

    [Header("중간성 설정 (3개 라인)")]
    public int midCastleMaxHealth = 500;
    public int leftMidCastleHealth = 500;
    public int centerMidCastleHealth = 500;
    public int rightMidCastleHealth = 500;

    [Header("중간성 오브젝트")]
    public GameObject leftMidCastle;
    public GameObject centerMidCastle;
    public GameObject rightMidCastle;

    [Header("최종성 오브젝트")]
    public GameObject finalCastle;

    [Header("체력바 UI")]
    public List<GameObject> finalCastleHearts = new List<GameObject>();
    public UnityEngine.UI.Image leftMidCastleHealthBar;
    public UnityEngine.UI.Image centerMidCastleHealthBar;
    public UnityEngine.UI.Image rightMidCastleHealthBar;

    // 중간성 파괴 상태
    public bool isLeftMidCastleDestroyed = false;
    public bool isCenterMidCastleDestroyed = false;
    public bool isRightMidCastleDestroyed = false;

    private void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 시작 시 체력 초기화
        UpdateAllHealthBars();
    }

    /// <summary>
    /// 특정 라인의 중간성에 데미지
    /// </summary>
    public void TakeDamageToMidCastle(RouteType route, int amount)
    {
        switch (route)
        {
            case RouteType.Left:
                if (!isLeftMidCastleDestroyed)
                {
                    leftMidCastleHealth -= amount;
                    if (leftMidCastleHealth <= 0)
                    {
                        leftMidCastleHealth = 0;
                        OnMidCastleDestroyed(RouteType.Left);
                    }
                    UpdateMidCastleHealthBar(leftMidCastleHealthBar, leftMidCastleHealth);
                }
                else
                {
                    // 중간성이 파괴되었으면 최종성에 데미지
                    TakeDamageToFinalCastle(amount);
                }
                break;

            case RouteType.Center:
                if (!isCenterMidCastleDestroyed)
                {
                    centerMidCastleHealth -= amount;
                    if (centerMidCastleHealth <= 0)
                    {
                        centerMidCastleHealth = 0;
                        OnMidCastleDestroyed(RouteType.Center);
                    }
                    UpdateMidCastleHealthBar(centerMidCastleHealthBar, centerMidCastleHealth);
                }
                else
                {
                    TakeDamageToFinalCastle(amount);
                }
                break;

            case RouteType.Right:
                if (!isRightMidCastleDestroyed)
                {
                    rightMidCastleHealth -= amount;
                    if (rightMidCastleHealth <= 0)
                    {
                        rightMidCastleHealth = 0;
                        OnMidCastleDestroyed(RouteType.Right);
                    }
                    UpdateMidCastleHealthBar(rightMidCastleHealthBar, rightMidCastleHealth);
                }
                else
                {
                    TakeDamageToFinalCastle(amount);
                }
                break;
        }

        Debug.Log($"[CastleHealthManager] {route} 중간성 체력 감소: {amount}");
    }

    /// <summary>
    /// 최종성에 데미지
    /// </summary>
    public void TakeDamageToFinalCastle(int amount)
    {
        finalCastleCurrentHealth -= amount;
        if (finalCastleCurrentHealth < 0)
            finalCastleCurrentHealth = 0;

        Debug.Log($"[CastleHealthManager] 최종성 체력 감소: {amount} => 남은 HP={finalCastleCurrentHealth}");

        UpdateFinalCastleHearts();

        if (finalCastleCurrentHealth <= 0)
        {
            OnFinalCastleDestroyed();
        }
    }

    /// <summary>
    /// 중간성이 파괴되었을 때
    /// </summary>
    private void OnMidCastleDestroyed(RouteType route)
    {
        Debug.LogWarning($"[CastleHealthManager] {route} 중간성 파괴됨!");

        switch (route)
        {
            case RouteType.Left:
                isLeftMidCastleDestroyed = true;
                if (leftMidCastle != null)
                    leftMidCastle.SetActive(false);
                break;
            case RouteType.Center:
                isCenterMidCastleDestroyed = true;
                if (centerMidCastle != null)
                    centerMidCastle.SetActive(false);
                break;
            case RouteType.Right:
                isRightMidCastleDestroyed = true;
                if (rightMidCastle != null)
                    rightMidCastle.SetActive(false);
                break;
        }

        // 모든 중간성이 파괴되었는지 확인
        if (isLeftMidCastleDestroyed && isCenterMidCastleDestroyed && isRightMidCastleDestroyed)
        {
            Debug.LogWarning("[CastleHealthManager] 모든 중간성이 파괴됨! 이제 최종성만 남았습니다.");
        }
    }

    /// <summary>
    /// 최종성이 파괴되었을 때 (패배)
    /// </summary>
    private void OnFinalCastleDestroyed()
    {
        Debug.LogWarning("[CastleHealthManager] 최종성 파괴됨! (HP=0)");
        GameManager.Instance.SetGameOver(false); // false -> 패배
    }

    /// <summary>
    /// 모든 체력바 업데이트
    /// </summary>
    private void UpdateAllHealthBars()
    {
        UpdateMidCastleHealthBar(leftMidCastleHealthBar, leftMidCastleHealth);
        UpdateMidCastleHealthBar(centerMidCastleHealthBar, centerMidCastleHealth);
        UpdateMidCastleHealthBar(rightMidCastleHealthBar, rightMidCastleHealth);
        UpdateFinalCastleHearts();
    }

    /// <summary>
    /// 중간성 체력바 업데이트
    /// </summary>
    private void UpdateMidCastleHealthBar(UnityEngine.UI.Image healthBar, int currentHealth)
    {
        if (healthBar != null)
        {
            float fillAmount = (float)currentHealth / midCastleMaxHealth;
            healthBar.fillAmount = fillAmount;
        }
    }

    /// <summary>
    /// 최종성 하트 UI 업데이트 (1000HP = 10하트)
    /// </summary>
    private void UpdateFinalCastleHearts()
    {
        int heartsToShow = finalCastleCurrentHealth / 100; // 100당 하트 1개

        for (int i = 0; i < finalCastleHearts.Count; i++)
        {
            if (i < heartsToShow)
                finalCastleHearts[i].SetActive(true);
            else
                finalCastleHearts[i].SetActive(false);
        }
    }

    /// <summary>
    /// 특정 라인의 중간성이 파괴되었는지 확인
    /// </summary>
    public bool IsMidCastleDestroyed(RouteType route)
    {
        switch (route)
        {
            case RouteType.Left:
                return isLeftMidCastleDestroyed;
            case RouteType.Center:
                return isCenterMidCastleDestroyed;
            case RouteType.Right:
                return isRightMidCastleDestroyed;
            default:
                return false;
        }
    }

    /// <summary>
    /// 몬스터가 성에 도달했을 때 호출 (기존 메서드 호환용)
    /// </summary>
    public void TakeDamage(int amount)
    {
        // 기본적으로 중앙 라인으로 처리
        TakeDamageToMidCastle(RouteType.Center, amount);
    }
}