using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_BattleCharacter에 붙는 뷰.
/// - HP/MP는 Slider가 아니라 Filled Image(HpBarFill/MpBarFill) 방식으로 표시.
/// - Icon은 BattleUnit.Icon을 그대로 바인딩
/// - HP/MP는 BattleUnit.OnHpChanged/OnMpChanged를 구독해서 실시간 갱신
/// - 죽으면 CharacterIcon(_iconImg)의 색상을 _deadIconColor로 변경 (별도 오버레이 이미지 없이 색상만 변경)
/// - 상태이상 아이콘은 BattleUIContext.OnStatusApplied/OnStatusRemoved를 구독해서
///   StatusIcons(Layout Group) 밑에 _statusIconTemplate을 복제/제거하는 방식으로 여러 개 동시 표시.
/// * RearFrame/StatusIcon(템플릿 제외)은 필드만 연결해두고 로직은 추후 작업 예정.
/// </summary>
public class BattleCharacterStatusView : MonoBehaviour
{
    [Header("Scale 대상 (본인 턴 강조 등에 사용)")]
    [SerializeField] private RectTransform _visualRoot;

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

    [Header("Status Effect Icons (동적 생성, StatusIcons에 Layout Group 있음)")]
    [Tooltip("StatusIcons 밑에 미리 배치된 템플릿. 활성 상태이상 개수만큼 이걸 복제해서 사용.")]
    [SerializeField] private Image _statusIconTemplate;
    [Tooltip("생성된 아이콘들이 들어갈 부모(Layout Group 붙은 StatusIcons)")]
    [SerializeField] private Transform _statusIconsParent;

    [Header("Rear Frame (본인 턴 강조, Reveal 셰이더)")]
    [Tooltip("본인 턴일 때 Reveal 애니메이션과 함께 나타나는 프레임. 평소엔 비활성.")]
    [SerializeField] private Image _rearFrame;
    [SerializeField, Min(0.01f)] private float _rearFrameRevealDuration = 0.35f;
    [SerializeField] private Ease _rearFrameRevealEase = Ease.OutCubic;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private readonly Dictionary<StatusEffectType, Image> _activeStatusIcons = new Dictionary<StatusEffectType, Image>();
    private Material _rearFrameRuntimeMaterial;
    private bool _isStatusSubscribed;
    private Tween _rearFrameRevealTween;
    private Tween _hpFillTween;
    private Tween _mpFillTween;

    public BattleUnit BoundUnit { get; private set; }

    /// <summary>
    /// 스케일 애니메이션 등 외부에서 크기를 조작할 대상. 지정 안 해두면 이 오브젝트 자신으로 대체.
    /// </summary>
    public RectTransform VisualRoot => _visualRoot != null ? _visualRoot : transform as RectTransform;

    private void Awake()
    {
        if (_statusIconTemplate != null)
        {
            _statusIconTemplate.gameObject.SetActive(false);
        }

        InitializeRearFrameMaterial();
        HideRearFrameImmediate();
    }

    /// <summary>
    /// 공유 Material을 직접 건드리지 않도록 이 뷰 전용 런타임 Material 복제.
    /// </summary>
    private void InitializeRearFrameMaterial()
    {
        if (_rearFrame == null) return;

        if (_rearFrame.material == null)
        {
            Debug.LogWarning($"{name}: RearFrame에 Reveal Material이 없습니다.", this);
            return;
        }

        _rearFrameRuntimeMaterial = new Material(_rearFrame.material);
        _rearFrameRuntimeMaterial.name = $"{_rearFrame.material.name}_{name}_Instance";

        _rearFrame.material = _rearFrameRuntimeMaterial;
    }

    /// <summary>
    /// 본인 턴 강조. true면 RearFrame이 Reveal 애니메이션과 함께 나타나고,
    /// false면 즉시(애니메이션 없이) 비활성화.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            PlayRearFrameReveal();
        }
        else
        {
            HideRearFrameImmediate();
        }
    }

    private void PlayRearFrameReveal()
    {
        if (_rearFrame != null)
        {
            _rearFrame.gameObject.SetActive(true);
        }

        if (_rearFrameRuntimeMaterial == null) return;

        _rearFrameRevealTween?.Kill();

        _rearFrameRuntimeMaterial.SetFloat(RevealId, 0f);

        _rearFrameRevealTween = DOTween.To(
                () => _rearFrameRuntimeMaterial.GetFloat(RevealId),
                value => _rearFrameRuntimeMaterial.SetFloat(RevealId, value),
                1f,
                _rearFrameRevealDuration)
            .SetEase(_rearFrameRevealEase)
            .SetUpdate(true);
    }

    private void HideRearFrameImmediate()
    {
        _rearFrameRevealTween?.Kill();
        _rearFrameRevealTween = null;

        if (_rearFrameRuntimeMaterial != null)
        {
            _rearFrameRuntimeMaterial.SetFloat(RevealId, 0f);
        }

        if (_rearFrame != null)
        {
            _rearFrame.gameObject.SetActive(false);
        }
    }

    public void Bind(BattleUnit unit)
    {
        UnsubscribeCurrent();
        ClearAllStatusIcons();
        HideRearFrameImmediate();

        BoundUnit = unit;

        if (unit == null)
        {
            UpdateDeadState(false);
            return;
        }

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;
        if (_levelTxt != null) _levelTxt.text = $"{unit.Level}";

        UpdateIcon(unit.Icon);

        unit.OnHpChanged += HandleHpChanged;
        unit.OnMpChanged += HandleMpChanged;

        UpdateHp(unit.CurrentHp, unit.MaxHp);
        UpdateMp(unit.CurrentMp, unit.MaxMp);
        UpdateDeadState(unit.IsAlive == false);

        SubscribeStatusEvents();
    }

    /// <summary>
    /// 캐릭터 아이콘 표시. 아이콘이 없으면(null) 이미지 자체를 비활성화.
    /// </summary>
    private void UpdateIcon(Sprite icon)
    {
        if (_iconImg == null) return;

        _iconImg.sprite = icon;
        _iconImg.enabled = icon != null;
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
        ClearAllStatusIcons();
        HideRearFrameImmediate();

        BoundUnit = null;
        if (_nameTxt != null) _nameTxt.text = string.Empty;

        _hpFillTween?.Kill();
        if (_hpBarFillImg != null) _hpBarFillImg.fillAmount = 0f;
        if (_hpTxt != null) _hpTxt.text = string.Empty;

        _mpFillTween?.Kill();
        if (_mpBarFillImg != null) _mpBarFillImg.fillAmount = 0f;
        if (_mpTxt != null) _mpTxt.text = string.Empty;

        UpdateIcon(null);
        UpdateDeadState(false);
    }

    private void HandleHpChanged()
    {
        if (BoundUnit == null) return;

        UpdateHp(BoundUnit.CurrentHp, BoundUnit.MaxHp);
        UpdateDeadState(BoundUnit.IsAlive == false);
    }

    private void HandleMpChanged()
    {
        if (BoundUnit != null) UpdateMp(BoundUnit.CurrentMp, BoundUnit.MaxMp);
    }

    // ---------- Status Effect Icons ----------

    private void SubscribeStatusEvents()
    {
        if (_isStatusSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnStatusApplied += HandleStatusApplied;
        BattleUIContext.Instance.OnStatusRemoved += HandleStatusRemoved;

        _isStatusSubscribed = true;
    }

    private void UnsubscribeStatusEvents()
    {
        if (_isStatusSubscribed == false) return;

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnStatusApplied -= HandleStatusApplied;
            BattleUIContext.Instance.OnStatusRemoved -= HandleStatusRemoved;
        }

        _isStatusSubscribed = false;
    }

    private void HandleStatusApplied(BattleUnit unit, StatusEffectType type)
    {
        if (unit != BoundUnit) return;

        ShowStatusIcon(type);
    }

    private void HandleStatusRemoved(BattleUnit unit, StatusEffectType type)
    {
        if (unit != BoundUnit) return;

        HideStatusIcon(type);
    }

    /// <summary>
    /// 해당 상태이상 아이콘을 템플릿으로부터 복제해서 표시. 이미 떠있으면 무시(중첩 표시 안 함).
    /// </summary>
    private void ShowStatusIcon(StatusEffectType type)
    {
        if (_activeStatusIcons.ContainsKey(type)) return;
        if (_statusIconTemplate == null || _statusIconsParent == null) return;
        if (BattleUIContext.Instance == null) return;

        Battle.Rules.StatusEffectData data = BattleUIContext.Instance.GetStatusEffectData(type);
        if (data == null || data.Icon == null) return;

        Image instance = Instantiate(_statusIconTemplate, _statusIconsParent);
        instance.sprite = data.Icon;
        instance.gameObject.SetActive(true);

        StatusTooltipTrigger trigger = instance.GetComponent<StatusTooltipTrigger>();
        if (trigger == null)
        {
            trigger = instance.gameObject.AddComponent<StatusTooltipTrigger>();
        }
        trigger.SetTooltipInfo(data.StatusName, data.Description);

        _activeStatusIcons.Add(type, instance);
    }

    private void HideStatusIcon(StatusEffectType type)
    {
        if (_activeStatusIcons.TryGetValue(type, out Image instance) == false) return;

        if (instance != null)
        {
            Destroy(instance.gameObject);
        }

        _activeStatusIcons.Remove(type);
    }

    private void ClearAllStatusIcons()
    {
        foreach (KeyValuePair<StatusEffectType, Image> pair in _activeStatusIcons)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        _activeStatusIcons.Clear();
    }

    private void UnsubscribeCurrent()
    {
        if (BoundUnit == null) return;

        BoundUnit.OnHpChanged -= HandleHpChanged;
        BoundUnit.OnMpChanged -= HandleMpChanged;

        UnsubscribeStatusEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeCurrent();

        _hpFillTween?.Kill();
        _mpFillTween?.Kill();

        _rearFrameRevealTween?.Kill();

        if (_rearFrameRuntimeMaterial != null)
        {
            Destroy(_rearFrameRuntimeMaterial);
        }
    }
}