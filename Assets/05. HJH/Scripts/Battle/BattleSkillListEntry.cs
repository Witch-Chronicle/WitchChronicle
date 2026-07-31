using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 고정된 스킬 슬롯 하나를 담당한다.
/// UpPlace / MiddlePlace / DownPlace에 각각 하나씩 배치한다.
/// </summary>
public class BattleSkillListEntry : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    private static readonly int RevealId =
        Shader.PropertyToID("_Reveal");

    [Header("Input Area")]
    [Tooltip("실제 마우스 입력을 감지할 FrameImg")]
    [SerializeField] private Image _frameImage;

    [Header("Skill Information")]
    [SerializeField] private TMP_Text _skillNameText;
    [SerializeField] private TMP_Text _descriptionText;

    [SerializeField] private TMP_Text _tierText;
    [SerializeField] private TMP_Text _damageTypeText;
    [SerializeField] private TMP_Text _elementTypeText;
    [SerializeField] private TMP_Text _costText;

    [Tooltip("스킬 속성(ElementType) 아이콘. BattleUIContext의 ElementIconDatabase에서 조회. 해당 속성 아이콘이 없으면 비활성화.")]
    [SerializeField] private Image _skillElementIconImage;

    [Tooltip("이 스킬이 마법진 그리기(DrawGuideJson/DrawExampleSprite)를 사용하는 스킬일 때만 활성화되는 표시 오브젝트.")]
    [SerializeField] private GameObject _drawingTxt;

    [Header("Selection Visuals")]
    [Tooltip("BackParticle1, BackParticle2를 모두 등록")]
    [SerializeField] private Image[] _backParticleImages;

    [SerializeField] private Image _backFrameImage;
    [SerializeField] private Image _frontParticleImage;

    [Header("Reveal Animation")]
    [SerializeField, Min(0.01f)]
    private float _revealDuration = 0.2f;

    [SerializeField]
    private Ease _revealEase =
        Ease.OutCubic;

    [Header("Unavailable")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [SerializeField, Range(0f, 1f)]
    private float _unavailableAlpha = 0.4f;

    private SkillListController _owner;

    private SkillData _skillData;
    private int _skillIndex = -1;

    private Material[] _backParticleMaterials;
    private Material _backFrameMaterial;
    private Material _frontParticleMaterial;

    private Tween _revealTween;
    private float _revealValue;

    private bool _canUse;
    private bool _isSelected;
    private bool _isBound;

    public SkillData SkillData => _skillData;
    public int SkillIndex => _skillIndex;
    public bool CanUse => _canUse;
    public bool IsSelected => _isSelected;
    public bool IsBound => _isBound;
    private Sequence _idleSequence;
    private SkillPresentationPalette _presentationPalette;
    private readonly List<Tween> _idleTweens = new List<Tween>();


    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_frameImage != null)
        {
            _frameImage.raycastTarget = true;
        }

        if (_drawingTxt != null)
        {
            _drawingTxt.SetActive(false);
        }

        InitializeRuntimeMaterials();
        SetSelectedImmediate(false);
    }

    /// <summary>
    /// 이 고정 슬롯에 스킬 데이터를 연결한다.
    /// </summary>
    public void Bind(
    SkillData skillData,
    int skillIndex,
    bool canUse,
    SkillListController owner,
    SkillPresentationPalette palette)
    {
        _skillData = skillData;
        _skillIndex = skillIndex;
        _canUse = canUse;
        _owner = owner;
        _presentationPalette = palette;
        _isBound = skillData != null;

        gameObject.SetActive(_isBound);

        if (!_isBound)
            return;

        RefreshTexts();
        RefreshUsableState();
        SetSelectedImmediate(false);
    }

    /// <summary>
    /// 슬롯에 연결된 데이터를 제거하고 숨긴다.
    /// </summary>
    public void Clear()
    {
        _revealTween?.Kill();
        _revealTween = null;

        _skillData = null;
        _skillIndex = -1;
        _canUse = false;
        _isSelected = false;
        _isBound = false;
        _owner = null;

        ApplyReveal(0f);
        gameObject.SetActive(false);

        _presentationPalette = null;

        if (_tierText != null)
        {
            _tierText.text = string.Empty;
        }

        if (_damageTypeText != null)
        {
            _damageTypeText.text = string.Empty;
            _damageTypeText.gameObject.SetActive(true);
        }

        if (_elementTypeText != null)
        {
            _elementTypeText.text = string.Empty;
            _elementTypeText.gameObject.SetActive(true);
        }

        if (_skillElementIconImage != null)
        {
            _skillElementIconImage.gameObject.SetActive(false);
        }

        if (_drawingTxt != null)
        {
            _drawingTxt.SetActive(false);
        }


        if (_costText != null)
        {
            _costText.text = string.Empty;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isBound)
        {
            return;
        }

        _owner?.SelectSkillByIndex(_skillIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isBound)
        {
            return;
        }

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        _owner?.SelectSkillByIndex(_skillIndex);
        _owner?.SubmitSelected();
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
            return;

        _isSelected = selected;

        if (selected)
        {
            PlayReveal();
        }
        else
        {
            StopIdleEffect();
            HideReveal();
        }
    }

    public void SetSelectedImmediate(bool selected)
    {
        _isSelected = selected;

        _revealTween?.Kill();
        _revealTween = null;

        _revealValue = selected ? 1f : 0f;
        ApplyReveal(_revealValue);
    }

    private void PlayReveal()
    {
        _revealTween?.Kill();

        StopIdleEffect();

        _revealValue = 0f;
        ApplyReveal(0f);

        _revealTween =
            DOTween.To(
                () => _revealValue,
                x =>
                {
                    _revealValue = x;
                    ApplyReveal(x);
                },
                1f,
                _revealDuration)
            .SetEase(_revealEase)
            .SetUpdate(true)
            .OnComplete(StartIdleEffect);
    }


    private void StartIdleEffect()
    {
        StopIdleEffect();

        //----------------------------------------
        // BackParticle1
        //----------------------------------------

        if (_backParticleImages.Length > 0 && _backParticleImages[0] != null)
        {
            RectTransform rt = _backParticleImages[0].rectTransform;

            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;

            _idleTweens.Add(
                rt.DOLocalMoveX(10f, 1.8f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));

            _idleTweens.Add(
                rt.DOLocalRotate(new Vector3(0, 0, 6), 2.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
        }

        //----------------------------------------
        // BackParticle2
        //----------------------------------------

        if (_backParticleImages.Length > 1 && _backParticleImages[1] != null)
        {
            RectTransform rt = _backParticleImages[1].rectTransform;

            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;

            _idleTweens.Add(
                rt.DOLocalMoveX(-8f, 2.3f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));

            _idleTweens.Add(
                rt.DOLocalRotate(new Vector3(0, 0, -8), 3.0f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
        }

        //----------------------------------------
        // FrontParticle
        //----------------------------------------

        if (_frontParticleImage != null)
        {
            RectTransform rt = _frontParticleImage.rectTransform;

            rt.localScale = Vector3.one;

            _idleTweens.Add(
                rt.DOScale(1.05f, 0.8f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
        }
    }

    private void StopIdleEffect()
    {
        for (int i = 0; i < _idleTweens.Count; i++)
        {
            _idleTweens[i]?.Kill();
        }

        _idleTweens.Clear();

        ResetParticleTransforms();
    }

    private void ResetParticleTransforms()
    {
        foreach (Image img in _backParticleImages)
        {
            if (img == null)
                continue;

            RectTransform rt = img.rectTransform;

            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        if (_frontParticleImage != null)
        {
            RectTransform rt =
                _frontParticleImage.rectTransform;

            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
    }

    private void HideReveal()
    {
        _revealTween?.Kill();
        _revealTween = null;

        _revealValue = 0f;
        ApplyReveal(0f);
    }

    private void RefreshTexts()
    {
        if (_skillData == null)
            return;

        //-------------------------
        // 이름
        //-------------------------

        if (_skillNameText != null)
        {
            _skillNameText.text =
                _skillData.SkillName;
        }

        //-------------------------
        // 설명
        //-------------------------

        if (_descriptionText != null)
        {
            _descriptionText.text =
                _skillData.Description;
        }

        //-------------------------
        // Tier
        //-------------------------

        if (_tierText != null)
        {
            _tierText.text =
                SkillTextFormatter.GetTierText(
                    _skillData.Tier);

            if (_presentationPalette != null)
            {
                _tierText.color =
                    _presentationPalette
                    .GetTierColor(
                        _skillData.Tier);
            }
        }

        //-------------------------
        // Damage Type
        //-------------------------

        if (_damageTypeText != null)
        {
            string damageText =
                SkillTextFormatter
                .GetDamageTypeText(
                    _skillData.DamageType);

            _damageTypeText.text =
                damageText;

            _damageTypeText.gameObject
                .SetActive(
                    !string.IsNullOrEmpty(
                        damageText));

            if (_presentationPalette != null)
            {
                _damageTypeText.color =
                    _presentationPalette
                    .GetDamageTypeColor(
                        _skillData.DamageType);
            }
        }

        //-------------------------
        // Element
        //-------------------------

        if (_elementTypeText != null)
        {
            string elementText =
                SkillTextFormatter
                .GetElementTypeText(
                    _skillData.ElementType);

            _elementTypeText.text =
                elementText;

            _elementTypeText.gameObject
                .SetActive(
                    !string.IsNullOrEmpty(
                        elementText));

            if (_presentationPalette != null)
            {
                _elementTypeText.color =
                    _presentationPalette
                    .GetElementColor(
                        _skillData.ElementType);
            }
        }

        if (_skillElementIconImage != null)
        {
            Sprite elementIcon = BattleUIContext.Instance != null
                ? BattleUIContext.Instance.GetElementIcon(_skillData.ElementType)
                : null;

            _skillElementIconImage.sprite = elementIcon;
            _skillElementIconImage.gameObject.SetActive(elementIcon != null);
        }

        //-------------------------
        // Drawing (마법진 그리기 스킬 여부)
        //-------------------------

        if (_drawingTxt != null)
        {
            bool hasDrawGuide = _skillData.DrawGuideJson != null && _skillData.DrawExampleSprite != null;
            _drawingTxt.SetActive(hasDrawGuide);
        }

        //-------------------------
        // MP Cost
        //-------------------------

        if (_costText != null)
        {
            _costText.text =
                $"MP : {_skillData.MpCost}";
        }
    }

    private void RefreshUsableState()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha =
                _canUse ? 1f : _unavailableAlpha;
        }

        /*
         * 사용 불가능한 스킬도 마우스로 Hover할 수 있도록
         * Raycast는 유지한다.
         *
         * 실제 실행만 SubmitSelected에서 차단한다.
         */
        if (_frameImage != null)
        {
            _frameImage.raycastTarget = true;
        }
    }

    private void InitializeRuntimeMaterials()
    {
        if (_backParticleImages != null)
        {
            _backParticleMaterials =
                new Material[_backParticleImages.Length];

            for (int i = 0;
                 i < _backParticleImages.Length;
                 i++)
            {
                _backParticleMaterials[i] =
                    CreateRuntimeMaterial(
                        _backParticleImages[i]
                    );
            }
        }

        _backFrameMaterial =
            CreateRuntimeMaterial(_backFrameImage);

        _frontParticleMaterial =
            CreateRuntimeMaterial(_frontParticleImage);
    }

    private Material CreateRuntimeMaterial(Image image)
    {
        if (image == null)
        {
            return null;
        }

        image.raycastTarget = false;

        if (image.material == null)
        {
            Debug.LogWarning(
                $"[{nameof(BattleSkillListEntry)}] " +
                $"{name}/{image.name}에 Material이 없습니다.",
                image
            );

            return null;
        }

        Material runtimeMaterial =
            new Material(image.material);

        runtimeMaterial.name =
            $"{image.material.name}_{name}_Runtime";

        image.material = runtimeMaterial;

        if (!runtimeMaterial.HasProperty(RevealId))
        {
            Debug.LogWarning(
                $"[{nameof(BattleSkillListEntry)}] " +
                $"{runtimeMaterial.name}에 _Reveal 프로퍼티가 없습니다.",
                image
            );
        }

        return runtimeMaterial;
    }

    private void ApplyReveal(float value)
    {
        if (_backParticleMaterials != null)
        {
            for (int i = 0;
                 i < _backParticleMaterials.Length;
                 i++)
            {
                SetMaterialReveal(
                    _backParticleMaterials[i],
                    value
                );
            }
        }

        SetMaterialReveal(_backFrameMaterial, value);
        SetMaterialReveal(_frontParticleMaterial, value);
    }

    private static void SetMaterialReveal(
        Material material,
        float value)
    {
        if (material == null ||
            !material.HasProperty(RevealId))
        {
            return;
        }

        material.SetFloat(RevealId, value);
    }

    private void OnDestroy()
    {
        _revealTween?.Kill();
        StopIdleEffect();

        DestroyMaterials(_backParticleMaterials);
        DestroyMaterial(_backFrameMaterial);
        DestroyMaterial(_frontParticleMaterial);
    }

    private static void DestroyMaterials(
        Material[] materials)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            DestroyMaterial(materials[i]);
        }
    }

    private static void DestroyMaterial(Material material)
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}