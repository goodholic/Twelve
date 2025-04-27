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
    
    [SerializeField] private string databaseUrl = "https://twelve-31d24-default-rtdb.firebaseio.com/";

    // 인증 완료/실패 시 알림용
    public event Action<FirebaseUser> OnLoginSuccess;
    public event Action<string> OnLoginFail;

    private void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;

            // (추가) 만약 현재 오브젝트가 루트가 아니라면, 부모에서 떼어낸다.
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
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
                
                // Firebase 앱이 이미 초기화되었는지 확인
                if (FirebaseApp.DefaultInstance == null)
                {
                    // Firebase 앱이 초기화되지 않았으면 초기화 (DB URL 포함)
                    AppOptions options = new AppOptions();
                    options.DatabaseUrl = new System.Uri(databaseUrl);
                    FirebaseApp.Create(options);
                    Debug.Log("[FirebaseAuthManager] Firebase app initialized with database URL.");
                }
                else if (FirebaseApp.DefaultInstance.Options.DatabaseUrl == null)
                {
                    // Firebase 앱은 초기화되었으나 DB URL이 설정되지 않은 경우
                    FirebaseApp.DefaultInstance.Options.DatabaseUrl = new System.Uri(databaseUrl);
                    Debug.Log("[FirebaseAuthManager] Set database URL to existing Firebase app.");
                }
                
                firebaseAuth = FirebaseAuth.DefaultInstance;

                // (아래 부분) 기존에는 currentUser != null이면 무조건 SignOut()을 했음.
                // [수정됨] : 이미 로그인되어 있으면 강제 로그아웃하지 않는다.
                currentUser = firebaseAuth.CurrentUser;
                if (currentUser != null)
                {
                    Debug.Log($"[FirebaseAuthManager] Already signed in as {currentUser.UserId}");
                    // ---- 여기에 있던 SignOut()을 제거했습니다. ----
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
