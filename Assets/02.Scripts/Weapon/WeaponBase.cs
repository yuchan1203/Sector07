using UnityEngine;

// 탄약과 재장전처럼 모든 무기가 공통으로 가지는 동작. 발사 방식은 자식 클래스가 정한다.
public abstract class WeaponBase : IWeapon
{
    protected readonly WeaponData _data;

    private int _ammo;
    private bool _isReloading;
    private float _reloadTimer;

    public WeaponData Data => _data;
    public int CurrentAmmo => _ammo;
    public int MaxAmmo => _data.MagazineSize;
    public virtual bool UsesAmmo => true;
    public bool CanReload => _data.CanReload && _isReloading == false && _ammo < MaxAmmo;
    public bool IsReloading => _isReloading;
    public float ReloadRatio => _isReloading && _data.ReloadTime > 0f ? _reloadTimer / _data.ReloadTime : 0f;
    public float FireInterval => _data.FireInterval;

    protected WeaponBase(WeaponData data)
    {
        _data = data;
        _ammo = data.MagazineSize;
    }

    public bool Shoot()
    {
        if (_isReloading)
        {
            return false;
        }

        if (UsesAmmo && _ammo <= 0)
        {
            //
            return false;
        }
        if (Fire() == false)
        {
            return false;
        }
        if (UsesAmmo)
        {
            _ammo--;
        }
        SoundManager.Play(_data.FireSfx);
        return true;
    }

    // 실제 발사 처리. 발사하지 못했으면 false를 반환해 탄약을 소모하지 않는다.
    protected abstract bool Fire();

    public void Reload()
    {
        if (CanReload == false)
        {
            return;
        }
        SoundManager.Play(_data.ReloadSfx);
        if (_data.ReloadTime <= 0f)
        {
            _ammo = MaxAmmo;
            return;
        }
        _isReloading = true;
        _reloadTimer = 0f;
    }

    public virtual void Tick(float deltaTime)
    {
        if (_isReloading == false)
        {
            return;
        }
        _reloadTimer += deltaTime;
        if (_reloadTimer >= _data.ReloadTime)
        {
            _ammo = MaxAmmo;
            _isReloading = false;
        }
    }

    public virtual void OnUnequip()
    {
        // 무기를 바꾸면 진행 중이던 재장전은 취소한다.
        _isReloading = false;
        _reloadTimer = 0f;
    }

    // owner: 무기를 든 캐릭터. 근접 무기는 이 위치와 방향을 기준으로 판정한다.
    public static IWeapon Create(WeaponData data, Transform owner)
    {
        switch (data.Category)
        {
            case WeaponCategory.Gun:
                return new HitscanWeapon(data);
            case WeaponCategory.Projectile:
                return new ProjectileWeapon(data);
            case WeaponCategory.Melee:
                return new MeleeWeapon(data, owner);
            default:
                GameLogger.LogError($"{data.Category} 유형의 무기 클래스가 없습니다.");
                return null;
        }
    }
}
