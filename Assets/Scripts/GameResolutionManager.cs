using UnityEngine;
using UnityEngine.UI;

public class GameResolutionManager : MonoBehaviour
{
    [Header("해상도 설정 (PC 빌드 전용)")]
    [Tooltip("PC 윈도우 빌드에서만 적용 (모바일은 실제 기기 해상도를 사용).")]
    public int targetWidth = 1080;
    public int targetHeight = 1920;
    public bool fullScreen = false;

    [Header("메인 Canvas (CanvasScaler가 있어야 함)")]
    [Tooltip("UI Scale Mode: ScaleWithScreenSize 로 설정할 CanvasScaler를 연결하세요.")]
    public CanvasScaler mainCanvasScaler;

    [Header("Match Mode 설정")]
    [Tooltip("Expand 모드 사용 시, 세로가 잘리지 않고 전체가 표시됨(넓은 화면에선 옆이 생길 수 있음).")]
    public bool useExpandMode = true;

    [Range(0f, 1f)]
    [Tooltip("useExpandMode==false 일 때만 유효. 가로(0)/세로(1) 중 어느 쪽을 기준으로 매칭할지 결정")]
    public float matchWidthOrHeight = 0.5f;

    private void Awake()
    {
        // PC(Windows/Mac/Linux) 빌드에서만 해상도 강제 적용
        // (모바일(Android/iOS)은 Screen.SetResolution이 의미 없음)
#if !UNITY_ANDROID && !UNITY_IOS
        Screen.SetResolution(targetWidth, targetHeight, fullScreen);
#endif
    }

    private void Start()
    {
        if (mainCanvasScaler != null)
        {
            // 1) CanvasScaler 모드: ScaleWithScreenSize
            mainCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // 2) 기준 해상도: (1080,1920)
            mainCanvasScaler.referenceResolution = new Vector2(targetWidth, targetHeight);

            // 3) 화면 매칭 모드
#if UNITY_2021_2_OR_NEWER
            // Unity 2021.2+ 에서는 ScreenMatchMode에 Expand / Shrink / MatchWidthOrHeight 있음
            if (useExpandMode)
            {
                // Expand 모드: 전체 화면에 맞추어 "세로가 절대 잘리지 않도록" 맞춤
                mainCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            else
            {
                // MatchWidthOrHeight 사용
                mainCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                mainCanvasScaler.matchWidthOrHeight = matchWidthOrHeight;
            }
#else
            // 구버전 Unity라면 Expand/Shrink가 없고 MatchWidthOrHeight만 있음
            // 가급적 matchWidthOrHeight = 0(가로우선) ~ 1(세로우선)을 조정해서 써야 함
            mainCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            mainCanvasScaler.matchWidthOrHeight = matchWidthOrHeight;
#endif
        }
        else
        {
            Debug.LogWarning("[GameResolutionManager] mainCanvasScaler가 null이라 Canvas 설정 불가!");
        }
    }
}
