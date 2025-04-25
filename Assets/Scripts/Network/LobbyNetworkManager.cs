using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Fusion;

public class LobbyNetworkManager : MonoBehaviour
{
    [Header("Login Panel UI")]
    [SerializeField] private GameObject loginPanel;              // 로그인 패널
    [SerializeField] private Button guestLoginButton;            // 게스트 로그인 버튼
    [SerializeField] private Button googleLoginButton;           // 구글 로그인 버튼
    [SerializeField] private TextMeshProUGUI userInfoText;       // 로그인 상태 표시 텍스트

    private void Start()
    {
        // 1) Inspector 연결 누락 여부 점검
        if (loginPanel == null)
            Debug.LogError("[LobbyNetworkManager] loginPanel이 Inspector에서 할당되지 않았습니다!");
        if (guestLoginButton == null)
            Debug.LogError("[LobbyNetworkManager] guestLoginButton이 Inspector에서 할당되지 않았습니다!");
        if (googleLoginButton == null)
            Debug.LogError("[LobbyNetworkManager] googleLoginButton이 Inspector에서 할당되지 않았습니다!");
        if (userInfoText == null)
            Debug.LogError("[LobbyNetworkManager] userInfoText가 Inspector에서 할당되지 않았습니다!");

        // 2) 로그인 패널 열기
        if (loginPanel)
            loginPanel.SetActive(true);

        // 3) 버튼 클릭 리스너 설정
        if (guestLoginButton != null)
        {
            guestLoginButton.onClick.AddListener(() =>
            {
                if (FirebaseAuthManager.Instance == null)
                {
                    Debug.LogError("[LobbyNetworkManager] FirebaseAuthManager.Instance가 null입니다. "
                                   + "씬에 FirebaseAuthManager 오브젝트가 있나요?");
                    return;
                }

                // 게스트 로그인
                FirebaseAuthManager.Instance.SignInAnonymously();
            });
        }

        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.AddListener(() =>
            {
                // GoogleSignInManager 통해 실제 구글 로그인 시도
                if (GoogleSignInManager.Instance == null)
                {
                    Debug.LogError("[LobbyNetworkManager] GoogleSignInManager.Instance가 null입니다. "
                                   + "씬에 GoogleSignInManager 오브젝트가 있나요?");
                    return;
                }

                GoogleSignInManager.Instance.OnPressGoogleSignIn();
            });

#if !GOOGLE_SIGNIN_AVAILABLE
            // SDK가 없으면 구글 버튼 비활성화
            googleLoginButton.interactable = false;
            Debug.LogWarning("[LobbyNetworkManager] GOOGLE_SIGNIN_AVAILABLE가 정의되지 않아 구글 로그인 버튼을 비활성화합니다.");
#endif
        }

        // 4) FirebaseAuthManager 이벤트 등록
        if (FirebaseAuthManager.Instance == null)
        {
            Debug.LogError("[LobbyNetworkManager] FirebaseAuthManager.Instance가 null이라 이벤트 등록 불가");
            return;
        }

        FirebaseAuthManager.Instance.OnLoginSuccess += OnLoginSuccess;
        FirebaseAuthManager.Instance.OnLoginFail    += OnLoginFail;
    }

    private void OnDestroy()
    {
        // 씬이 닫히거나 오브젝트 파괴 시점에 이벤트 해제
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
            FirebaseAuthManager.Instance.OnLoginFail    -= OnLoginFail;
        }
    }

    private void OnLoginSuccess(FirebaseUser user)
    {
        // 로그인 성공
        Debug.Log($"[LobbyNetworkManager] Login success => userId={user.UserId}");

        // 로그인 패널 닫기
        if (loginPanel)
            loginPanel.SetActive(false);

        // Firestore (또는 임시 Mock)로부터 유저 정보 로드
        FirebaseUserDataManager.Instance.LoadUserData(
            user.UserId,
            (profileData) =>
            {
                Debug.Log($"[LobbyNetworkManager] LoadUserData success. Level={profileData.level}, Gold={profileData.gold}");

                // UI 텍스트 표시
                if (userInfoText)
                {
                    userInfoText.text = $"Welcome, {profileData.userName}\nLV={profileData.level} / Gold={profileData.gold}";
                }

                // 간단한 Analytics 예시 (커스텀)
                FirebaseAnalyticsHelper.SetUserProperty("PlayerLevel", profileData.level.ToString());
            },
            (err) =>
            {
                // 로드 실패
                Debug.LogWarning($"[LobbyNetworkManager] LoadUserData fail: {err}");
            }
        );
    }

    private void OnLoginFail(string reason)
    {
        // 로그인 실패
        Debug.LogWarning("[LobbyNetworkManager] Login failed => " + reason);

        if (userInfoText)
        {
            userInfoText.text = "Login Failed!";
        }
    }

    // =================================
    //    (선택) Fusion 네트워킹 예시
    // =================================
    public void OnClickHostGame()
    {
        FusionNetworkManager fusionMgr = FindFirstObjectByType<FusionNetworkManager>();
        if (fusionMgr != null)
        {
            fusionMgr.StartGame(GameMode.Host);
        }
        else
        {
            Debug.LogWarning("[LobbyNetworkManager] FusionNetworkManager를 찾지 못했습니다.");
        }
    }

    public void OnClickJoinGame()
    {
        FusionNetworkManager fusionMgr = FindFirstObjectByType<FusionNetworkManager>();
        if (fusionMgr != null)
        {
            fusionMgr.JoinGame();
        }
        else
        {
            Debug.LogWarning("[LobbyNetworkManager] FusionNetworkManager를 찾지 못했습니다.");
        }
    }
}

public static class FirebaseAnalyticsHelper
{
    public static void SetUserProperty(string propertyName, string propertyValue)
    {
        Debug.Log($"[FirebaseAnalyticsHelper] SetUserProperty: {propertyName}={propertyValue}");
    }
}
