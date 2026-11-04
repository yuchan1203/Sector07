using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;

    // 대미지와 폭발 방식은 던진 무기의 WeaponData를 따른다.
    private WeaponData _data;
    private bool _exploded;

    public void Setup(WeaponData data)
    {
        _data = data;
        if (_data.UseFuse)
        {
            Invoke(nameof(Explode), _data.FuseTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_data == null || _data.UseFuse)
        {
            return;
        }
        Explode();
    }

    private void Explode()
    {
        if (_exploded || _data == null)
        {
            return;
        }
        _exploded = true;

        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.Play();
        }
        SoundManager.PlayAt(_data.ImpactSfx, transform.position);
        ApplyAreaDamage();
        Destroy(gameObject);
    }

    private void ApplyAreaDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));
        foreach (var col in colliders)
        {
            Vector3 closestPoint = col.ClosestPoint(transform.position);
            float damage = _data.Damage;
            if (_data.UseDistanceFalloff)
            {
                float distance = Vector3.Distance(transform.position, closestPoint);
                damage = DamageFalloff.Calculate(_data.Damage, distance, _explosionRange, _data.MinDamageRatio);
            }
            DamageInfo damageInfo = new DamageInfo
            {
                Value = damage,
                HitPoint = closestPoint,
                HitNormal = (transform.position - closestPoint).normalized,
            };
            col.GetComponent<IDamageAble>()?.TakeDamage(damageInfo);
        }
    }
}
