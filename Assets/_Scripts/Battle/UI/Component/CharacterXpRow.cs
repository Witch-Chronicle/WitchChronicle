using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Result 패널의 캐릭터별 경험치 결과 한 줄(Prefab_CharacterXp) 표시 담당.
/// - 전투 전(Before) 경험치 상태로 먼저 그려두고, DOTween으로 Fill(Image)+텍스트가 함께 차오름.
/// - 레벨업이 있었다면: 기존 레벨 게이지를 가득 채운 뒤 0으로 리셋(레벨 텍스트 갱신),
///   이어서 새 레벨 기준으로 남은 경험치까지 다시 채움.
/// - 애니메이션이 끝나면(자연 종료든 CompleteImmediately로 스킵되든) OnCompleted를 한 번만 발동.
/// * 한 번의 전투로 레벨이 2번 이상 오르는 경우, 중간 레벨은 표시하지 않고
///   "이전 레벨 가득 참 -> 리셋 -> 최종 레벨 결과"로 단순화해서 보여줌.
/// </summary>
public class CharacterXpRow : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _levelTxt;

    [Header("Exp")]
    [SerializeField] private Image _xpFillImg;
    [SerializeField] private TMP_Text _xpTxt;

    [Header("Level Up")]
    [SerializeField] private GameObject _levelUpImg;
    [SerializeField] private float _levelUpBounceHeight = 15f;
    [SerializeField] private float _levelUpBounceDuration = 0.4f;
    [Tooltip("몇 번 튀어오를지 (1이면 한 번 튀고 끝, 2 이상이면 통통 튀는 느낌)")]
    [SerializeField] private int _levelUpBounceCount = 2;

    [Header("Fill Animation")]
    [SerializeField] private float _fillDuration = 1.5f;
    [SerializeField] private Ease _fillEase = Ease.OutQuad;
    [Tooltip("레벨업 리셋 시, 가득 찬 상태를 보여준 뒤 리셋되기까지의 짧은 정지 시간")]
    [SerializeField] private float _levelUpPauseDuration = 0f;

    /// <summary>
    /// 이 Row의 애니메이션이 끝났을 때(자연 종료 또는 스킵) 발동. 한 번만 호출됨.
    /// </summary>
    public event Action OnCompleted;

    private Sequence _fillSequence;
    private Tween _levelUpBounceTween;
    private CharacterRewardResult _currentResult;
    private bool _isCompleted;

    private RectTransform _levelUpRect;
    private Vector2 _levelUpOriginalPosition;

    private void Awake()
    {
        if (_levelUpImg != null)
        {
            _levelUpRect = _levelUpImg.transform as RectTransform;

            if (_levelUpRect != null)
            {
                _levelUpOriginalPosition = _levelUpRect.anchoredPosition;
            }
        }
    }

    /// <summary>
    /// 보상 결과 데이터를 UI에 반영하고, Fill/텍스트 애니메이션을 시작.
    /// </summary>
    public void SetData(CharacterRewardResult result)
    {
        if (result == null)
        {
            return;
        }

        _fillSequence?.Kill();
        _levelUpBounceTween?.Kill();

        _currentResult = result;
        _isCompleted = false;

        if (_icon != null)
        {
            _icon.sprite = result.Icon;
            _icon.enabled = result.Icon != null;
        }

        if (_nameTxt != null) _nameTxt.text = result.CharacterName;

        // 시작 상태: 전투 전 레벨/경험치 기준으로 세팅
        if (_levelTxt != null) _levelTxt.text = $"{result.LevelBefore}";

        if (_levelUpImg != null)
        {
            _levelUpImg.SetActive(false);

            if (_levelUpRect != null) _levelUpRect.anchoredPosition = _levelUpOriginalPosition;
        }

        int startExp = result.ExpBefore;
        int startRequired = Mathf.Max(1, result.RequiredExpBefore);

        if (_xpFillImg != null) _xpFillImg.fillAmount = startRequired > 0 ? (float)startExp / startRequired : 0f;

        UpdateExpText(startExp, startRequired);

        if (result.DidLevelUp)
        {
            PlayLevelUpSequence(result, startExp, startRequired);
        }
        else
        {
            PlaySimpleFillSequence(result, startExp);
        }
    }

    /// <summary>
    /// 레벨업 없는 경우: 전투 전 경험치에서 전투 후 경험치까지 한 번에 채움.
    /// </summary>
    private void PlaySimpleFillSequence(CharacterRewardResult result, int startExp)
    {
        int required = Mathf.Max(1, result.RequiredExp);

        _fillSequence = DOTween.Sequence();
        _fillSequence.Append(AnimateExpValue(startExp, result.CurrentExp, required));
        _fillSequence.OnComplete(HandleSequenceCompleted);
    }

    /// <summary>
    /// 레벨업이 있었던 경우: 기존 레벨 게이지를 가득 채운 뒤 리셋하고, 새 레벨 기준으로 다시 채움.
    /// </summary>
    private void PlayLevelUpSequence(CharacterRewardResult result, int startExp, int startRequired)
    {
        _fillSequence = DOTween.Sequence();

        // 1. 기존 레벨 게이지를 가득 채움
        _fillSequence.Append(AnimateExpValue(startExp, startRequired, startRequired));

        // 2. 잠깐 정지 (가득 찬 상태를 눈에 담을 시간)
        _fillSequence.AppendInterval(_levelUpPauseDuration);

        // 3. 레벨업 처리: 레벨 텍스트/Fill 리셋 + LevelUpImg 표시
        int finalRequired = Mathf.Max(1, result.RequiredExp);

        _fillSequence.AppendCallback(() =>
        {
            if (_levelTxt != null) _levelTxt.text = $"{result.LevelAfter}";

            if (_xpFillImg != null) _xpFillImg.fillAmount = 0f;

            UpdateExpText(0, finalRequired);

            PlayLevelUpBounce();
        });

        // 4. 새 레벨 기준으로 남은 경험치까지 다시 채움
        _fillSequence.Append(AnimateExpValue(0, result.CurrentExp, finalRequired));
        _fillSequence.OnComplete(HandleSequenceCompleted);
    }

    /// <summary>
    /// LevelUpImg를 활성화하고, 위로 살짝 튀었다가 제자리로 돌아오는 걸 _levelUpBounceCount번 반복.
    /// </summary>
    private void PlayLevelUpBounce()
    {
        if (_levelUpImg == null || _levelUpRect == null) return;

        _levelUpBounceTween?.Kill();

        _levelUpImg.SetActive(true);
        _levelUpRect.anchoredPosition = _levelUpOriginalPosition;

        float fixedX = _levelUpOriginalPosition.x;
        float startY = _levelUpOriginalPosition.y;
        float targetY = startY + _levelUpBounceHeight;

        float y = startY;

        _levelUpBounceTween = DOTween.To(
                () => y,
                value =>
                {
                    y = value;
                    _levelUpRect.anchoredPosition = new Vector2(fixedX, y);
                },
                targetY,
                _levelUpBounceDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(_levelUpBounceCount * 2, LoopType.Yoyo)
            .OnComplete(() =>
            {
                _levelUpRect.anchoredPosition = _levelUpOriginalPosition;
            });
    }
    /// <summary>
    /// fromExp -> toExp까지 Fill 값과 텍스트를 동시에 채우는 트윈 생성 (required는 그 구간 동안 고정).
    /// </summary>
    private Tween AnimateExpValue(int fromExp, int toExp, int required)
    {
        float value = fromExp;

        return DOTween.To(
                () => value,
                x =>
                {
                    value = x;

                    if (_xpFillImg != null) _xpFillImg.fillAmount = required > 0 ? value / required : 0f;

                    UpdateExpText(Mathf.RoundToInt(value), required);
                },
                toExp,
                _fillDuration)
            .SetEase(_fillEase);
    }

    private void UpdateExpText(int currentExp, int requiredExp)
    {
        if (_xpTxt != null) _xpTxt.text = $"{currentExp} / {requiredExp}";
    }

    private void HandleSequenceCompleted()
    {
        if (_isCompleted) return;

        _isCompleted = true;
        OnCompleted?.Invoke();
    }

    /// <summary>
    /// 진행 중인 애니메이션을 즉시 중단하고 최종 상태(전투 후 결과)로 바로 점프.
    /// 이미 완료된 상태라면 아무것도 하지 않음.
    /// </summary>
    public void CompleteImmediately()
    {
        if (_isCompleted) return;
        if (_currentResult == null) return;

        _fillSequence?.Kill();
        _fillSequence = null;

        if (_currentResult.DidLevelUp && _levelTxt != null)
        {
            _levelTxt.text = $"{_currentResult.LevelAfter}";
        }

        int finalRequired = Mathf.Max(1, _currentResult.RequiredExp);

        if (_xpFillImg != null) _xpFillImg.fillAmount = finalRequired > 0 ? (float)_currentResult.CurrentExp / finalRequired : 0f;

        UpdateExpText(_currentResult.CurrentExp, finalRequired);

        _isCompleted = true;
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        _fillSequence?.Kill();
        _levelUpBounceTween?.Kill();
    }
}