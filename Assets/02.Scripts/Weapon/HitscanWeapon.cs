using UnityEngine;

// 레이캐스트로 즉시 판정하는 총. 펠릿 개수와 퍼짐 각도로 일반 총/샷건을 모두 처리한다.
public class HitscanWeapon : WeaponBase
{
    public HitscanWeapon(WeaponData data) : base(data)
    {
    }

    protected override bool Fire()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        CameraViewSwitcher viewSwitcher = CameraViewSwitcher.Instance;
        if (viewSwitcher != null)
        {
            viewSwitcher.ApplyRecoil(_data.RecoilStrength);
        }

        // 펠릿을 가운데 기준으로 좌우 대칭으로 퍼뜨린다. (5발이면 -2, -1, 0, 1, 2)
        float center = (_data.PelletCount - 1) * 0.5f;
        for (int i = 0; i < _data.PelletCount; i++)
        {
            float yawOffset = (i - center) * _data.SpreadAngle;
            Vector3 direction = Quaternion.Euler(0f, yawOffset, 0f) * camera.transform.forward;
            Ray ray = new Ray(camera.transform.position, direction);
            // 3인칭에서는 카메라가 플레이어 뒤에 있으므로 플레이어 자신은 맞지 않게 제외한다.
            if (Physics.Raycast(ray, out RaycastHit hitInfo, _data.Range, ~LayerMask.GetMask("Player")))
            {
                PlayHitEffect(hitInfo);
                hitInfo.collider.GetComponent<IDamageAble>()?.TakeDamage(new DamageInfo
                {
                    Value = _data.UseDistanceFalloff
                        ? DamageFalloff.Calculate(_data.Damage, hitInfo.distance, _data.Range, _data.MinDamageRatio)
                        : _data.Damage,
                    HitPoint = hitInfo.point,
                    HitNormal = hitInfo.normal
                });
            }
        }
        return true;
    }

    private void PlayHitEffect(RaycastHit hitInfo)
    {
        if (_data.HitEffectPrefab == null)
        {
            return;
        }
        ParticleSystem effect = PoolManager.Spawn(_data.HitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
        effect.Play();
    }
}
