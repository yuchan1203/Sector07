using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;
    private GameState _gameState = GameState.Ready;
    public GameState GameState => _gameState;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Animator _playerAnimator;
    [Header("일시정지 UI")]
    [SerializeField] private GameObject _stopUI;
    [Header("커서")]
    [SerializeField] private CursorManager _cursorManager;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        // 씬을 다시 불러올 때 상태와 UI 참조가 새로 초기화되도록 DontDestroyOnLoad를 쓰지 않는다.
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
        else if (Input.GetKeyDown(KeyCode.R) && _gameState == GameState.GameOver)
        {
            Restart();
        }
    }

    private void TogglePause()
    {
        if (_stateText == null)
        {
            return;
        }
        if (_gameState == GameState.Playing)
        {
            _gameState = GameState.Pause;
            Time.timeScale = 0f;
            _stateText.gameObject.SetActive(true);
            _stateText.color = Color.white;
            _stateText.text = "일시정지";
            SetStopUIActive(true);
            if (_cursorManager != null)
            {
                _cursorManager.SetAltPeekEnabled(false);
                _cursorManager.UnlockCursor();
            }
        }
        else if (_gameState == GameState.Pause)
        {
            Resume();
        }
    }

    private void SetStopUIActive(bool active)
    {
        if (_stopUI != null)
        {
            _stopUI.SetActive(active);
        }
    }

    // 계속하기 버튼 OnClick
    public void Resume()
    {
        if (_gameState != GameState.Pause)
        {
            return;
        }
        _gameState = GameState.Playing;
        Time.timeScale = 1f;
        _stateText.text = "";
        _stateText.gameObject.SetActive(false);
        SetStopUIActive(false);
        if (_cursorManager != null)
        {
            _cursorManager.LockCursor();
            _cursorManager.SetAltPeekEnabled(true);
        }
    }

    // 다시 시작 버튼 OnClick
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 게임 종료 버튼 OnClick
    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }
    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.color = Color.white;
        _stateText.text = "3";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "2";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "1";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "";
        _stateText.gameObject.SetActive(false);
        _gameState = GameState.Playing;
        if (_cursorManager != null)
        {
            _cursorManager.SetAltPeekEnabled(true);
        }
    }

    public void GameOver()
    {
        if (_cursorManager != null)
        {
            _cursorManager.SetAltPeekEnabled(false);
        }
        _stateText.gameObject.SetActive(true);
        _gameState = GameState.GameOver;
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
        if (_playerAnimator != null)
        {
            _playerAnimator.SetFloat(AnimatorParams.MoveMotion, 0f);
        }
    }
}