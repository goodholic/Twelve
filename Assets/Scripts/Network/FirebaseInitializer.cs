using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class FirebaseInitializer : MonoBehaviour
{
    private FirebaseApp app;

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
            app = FirebaseApp.DefaultInstance;
            Debug.Log("Firebase is ready to use.");
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }
}
