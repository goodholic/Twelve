using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class oxGameManager : MonoBehaviour
{
    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static oxGameManager Instance { get; private set; }

    [Header("판/보드 매니저")]
    [SerializeField] private BoardManager boardManager;

    [Header("UI - 턴/승리 상태 표시")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("턴 설정(랜덤 선공)")]
    [SerializeField] private bool randomFirstTurn = true;
    private TeamType currentTeam;
    private bool isGameOver;

    [Header("소환 매니저(1~4 버튼)")]
    [SerializeField] private SummonUIManager summonUIManager;

    [Header("등록할 SummonButtons (1~4)")]
    [SerializeField] private SummonButton summonButton1;
    [SerializeField] private SummonButton summonButton2;
    [SerializeField] private SummonButton summonButton3;
    [SerializeField] private SummonButton summonButton4;

    [Header("슬롯별 미리보기 이미지(1~4)")]
    [SerializeField] private Image slotImage1;
    [SerializeField] private Image slotImage2;
    [SerializeField] private Image slotImage3;
    [SerializeField] private Image slotImage4;

    // oxCharacter ↔ Sprite 연결(선택사항)
    [Header("oxCharacter→Sprite 맵 (선택 사항)")]
    [SerializeField] private Dictionary<oxCharacter, Sprite> characterSpriteMap
        = new Dictionary<oxCharacter, Sprite>();

    private oxCharacter currentSelectedCharacter = null;
    private int currentSlotIndex = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Debug.LogWarning("[oxGameManager] 이미 다른 인스턴스가 존재 -> 파괴");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        isGameOver = false;
        DecideFirstTurn();
        UpdateStatusText();
    }

    private void Update()
    {
        if (isGameOver) return;

        // 예: 왼쪽 클릭 시 보드 셀 배치 체크
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void DecideFirstTurn()
    {
        if (randomFirstTurn)
            currentTeam = (Random.Range(0, 2) == 0) ? TeamType.X : TeamType.O;
        else
            currentTeam = TeamType.X;
    }

    private void HandleMouseClick()
    {
        // 2D 레이캐스트
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, Vector2.zero);
        if (!hit) return;

        Cell cell = hit.collider.GetComponent<Cell>();
        if (cell == null) return;

        // summonUIManager 쪽에서 선택된 소환 프리팹이 있는지 확인
        var summonPrefab = summonUIManager ? summonUIManager.GetCurrentSelectedPrefab() : null;
        if (summonPrefab != null)
        {
            SummonCharacter(cell, summonPrefab);
        }
        else
        {
            // 여기에 "공격" 등 다른 클릭 행동을 구현할 수도 있음
        }
    }

    /// <summary>
    /// 빈 칸이면 바로 배치, 아니면 전투 후 교체
    /// </summary>
    private void SummonCharacter(Cell targetCell, oxCharacter summonPrefab)
    {
        if (targetCell.IsEmpty())
        {
            // 빈 칸이면 바로 배치
            boardManager.PlaceCharacter(targetCell.Row, targetCell.Col, summonPrefab, currentTeam);
            if (summonUIManager) summonUIManager.ClearSelection();

            CheckAndAttackEnemiesAround(targetCell);
            EndTurnCheckWin();
        }
        else
        {
            // 누군가 이미 있으면 교체 전투
            BattleAndSummon(targetCell, summonPrefab);
        }
    }

    /// <summary>
    /// 타일을 점유 중인 캐릭터와 새로 소환할 캐릭터 간 동시 공격 로직
    /// </summary>
    private void BattleAndSummon(Cell targetCell, oxCharacter summonPrefab)
    {
        oxCharacter occupant = targetCell.Occupant;
        if (occupant == null)
        {
            // 혹시나 중간에 제거되었을 수도 있으니 예외 처리
            boardManager.PlaceCharacter(targetCell.Row, targetCell.Col, summonPrefab, currentTeam);
            if (summonUIManager) summonUIManager.ClearSelection();

            CheckAndAttackEnemiesAround(targetCell);
            EndTurnCheckWin();
            return;
        }

        // 새로 임시 소환
        oxCharacter temp = Instantiate(summonPrefab);
        float occupantHP  = occupant.CurrentHP;
        float occupantATK = occupant.AttackPower;
        float summonerHP  = temp.CurrentHP;
        float summonerATK = temp.AttackPower;

        occupantHP  -= summonerATK;
        summonerHP  -= occupantATK;

        bool occupantDead = occupantHP <= 0;
        bool summonerDead = summonerHP <= 0;

        if (occupantDead && !summonerDead)
        {
            Destroy(occupant.gameObject);
            targetCell.ClearCell();

            temp.transform.position = targetCell.transform.position;
            targetCell.SetOccupant(temp, currentTeam);

            CheckAndAttackEnemiesAround(targetCell);
        }
        else if (!occupantDead && summonerDead)
        {
            Destroy(temp.gameObject);
        }
        else if (occupantDead && summonerDead)
        {
            Destroy(occupant.gameObject);
            Destroy(temp.gameObject);
            targetCell.ClearCell();
        }
        else
        {
            // 둘 다 생존이면 아무 변화 없음
            Destroy(temp.gameObject);
        }

        if (summonUIManager) summonUIManager.ClearSelection();
        EndTurnCheckWin();
    }

    /// <summary>
    /// 캐릭터 배치 후, 그 캐릭터의 공격 패턴에 따라 주변 적에게 데미지
    /// </summary>
    private void CheckAndAttackEnemiesAround(Cell myCell)
    {
        var myChar = myCell.Occupant;
        if (myChar == null) return;

        float myATK = myChar.AttackPower;
        var pattern = myChar.AttackPattern;
        if (pattern == null) return;

        int centerR = myCell.Row;
        int centerC = myCell.Col;

        // 5x5 패턴(가운데 [2,2]) 기준 공격 가능 위치
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                if (r == 2 && c == 2) continue; // 자기 자신(중심)

                if (pattern.IsAttackable(r, c))
                {
                    int deltaR = r - 2;
                    int deltaC = c - 2;
                    int targetR = centerR + deltaR;
                    int targetC = centerC + deltaC;

                    oxCharacter enemy = boardManager.GetOccupant(targetR, targetC);
                    if (enemy != null)
                    {
                        var enemyTeam = boardManager.GetOccupantTeam(targetR, targetC);
                        if (enemyTeam != null && enemyTeam != currentTeam)
                        {
                            bool dead = enemy.TakeDamage(myATK);
                            if (dead)
                            {
                                boardManager.RemoveCharacter(targetR, targetC);
                            }
                        }
                    }
                }
            }
        }
    }

    private void EndTurnCheckWin()
    {
        if (boardManager.CheckWin(currentTeam))
        {
            isGameOver = true;
            if (statusText) statusText.text = $"{currentTeam} 승리!";
            return;
        }

        // 보드가 꽉 찼으면 무작위로 하나 제거 (예시)
        if (boardManager.IsBoardFull())
        {
            RemoveRandomCharacter();
        }

        // 턴 교대
        currentTeam = (currentTeam == TeamType.X) ? TeamType.O : TeamType.X;
        UpdateStatusText();
    }

    private void RemoveRandomCharacter()
    {
        oxCharacter[] allChars = FindObjectsByType<oxCharacter>(FindObjectsSortMode.None);
        if (allChars.Length == 0) return;
        var victim = allChars[Random.Range(0, allChars.Length)];
        Cell cell = victim.GetComponentInParent<Cell>();
        if (cell != null) cell.ClearCell();
        Destroy(victim.gameObject);
    }

    private void UpdateStatusText()
    {
        if (isGameOver) return;
        if (statusText) statusText.text = $"현재 턴: {currentTeam}";
    }

    public void OnClickEnterStageInGameManager()
    {
        SceneManager.LoadScene("oxGameScene");
    }

    //--------------------------------------------------------------------------
    // "자동으로 클릭한 캐릭터를 넣어주는" 방식
    //--------------------------------------------------------------------------
    public void SelectCharacter(oxCharacter c)
    {
        currentSelectedCharacter = c;
        Debug.Log($"[oxGameManager] 캐릭터 선택됨: {c.name}");
        SetSlotPreviewImage(currentSlotIndex, c);
    }

    private void SetSlotPreviewImage(int slotNum, oxCharacter chara)
    {
        Sprite sp = null;
        if (characterSpriteMap != null && characterSpriteMap.ContainsKey(chara))
        {
            sp = characterSpriteMap[chara];
        }

        switch (slotNum)
        {
            case 1: if (slotImage1) slotImage1.sprite = sp; break;
            case 2: if (slotImage2) slotImage2.sprite = sp; break;
            case 3: if (slotImage3) slotImage3.sprite = sp; break;
            case 4: if (slotImage4) slotImage4.sprite = sp; break;
        }
    }

    //----------------------------------------------------------------------
    // SummonButton 1~4 등록
    //----------------------------------------------------------------------
    public void OnClickRegister1()
    {
        if (currentSelectedCharacter == null)
        {
            Debug.LogWarning("등록 실패: 캐릭터 미선택");
            return;
        }
        if (summonButton1)
        {
            summonButton1.SetAssignedPrefab(currentSelectedCharacter);
            Debug.Log($"[oxGameManager] 1번에 '{currentSelectedCharacter.name}' 등록");
        }
        currentSlotIndex = 2;
    }

    public void OnClickRegister2()
    {
        if (currentSelectedCharacter == null)
        {
            Debug.LogWarning("등록 실패: 캐릭터 미선택");
            return;
        }
        if (summonButton2)
        {
            summonButton2.SetAssignedPrefab(currentSelectedCharacter);
            Debug.Log($"[oxGameManager] 2번에 '{currentSelectedCharacter.name}' 등록");
        }
        currentSlotIndex = 3;
    }

    public void OnClickRegister3()
    {
        if (currentSelectedCharacter == null)
        {
            Debug.LogWarning("등록 실패: 캐릭터 미선택");
            return;
        }
        if (summonButton3)
        {
            summonButton3.SetAssignedPrefab(currentSelectedCharacter);
            Debug.Log($"[oxGameManager] 3번에 '{currentSelectedCharacter.name}' 등록");
        }
        currentSlotIndex = 4;
    }

    public void OnClickRegister4()
    {
        if (currentSelectedCharacter == null)
        {
            Debug.LogWarning("등록 실패: 캐릭터 미선택");
            return;
        }
        if (summonButton4)
        {
            summonButton4.SetAssignedPrefab(currentSelectedCharacter);
            Debug.Log($"[oxGameManager] 4번에 '{currentSelectedCharacter.name}' 등록");
        }
        currentSlotIndex = 1;
    }

    //----------------------------------------------------------------------
    // 다른 스크립트에서 SummonButton 참조가 필요할 때 사용
    //----------------------------------------------------------------------
    public SummonButton GetSummonButton1() => summonButton1;
    public SummonButton GetSummonButton2() => summonButton2;
    public SummonButton GetSummonButton3() => summonButton3;
    public SummonButton GetSummonButton4() => summonButton4;

    //----------------------------------------------------------------------
    // 인벤토리/덱 갱신 등의 UI 호출 시
    //----------------------------------------------------------------------
    public void RefreshInventoryDisplay()
    {
        Debug.Log("[oxGameManager] 인벤토리 화면 갱신 요청됨");
    }
}
