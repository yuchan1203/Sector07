using UnityEngine;

// 구역 안쪽에 놓인 전용 폭발물. 플레이어가 닿으면 획득하고, 이 구역의 막힌 길을 폭파할 수 있게 된다.
// 해당 단계가 시작될 때 LevelManager가 켠다. 트리거 콜라이더가 필요하다.
[RequireComponent(typeof(Collider))]
public class ExplosivePickup : ObjectiveTarget
{
    [Tooltip("폭발물 모델. 비워 두면 모델 없이 빛만 보인다.")]
    [SerializeField] private GameObject _modelPrefab;
    [SerializeField] private float _modelScale = 4f;
    [SerializeField] private float _spinSpeed = 90f;
    [SerializeField] private float _bobHeight = 0.15f;
    [SerializeField] private float _bobSpeed = 2f;
    [Tooltip("멀리서도 찾을 수 있게 켜 두는 빛 (선택)")]
    [SerializeField] private Light _beaconLight;

    private Transform _visual;
    private Vector3 _visualStart;

    private void Start()
    {
        GameObject visualObject = new GameObject("Visual");
        _visual = visualObject.transform;
        _visual.SetParent(transform, false);
        if (_modelPrefab != null)
        {
            GameObject model = Instantiate(_modelPrefab, _visual);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * _modelScale;
            foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }
        }
        _visualStart = _visual.localPosition;
    }

    private void Update()
    {
        if (_visual == null)
        {
            return;
        }
        _visual.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        _visual.localPosition = _visualStart + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
        if (_beaconLight != null)
        {
            _beaconLight.intensity = 2f + Mathf.Sin(Time.time * 4f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsDone || other.GetComponentInParent<PlayerMove>() == null)
        {
            return;
        }
        MarkDone();
        SoundManager.Play(SfxId.GetWeapon);
        NoticeUI.Show("폭발물 획득!  막힌 길로 이동해 [E] 키로 설치하라");
        gameObject.SetActive(false);
    }
}
