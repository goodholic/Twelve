using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

public class MyClickTest : MonoBehaviour
{
    public Button targetButton;      // 인스펙터에서 할당
    public MMF_Player mmfPlayer;     // 클릭 시 재생하고 싶은 MMF_Player

    void Start()
    {
        // 버튼이 클릭되면 PlayFeedbacks() 호출
        if (targetButton != null && mmfPlayer != null)
        {
            targetButton.onClick.AddListener(mmfPlayer.PlayFeedbacks);
        }
    }
}
