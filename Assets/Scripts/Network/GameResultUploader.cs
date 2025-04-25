// Assets\Scripts\Network\GameResultUploader.cs

using UnityEngine;

public class GameResultUploader : MonoBehaviour
{
    // 임의 예시
    public struct GameResultData
    {
        public bool isWin;
        public int gainedExp;
        public int gainedGold;
    }

    public void UploadGameResult(GameResultData result)
    {
        var userId = FirebaseAuthManager.Instance.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[GameResultUploader] No user logged in => cannot upload result.");
            return;
        }

        // 1) 기존 UserData 로드
        FirebaseUserDataManager.Instance.LoadUserData(
            userId,
            (profile) =>
            {
                // 2) 보상 반영
                profile.exp += result.gainedExp;
                profile.gold += result.gainedGold;
                if (result.isWin) profile.level += 1;

                // 3) 저장
                FirebaseUserDataManager.Instance.SaveUserData(
                    userId,
                    profile,
                    onSuccess: () => 
                    {
                        Debug.Log("[GameResultUploader] Game result uploaded!");
                    },
                    onFail: (err) =>
                    {
                        Debug.LogWarning($"[GameResultUploader] fail save: {err}");
                    }
                );
            },
            (errLoad) =>
            {
                Debug.LogWarning($"[GameResultUploader] fail load: {errLoad}");
            }
        );
    }
}
