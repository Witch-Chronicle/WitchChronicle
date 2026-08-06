using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 프리팹(EnemyWorldSpaceCanvas)에 부착. 타겟으로 선택된 적 머리 위에 이름/HP/약점·저항/상태이상 아이콘을 표시.
/// - 평소엔 꺼져있다가, BattleTargetCycler가 이 적을 타겟으로 지정할 때만 Show().
/// - CharacterWorldSpaceCanvas와 동일하게 매 프레임 카메라를 향해 회전(billboard).
/// - HP는 BattleUnit.OnHpChanged를 구독해서 Image(Filled)가 DOTween으로 부드럽게 변화.
/// - 약점/저항 아이콘은 Show(SkillData) 호출 시 넘겨받은 스킬의 ElementType을
///   이 적의 EnemyBattleData.WeakElements/ResistElements와 비교해서 자체적으로 판단/표시.
/// - 상태이상 아이콘은 BattleUIContext.OnStatusApplied/OnStatusRemoved를 구독해서
///   StatusIcons(Layout Group) 밑에 _statusIconTemplate을 복제/제거하는 방식으로 여러 개 동시 표시.
///   * 유닛 바인딩(HP/상태이상 구독)은 Show()/Hide()와 무관하게 전투 내내 유지됨 - 타겟 아닐 때
///     걸린 상태이상도 놓치지 않고 누적됨. Show()/Hide()는 순수하게 화면 노출 여부만 담당.
/// * BattleUnit은 전투 시작 시점에야 생성되는 순수 C# 객체라서, BattleActor를 통해 매번 조회
///   (BattleCharacterUISet/BattleActionBarController와 동일한 패턴).
/// </summary>
public class EnemyTargetOverlay : MonoBehaviour
{
    [Header("Name / HP")]
    [SerializeField] private TMP_Text _nameTxt;
    [Tooltip("HpSlider 대신 Filled Image로 표시 (Image Type = Filled)")]
    [SerializeField] private Image _hpBarImage;
    [SerializeField] private TMP_Text _hpTxt;

    [Header("Billboard (카메라 향해 회전)")]
    [SerializeField] private bool _billboard = true;

    [Header("HP Bar Animation")]
    [SerializeField] private float _hpTweenDuration = 0.3f;
    [SerializeField] private Ease _hpTweenEase = Ease.OutQuad;

    [Header("Element Affinity (약점/저항 아이콘, 스프라이트 교체 방식)")]
    [Tooltip("약점/저항일 때 활성화되는 아이콘 오브젝트. 평소엔 비활성.")]
    [SerializeField] private Image _elementAffinityIndicatorImage;
    [SerializeField] private Sprite _weakSprite;
    [SerializeField] private Sprite _resistSprite;

    [Header("Status Effect Icons (동적 생성, StatusIcons에 Layout Group 있음)")]
    [Tooltip("StatusIcons 밑에 미리 배치된 템플릿. 활성 상태이상 개수만큼 이걸 복제해서 사용.")]
    [SerializeField] private Image _statusIconTemplate;
    [Tooltip("생성된 아이콘들이 들어갈 부모(Layout Group 붙은 StatusIcons)")]
    [SerializeField] private Transform _statusIconsParent;

    [Header("Particle (타겟팅 중 은은하게 움직이는 장식 이미지)")]
    [SerializeField] private Image _particleImage;
    [SerializeField] private float _particleMoveDistance = 8f;
    [SerializeField] private float _particleMoveDuration = 2f;
    [SerializeField] private float _particleRotateAngle = 6f;
    [SerializeField] private float _particleRotateDuration = 2.5f;

    [Header("Targeting-only Visuals (Idle 상태에선 숨김, SliderFrame/HpBar만 노출)")]
    [Tooltip("Idle일 때 숨겨질 배경 프레임")]
    [SerializeField] private GameObject _bgFrameObject;

    private readonly List<Tween> _particleTweens = new List<Tween>();
    private readonly Dictionary<StatusEffectType, Image> _activeStatusIcons = new Dictionary<StatusEffectType, Image>();

    private BattleActor _ownerActor;
    private Tween _hpTween;
    private bool _isBound;
    private bool _isContextSubscribed;

    private void Awake()
    {
        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        if (_statusIconTemplate != null)
        {
            _statusIconTemplate.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        TrySubscribeBattleContext();
        BindIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeBattleContext();
    }

    private void LateUpdate()
    {
        if (_billboard == false) return;
        if (Camera.main == null) return;

        transform.rotation = Camera.main.transform.rotation;
    }

    private void OnDestroy()
    {
        UnbindCurrent();
    }

    /// <summary>
    /// BattleUnit이 생성되는 시점(전투 시작)을 놓쳤을 수 있으니 OnBattleStarted에도 재시도.
    /// </summary>
    private void TrySubscribeBattleContext()
    {
        if (_isContextSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnBattleStarted += HandleBattleStarted;
        _isContextSubscribed = true;
    }

    private void UnsubscribeBattleContext()
    {
        if (_isContextSubscribed == false) return;

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        }

        _isContextSubscribed = false;
    }

    private void HandleBattleStarted()
    {
        BindIfNeeded();
    }

    /// <summary>
    /// BattleTargetCycler가 이 적을 타겟으로 지정할 때 호출.
    /// isTargeting = false(Idle 기본 타겟)면 SliderFrame/HpBar만 노출, 나머지(BG_Frame/NameTxt/Particle/StatusIcons)는 숨김.
    /// isTargeting = true(공격/스킬 조준 중)면 전부 노출.
    /// pendingSkill이 있으면(스킬 조준 중) 약점/저항 아이콘도 같이 판단해서 표시.
    /// * 이름/HP/상태이상 바인딩은 여기서 하지 않음 - OnEnable/OnBattleStarted 시점에 이미 되어있음.
    /// </summary>
    public void Show(bool isTargeting, SkillData pendingSkill = null)
    {
        gameObject.SetActive(true);
        UpdateElementAffinity(pendingSkill);
        SetTargetingOnlyVisuals(isTargeting);
    }

    /// <summary>
    /// 타겟에서 해제되거나 죽었을 때 호출. 화면에서만 숨기고, 유닛 바인딩/상태이상 아이콘은 유지.
    /// </summary>
    public void Hide()
    {
        HideElementAffinity();
        gameObject.SetActive(false);
    }

    private void BindIfNeeded()
    {
        if (_isBound) return;
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;

        BattleUnit unit = _ownerActor.BattleUnit;

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;

        unit.OnHpChanged += HandleHpChanged;
        _isBound = true;

        UpdateHpImmediate(unit.CurrentHp, unit.MaxHp);

        SubscribeStatusEvents();
    }

    private void UnbindCurrent()
    {
        if (_isBound == false) return;

        if (_ownerActor != null && _ownerActor.HasBattleUnit)
        {
            _ownerActor.BattleUnit.OnHpChanged -= HandleHpChanged;
        }

        _isBound = false;
        _hpTween?.Kill();

        UnsubscribeStatusEvents();
        ClearAllStatusIcons();
    }

    /// <summary>
    /// Idle(기본 타겟 표시)일 땐 BG_Frame/NameTxt/Particle/StatusIcons를 숨기고 SliderFrame/HpBar만 남김.
    /// Targeting(조준 중)일 땐 전부 노출.
    /// </summary>
    private void SetTargetingOnlyVisuals(bool visible)
    {
        if (_bgFrameObject != null) _bgFrameObject.SetActive(visible);
        if (_nameTxt != null) _nameTxt.gameObject.SetActive(visible);
        if (_particleImage != null) _particleImage.gameObject.SetActive(visible);
        if (_statusIconsParent != null) _statusIconsParent.gameObject.SetActive(visible);
    }

    private void HandleHpChanged()
    {
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;

        BattleUnit unit = _ownerActor.BattleUnit;
        UpdateHpAnimated(unit.CurrentHp, unit.MaxHp);
    }

    private void UpdateHpImmediate(int currentHp, int maxHp)
    {
        if (_hpBarImage != null)
        {
            _hpBarImage.DOKill();
            _hpBarImage.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        }

        if (_hpTxt != null) _hpTxt.text = $"{currentHp} / {maxHp}";
    }

    private void UpdateHpAnimated(int currentHp, int maxHp)
    {
        if (_hpBarImage != null)
        {
            float targetValue = maxHp > 0 ? (float)currentHp / maxHp : 0f;

            _hpTween?.Kill();
            _hpTween = _hpBarImage.DOFillAmount(targetValue, _hpTweenDuration).SetEase(_hpTweenEase);
        }

        if (_hpTxt != null) _hpTxt.text = $"{currentHp} / {maxHp}";
    }

    // ---------- Element Affinity ----------

    private void UpdateElementAffinity(SkillData pendingSkill)
    {
        if (pendingSkill == null)
        {
            HideElementAffinity();
            return;
        }

        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        EnemyBattleData enemyData = _ownerActor != null ? _ownerActor.EnemyBattleData : null;

        if (enemyData == null)
        {
            HideElementAffinity();
            return;
        }

        ElementType skillElement = pendingSkill.ElementType;

        if (ContainsElement(enemyData.WeakElements, skillElement))
        {
            SetAffinityIcon(_weakSprite);
        }
        else if (ContainsElement(enemyData.ResistElements, skillElement))
        {
            SetAffinityIcon(_resistSprite);
        }
        else
        {
            HideElementAffinity();
        }
    }

    private void SetAffinityIcon(Sprite sprite)
    {
        if (_elementAffinityIndicatorImage == null) return;

        _elementAffinityIndicatorImage.sprite = sprite;
        _elementAffinityIndicatorImage.gameObject.SetActive(sprite != null);
    }

    private void HideElementAffinity()
    {
        if (_elementAffinityIndicatorImage == null) return;

        _elementAffinityIndicatorImage.gameObject.SetActive(false);
    }

    private static bool ContainsElement(IReadOnlyList<ElementType> elements, ElementType element)
    {
        if (elements == null) return false;

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] == element) return true;
        }

        return false;
    }

    // ---------- Status Effect Icons ----------

    private bool _isStatusSubscribed;

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
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;
        if (unit != _ownerActor.BattleUnit) return;

        ShowStatusIcon(type);
    }

    private void HandleStatusRemoved(BattleUnit unit, StatusEffectType type)
    {
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;
        if (unit != _ownerActor.BattleUnit) return;

        HideStatusIcon(type);
    }

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
}