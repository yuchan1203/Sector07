using UnityEngine;

// 시작 화면 배경 카메라 연출: 처음에 천천히 밀고 들어오고, 이후에는 숨 쉬듯 아주 조금씩 흔들린다.
public class StartCameraSway : MonoBehaviour
{
    [Header("등장 이동")]
    [SerializeField] private float _pushInDistance = 1.2f;
    [SerializeField] private float _pushInDuration = 4f;

    [Header("흔들림")]
    [SerializeField] private Vector3 _positionAmplitude = new Vector3(0.06f, 0.03f, 0.05f);
    [SerializeField] private Vector2 _rotationAmplitude = new Vector2(0.5f, 1.2f);
    [SerializeField] private float _speed = 0.25f;

    private Vector3 _homePosition;
    private Quaternion _homeRotation;
    private float _startTime;

    private void Awake()
    {
        _homePosition = transform.position;
        _homeRotation = transform.rotation;
    }

    private void OnEnable()
    {
        _startTime = Time.unscaledTime;
    }

    private void Update()
    {
        float elapsed = Time.unscaledTime - _startTime;
        float push = Mathf.Clamp01(elapsed / _pushInDuration);
        push = 1f - Mathf.Pow(1f - push, 3f);

        float t = Time.unscaledTime * _speed;
        Vector3 sway = new Vector3(
            (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * _positionAmplitude.x,
            (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * _positionAmplitude.y,
            (Mathf.PerlinNoise(t, t) - 0.5f) * 2f * _positionAmplitude.z);
        float pitch = (Mathf.PerlinNoise(t + 10f, 0f) - 0.5f) * 2f * _rotationAmplitude.x;
        float yaw = (Mathf.PerlinNoise(0f, t + 20f) - 0.5f) * 2f * _rotationAmplitude.y;

        Quaternion rotation = _homeRotation * Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = -(_homeRotation * Vector3.forward) * _pushInDistance * (1f - push);
        transform.SetPositionAndRotation(_homePosition + back + sway, rotation);
    }
}
