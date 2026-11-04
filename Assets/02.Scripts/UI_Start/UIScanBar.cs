using UnityEngine;

// 화면 위에서 아래로 천천히 흘러내리는 CRT 스캔 띠.
public class UIScanBar : MonoBehaviour
{
    [SerializeField] private float _period = 7f;
    [SerializeField] private float _screenHeight = 1080f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = (RectTransform)transform;
    }

    private void Update()
    {
        float t = Mathf.Repeat(Time.unscaledTime / _period, 1f);
        float y = Mathf.Lerp(_rect.rect.height, -_screenHeight, t);
        _rect.anchoredPosition = new Vector2(0f, y);
    }
}
