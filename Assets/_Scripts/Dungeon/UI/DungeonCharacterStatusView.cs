using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 필드용 아군 상태 뷰. BattleCharacterStatusView와 구조는 비슷하지만
/// 데이터 소스가 BattleUnit이 아니라 CharacterStats/CharacterVitals(필드에서 계속 유지되는 실제 데이터).
/// - RearFrame(본인 턴 강조), StatusIcon(상태이상)은 던전에서는 필요 없어 제외.
/// - HP/MP는 CharacterVitals.OnVitalsChanged를 구독해서 실시간 갱신.
/// </summary>
public class DungeonCharacterStatusView : MonoBehaviour
{
    [Header("Icon")]
    [SerializeField] private Image _iconImg;

    [Header("Texts")]
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private TMP_Text _hpTxt;
    [SerializeField] private TMP_Text _mpTxt;

    [Header("HP / MP (Filled Image)")]
    [SerializeField] private Image _hpBarFillImg;
    [SerializeField] private Image _mpBarFillImg;
    [SerializeField] private float _fillTweenDuration = 0.3f;
    [SerializeField] private Ease _fillTweenEase = Ease.OutQuad;

    [Header("Dead State")]
    [Tooltip("살아있을 때 아이콘 색상")]
    [SerializeField] private Color _aliveIconColor = Color.white;
    [Tooltip("죽었을 때 아이콘 색상")]
    [SerializeField] private Color _deadIconColor = new Color(142f / 255f, 142f / 255f, 142f / 255f, 1f);

    private Tween _hpFillTween;
    private Tween _mpFillTween;

    private PersistentCharacterUnit _boundUnit;

    public PersistentCharacterUnit BoundUnit => _boundUnit;

    public void Bind(PersistentCharacterUnit unit)
    {
        UnsubscribeCurrent();

        _boundUnit = unit;

        if (unit == null)
        {
            UpdateDeadState(false);
            return;
        }

        if (_nameTxt != null) _nameTxt.text = unit.CharacterName;

        if (unit.CharacterStats != null)
        {
            UpdateIcon(unit.CharacterStats.Icon);

            unit.CharacterStats.OnStatsChanged += HandleStatsChanged;
        }

        if (unit.CharacterVitals != null)
        {
            unit.CharacterVitals.OnVitalsChanged += HandleVitalsChanged;
        }

        RefreshLevel();
        RefreshVitals();
    }

    private void RefreshLevel()
    {
        if (_boundUnit == null || _boundUnit.CharacterStats == null) return;

        if (_levelTxt != null) _levelTxt.text = $"{_boundUnit.CharacterStats.Level}";
    }

    private void HandleStatsChanged()
    {
        RefreshLevel();
    }

    private void UpdateIcon(Sprite icon)
    {
        if (_iconImg == null) return;

        _iconImg.sprite = icon;
        _iconImg.enabled = icon != null;
    }

    private void RefreshVitals()
    {
        if (_boundUnit == null || _boundUnit.CharacterVitals == null) return;

        CharacterVitals vitals = _boundUnit.CharacterVitals;

        UpdateHp(vitals.CurrentHp, vitals.MaxHp);
        UpdateMp(vitals.CurrentMp, vitals.MaxMp);
        UpdateDeadState(vitals.IsDead);
    }

    public void UpdateHp(int currentHp, int maxHp)
    {
        if (_hpBarFillImg != null)
        {
            float targetValue = maxHp > 0 ? (float)currentHp / maxHp : 0f;

            _hpFillTween?.Kill();
            _hpFillTween = _hpBarFillImg.DOFillAmount(targetValue, _fillTweenDuration).SetEase(_fillTweenEase);
        }

        if (_hpTxt != null) _hpTxt.text = $"{currentHp}/{maxHp}";
    }

    public void UpdateMp(int currentMp, int maxMp)
    {
        if (_mpBarFillImg != null)
        {
            float targetValue = maxMp > 0 ? (float)currentMp / maxMp : 0f;

            _mpFillTween?.Kill();
            _mpFillTween = _mpBarFillImg.DOFillAmount(targetValue, _fillTweenDuration).SetEase(_fillTweenEase);
        }

        if (_mpTxt != null) _mpTxt.text = $"{currentMp}/{maxMp}";
    }

    /// <summary>
    /// 캐릭터가 죽었을 때 아이콘 색상을 어둡게, 살아있으면 원래 색으로 되돌림.
    /// </summary>
    private void UpdateDeadState(bool isDead)
    {
        if (_iconImg == null) return;

        _iconImg.color = isDead ? _deadIconColor : _aliveIconColor;
    }

    public void Clear()
    {
        UnsubscribeCurrent();

        _boundUnit = null;

        if (_nameTxt != null) _nameTxt.text = string.Empty;
        if (_levelTxt != null) _levelTxt.text = string.Empty;

        _hpFillTween?.Kill();
        if (_hpBarFillImg != null) _hpBarFillImg.fillAmount = 0f;
        if (_hpTxt != null) _hpTxt.text = string.Empty;

        _mpFillTween?.Kill();
        if (_mpBarFillImg != null) _mpBarFillImg.fillAmount = 0f;
        if (_mpTxt != null) _mpTxt.text = string.Empty;

        UpdateIcon(null);
        UpdateDeadState(false);
    }
    private void HandleVitalsChanged()
    {
        RefreshVitals();
    }

    private void UnsubscribeCurrent()
    {
        if (_boundUnit == null) return;

        if (_boundUnit.CharacterVitals != null)
        {
            _boundUnit.CharacterVitals.OnVitalsChanged -= HandleVitalsChanged;
        }

        if (_boundUnit.CharacterStats != null)
        {
            _boundUnit.CharacterStats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    private void OnDisable()
    {
        _hpFillTween?.Kill();
        _mpFillTween?.Kill();
    }

    private void OnDestroy()
    {
        UnsubscribeCurrent();

        _hpFillTween?.Kill();
        _mpFillTween?.Kill();
    }
}