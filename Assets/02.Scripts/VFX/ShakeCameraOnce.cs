using UnityEngine;
public class ShakeCameraOnce : MonoBehaviour
{
    [SerializeField] private float _shakePower = 1f;
    // 풀에서 다시 꺼낼 때도 흔들리도록 OnEnable에서 실행한다.
    private void OnEnable()
    {
        CameraViewSwitcher viewSwitcher = CameraViewSwitcher.Instance;
        if (viewSwitcher != null)
        {
            viewSwitcher.ApplyRecoil(_shakePower);
        }
    }
}