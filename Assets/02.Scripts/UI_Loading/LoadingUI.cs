using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// LoadingScene의 로딩 화면. SceneLoader가 정한 목표 씬을 비동기로 불러오며 진행 막대를 채운다.
public class LoadingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _statusText;
    [Tooltip("막대의 가로 크기를 0~1로 늘려 진행률을 보여 주는 RectTransform")]
    [SerializeField] private RectTransform _loadingBar;
    [Tooltip("이 씬을 에디터에서 바로 실행해 목표 씬이 정해지지 않았을 때 가는 씬")]
    [SerializeField] private string _fallbackScene = "GameScene";
    [Tooltip("불러오기가 끝난 뒤 씬을 켜기 전에 잠깐 기다리는 시간(초)")]
    [SerializeField] private float _readyDelay = 0.2f;

    private const float LoadReadyProgress = 0.9f;

    private void Start()
    {
        if (_statusText == null || _loadingBar == null)
        {
            GameLogger.LogError("LoadingUI: 참조가 비어 있습니다. 텍스트와 막대 연결을 확인하세요.");
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        string target = SceneLoader.TargetScene;
        if (string.IsNullOrEmpty(target))
        {
            GameLogger.LogWarning("LoadingUI: 목표 씬이 정해지지 않아 " + _fallbackScene + "으로 이동합니다.");
            target = _fallbackScene;
        }
        _statusText.text = SceneLoader.Message;
        SetBar(0f);
        StartCoroutine(Load_Coroutine(target, SceneLoader.MinLoadingTime));
    }

    private IEnumerator Load_Coroutine(string target, float minTime)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(target);
        if (load == null)
        {
            GameLogger.LogError("LoadingUI: " + target + " 씬을 불러올 수 없습니다. 빌드 설정을 확인하세요.");
            yield break;
        }
        // 0.9에서 멈추는 비동기 로딩 진행률을 직접 켜 주기 전까지 씬을 활성화하지 않는다.
        load.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < minTime || load.progress < LoadReadyProgress)
        {
            elapsed += Time.unscaledDeltaTime;
            float timeRatio = minTime > 0f ? elapsed / minTime : 1f;
            SetBar(Mathf.Min(timeRatio, Mathf.Clamp01(load.progress / LoadReadyProgress)));
            yield return null;
        }

        SetBar(1f);
        yield return new WaitForSecondsRealtime(_readyDelay);
        load.allowSceneActivation = true;
    }

    private void SetBar(float value)
    {
        _loadingBar.localScale = new Vector3(Mathf.Clamp01(value), 1f, 1f);
    }
}
