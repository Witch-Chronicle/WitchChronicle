using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Alert 요청을 생성하고 Queue에 적재하는 전역 관리자입니다.
///
/// 사용 예시:
/// AlertManager.Instance.Enqueue(AlertType.InventoryFull);
/// AlertManager.Instance.Enqueue(AlertType.ItemAcquired, itemName, count);
/// </summary>
public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private AlertDatabaseSO _database;

    private readonly Queue<AlertRequest> _requestQueue = new();

    /// <summary>
    /// 새로운 Alert가 Queue에 들어왔음을 UI에 알리는 이벤트입니다.
    /// </summary>
    public event Action OnAlertEnqueued;

    public int PendingCount => _requestQueue.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_database == null)
        {
            Debug.LogError(
                "[AlertManager] AlertDatabaseSO가 연결되지 않았습니다.",
                this
            );

            return;
        }

        _database.Initialize();
    }

    /// <summary>
    /// AlertType에 해당하는 문구를 Queue에 등록합니다.
    /// Text에 {0}, {1} 등이 있으면 args 값이 순서대로 적용됩니다.
    /// </summary>
    public void Enqueue(
        AlertType type,
        params object[] args)
    {
        if (_database == null)
        {
            Debug.LogWarning(
                "[AlertManager] AlertDatabaseSO가 없습니다.",
                this
            );

            return;
        }

        if (!_database.TryGetEntry(type, out AlertEntry entry))
        {
            Debug.LogWarning(
                $"[AlertManager] AlertType에 해당하는 Entry가 없습니다: {type}",
                this
            );

            return;
        }

        string formattedMessage;

        try
        {
            formattedMessage = FormatMessage(entry.Text, args);
        }
        catch (FormatException exception)
        {
            Debug.LogError(
                $"[AlertManager] Alert 문구 포맷에 실패했습니다.\n" +
                $"Type: {type}\n" +
                $"Text: {entry.Text}\n" +
                $"Parameter Count: {args?.Length ?? 0}\n" +
                $"Exception: {exception.Message}",
                this
            );

            return;
        }

        AlertRequest request = new AlertRequest(
            type,
            formattedMessage,
            entry.LifeTime
        );

        _requestQueue.Enqueue(request);
        OnAlertEnqueued?.Invoke();
    }

    /// <summary>
    /// 데이터베이스를 통하지 않고 직접 작성한 문구를 Alert로 등록합니다.
    /// 예외적인 상황에서만 사용하는 것을 권장합니다.
    /// </summary>
    public void EnqueueRaw(
        string message,
        float lifeTime = 2.5f)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning(
                "[AlertManager] 빈 Alert 문구는 등록할 수 없습니다.",
                this
            );

            return;
        }

        AlertRequest request = new AlertRequest(
            AlertType.None,
            message,
            Mathf.Max(0.1f, lifeTime)
        );

        _requestQueue.Enqueue(request);
        OnAlertEnqueued?.Invoke();
    }

    /// <summary>
    /// Queue에서 다음 Alert 요청을 가져옵니다.
    /// </summary>
    public bool TryDequeue(out AlertRequest request)
    {
        if (_requestQueue.Count <= 0)
        {
            request = default;
            return false;
        }

        request = _requestQueue.Dequeue();
        return true;
    }

    /// <summary>
    /// 아직 표시되지 않은 모든 Alert 요청을 제거합니다.
    /// 현재 화면에 표시된 팝업은 AlertUIController가 관리합니다.
    /// </summary>
    public void ClearPendingQueue()
    {
        _requestQueue.Clear();
    }

    private string FormatMessage(
        string template,
        object[] args)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (args == null || args.Length == 0)
        {
            return template;
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            template,
            args
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        OnAlertEnqueued = null;
        _requestQueue.Clear();
    }
}