using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 프리팹(EnemyWorldSpaceCanvas)에 부착. 타겟으로 선택된 적 머리 위에 이름/HP/약점·저항 아이콘을 표시.
/// - 평소엔 꺼져있다가, BattleTargetCycler가 이 적을 타겟으로 지정할 때만 Show().
/// - CharacterWorldSpaceCanvas와 동일하게 매 프레임 카메라를 향해 회전(billboard).
/// - HP는 BattleUnit.OnHpChanged를 구독해서 Image(Filled)가 DOTween으로 부드럽게 변화.
/// - 약점/저항 아이콘은 Show(SkillData) 호출 시 넘겨받은 스킬의 ElementType을
///   이 적의 EnemyBattleData.WeakElements/ResistElements와 비교해서 자체적으로 판단/표시.
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

    [Header("Particle (타겟팅 중 은은하게 움직이는 장식 이미지)")]
    [SerializeField] private Image _particleImage;
    [SerializeField] private float _particleMoveDistance = 8f;
    [SerializeField] private float _particleMoveDuration = 2f;
    [SerializeField] private float _particleRotateAngle = 6f;
    [SerializeField] private float _particleRotateDuration = 2.5f;

    private readonly List<Tween> _particleTweens = new List<Tween>();

    private BattleActor _ownerActor;
    private Tween _hpTween;
    private bool _isBound;

    private void Awake()
    {
        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        HideImmediate();
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
        // StopParticleIdle();
    }

    /// <summary>
    /// BattleTargetCycler가 이 적을 타겟으로 지정할 때 호출.
    /// pendingSkill이 있으면(스킬 조준 중) 약점/저항 아이콘도 같이 판단해서 표시.
    /// 기본 공격 조준 중이거나 Idle 상태면 null로 넘어와 아이콘은 숨김 상태 유지.
    /// </summary>
    public void Show(SkillData pendingSkill = null)
    {
        gameObject.SetActive(true);
        BindIfNeeded();
        UpdateElementAffinity(pendingSkill);
        // StartParticleIdle();
    }

    /// <summary>
    /// 타겟에서 해제되거나 죽었을 때 호출.
    /// </summary>
    public void Hide()
    {
        UnbindCurrent();
        HideElementAffinity();
        // StopParticleIdle();
        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        UnbindCurrent();
        HideElementAffinity();
        // StopParticleIdle();
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

    /// <summary>
    /// pendingSkill이 null이면 무조건 숨김. 아니면 이 적(EnemyBattleData)의 Weak/Resist와 비교해서
    /// 아이콘 스프라이트를 교체 + 활성화, 해당 없으면 숨김.
    /// </summary>
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

    // ---------- Particle (장식용 아이들 모션) ----------

    private void StartParticleIdle()
    {
        StopParticleIdle();

        if (_particleImage == null) return;

        RectTransform rt = _particleImage.rectTransform;

        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;

        _particleTweens.Add(
            rt.DOLocalMoveX(_particleMoveDistance, _particleMoveDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));

        _particleTweens.Add(
            rt.DOLocalRotate(new Vector3(0f, 0f, _particleRotateAngle), _particleRotateDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));
    }

    private void StopParticleIdle()
    {
        for (int i = 0; i < _particleTweens.Count; i++)
        {
            _particleTweens[i]?.Kill();
        }

        _particleTweens.Clear();

        if (_particleImage != null)
        {
            RectTransform rt = _particleImage.rectTransform;
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
        }
    }
}