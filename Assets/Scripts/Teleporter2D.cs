using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(Teleporter2D))]
public class Teleporter2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Open Destination Scene"))
        {
            Teleporter2D teleporter = (Teleporter2D)target;
            if (teleporter.teleportDestination != null)
            {
                // 씬 자산 가져오는 부분 (간단 예시)
                string scenePath = AssetDatabase.GetAssetPath(teleporter.teleportDestination);
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(
                            AssetDatabase.GetAssetPath(sceneAsset),
                            OpenSceneMode.Single
                        );
                    }
                }
            }
        }
    }
}
#endif

/// <summary>
/// 2D 환경(2D 캔버스 포함)에서 캐릭터 또는 플레이어를
/// 트리거로 감지하여 다른 지점(텔레포트 목적지)로 순간이동시키는 스크립트.
///
/// (사용법)
/// 1) 지역1에 'teleporterA' 오브젝트(예: BoxCollider2D, isTrigger 체크)를 만든 뒤
///    이 스크립트를 붙이고, teleportDestination = 지역2 위치(Transform) 지정.
/// 2) 지역2에도 'teleporterB' 오브젝트를 만들어
///    teleportDestination = 지역1 위치(Transform) 지정.
/// 3) 인스펙터에서 'teleportTag'를 "Player"나 원하는 태그로 설정하면,
///    해당 태그의 오브젝트만 텔레포트.
/// 4) 필요 시 쿨다운, 이펙트, 사운드 등 설정 가능.
/// </summary>
public class Teleporter2D : MonoBehaviour
{
    [Header("텔레포트 목적지 (필수)")]
    [Tooltip("기존에는 여기로 이동했으나, 이제 Character 스크립트의 regionXTeleportSpawn으로 이동하도록 변경되었습니다.")]
    public Transform teleportDestination;

    [Header("텔레포트 대상 태그")]
    [Tooltip("이 태그를 가진 2D 오브젝트만 텔레포트 (예: Player). 비워두면 무시")]
    public string teleportTag = "Player";

    [Header("텔레포트 쿨다운(초)")]
    [Tooltip("텔레포트 후 재진입을 방지하기 위한 쿨다운 시간. 0이면 연속 텔레포트 가능.")]
    public float teleportCooldown = 1f;

    // 내부에서 시간 체크용
    private float nextTeleportTime = 0f;

    [Header("텔레포트 시 회전값 동기화 여부")]
    [Tooltip("true면 목적지의 회전값(Transform.rotation)까지 동일하게 맞춤. false면 회전은 변경 안 함.")]
    public bool matchDestinationRotation = false;

    [Header("텔레포트 이펙트(선택)")]
    [Tooltip("텔레포트 직전 재생할 파티클")]
    public ParticleSystem effectBeforeTeleport;
    [Tooltip("텔레포트 직후 재생할 파티클")]
    public ParticleSystem effectAfterTeleport;

    [Header("사운드 효과(선택)")]
    [Tooltip("텔레포트 순간에 재생할 오디오 클립")]
    public AudioClip teleportSound;
    [Tooltip("오디오 출력용 AudioSource (지정 안 하면 스크립트 추가)")]
    public AudioSource audioSource;

    private void Start()
    {
        // AudioSource가 연결 안 되어 있고 teleportSound가 있다면 자동 추가
        if (audioSource == null && teleportSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 2D 텔레포트이므로, 필수로 Collider2D(isTrigger) 필요
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[Teleporter2D] {name} 오브젝트에 Collider2D가 없음. (isTrigger 필요)");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[Teleporter2D] {name}의 Collider2D.isTrigger=false -> 트리거로 설정해야 함.");
        }
    }

    /// <summary>
    /// 2D 트리거 진입 시 콜백
    /// </summary>
    /// <param name="collision">충돌한 2D 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1) 쿨다운 체크
        if (Time.time < nextTeleportTime) return;

        // 2) 태그 체크
        if (!string.IsNullOrEmpty(teleportTag) && !collision.CompareTag(teleportTag))
            return;

        // 3) 실제 텔레포트 대상(플레이어 등)
        Transform target = collision.transform;

        // 4) 텔레포트 직전 이펙트/사운드
        if (effectBeforeTeleport != null)
        {
            Instantiate(effectBeforeTeleport, target.position, Quaternion.identity);
        }
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // ============ [수정된 핵심 로직 + 추가 로직] ============
        // Character 스크립트 확인
        Character charComp = target.GetComponent<Character>();
        if (charComp != null)
        {
            // ------------------------------------------------------
            // region1 -> region2 일 때 -> Opponent OurMonster Panel로
            // ------------------------------------------------------
            if (charComp.areaIndex == 1)
            {
                // region2TeleportSpawn.position(월드 좌표)로 이동
                if (charComp.region2TeleportSpawn != null)
                {
                    target.position = charComp.region2TeleportSpawn.position;
                }
                else
                {
                    Debug.LogWarning($"[Teleporter2D] '{target.name}' 의 region2TeleportSpawn이 null이어서 텔레포트 기본 위치로 이동");
                    // fallback
                    if (teleportDestination != null)
                        target.position = teleportDestination.position;
                }

                // areaIndex 전환
                charComp.areaIndex = 2;

                // 공격 유닛이면 텔레포트 카운트 증가
                if (charComp.isCharAttack)
                {
                    charComp.teleportCount++;
                }

                // ---- [추가] Opponent OurMonster Panel로 re-parent ----
                // (PlacementManager.instance.opponentCharacterPanel가 있다고 가정)
                PlacementManager pm = PlacementManager.Instance;
                if (pm != null && pm.opponentCharacterPanel != null)
                {
                    // target을 RectTransform으로 취급하기 위해
                    RectTransform rtTarget = target.GetComponent<RectTransform>();
                    if (rtTarget != null)
                    {
                        // 부모를 opponentCharacterPanel로 설정
                        rtTarget.SetParent(pm.opponentCharacterPanel, worldPositionStays: false);

                        // localPosition 재계산
                        Vector2 localPos =
                            pm.opponentCharacterPanel.InverseTransformPoint(charComp.region2TeleportSpawn != null
                                ? charComp.region2TeleportSpawn.position
                                : target.position);
                        rtTarget.anchoredPosition = localPos;
                        rtTarget.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        // 그냥 월드 좌표로 이동
                        target.SetParent(null, false);
                        target.position = charComp.region2TeleportSpawn != null
                            ? charComp.region2TeleportSpawn.position
                            : target.position;
                    }
                    Debug.Log("[Teleporter2D] area1 → area2 텔레포트 후 OpponentOurMonsterPanel로 re-parent됨.");
                }
            }
            else
            {
                // ------------------------------------------------------
                // region2 -> region1 일 때 -> OurMonster Panel로
                // ------------------------------------------------------
                if (charComp.region1TeleportSpawn != null)
                {
                    target.position = charComp.region1TeleportSpawn.position;
                }
                else
                {
                    Debug.LogWarning($"[Teleporter2D] '{target.name}' 의 region1TeleportSpawn이 null이어서 텔레포트 기본 위치로 이동");
                    if (teleportDestination != null)
                        target.position = teleportDestination.position;
                }

                // areaIndex 전환
                charComp.areaIndex = 1;

                // 공격 유닛이면 텔레포트 카운트 증가
                if (charComp.isCharAttack)
                {
                    charComp.teleportCount++;
                }

                // ---- [추가] OurMonster Panel로 re-parent ----
                PlacementManager pm = PlacementManager.Instance;
                if (pm != null && pm.ourMonsterPanel != null)
                {
                    RectTransform rtTarget = target.GetComponent<RectTransform>();
                    if (rtTarget != null)
                    {
                        rtTarget.SetParent(pm.ourMonsterPanel, worldPositionStays: false);

                        Vector2 localPos =
                            pm.ourMonsterPanel.InverseTransformPoint(charComp.region1TeleportSpawn != null
                                ? charComp.region1TeleportSpawn.position
                                : target.position);
                        rtTarget.anchoredPosition = localPos;
                        rtTarget.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        target.SetParent(null, false);
                        target.position = charComp.region1TeleportSpawn != null
                            ? charComp.region1TeleportSpawn.position
                            : target.position;
                    }
                    Debug.Log("[Teleporter2D] area2 → area1 텔레포트 후 OurMonsterPanel로 re-parent됨.");
                }
            }

            // 회전값 동기화 필요 시
            if (matchDestinationRotation && teleportDestination != null)
            {
                target.rotation = teleportDestination.rotation;
            }
        }
        else
        {
            // Character 없는 경우(플레이어 등)만 기존 목적지 사용
            // (혹은 원하는 로직으로 변경)
            if (teleportDestination != null)
            {
                target.position = teleportDestination.position;
                if (matchDestinationRotation)
                {
                    target.rotation = teleportDestination.rotation;
                }
            }
        }
        // ======================================================

        // 5) 텔레포트 직후 이펙트
        if (effectAfterTeleport != null)
        {
            Instantiate(effectAfterTeleport, target.position, Quaternion.identity);
        }

        // 6) 쿨다운
        if (teleportCooldown > 0f)
        {
            nextTeleportTime = Time.time + teleportCooldown;
        }
    }
}
