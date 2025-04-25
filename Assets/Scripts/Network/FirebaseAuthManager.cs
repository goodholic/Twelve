// Assets\Scripts\Network\FirebaseAuthManager.cs

using System;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    private FirebaseAuth firebaseAuth;
    private FirebaseUser currentUser;

    // 인증 완료/실패 시 알림용
    public event Action<FirebaseUser> OnLoginSuccess;
    public event Action<string> OnLoginFail;

    private void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Firebase 종속성 확인
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus result = task.Result;
            if (result == DependencyStatus.Available)
            {
                Debug.Log("[FirebaseAuthManager] Firebase Ready.");
                firebaseAuth = FirebaseAuth.DefaultInstance;

                // 만약 이미 로그인된 상태가 있다면 currentUser에 세팅
                currentUser = firebaseAuth.CurrentUser;
                if (currentUser != null)
                {
                    Debug.Log($"[FirebaseAuthManager] Already signed in as {currentUser.UserId}");

                    // (추가) 로그인 화면을 무조건 먼저 띄우고 싶으므로, 이전 세션을 강제로 종료
                    Debug.Log("[FirebaseAuthManager] Already signed in => forcing sign out now to show login screen first.");
                    SignOut();      // 실제 로그아웃
                    currentUser = null;
                }
            }
            else
            {
                Debug.LogError($"[FirebaseAuthManager] Could not resolve Firebase dependencies: {result}");
            }
        });
    }

    // ------------------------------
    // (1) 익명(게스트) 로그인 예시
    // ------------------------------
    public void SignInAnonymously()
    {
        if (firebaseAuth == null)
        {
            Debug.LogError("[FirebaseAuthManager] firebaseAuth is null. Check initialization.");
            return;
        }

        firebaseAuth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("[FirebaseAuthManager] Anonymous sign-in failed.");
                OnLoginFail?.Invoke("Sign-In Failed (Anonymous)");
            }
            else
            {
                currentUser = firebaseAuth.CurrentUser;
                Debug.Log($"[FirebaseAuthManager] Anonymous sign-in success: {currentUser.UserId}");
                OnLoginSuccess?.Invoke(currentUser);
            }
        });
    }

    // ------------------------------
    // (2) 구글 로그인 (실제 사용)
    // ------------------------------
    public void SignInWithGoogle(string idToken)
    {
        if (firebaseAuth == null)
        {
            Debug.LogError("[FirebaseAuthManager] firebaseAuth is null.");
            return;
        }
        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogWarning("[FirebaseAuthManager] SignInWithGoogle called with empty idToken!");
            OnLoginFail?.Invoke("Sign-In Failed (Google) - empty token");
            return;
        }

        // 구글에서 받은 idToken으로 Credential 생성
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[FirebaseAuthManager] Google sign-in failed.");
                OnLoginFail?.Invoke("Sign-In Failed (Google)");
            }
            else
            {
                currentUser = firebaseAuth.CurrentUser;
                Debug.Log($"[FirebaseAuthManager] Google sign-in success: {currentUser.UserId}");
                OnLoginSuccess?.Invoke(currentUser);
            }
        });
    }

    // ------------------------------
    // (3) 애플 로그인 (간단 예시)
    // ------------------------------
    public void SignInWithApple(string idToken)
    {
        if (firebaseAuth == null)
        {
            Debug.LogError("[FirebaseAuthManager] firebaseAuth is null.");
            return;
        }

        // idToken은 애플 OAuth 후 얻은 토큰
        Credential credential = OAuthProvider.GetCredential("apple.com", idToken, null, null);

        firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[FirebaseAuthManager] Apple sign-in failed.");
                OnLoginFail?.Invoke("Sign-In Failed (Apple)");
            }
            else
            {
                currentUser = firebaseAuth.CurrentUser;
                Debug.Log($"[FirebaseAuthManager] Apple sign-in success: {currentUser.UserId}");
                OnLoginSuccess?.Invoke(currentUser);
            }
        });
    }

    public FirebaseUser GetCurrentUser()
    {
        return currentUser;
    }

    public string GetUserId()
    {
        return currentUser?.UserId;
    }

    // 원한다면 로그아웃 함수도 가능
    public void SignOut()
    {
        if (firebaseAuth != null)
        {
            firebaseAuth.SignOut();
            Debug.Log("[FirebaseAuthManager] Signed out.");
            currentUser = null;
        }
    }
}
