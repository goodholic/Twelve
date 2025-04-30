using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 "성(캐슬) 체력"을 관리하는 매니저.
/// - 기본 HP=10
/// - 하트(GameObject) 10개를 Inspector에서 연결하여
///   HP가 깎일 때마다 하트 하나씩 비활성화.
/// - HP가 0 이하가 되면 게임 오버(또는 다른 처리) 가능.
/// </summary>
public class CastleHealthManager : MonoBehaviour
{
    public static CastleHealthManager Instance { get; private set; }

    [Header("성 최대 체력(하트 개수)")]
    public int maxHealth = 10;

    [Header("현재 성 체력(시작=10)")]
    public int currentHealth = 10;

    [Header("Red Heart 오브젝트들(인덱스 순으로 0~9, 총 10개)")]
    public List<GameObject> redHearts = new List<GameObject>();

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

        // DontDestroyOnLoad 등 필요하면 추가 가능
        // DontDestroyOnLoad(this.gameObject);

        // 시작 시 체력 보정
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHeartsUI();
    }

    /// <summary>
    /// 적 몬스터가 성에 도달했을 때 등,
    /// 체력을 amount만큼 깎는 메서드.
    /// HP가 0 이하로 떨어지면 게임 오버 처리 가능.
    /// </summary>
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"[CastleHealthManager] 성 체력 감소: {amount} => 남은 HP={currentHealth}");

        UpdateHeartsUI();

        if (currentHealth <= 0)
        {
            OnCastleDestroyed();
        }
    }

    /// <summary>
    /// 현재 HP에 따라 Heart 오브젝트들을 SetActive(true/false)로 갱신.
    /// HP가 7이면 0..6번 인덱스만 활성, 7번~9번은 비활성.
    /// </summary>
    private void UpdateHeartsUI()
    {
        for (int i = 0; i < redHearts.Count; i++)
        {
            if (i < currentHealth)
                redHearts[i].SetActive(true);
            else
                redHearts[i].SetActive(false);
        }
    }

    /// <summary>
    /// 체력이 0 이하가 되면 실행할 로직(게임 오버 등)
    /// </summary>
    private void OnCastleDestroyed()
    {
        Debug.LogWarning("[CastleHealthManager] 성 파괴됨! (HP=0)");
        // TODO: 게임 오버 UI 표시, 씬 전환 등 원하는 로직
        // 예) LobbySceneManager로 돌아가거나, 패배 화면 뜨기 등
    }
}
