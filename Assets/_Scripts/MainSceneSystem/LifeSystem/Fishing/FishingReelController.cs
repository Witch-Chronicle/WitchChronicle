using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FishingReelController : MonoBehaviour
{
    [Header("텐션 게이지 참조")]
    [SerializeField] private RectTransform gaugeBarRect;
    [SerializeField] private RectTransform fishIndicatorRect;

    [Header("진행 게이지")]
    [SerializeField] private Image progressGaugeFill;

    [Header("액션 버튼")]
    [SerializeField] private Button actionButton;
    [SerializeField] private EventTrigger actionButtonEventTrigger;

    [Header("인디케이터 이동 범위 여백")]
    [Range(0f, 0.2f)]
    [SerializeField] private float edgePadding = 0.05f;

    [Header("구간 판정 (0~1 정규화 값 기준)")]
    [Range(0f, 1f)]
    [SerializeField] private float greenZoneRadius = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float yellowZoneRadius = 0.6f;

    [Header("실패 시간")]
    [SerializeField] private float lineBreakTime = 2f;
    [SerializeField] private float escapeTime = 3f;

    [Header("제한 시간 UI (선택)")]
    [SerializeField] private TMPro.TMP_Text timeLimitText;

    private float fishPullSpeed = 0.4f;
    private float playerPullSpeed = 0.6f;
    private float progressFillSpeed = 0.15f;
    private float tensionShake = 0.3f;
    private float timeLimit = 10f;

    private bool _isMiniGameActive = false;
    private bool _isHolding = false;
    private float _tensionNormalized = 0f;
    private float _progressFill = 0f;
    private float _leftRedTimer = 0f;
    private float _rightRedTimer = 0f;
    private float _elapsedTime = 0f;

    private FishItemData _currentFish;

    public bool IsMiniGameActive => _isMiniGameActive;

    private void Awake()
    {
        RegisterButtonHoldEvents();
    }

    private void RegisterButtonHoldEvents()
    {
        if (actionButtonEventTrigger == null) return;

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ => _isHolding = true);
        actionButtonEventTrigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ => _isHolding = false);
        actionButtonEventTrigger.triggers.Add(pointerUp);

        var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(_ => _isHolding = false);
        actionButtonEventTrigger.triggers.Add(pointerExit);
    }

    public void StartMiniGame(FishItemData fish, RodItemData rod)
    {
        if (fish == null) return;

        _currentFish = fish;

        fishPullSpeed = fish.tensionRange;
        tensionShake = fish.tensionShake;
        playerPullSpeed = fish.playerPullSpeed;

        progressFillSpeed = fish.reelDuration > 0f ? (1f / fish.reelDuration) : 0.15f;
        timeLimit = fish.timeLimit;

        _tensionNormalized = 0f;
        _progressFill = 0f;
        _leftRedTimer = 0f;
        _rightRedTimer = 0f;
        _elapsedTime = 0f;
        _isHolding = false;
        _isMiniGameActive = true;

        UpdateIndicatorPosition();
        UpdateProgressGauge();
        UpdateTimeLimitText();

        // ★ 낚시(릴링) 시작 사운드
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(SfxType.FishingCast);
    }

    public void StopMiniGame()
    {
        _isMiniGameActive = false;
        _isHolding = false;
        if (timeLimitText != null) timeLimitText.text = "";
    }

    private void Update()
    {
        if (!_isMiniGameActive) return;

        UpdateTension();
        UpdateIndicatorPosition();
        UpdateElapsedTime();
        CheckZone();
        UpdateProgressGauge();
        UpdateTimeLimitText();
    }

    private void UpdateTension()
    {
        float direction = _isHolding ? -playerPullSpeed : fishPullSpeed;
        float shake = (Mathf.PerlinNoise(Time.time * 5f, 0f) - 0.5f) * 2f * tensionShake;

        _tensionNormalized += (direction + shake) * Time.deltaTime;
        _tensionNormalized = Mathf.Clamp(_tensionNormalized, -1f, 1f);
    }

    private void UpdateIndicatorPosition()
    {
        if (gaugeBarRect == null || fishIndicatorRect == null) return;

        float barWidth = gaugeBarRect.rect.width;
        float indicatorHalf = fishIndicatorRect.rect.width * 0.5f;
        float maxOffset = (barWidth * 0.5f) - indicatorHalf - (barWidth * edgePadding);

        float targetX = _tensionNormalized * maxOffset;
        float currentY = fishIndicatorRect.anchoredPosition.y;
        fishIndicatorRect.anchoredPosition = new Vector2(targetX, currentY);
    }

    private void UpdateElapsedTime()
    {
        if (timeLimit <= 0f) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= timeLimit)
        {
            CompleteReeling(false, FailReason.Timeout);
        }
    }

    private void CheckZone()
    {
        float abs = Mathf.Abs(_tensionNormalized);

        if (_tensionNormalized < -yellowZoneRadius)
        {
            _leftRedTimer += Time.deltaTime;
            _rightRedTimer = 0f;
            if (_leftRedTimer >= lineBreakTime)
            {
                CompleteReeling(false, FailReason.LineBreak);
            }
        }
        else if (_tensionNormalized > yellowZoneRadius)
        {
            _rightRedTimer += Time.deltaTime;
            _leftRedTimer = 0f;
            if (_rightRedTimer >= escapeTime)
            {
                CompleteReeling(false, FailReason.Escape);
            }
        }
        else
        {
            _leftRedTimer = 0f;
            _rightRedTimer = 0f;

            if (abs <= greenZoneRadius)
            {
                _progressFill += progressFillSpeed * Time.deltaTime;
                if (_progressFill >= 1f)
                {
                    _progressFill = 1f;
                    CompleteReeling(true, FailReason.None);
                }
            }
        }
    }

    private void UpdateProgressGauge()
    {
        if (progressGaugeFill != null)
        {
            progressGaugeFill.fillAmount = _progressFill;
        }
    }

    private void UpdateTimeLimitText()
    {
        if (timeLimitText == null) return;

        if (timeLimit <= 0f)
        {
            timeLimitText.text = "";
            return;
        }

        float remaining = Mathf.Max(0f, timeLimit - _elapsedTime);
        timeLimitText.text = $"{remaining:F1}s";
        timeLimitText.color = remaining <= 3f ? new Color(1f, 0.3f, 0.3f) : Color.white;
    }

    private void CompleteReeling(bool success, FailReason reason)
{
    if (!_isMiniGameActive) return;
    _isMiniGameActive = false;
    _isHolding = false;

    // ★ 낚시 결과 사운드 (성공/실패)
    if (SoundManager.Instance != null)
    {
        SoundManager.Instance.PlaySfx(success ? SfxType.FishingSuccess : SfxType.FishingFail);
    }

    if (FishingManager.Instance != null)
    {
        FishingManager.Instance.CompleteReeling(success, _currentFish, reason);
    }
}

    public enum FailReason
    {
        None,
        LineBreak,
        Escape,
        Timeout
    }
}