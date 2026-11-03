using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public class CameraViewSwitcher : SingletonBehaviour<CameraViewSwitcher>
{
    private static readonly int CameraStateCount = System.Enum.GetValues(typeof(CameraState)).Length;

    [SerializeField] private TextMeshProUGUI _viewStateText;
    [SerializeField] private Transform _target;
    [SerializeField] private CameraViewSettings[] _views;
    [SerializeField] private CameraState _currentMode = CameraState.FirstPerson;
    [SerializeField] private float _rotationSpeed = 200f;

    [Header("Recoil")]
    [SerializeField] private bool _useRecoil = true;
    [SerializeField] private float _recoilPitchAmount = 2f;
    [SerializeField] private float _recoilYawAmount = 1f;
    [SerializeField] private float _recoilRecoverySpeed = 8f;

    [Header("조준")]
    [Tooltip("조준 시 FOV가 목표값으로 바뀌는 속도")]
    [SerializeField] private float _aimSpeed = 10f;

    [Header("Fade")]
    [SerializeField] private bool _useFade = false;
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("1인칭 최적화")]
    [Tooltip("1인칭에서는 화면에 보이지 않으므로 그림자만 그리는 플레이어 몸 렌더러")]
    [SerializeField] private Renderer[] _firstPersonHiddenRenderers;

    private CameraOrbitAngles _orbitAngles;
    private readonly CameraRecoil _recoil = new CameraRecoil();
    private CameraScreenFader _fader;
    private Camera _camera;
    private float _baseFieldOfView;
    private float _aimMultiplier = 1f;

    private bool _isTransitioning = false;

    // 전환 가능한 시점 개수 (인스펙터 배열과 열거형 중 작은 쪽)
    private int ViewCount
    {
        get
        {
            if (_views == null)
            {
                return 0;
            }
            return Mathf.Min(_views.Length, CameraStateCount);
        }
    }

    public bool IsFixedView
    {
        get
        {
            CameraViewSettings view = GetView(_currentMode);
            if (view == null)
            {
                return false;
            }
            return view.inheritPlayerYaw == false;
        }
    }

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            GameLogger.LogWarning("CameraViewSwitcher가 씬에 둘 이상 있습니다.");
        }
        base.Awake();

        if (_target == null)
        {
            GameLogger.LogError("No target assigned!");
        }

        if (ViewCount == 0)
        {
            GameLogger.LogError("시점 설정(_views)이 비어 있습니다.");
        }
        else if ((int)_currentMode >= ViewCount)
        {
            GameLogger.LogWarning($"{_currentMode} 시점 설정이 없어 첫 번째 시점으로 시작합니다.");
            _currentMode = (CameraState)0;
        }

        if (_useFade)
        {
            _fader = new CameraScreenFader(transform);
        }

        _camera = GetComponent<Camera>();
        if (_camera != null)
        {
            _baseFieldOfView = _camera.fieldOfView;
        }
    }

    private void Start()
    {
        if (ViewCount > 0)
        {
            _orbitAngles = new CameraOrbitAngles(_views, _currentMode);
        }
        UpdateViewStateText();
        UpdateBodyVisibility();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            return;
        }

        if (_orbitAngles == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.V) && _isTransitioning == false)
        {
            SwitchView();
        }

        if (IsOrbitView(_currentMode) == true && Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            _orbitAngles.AddInput(mouseX * _rotationSpeed * Time.deltaTime, mouseY * _rotationSpeed * Time.deltaTime);
        }

        // 반동 복귀
        _recoil.Recover(_recoilRecoverySpeed, Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        CameraViewSettings view = GetView(_currentMode);
        if (view == null || _orbitAngles == null)
        {
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;
        if (view.inheritPlayerYaw == true)
        {
            targetPosition = _target.position + _target.rotation * view.positionOffset;
            targetRotation = Quaternion.Euler(_orbitAngles.Y + _recoil.Pitch, _orbitAngles.X + _recoil.Yaw, 0f);
        }
        else
        {
            targetPosition = _target.position + view.positionOffset;
            targetRotation = Quaternion.Euler(view.rotationEuler.x + _recoil.Pitch, view.rotationEuler.y + _recoil.Yaw, view.rotationEuler.z);
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        UpdateAim();
    }

    // 1이면 조준 해제, 2면 FOV를 절반으로 줄여 2배 확대한다.
    public void SetAimMultiplier(float multiplier)
    {
        _aimMultiplier = Mathf.Max(1f, multiplier);
    }

    private void UpdateAim()
    {
        if (_camera == null)
        {
            return;
        }
        float targetFieldOfView = _baseFieldOfView / _aimMultiplier;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFieldOfView, _aimSpeed * Time.deltaTime);
    }

    public void SwitchView()
    {
        SwitchTo(NextMode(_currentMode));
    }

    public void AddRecoil(float pitch, float yaw)
    {
        if (_useRecoil == false)
        {
            return;
        }
        _recoil.Add(pitch, yaw);
    }

    public void ApplyRecoil(float strength)
    {
        if (_useRecoil == false)
        {
            return;
        }
        strength = Mathf.Clamp01(strength);
        AddRecoil(_recoilPitchAmount * strength, Random.Range(-_recoilYawAmount, _recoilYawAmount) * strength);
    }

    private CameraState NextMode(CameraState current)
    {
        // 인스펙터에 설정된 시점까지만 순환한다.
        int nextIndex = ((int)current + 1) % ViewCount;
        return (CameraState)nextIndex;
    }

    private CameraViewSettings GetView(CameraState mode)
    {
        int index = (int)mode;
        if (index < 0 || index >= ViewCount)
        {
            return null;
        }
        return _views[index];
    }

    private bool IsOrbitView(CameraState mode)
    {
        CameraViewSettings view = GetView(mode);
        return view != null && view.inheritPlayerYaw;
    }

    private void SwitchTo(CameraState next)
    {
        _orbitAngles.OnViewChanged(_currentMode, next);

        if (_useFade == true && _fader != null)
        {
            StartCoroutine(FadeTransition(next));
        }
        else
        {
            ApplyMode(next);
        }
    }

    private IEnumerator FadeTransition(CameraState next)
    {
        _isTransitioning = true;
        yield return _fader.Fade(_fadeDuration, () => ApplyMode(next));
        _isTransitioning = false;
    }

    private void ApplyMode(CameraState next)
    {
        _currentMode = next;
        UpdateViewStateText();
        UpdateBodyVisibility();
    }

    // 1인칭에서는 몸을 그리지 않고 그림자만 남겨 정점 처리 비용을 줄인다.
    private void UpdateBodyVisibility()
    {
        if (_firstPersonHiddenRenderers == null)
        {
            return;
        }
        ShadowCastingMode mode = _currentMode == CameraState.FirstPerson
            ? ShadowCastingMode.ShadowsOnly
            : ShadowCastingMode.On;
        foreach (Renderer bodyRenderer in _firstPersonHiddenRenderers)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.shadowCastingMode = mode;
            }
        }
    }

    private void UpdateViewStateText()
    {
        if (_viewStateText == null)
        {
            return;
        }
        _viewStateText.text = "현재 뷰: " + _currentMode.ToString();
    }
}
