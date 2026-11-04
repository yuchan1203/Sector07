using UnityEngine;

// 무기 한 종류의 수치. 무기를 추가하거나 밸런스를 바꿀 때는 이 에셋만 수정한다.
[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("기본")]
    [SerializeField] private WeaponType _type;
    [SerializeField] private WeaponCategory _category;

    [Header("스탯")]
    [SerializeField] private float _damage = 10f;
    [Tooltip("발사 간격 (초)")]
    [SerializeField] private float _fireInterval = 0.5f;
    [SerializeField] private int _magazineSize = 30;

    [Header("재장전")]
    [SerializeField] private bool _canReload = true;
    [Tooltip("0이면 즉시 재장전")]
    [SerializeField] private float _reloadTime = 2f;

    [Header("조준")]
    [SerializeField] private bool _canAim = true;
    [Tooltip("조준 시 확대 배수 (카메라 FOV를 이 값으로 나눈다)")]
    [SerializeField] private float _aimMultiplier = 1f;

    [Header("거리 비례 대미지 감소")]
    [SerializeField] private bool _useDistanceFalloff = false;
    [Tooltip("사거리(투사체는 폭발 범위) 끝에서 적용되는 대미지 비율 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float _minDamageRatio = 0.3f;

    [Header("총 전용")]
    [SerializeField] private ParticleSystem _hitEffectPrefab;
    [SerializeField] private float _range = 100f;
    [Tooltip("한 번에 발사되는 탄 개수 (샷건 5)")]
    [SerializeField] private int _pelletCount = 1;
    [Tooltip("탄 사이의 좌우 각도")]
    [SerializeField] private float _spreadAngle = 0f;
    [Tooltip("발사 시 카메라 반동 세기 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float _recoilStrength = 1f;

    [Header("투사체 전용")]
    [Tooltip("BombProjectile이 붙은 프리팹")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _throwForce = 12f;
    [SerializeField] private float _upForce = 4f;

    [Header("투사체 옵션 1: 차징")]
    [Tooltip("켜면 버튼을 누르는 동안 힘을 모으고, 뗄 때 던진다")]
    [SerializeField] private bool _useCharge = false;
    [SerializeField] private float _maxChargeTime = 1.5f;
    [SerializeField] private float _minForceMultiplier = 0.5f;
    [SerializeField] private float _maxForceMultiplier = 2f;

    [Header("투사체 옵션 2: 시간차 폭발")]
    [Tooltip("켜면 충돌해도 터지지 않고, 던진 뒤 일정 시간이 지나면 터진다")]
    [SerializeField] private bool _useFuse = false;
    [SerializeField] private float _fuseTime = 3f;

    [Header("근접 전용")]
    [Tooltip("플레이어 앞쪽으로 판정 구의 중심까지 거리")]
    [SerializeField] private float _meleeRange = 1.2f;
    [SerializeField] private float _meleeRadius = 1f;
    [Tooltip("공격 버튼을 누른 뒤 실제로 맞는 시점 (초). 휘두르는 동작에 맞춘다")]
    [SerializeField] private float _meleeHitDelay = 0.45f;

    [Header("효과음")]
    [Tooltip("쏘거나 휘두르거나 던질 때")]
    [SerializeField] private SfxId _fireSfx;
    [SerializeField] private SfxId _reloadSfx;
    [Tooltip("탄이 없는데 쏘려고 할 때")]
    [SerializeField] private SfxId _emptySfx;
    [Tooltip("근접 공격이 맞았을 때, 또는 투사체가 터질 때 더해지는 소리")]
    [SerializeField] private SfxId _impactSfx;

    [Header("UI")]
    [SerializeField] private Sprite _icon;
    [Tooltip("획득 안내 등에 쓰는 한국어 이름")]
    [SerializeField] private string _displayName;

    [Header("1인칭 모델")]
    [SerializeField] private GameObject _viewModelPrefab;
    [Tooltip("카메라 기준 위치/회전/크기. 무기 모델마다 맞춰 준다.")]
    [SerializeField] private Vector3 _viewModelPosition = new Vector3(0.18f, -0.18f, 0.4f);
    [SerializeField] private Vector3 _viewModelRotation = Vector3.zero;
    [SerializeField] private float _viewModelScale = 1f;

    public WeaponType Type => _type;
    public WeaponCategory Category => _category;

    public float Damage => _damage;
    public float FireInterval => _fireInterval;
    public int MagazineSize => _magazineSize;

    public bool CanReload => _canReload;
    public float ReloadTime => _reloadTime;

    public bool CanAim => _canAim;
    public float AimMultiplier => _aimMultiplier;

    public bool UseDistanceFalloff => _useDistanceFalloff;
    public float MinDamageRatio => _minDamageRatio;

    public ParticleSystem HitEffectPrefab => _hitEffectPrefab;
    public float Range => _range;
    public int PelletCount => _pelletCount;
    public float SpreadAngle => _spreadAngle;
    public float RecoilStrength => _recoilStrength;

    public GameObject ProjectilePrefab => _projectilePrefab;
    public float ThrowForce => _throwForce;
    public float UpForce => _upForce;

    public bool UseCharge => _useCharge;
    public float MaxChargeTime => _maxChargeTime;
    public float MinForceMultiplier => _minForceMultiplier;
    public float MaxForceMultiplier => _maxForceMultiplier;

    public bool UseFuse => _useFuse;
    public float FuseTime => _fuseTime;

    public float MeleeRange => _meleeRange;
    public float MeleeRadius => _meleeRadius;
    public float MeleeHitDelay => _meleeHitDelay;

    public SfxId FireSfx => _fireSfx;
    public SfxId ReloadSfx => _reloadSfx;
    public SfxId EmptySfx => _emptySfx;
    public SfxId ImpactSfx => _impactSfx;

    public Sprite Icon => _icon;
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? _type.ToString() : _displayName;

    public GameObject ViewModelPrefab => _viewModelPrefab;
    public Vector3 ViewModelPosition => _viewModelPosition;
    public Vector3 ViewModelRotation => _viewModelRotation;
    public float ViewModelScale => _viewModelScale;

    private void OnValidate()
    {
        _fireInterval = Mathf.Max(0f, _fireInterval);
        _magazineSize = Mathf.Max(1, _magazineSize);
        _reloadTime = Mathf.Max(0f, _reloadTime);
        _aimMultiplier = Mathf.Max(1f, _aimMultiplier);
        _pelletCount = Mathf.Max(1, _pelletCount);
        _meleeRadius = Mathf.Max(0f, _meleeRadius);
        _meleeHitDelay = Mathf.Max(0f, _meleeHitDelay);
    }
}
