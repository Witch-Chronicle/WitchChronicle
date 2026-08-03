using System.Collections;
using TMPro;
using UnityEngine;

public class ShowMessageManager : MonoBehaviour
{
    public static ShowMessageManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text _message;

    [SerializeField]
    private float _duration;

    [SerializeField]
    private PlayerInteractor _playerInteractor;

    private bool _isShowingMessage;

    // 인벤토리, 스탯, 상점 등의 UI가 열렸을 때
    // 상호작용 메시지 표시를 막기 위한 상태
    private bool _isBlockedByUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindPlayerInteractor();
    }

    private void Update()
    {
        // 외부 UI가 열려 있는 동안에는 메시지를 절대 표시하지 않음
        if (_isBlockedByUI)
        {
            SetMessageVisible(false);
            return;
        }

        if (_isShowingMessage)
        {
            return;
        }

        if (_playerInteractor == null)
        {
            FindPlayerInteractor();
            SetMessageVisible(false);
            return;
        }

        if (_playerInteractor.Current == null)
        {
            SetMessageVisible(false);
            return;
        }

        SetMessageVisible(true);
        _message.text = _playerInteractor.Current.Prompt;
    }

    /// <summary>
    /// 인벤토리, 스탯, Pause 등의 전체 화면 UI가 열릴 때 호출합니다.
    /// 상호작용 메시지를 즉시 숨기고 자동 표시를 차단합니다.
    /// </summary>
    public void BlockByUI()
    {
        _isBlockedByUI = true;

        StopAllCoroutines();
        _isShowingMessage = false;

        SetMessageVisible(false);
    }

    /// <summary>
    /// 전체 화면 UI가 모두 닫혔을 때 호출합니다.
    /// 이후 Update에서 현재 상호작용 대상에 따라 다시 표시됩니다.
    /// </summary>
    public void UnblockByUI()
    {
        _isBlockedByUI = false;
    }

    /// <summary>
    /// PlayerInteractor를 런타임에 탐색합니다.
    /// </summary>
    private void FindPlayerInteractor()
    {
        if (_playerInteractor != null)
        {
            return;
        }

        _playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        if (_playerInteractor == null)
        {
            Debug.LogWarning(
                "[ShowMessageManager] PlayerInteractor를 찾을 수 없습니다.",
                this
            );
        }
    }

    /// <summary>
    /// 지정한 시간 동안 시스템 메시지를 표시합니다.
    /// UI에 의해 차단된 상태에서는 표시하지 않습니다.
    /// </summary>
    public void ShowMessage(string message)
    {
        if (_isBlockedByUI)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, _duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        _isShowingMessage = true;

        SetMessageVisible(true);
        _message.text = message;

        yield return new WaitForSeconds(duration);

        _isShowingMessage = false;
    }

    private void SetMessageVisible(bool visible)
    {
        if (_message == null)
        {
            return;
        }

        _message.gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}