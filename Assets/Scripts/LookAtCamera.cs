/// <summary>
/// 카메라를 바라보는 컴포넌트 (HP바, 텍스트 등에 사용)
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}