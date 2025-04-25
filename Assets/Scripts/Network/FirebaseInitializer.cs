using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class FirebaseInitializer : MonoBehaviour
{
    private FirebaseApp app;
    
    [SerializeField] private string databaseUrl = "https://twelve-31d24-default-rtdb.firebaseio.com/";

    private async void Awake()
    {
        // Firebase 초기화
        await InitializeFirebase();
    }

    private async Task InitializeFirebase()
    {
        // Firebase SDK 준비. (비동기로 동작함)
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            // Firebase 앱 옵션 설정 (Database URL 포함)
            FirebaseApp.DefaultInstance.Options.DatabaseUrl = 
                new System.Uri(databaseUrl);
                
            app = FirebaseApp.DefaultInstance;
            Debug.Log("Firebase is ready to use.");
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }
}
