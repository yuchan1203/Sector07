using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpPower = 5f;
    [SerializeField] private float _sprintSpeed = 10f;
    [SerializeField] private float _staminaRecovery = 15f;
    [SerializeField] private float _sprintCost = 10f;
    [SerializeField] private float _jumpCost = 20f;
    [Tooltip("스태미나가 바닥난 뒤 다시 질주할 수 있게 되는 최소 스태미나")]
    [SerializeField] private float _sprintResumeStamina = 20f;

    [SerializeField] private CharacterController _characterController;

    [Header("애니메이션")]
    [Tooltip("MoveMotion이 목표 값까지 바뀌는 데 걸리는 시간(초). 서기·달리기·질주가 부드럽게 섞인다")]
    [SerializeField] private float _moveMotionDampTime = 0.1f;

    private const float Gravity = -20f;
    // 블렌드 트리 Threshold: 서기 0, 달리기 1, 질주 2
    private const float RunMotion = 1f;
    private const float SprintMotion = 2f;
    private const int MaxJumps = 2;
    private const float MaxStamina = 100f;

    private float _yVelocity = 0f;
    private int _jumpCount = 0;
    private float _stamina = 0f;
    private bool _isExhausted = false;

    private Animator _animator;

    public event System.Action StaminaChanged;

    public float Stamina => _stamina;
    public float MMaxStamina => MaxStamina;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = MaxStamina;
        _animator = GetComponentInChildren<Animator>();
    }

    // 값이 실제로 바뀐 경우에만 UI에 알린다. (최대치에서 회복할 때는 알리지 않는다.)
    private void SetStamina(float value)
    {
        float newStamina = Mathf.Clamp(value, 0f, MaxStamina);
        if (Mathf.Approximately(newStamina, _stamina))
        {
            return;
        }
        _stamina = newStamina;
        StaminaChanged?.Invoke();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            return;
        }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction;
        CameraViewSwitcher viewSwitcher = CameraViewSwitcher.Instance;
        bool isFixedView = viewSwitcher != null && viewSwitcher.IsFixedView == true;
        if (isFixedView == true)
        {
            // 고정 시점에서는 카메라 기준으로 이동 방향을 계산한다.
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
                direction = (right * h + forward * v).normalized;
            }
            else
            {
                direction = new Vector3(h, 0f, v).normalized;
            }
        }
        else
        {
            direction = new Vector3(h, 0f, v).normalized;
            direction = transform.TransformDirection(direction);
        }
        if (_characterController.isGrounded == false)
        {
            _yVelocity += Gravity * Time.deltaTime;
        }
        else
        {
            _yVelocity = -1f;
            _jumpCount = 0;
        }
        if (Input.GetButtonDown("Jump") && _jumpCount < MaxJumps && _stamina >= _jumpCost)
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            SetStamina(_stamina - _jumpCost);
        }
        float requiredStamina = _sprintCost * Time.deltaTime;
        bool isMoving = direction.sqrMagnitude > 0.01f;
        bool isSprint = Input.GetButton("Sprint") && isMoving && !_isExhausted && _stamina >= requiredStamina;
        float currentSpeed = _moveSpeed;

        if (isSprint)
        {
            currentSpeed = _sprintSpeed;
            SetStamina(_stamina - requiredStamina);
            // 바닥나면 일정량 회복할 때까지 질주할 수 없다.
            if (_stamina <= requiredStamina)
            {
                _isExhausted = true;
            }
        }
        else
        {
            // 질주 중에는 회복하지 않는다.
            SetStamina(_stamina + Time.deltaTime * _staminaRecovery);
            if (_isExhausted && _stamina >= _sprintResumeStamina)
            {
                _isExhausted = false;
            }
        }
        if (_animator != null)
        {
            float targetMotion = direction.magnitude * (isSprint ? SprintMotion : RunMotion);
            _animator.SetFloat(AnimatorParams.MoveMotion, targetMotion, _moveMotionDampTime, Time.deltaTime);
        }
        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
