using UnityEngine;

// 바닥에 놓인 무기. 플레이어가 닿으면 획득한다. 트리거 콜라이더가 필요하다.
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData _weapon;
    [Tooltip("무기 모델을 만들어 돌려 보여 줄 부모. 비우면 이 오브젝트를 쓴다.")]
    [SerializeField] private Transform _visual;
    [SerializeField] private float _spinSpeed = 90f;
    [SerializeField] private float _bobHeight = 0.15f;
    [SerializeField] private float _bobSpeed = 2f;

    private Vector3 _visualStart;

    private void Start()
    {
        if (_weapon == null)
        {
            GameLogger.LogError("WeaponPickup: 무기 데이터가 비어 있습니다. (" + name + ")");
            enabled = false;
            return;
        }
        if (_visual == null)
        {
            _visual = transform;
        }
        if (_visual.childCount == 0 && _weapon.ViewModelPrefab != null)
        {
            GameObject model = Instantiate(_weapon.ViewModelPrefab, _visual);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 1.5f;
            foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }
        }
        _visualStart = _visual.localPosition;
    }

    private void Update()
    {
        _visual.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        _visual.localPosition = _visualStart + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_weapon == null)
        {
            return;
        }
        WeaponSwitcher switcher = other.GetComponentInParent<WeaponSwitcher>();
        if (switcher == null)
        {
            return;
        }
        if (switcher.Unlock(_weapon, true))
        {
            NoticeUI.Show(_weapon.DisplayName + " 획득!  [C] 키로 무기 교체");
        }
        Destroy(gameObject);
    }
}
