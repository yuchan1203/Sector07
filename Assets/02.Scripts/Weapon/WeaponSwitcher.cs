using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("무기 목록")]
    [Tooltip("C 키로 이 순서대로 무기를 바꾼다. 새 무기는 WeaponData 에셋을 만들어 여기에 추가한다.")]
    [SerializeField] private List<WeaponData> _weaponDatas = new List<WeaponData>();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _weaponStateText;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _reloadText;
    [SerializeField] private RectTransform _crossHair;
    [SerializeField] private Image _weaponStateImage;
    [Tooltip("재장전 중 조준선 회전 속도 (도/초)")]
    [SerializeField] private float _crossHairRotationSpeed = 360f;

    private const int NoAmmoDisplay = -2;

    private readonly List<IWeapon> _weapons = new List<IWeapon>();
    // F 키 빠른 근접 공격에 쓰는 무기. 목록의 첫 근접 무기다.
    private IWeapon _meleeWeapon;
    private Animator _animator;
    private int _currentIndex = 0;
    private float _fireTimer = 0f;
    private int _shownAmmo = -1;
    private int _shownMaxAmmo = -1;
    private bool _wasReloading = false;
    private string _shownReloadMessage = null;

    private IWeapon CurrentWeapon => _weapons.Count > 0 ? _weapons[_currentIndex] : null;

    public int CurrentAmmo => CurrentWeapon != null ? CurrentWeapon.CurrentAmmo : 0;
    public int CurrentMaxAmmo => CurrentWeapon != null ? CurrentWeapon.MaxAmmo : 0;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        foreach (WeaponData data in _weaponDatas)
        {
            if (data == null)
            {
                GameLogger.LogWarning("WeaponSwitcher: 비어 있는 무기 데이터 칸이 있습니다.");
                continue;
            }
            IWeapon weapon = WeaponBase.Create(data, transform);
            if (weapon != null)
            {
                _weapons.Add(weapon);
                if (_meleeWeapon == null && data.Category == WeaponCategory.Melee)
                {
                    _meleeWeapon = weapon;
                }
            }
        }

        if (_weapons.Count == 0)
        {
            GameLogger.LogError("WeaponSwitcher: 사용할 무기 데이터가 없습니다.");
        }

        if (_reloadText != null)
        {
            _reloadText.gameObject.SetActive(false);
        }
        UpdateWeaponStateText();
        UpdateBulletText();
    }

    public void Reload()
    {
        if (CurrentWeapon == null)
        {
            return;
        }
        CurrentWeapon.Reload();
    }

    public void ChangeWeapon()
    {
        if (CurrentWeapon == null)
        {
            return;
        }
        CurrentWeapon.OnUnequip();
        _currentIndex = (_currentIndex + 1) % _weapons.Count;
        UpdateWeaponStateText();
        UpdateBulletText();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            return;
        }

        if (CurrentWeapon == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeWeapon();
        }

        if (Input.GetKeyDown(KeyCode.R) && CurrentWeapon.CanReload)
        {
            Reload();
        }

        CurrentWeapon.Tick(Time.deltaTime);
        // 다른 무기를 든 채 F 키로 휘두른 공격도 판정이 나가야 한다.
        if (_meleeWeapon != null && _meleeWeapon != CurrentWeapon)
        {
            _meleeWeapon.Tick(Time.deltaTime);
        }
        _fireTimer -= Time.deltaTime;
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetKeyDown(KeyCode.F) && _meleeWeapon != null)
        {
            TryAttack(_meleeWeapon);
        }

        if (CurrentWeapon is IChargeable chargeable && chargeable.UseCharge)
        {
            HandleCharge(chargeable, isPointerOverUI);
        }
        else if (Input.GetButton("Fire1") && !isPointerOverUI)
        {
            TryAttack(CurrentWeapon);
        }

        UpdateAim();
        UpdateReloadUI();
        UpdateBulletText();
    }

    private void HandleCharge(IChargeable chargeable, bool isPointerOverUI)
    {
        if (Input.GetButtonDown("Fire1") && !isPointerOverUI && _fireTimer <= 0f)
        {
            chargeable.BeginCharge();
        }
        chargeable.UpdateCharge(Time.deltaTime);
        if (Input.GetButtonUp("Fire1") && chargeable.ReleaseCharge())
        {
            _fireTimer = CurrentWeapon.FireInterval;
        }
    }

    private void TryAttack(IWeapon weapon)
    {
        if (_fireTimer > 0f)
        {
            return;
        }
        if (weapon.Shoot())
        {
            _fireTimer = weapon.FireInterval;
            OnShot(weapon);
        }
    }

    private void OnShot(IWeapon weapon)
    {
        if (_animator == null)
        {
            return;
        }
        // 쏘기·휘두르기 동작은 UpperBody 레이어에서 상체에만 재생되므로 달리면서도 보낸다.
        switch (weapon.Data.Category)
        {
            case WeaponCategory.Gun:
                _animator.SetTrigger(AnimatorParams.Attack);
                break;
            case WeaponCategory.Melee:
                _animator.SetTrigger(AnimatorParams.Melee);
                break;
        }
    }

    private void UpdateAim()
    {
        CameraViewSwitcher viewSwitcher = CameraViewSwitcher.Instance;
        if (viewSwitcher == null)
        {
            return;
        }
        WeaponData data = CurrentWeapon.Data;
        bool isAiming = data.CanAim && Input.GetButton("Fire2");
        viewSwitcher.SetAimMultiplier(isAiming ? data.AimMultiplier : 1f);
    }

    private void UpdateReloadUI()
    {
        bool isReloading = CurrentWeapon.IsReloading;
        if (isReloading && _crossHair != null)
        {
            _crossHair.Rotate(0f, 0f, -_crossHairRotationSpeed * Time.deltaTime);
        }

        if (isReloading == false && _wasReloading && _crossHair != null)
        {
            _crossHair.localEulerAngles = Vector3.zero;
        }
        _wasReloading = isReloading;

        // 문구가 바뀔 때만 UI를 갱신한다.
        string message = GetReloadMessage(isReloading);
        if (message == _shownReloadMessage)
        {
            return;
        }
        _shownReloadMessage = message;
        if (_reloadText != null)
        {
            _reloadText.gameObject.SetActive(message != null);
            if (message != null)
            {
                _reloadText.text = message;
            }
        }
    }

    // 표시할 문구가 없으면 null을 반환한다.
    private string GetReloadMessage(bool isReloading)
    {
        if (isReloading)
        {
            return "Reloading...";
        }
        if (CurrentWeapon.CurrentAmmo > 0)
        {
            return null;
        }
        return CurrentWeapon.Data.CanReload ? "재장전이 필요합니다" : "폭탄을 모두 소모했습니다";
    }

    private void UpdateWeaponStateText()
    {
        if (_weaponStateText == null || CurrentWeapon == null)
        {
            return;
        }
        if (_weaponStateImage != null)
        {
            // 아이콘이 없으면 흰 사각형 대신 이미지를 숨긴다.
            _weaponStateImage.sprite = CurrentWeapon.Data.Icon;
            _weaponStateImage.enabled = CurrentWeapon.Data.Icon != null;
        }
        _weaponStateText.text = "현재 무기: " + CurrentWeapon.Data.Type.ToString();
    }

    private void UpdateBulletText()
    {
        if (_bulletText == null || CurrentWeapon == null)
        {
            return;
        }
        if (CurrentWeapon.UsesAmmo == false)
        {
            if (_shownAmmo == NoAmmoDisplay)
            {
                return;
            }
            _shownAmmo = NoAmmoDisplay;
            _shownMaxAmmo = NoAmmoDisplay;
            _bulletText.text = "Bullet: -";
            return;
        }
        int ammo = CurrentWeapon.CurrentAmmo;
        int maxAmmo = CurrentWeapon.MaxAmmo;
        // 값이 바뀌지 않았으면 문자열을 새로 만들지 않는다.
        if (ammo == _shownAmmo && maxAmmo == _shownMaxAmmo)
        {
            return;
        }
        _shownAmmo = ammo;
        _shownMaxAmmo = maxAmmo;
        _bulletText.text = "Bullet: " + ammo + " / " + maxAmmo;
    }
}
