using System.Collections;
using TMPro;
using UnityEngine;

public class ShowMessageManager : MonoBehaviour
{
    public static ShowMessageManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text _message;

    [SerializeField] private float _duration;

    [SerializeField]
    private PlayerInteractor _playerInteractor;

    private bool _isShowingMessage;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;
    }

    void Start()
    {
        FindPlayerInteractor();
    }

    private void Update()
    {
        if (_isShowingMessage)
        {
            return;
        }

        if (_playerInteractor == null)
        {
            FindPlayerInteractor();

            return;
        }

        if (_playerInteractor.Current == null)
        {
            _message.gameObject.SetActive(false);

            return;
        }

        _message.gameObject.SetActive(true);
        _message.text = _playerInteractor.Current.Prompt;
    }

    /// <summary>
    /// PlayerInteractor를 런타임에 탐색
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
            Debug.LogWarning("PlayerInteractor를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 메세지 표시, duration 동안만 코루틴으로 표시
    /// </summary>
    /// <param name="message">표시할 메세지</param>
    public void ShowMessage(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, _duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        _isShowingMessage = true;

        _message.gameObject.SetActive(true);
        _message.text = message;

        yield return new WaitForSeconds(duration);

        _isShowingMessage = false;
    }
}