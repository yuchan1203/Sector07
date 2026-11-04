using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 시작 화면의 로그인/회원가입 UI. 계정은 로컬(PlayerPrefs)에만 저장하며, 로그인에 성공하면 게임 씬으로 넘어간다.
public class StartUI : MonoBehaviour
{
    private const string SavedIdKey = "StartUI.SavedId";
    private const string SaveIdOnKey = "StartUI.SaveIdOn";

    [Header("패널")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _signUpPanel;

    [Header("입력")]
    [SerializeField] private TMP_InputField _loginIdInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private Toggle _saveIdToggle;
    [SerializeField] private TMP_InputField _signUpIdInput;
    [SerializeField] private TMP_InputField _signUpPasswordInput;
    [SerializeField] private TMP_InputField _signUpConfirmInput;

    [Header("안내 문구")]
    [SerializeField] private TMP_Text _loginMessage;
    [SerializeField] private TMP_Text _signUpMessage;

    [Header("전환 연출")]
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private CanvasGroup _flash;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private float _introDuration = 1f;
    [Tooltip("ACCESS GRANTED 문구와 효과음을 보여 준 뒤 로딩 화면으로 넘어가기까지의 시간(초)")]
    [SerializeField] private float _accessHoldTime = 1.4f;

    [Header("씬")]
    [Tooltip("로그인 성공 후 SceneLoader로 이동할 씬")]
    [SerializeField] private string _gameSceneName = "GameScene";

    private const int MinIdLength = 3;
    private const int MaxIdLength = 16;
    private const int MinPasswordLength = 4;

    private readonly PlayerPrefsAccountRepository _accounts = new PlayerPrefsAccountRepository();
    private bool _busy;

    private bool HasAllReferences()
    {
        if (_loginPanel == null || _signUpPanel == null || _loginIdInput == null || _loginPasswordInput == null
            || _saveIdToggle == null || _signUpIdInput == null || _signUpPasswordInput == null || _signUpConfirmInput == null
            || _loginMessage == null || _signUpMessage == null
            || _fade == null || _flash == null || _statusText == null)
        {
            GameLogger.LogError("StartUI: 인스펙터 참조가 비어 있습니다. 패널/입력창/페이드 연결을 확인하세요.");
            return false;
        }
        return true;
    }

    private void Start()
    {
        if (!HasAllReferences())
        {
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LoadSavedId();
        _loginMessage.text = "";
        _signUpMessage.text = "";
        _signUpPanel.SetActive(false);
        _loginPanel.SetActive(false);
        _statusText.gameObject.SetActive(false);
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
            if (_signUpPanel.activeInHierarchy)
            {
                SignUp();
            }
            else if (_loginPanel.activeInHierarchy)
            {
                Login();
            }
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

    // 로그인 버튼 OnClick. 아이디/비밀번호가 맞으면 게임을 시작한다.
    public void Login()
    {
        if (_busy)
        {
            return;
        }

        string id = _loginIdInput.text.Trim();
        string password = _loginPasswordInput.text;
        if (id.Length == 0 || password.Length == 0)
        {
            _loginMessage.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }
        if (!_accounts.CheckPassword(id, password))
        {
            _loginMessage.text = "아이디 또는 비밀번호가 올바르지 않습니다.";
            _loginPasswordInput.text = "";
            _loginPasswordInput.Select();
            return;
        }

        _loginMessage.text = "";
        SaveId(id);
        StartCoroutine(StartGame_Coroutine());
    }

    // 가입하기 버튼 OnClick. 가입에 성공하면 로그인 화면으로 돌아간다.
    public void SignUp()
    {
        if (_busy)
        {
            return;
        }

        string id = _signUpIdInput.text.Trim();
        string password = _signUpPasswordInput.text;
        string error = ValidateSignUp(id, password, _signUpConfirmInput.text);
        if (error != null)
        {
            _signUpMessage.text = error;
            return;
        }
        if (!_accounts.Register(id, password))
        {
            _signUpMessage.text = "이미 사용 중인 아이디입니다.";
            return;
        }

        _signUpMessage.text = "";
        _signUpIdInput.text = "";
        _signUpPasswordInput.text = "";
        _signUpConfirmInput.text = "";
        _loginIdInput.text = id;
        _loginPasswordInput.text = "";
        _loginMessage.text = "가입이 완료되었습니다. 로그인해 주세요.";
        StartCoroutine(SwitchPanel_Coroutine(_signUpPanel, _loginPanel));
    }

    // 문제가 없으면 null, 있으면 안내 문구를 반환한다.
    private static string ValidateSignUp(string id, string password, string confirm)
    {
        if (id.Length < MinIdLength || id.Length > MaxIdLength)
        {
            return "아이디는 " + MinIdLength + "~" + MaxIdLength + "자로 입력해 주세요.";
        }
        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return "아이디는 글자, 숫자, _ 만 쓸 수 있습니다.";
            }
        }
        if (password.Length < MinPasswordLength)
        {
            return "비밀번호는 " + MinPasswordLength + "자 이상이어야 합니다.";
        }
        if (password != confirm)
        {
            return "비밀번호 확인이 일치하지 않습니다.";
        }
        return null;
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

    private void SaveId(string id)
    {
        if (_saveIdToggle.isOn)
        {
            PlayerPrefs.SetInt(SaveIdOnKey, 1);
            PlayerPrefs.SetString(SavedIdKey, id);
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
        _statusText.text = "ACCESS GRANTED";
        SoundManager.Play(SfxId.LoginSuccess);

        // 효과음이 끝까지 들리도록 잠시 기다린 뒤, 실제 로딩은 LoadingScene이 맡는다.
        yield return new WaitForSecondsRealtime(_accessHoldTime);
        SceneLoader.Load(_gameSceneName);
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

}
