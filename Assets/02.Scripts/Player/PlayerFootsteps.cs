using UnityEngine;

// 플레이어가 땅 위에서 이동할 때 발자국 소리를 재생한다.
// 이동 거리 기준으로 걸음 간격을 계산하므로 달리기 속도가 빨라지면 발소리도 빨라진다.
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("발자국 소리")]
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
    [SerializeField, Tooltip("재생할 때마다 피치를 이 범위만큼 무작위로 흔든다.")]
    private float _pitchVariation = 0.08f;

    [Header("걸음 간격")]
    [SerializeField, Tooltip("한 걸음 소리가 나는 이동 거리(m)")]
    private float _stepDistance = 2.2f;
    [SerializeField, Tooltip("이 속도(m/s) 미만이면 걷는 것으로 보지 않는다.")]
    private float _minMoveSpeed = 0.5f;

    private CharacterController _characterController;
    private float _distanceSinceStep;
    private int _lastIndex = -1;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            return;
        }

        Vector3 velocity = _characterController.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        if (!_characterController.isGrounded || speed < _minMoveSpeed)
        {
            // 멈추거나 공중에 뜨면 다음 한 걸음은 바로 소리가 나도록 거리를 채워 둔다.
            _distanceSinceStep = _stepDistance * 0.5f;
            return;
        }

        _distanceSinceStep += speed * Time.deltaTime;
        if (_distanceSinceStep < _stepDistance)
        {
            return;
        }

        _distanceSinceStep = 0f;
        PlayStep();
    }

    private void PlayStep()
    {
        if (_clips == null || _clips.Length == 0)
        {
            return;
        }

        // 직전에 쓴 소리가 연속으로 나오지 않게 고른다.
        int index = Random.Range(0, _clips.Length);
        if (_clips.Length > 1 && index == _lastIndex)
        {
            index = (index + 1 + Random.Range(0, _clips.Length - 1)) % _clips.Length;
        }
        _lastIndex = index;

        _audioSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
        _audioSource.PlayOneShot(_clips[index], _volume);
    }
}
