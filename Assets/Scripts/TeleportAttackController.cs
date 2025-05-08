using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터가 텔레포트를 통해 반대 지역으로 넘어가서,
/// 해당 지역의 "몬스터가 아닌 적 캐릭터"를 공격하는 스크립트.
///
/// (주의) 실제 텔레포트 동작은 Character.cs의 OnArriveCastle() 등에서 처리될 수도 있으나,
/// 여기서는 데모/테스트용으로 수동/자동 텔레포트 로직을 포함하고 있습니다.
/// 
/// [수정 사항]
/// - 텔레포트 후, areaIndex에 따라
///   area1 -> OurMonster Panel,
///   area2 -> Opponent OurMonster Panel
///   에 재배치(부모 설정 + localPosition 재계산)하도록 변경.
/// </summary>
public class TeleportAttackController : MonoBehaviour
{
    [Header("=== 텔레포트 위치 설정 ===")]
    [Tooltip("지역1 → 지역2로 이동할 때 도착할 위치 (캔버스의 Tile Panel 자식 타일)")]
    public Transform region2TeleportPoint;

    [Tooltip("지역2 → 지역1로 이동할 때 도착할 위치 (캔버스의 Tile Panel 자식 타일)")]
    public Transform region1TeleportPoint;

    [Header("=== 텔레포트 관련 옵션 ===")]
    [Tooltip("텔레포트 후 재사용 대기 시간(쿨다운)")]
    public float teleportCooldown = 5f;

    [Tooltip("수동 텔레포트 키 (데모용)")]
    public KeyCode userInputKey = KeyCode.T;

    [Tooltip("true면, 일정 간격으로 자동 텔레포트 시도(데모용)")]
    public bool autoTeleport = false;

    [Tooltip("autoTeleport=true 일 때, 텔레포트 실행 간격(초)")]
    public float autoTeleportInterval = 8f;

    [Header("=== 공격 대상 옵션 ===")]
    [Tooltip("true면 '몬스터'는 공격하지 않음")]
    public bool skipMonster = true;

    // 내부 상태
    private Character myCharacter;
    private float nextTeleportTime = 0f;
    private float autoTeleportTimer = 0f;

    private void Awake()
    {
        myCharacter = GetComponent<Character>();
        if (myCharacter == null)
        {
            Debug.LogError("[TeleportAttackController] Character 컴포넌트를 찾지 못했습니다!");
        }
    }

    private void Update()
    {
        if (myCharacter == null) return;

        // (1) 수동 텔레포트(키 입력)
        if (!autoTeleport)
        {
            if (Input.GetKeyDown(userInputKey))
            {
                TryDoTeleportAttack();
            }
        }
        // (2) 자동 텔레포트
        else
        {
            autoTeleportTimer += Time.deltaTime;
            if (autoTeleportTimer >= autoTeleportInterval)
            {
                autoTeleportTimer = 0f;
                TryDoTeleportAttack();
            }
        }
    }

    /// <summary>
    /// 텔레포트 + 공격 시도(쿨다운 검사 후 실행)
    /// </summary>
    public void TryDoTeleportAttack()
    {
        if (Time.time < nextTeleportTime)
        {
            Debug.Log($"[TeleportAttackController] 텔레포트 쿨다운 남음: {(nextTeleportTime - Time.time):F1}초");
            return;
        }

        // 텔레포트 시도
        DoTeleport();

        // 쿨타임 갱신
        nextTeleportTime = Time.time + teleportCooldown;

        // 같은 지역에 있는 적 캐릭터를 공격
        DoAttackInCurrentArea();
    }

    /// <summary>
    /// 실제 텔레포트 동작:
    /// - areaIndex=1 → areaIndex=2 (Opponent OurMonster Panel로 이동)
    /// - areaIndex=2 → areaIndex=1 (OurMonster Panel로 이동)
    /// </summary>
    private void DoTeleport()
    {
        if (myCharacter == null) return;

        // PlacementManager에서 OurMonsterPanel / OpponentOurMonsterPanel 참조
        PlacementManager pm = PlacementManager.Instance;
        if (pm == null)
        {
            Debug.LogWarning("[TeleportAttackController] PlacementManager가 없어 텔레포트 작업 불가!");
            return;
        }

        // 지역1 → 지역2
        if (myCharacter.areaIndex == 1)
        {
            if (region2TeleportPoint != null)
            {
                // 부모를 Opponent OurMonster Panel로
                if (pm.opponentCharacterPanel != null)
                {
                    Transform targetParent = pm.opponentCharacterPanel;
                    myCharacter.transform.SetParent(targetParent, worldPositionStays: false);

                    // localPosition 재계산
                    Vector2 localPos = targetParent.InverseTransformPoint(region2TeleportPoint.position);
                    myCharacter.transform.localPosition = localPos;
                    myCharacter.transform.localRotation = Quaternion.identity;

                    // areaIndex 갱신
                    myCharacter.areaIndex = 2;

                    Debug.Log("[TeleportAttackController] area1→area2 텔레포트 (부모=OpponentOurMonsterPanel)");
                }
                else
                {
                    // Opponent OurMonster Panel이 없으면 -> 그냥 월드 위치로 배치
                    myCharacter.transform.SetParent(null, false);
                    myCharacter.transform.position = region2TeleportPoint.position;
                    myCharacter.transform.localRotation = Quaternion.identity;

                    myCharacter.areaIndex = 2;
                    Debug.LogWarning("[TeleportAttackController] OpponentOurMonsterPanel이 없어 월드 위치 이동 처리");
                }
            }
            else
            {
                Debug.LogWarning("[TeleportAttackController] region2TeleportPoint가 null입니다.");
            }
        }
        // 지역2 → 지역1
        else
        {
            if (region1TeleportPoint != null)
            {
                // 부모를 OurMonsterPanel로
                if (pm.ourMonsterPanel != null)
                {
                    Transform targetParent = pm.ourMonsterPanel;
                    myCharacter.transform.SetParent(targetParent, worldPositionStays: false);

                    // localPosition 재계산
                    Vector2 localPos = targetParent.InverseTransformPoint(region1TeleportPoint.position);
                    myCharacter.transform.localPosition = localPos;
                    myCharacter.transform.localRotation = Quaternion.identity;

                    // areaIndex 갱신
                    myCharacter.areaIndex = 1;

                    Debug.Log("[TeleportAttackController] area2→area1 텔레포트 (부모=OurMonsterPanel)");
                }
                else
                {
                    // OurMonsterPanel이 없으면 -> 그냥 월드 위치로 배치
                    myCharacter.transform.SetParent(null, false);
                    myCharacter.transform.position = region1TeleportPoint.position;
                    myCharacter.transform.localRotation = Quaternion.identity;

                    myCharacter.areaIndex = 1;
                    Debug.LogWarning("[TeleportAttackController] OurMonsterPanel이 없어 월드 위치 이동 처리");
                }
            }
            else
            {
                Debug.LogWarning("[TeleportAttackController] region1TeleportPoint가 null입니다.");
            }
        }
    }

    /// <summary>
    /// 텔레포트 후, 내 areaIndex와 같은 곳에 있는 '적 캐릭터' 공격
    /// (skipMonster=true 시, Monster는 무시)
    /// </summary>
    private void DoAttackInCurrentArea()
    {
        if (myCharacter == null) return;

        // 모든 Character를 훑으며, 내 areaIndex와 같은 '적'을 찾는다
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> targetList = new List<Character>();

        foreach (var c in allChars)
        {
            if (c == null || c == myCharacter) 
                continue;

            // 몬스터를 공격 제외하고 싶으면
            if (skipMonster && !c.isCharAttack)
                continue;

            // areaIndex가 같아야 공격 가능
            if (c.areaIndex == myCharacter.areaIndex)
            {
                float dist = Vector2.Distance(myCharacter.transform.position, c.transform.position);
                if (dist <= myCharacter.attackRange)
                {
                    targetList.Add(c);
                }
            }
        }

        // 공격 실행
        if (targetList.Count > 0)
        {
            foreach (var tgt in targetList)
            {
                tgt.TakeDamage(myCharacter.attackPower);
                Debug.Log($"[TeleportAttackController] {tgt.characterName}에게 {myCharacter.attackPower} 데미지");
            }
        }
        else
        {
            Debug.Log("[TeleportAttackController] 공격 범위 안에 적 캐릭터가 없음");
        }
    }
}
