using UnityEngine;

// 데미지 숫자가 겹치지 않도록 생성 위치를 화면 기준으로 흩뜨리고, 사는 동안 점점 커지게 한다.
// 위로 떠오르기와 삭제는 Mover가 담당한다.
public class DamageTextEffect : MonoBehaviour
{
    [Header("랜덤 위치")]
    [Tooltip("화면 가로 방향으로 흩어지는 최대 거리")]
    [SerializeField] private float _horizontalSpread = 0.5f;
    [Tooltip("화면 세로 방향으로 흩어지는 최대 거리")]
    [SerializeField] private float _verticalSpread = 0.3f;

    [Header("크기 변화")]
    [SerializeField] private float _startScale = 0.6f;
    [SerializeField] private float _endScale = 1.5f;
    [Tooltip("시작 크기에서 끝 크기까지 커지는 시간(초). Mover의 유지 시간과 맞춘다")]
    [SerializeField] private float _growDuration = 1f;

    private Vector3 _baseScale;
    private float _elapsedTime = 0f;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    // 풀에서 다시 꺼낼 때마다 위치를 흩뜨리고 크기를 처음으로 되돌린다.
    private void OnEnable()
    {
        _elapsedTime = 0f;
        ApplyRandomOffset();
        ApplyScale(0f);
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
        float t = _growDuration > 0f ? Mathf.Clamp01(_elapsedTime / _growDuration) : 1f;
        ApplyScale(t);
    }

    // 깊이 방향으로 흩어지면 화면에서 겹쳐 보이므로 카메라의 가로·세로 방향으로만 옮긴다.
    private void ApplyRandomOffset()
    {
        Camera mainCamera = Camera.main;
        Vector3 right = mainCamera != null ? mainCamera.transform.right : Vector3.right;
        Vector3 up = mainCamera != null ? mainCamera.transform.up : Vector3.up;
        float x = Random.Range(-_horizontalSpread, _horizontalSpread);
        float y = Random.Range(-_verticalSpread, _verticalSpread);
        transform.position += right * x + up * y;
    }

    private void ApplyScale(float t)
    {
        // 처음에 빠르게 커지고 끝으로 갈수록 천천히 커진다. (ease-out)
        float eased = 1f - (1f - t) * (1f - t);
        transform.localScale = _baseScale * Mathf.Lerp(_startScale, _endScale, eased);
    }
}
