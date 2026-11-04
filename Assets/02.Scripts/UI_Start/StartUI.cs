using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 시작 화면의 로그인/회원가입 UI. 실제 인증은 하지 않고 버튼을 누르면 바로 게임 씬으로 넘어간다.
public class StartUI : MonoBehaviour
{
    private const string SavedIdKey = "StartUI.SavedId";
    private const string SaveIdOnKey = "StartUI.SaveIdOn";

    [Header("패널")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _signUpPanel;

    [Header("입력")]
    [SerializeField] private TMP_InputField _loginIdInput;
    [SerializeField] private Toggle _saveIdToggle;

    [Header("전환 연출")]
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private CanvasGroup _flash;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private RectTransform _loadingBar;
    [SerializeField] private float _introDuration = 1f;
    [SerializeField] private float _minLoadingTime = 1.6f;

    [Header("씬")]
    [SerializeField] private string _gameSceneName = "GameScene";

    private bool _busy;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LoadSavedId();
        _signUpPanel.SetActive(false);
        _loginPanel.SetActive(false);
        _statusText.gameObject.SetActive(false);
        _loadingBar.parent.gameObject.SetActive(false);
        StartCoroutine(Intro_Coroutine());
    }

    private void Update()
    {
        if (_busy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectNext(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
    }

    // 회원가입 링크 OnClick (로그인 화면 -> 회원가입 화면)
    public void ShowSignUp()
    {
        if (!_busy)
        {
            StartCoroutine(SwitchPanel_Coroutine(_loginPanel, _signUpPanel));
        }
    }

    // 로그인 링크 OnClick (회원가입 화면 -> 로그인 화면)
    public void ShowLogin()
    {
        if (!_busy)
        {
            StartCoroutine(SwitchPanel_Coroutine(_signUpPanel, _loginPanel));
        }
    }

    // 로그인/가입하기 버튼 OnClick. 지금은 입력값과 상관없이 바로 게임을 시작한다.
    public void StartGame()
    {
        if (_busy)
        {
            return;
        }
        SaveId();
        StartCoroutine(StartGame_Coroutine());
    }

    // 게임 종료 버튼 OnClick
    public void QuitGame()
    {
        if (_busy)
        {
            return;
        }
        StartCoroutine(Quit_Coroutine());
    }

    private void LoadSavedId()
    {
        bool saveOn = PlayerPrefs.GetInt(SaveIdOnKey, 0) == 1;
        _saveIdToggle.SetIsOnWithoutNotify(saveOn);
        if (saveOn)
        {
            _loginIdInput.text = PlayerPrefs.GetString(SavedIdKey, "");
        }
    }

    private void SaveId()
    {
        if (_saveIdToggle.isOn)
        {
            PlayerPrefs.SetInt(SaveIdOnKey, 1);
            PlayerPrefs.SetString(SavedIdKey, _loginIdInput.text);
        }
        else
        {
            PlayerPrefs.DeleteKey(SaveIdOnKey);
            PlayerPrefs.DeleteKey(SavedIdKey);
        }
        PlayerPrefs.Save();
    }

    // Tab / Shift+Tab 으로 입력창 사이를 이동한다.
    private void SelectNext(bool backwards)
    {
        GameObject panel = _loginPanel.activeInHierarchy ? _loginPanel : _signUpPanel;
        if (!panel.activeInHierarchy)
        {
            return;
        }

        Selectable[] fields = panel.GetComponentsInChildren<TMP_InputField>();
        if (fields.Length == 0)
        {
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        int index = -1;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].gameObject == current)
            {
                index = i;
                break;
            }
        }

        int next = index < 0 ? 0 : (index + (backwards ? fields.Length - 1 : 1)) % fields.Length;
        fields[next].Select();
    }

    private IEnumerator Intro_Coroutine()
    {
        _fade.alpha = 1f;
        _fade.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < _introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _introDuration);
            _fade.alpha = (1f - t) * (Random.value < 0.08f ? 0.6f : 1f);
            yield return null;
        }
        _fade.alpha = 0f;
        _fade.blocksRaycasts = false;
        _loginPanel.SetActive(true);
    }

    private IEnumerator SwitchPanel_Coroutine(GameObject from, GameObject to)
    {
        _busy = true;
        yield return Flash_Coroutine(0.3f, 0.12f);
        from.SetActive(false);
        to.SetActive(true);
        yield return Flash_Coroutine(0.12f, 0.12f);
        _busy = false;
    }

    private IEnumerator Flash_Coroutine(float peak, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _flash.alpha = peak * (1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _flash.alpha = 0f;
    }

    private IEnumerator FadeToBlack_Coroutine(float duration)
    {
        _fade.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fade.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        _fade.alpha = 1f;
    }

    private IEnumerator StartGame_Coroutine()
    {
        _busy = true;
        EventSystem.current.SetSelectedGameObject(null);

        yield return Flash_Coroutine(0.55f, 0.18f);
        yield return FadeToBlack_Coroutine(0.5f);

        _statusText.gameObject.SetActive(true);
        _loadingBar.parent.gameObject.SetActive(true);
        _statusText.text = "ACCESS GRANTED";
        SetBar(0f);

        AsyncOperation load = SceneManager.LoadSceneAsync(_gameSceneName);
        load.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < _minLoadingTime || load.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            float shown = Mathf.Min(elapsed / _minLoadingTime, Mathf.Clamp01(load.progress / 0.9f));
            SetBar(shown);
            if (elapsed > _minLoadingTime * 0.45f)
            {
                _statusText.text = "구역 진입 중...";
            }
            yield return null;
        }

        SetBar(1f);
        yield return new WaitForSecondsRealtime(0.2f);
        load.allowSceneActivation = true;
    }

    private IEnumerator Quit_Coroutine()
    {
        _busy = true;
        yield return FadeToBlack_Coroutine(0.4f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetBar(float value)
    {
        _loadingBar.localScale = new Vector3(Mathf.Clamp01(value), 1f, 1f);
    }
}
