using System;
using UnityEngine;
public class PlayerHit : MonoBehaviour, IDamageAble
{
    [SerializeField] private float _maxHealth = 100f;
    private float _health;
    private Animator _animator;
    public event Action HealthChanged;
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _health;
    private void Awake()
    {
        _health = _maxHealth;
        _animator = GetComponentInChildren<Animator>();
    }
    public void TakeDamage(DamageInfo damageInfo)
    {
        // 이미 쓰러졌으면 더 맞지 않는다.
        if (_health <= 0f)
        {
            return;
        }
        _health -= damageInfo.Value;
        HealthChanged?.Invoke();
        if (_health <= 0f)
        {
            // 쓰러지는 동작을 보여 주기 위해 오브젝트를 파괴하지 않는다.
            SoundManager.Play(SfxId.PlayerDeath);
            PlayDeath();
            GameManager.Instance.GameOver();
        }
        else
        {
            SoundManager.Play(SfxId.PlayerHit);
            if (_animator != null)
            {
                _animator.SetTrigger(AnimatorParams.Hit);
            }
        }
    }

    private void PlayDeath()
    {
        if (_animator == null)
        {
            return;
        }
        // 상체 레이어(쏘기, 피격)가 쓰러지는 동작을 덮지 않도록 끈다.
        for (int i = 1; i < _animator.layerCount; i++)
        {
            _animator.SetLayerWeight(i, 0f);
        }
        _animator.SetTrigger(AnimatorParams.Death);
    }
}
