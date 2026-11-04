using System.Collections.Generic;
using UnityEngine;

// 손에 든 무기를 휘둘러 캐릭터 앞쪽 범위 안의 대상을 맞히는 무기. 탄약을 쓰지 않는다.
public class MeleeWeapon : WeaponBase
{
    private readonly Transform _owner;
    private readonly HashSet<IDamageAble> _hitTargets = new HashSet<IDamageAble>();
    // 판정까지 남은 시간. 음수면 대기 중인 공격이 없다.
    private float _hitTimer = -1f;

    public override bool UsesAmmo => false;

    public MeleeWeapon(WeaponData data, Transform owner) : base(data)
    {
        _owner = owner;
    }

    protected override bool Fire()
    {
        if (_owner == null || _hitTimer >= 0f)
        {
            return false;
        }
        // 휘두르는 동작에 맞춰 늦게 맞히기 위해 판정은 Tick에서 처리한다.
        _hitTimer = _data.MeleeHitDelay;
        return true;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
        if (_hitTimer < 0f)
        {
            return;
        }
        _hitTimer -= deltaTime;
        if (_hitTimer <= 0f)
        {
            _hitTimer = -1f;
            ApplyHit();
        }
    }

    private void ApplyHit()
    {
        if (_owner == null)
        {
            return;
        }
        Vector3 center = _owner.position + _owner.forward * _data.MeleeRange;
        Collider[] colliders = Physics.OverlapSphere(center, _data.MeleeRadius, ~0, QueryTriggerInteraction.Ignore);

        // 한 대상에 콜라이더가 여러 개여도 한 번만 맞힌다.
        _hitTargets.Clear();
        bool hitAny = false;
        foreach (Collider collider in colliders)
        {
            if (collider.transform.IsChildOf(_owner))
            {
                continue;
            }
            IDamageAble target = collider.GetComponentInParent<IDamageAble>();
            if (target == null || _hitTargets.Add(target) == false)
            {
                continue;
            }
            Vector3 hitPoint = collider.ClosestPoint(center);
            hitAny = true;
            target.TakeDamage(new DamageInfo
            {
                Value = _data.Damage,
                HitPoint = hitPoint,
                HitNormal = (center - hitPoint).normalized
            });
        }
        if (hitAny)
        {
            SoundManager.PlayAt(_data.ImpactSfx, center);
        }
    }
}
