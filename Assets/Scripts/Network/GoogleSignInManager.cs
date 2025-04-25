// Assets\Scripts\Network\GoogleSignInManager.cs

#if GOOGLE_SIGNIN_AVAILABLE
// 만약 Google Sign-In SDK 및 필요 라이브러리가 설치되어 있고,
// Scripting Define Symbols에 'GOOGLE_SIGNIN_AVAILABLE'를 추가했다면,
// 아래 UnityEngine 외에 실제 Google Sign-In 관련 using 문을 넣어야 합니다.
//
// 예시:
// using Google;
// using GoogleSignIn;
// using GoogleSignInOptions;
#endif

using UnityEngine;
using System;

/// <summary>
/// 구글 로그인 프로세스 담당.
/// 실제로는 Google Sign-In SDK가 필요하며,
/// 'GOOGLE_SIGNIN_AVAILABLE' 심볼을 사용해 분기 처리.
/// </summary>
public class GoogleSignInManager : MonoBehaviour
{
    [Header("Google SignIn 설정")]
    [Tooltip("Firebase 콘솔과 GCP 콘솔에서 발급받은 Web Client ID를 입력하세요.")]
    [SerializeField] private string webClientId = "";

    // 로그인 성공 시 호출될 이벤트
    public event Action<string> OnGoogleSignInSuccess; // idToken 전달

    private static GoogleSignInManager _instance;
    public static GoogleSignInManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬 어디에든 하나는 존재해야 함
                _instance = FindObjectOfType<GoogleSignInManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GoogleSignInManager");
                    _instance = go.AddComponent<GoogleSignInManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;

            // (추가) 루트로 이동 후 DontDestroyOnLoad
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if GOOGLE_SIGNIN_AVAILABLE
        Debug.Log("[GoogleSignInManager] Google Sign-In SDK가 감지되었습니다. 실제 Google 로그인을 시도할 수 있습니다.");
#else
        Debug.LogWarning("[GoogleSignInManager] Google SignIn SDK가 프로젝트에 없습니다. 이 기능은 현재 작동하지 않습니다.");
        Debug.LogWarning("구글 로그인을 사용하려면 Google SignIn SDK를 설치하고 'GOOGLE_SIGNIN_AVAILABLE' 심볼을 설정하세요.");
#endif
    }

    /// <summary>
    /// 구글 로그인 버튼을 눌렀을 때 호출
    /// </summary>
    public void OnPressGoogleSignIn()
    {
#if GOOGLE_SIGNIN_AVAILABLE
        SignInWithGoogleSDK();
#else
        Debug.LogWarning("[GoogleSignInManager] Google SignIn SDK가 없어 로그인을 시도할 수 없습니다.");
#endif
    }

#if GOOGLE_SIGNIN_AVAILABLE
    /// <summary>
    /// (실제) Google Sign-In 프로세스
    /// </summary>
    private void SignInWithGoogleSDK()
    {
        // 아래는 실제 Google Sign-In SDK 사용 시의 예시 로직입니다.
        // (GoogleSignIn, GoogleSignInConfiguration, etc.)
        //
        // 1) GoogleSignIn.Configuration = new GoogleSignInConfiguration { ... };
        // 2) GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task => { ... });
        
        // 1) 구성 설정
        // GoogleSignIn.Configuration = new GoogleSignInConfiguration
        // {
        //     WebClientId = webClientId,
        //     RequestEmail = true,
        //     RequestIdToken = true
        // };

        // 2) 비동기 로그인 시도
        // GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        // {
        //     if (task.IsCanceled || task.IsFaulted)
        //     {
        //         Debug.LogWarning("[GoogleSignInManager] 구글 로그인 실패(비정상 종료).");
        //         return;
        //     }
        //
        //     GoogleSignInUser user = task.Result;
        //     string idToken = user.IdToken;
        //     if (string.IsNullOrEmpty(idToken))
        //     {
        //         Debug.LogWarning("[GoogleSignInManager] 구글 로그인 성공했으나 idToken이 비어있습니다!");
        //         return;
        //     }
        //
        //     // FirebaseAuthManager 통해 파이어베이스에 전달
        //     FirebaseAuthManager.Instance.SignInWithGoogle(idToken);
        // });
        
        Debug.Log("[GoogleSignInManager] 실제 구글 로그인 SDK를 통한 로그인이 진행됩니다. (예시)");
    }
#else
    /// <summary>
    /// Google Sign-In SDK가 없을 때 사용되는 더미 메서드
    /// </summary>
    private void SignInWithGoogleSDK()
    {
        Debug.LogWarning("[GoogleSignInManager] 구글 SDK 없음 -> SignInWithGoogleSDK() 호출 무의미");
    }
#endif

    /// <summary>
    /// 구글 로그아웃
    /// </summary>
    public void SignOut()
    {
#if GOOGLE_SIGNIN_AVAILABLE
        // GoogleSignIn.DefaultInstance.SignOut();
        Debug.Log("[GoogleSignInManager] 구글 로그아웃 완료(예시).");
#else
        Debug.LogWarning("[GoogleSignInManager] Google SignIn SDK가 없어 로그아웃 불가.");
#endif
    }
}
