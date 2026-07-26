using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 고정된 아이템 슬롯 하나를 담당한다.
/// UpPlace_Item / MiddlePlace_Item / DownPlace_Item에
/// 각각 하나씩 배치한다.
/// </summary>
public class BattleItemListEntry : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    private static readonly int RevealId =
        Shader.PropertyToID("_Reveal");

    [Header("Input Area")]
    [Tooltip("실제 마우스 입력을 감지할 FrameImg")]
    [SerializeField]
    private Image _frameImage;

    [Header("Item Information")]
    [SerializeField]
    private TMP_Text _itemNameText;

    [SerializeField]
    private TMP_Text _descriptionText;

    [SerializeField]
    private TMP_Text _amountText;

    [Header("Selection Visuals")]
    [Tooltip("BackParticle1, BackParticle2를 모두 등록")]
    [SerializeField]
    private Image[] _backParticleImages;

    [SerializeField]
    private Image _backFrameImage;

    [SerializeField]
    private Image _frontParticleImage;

    [Header("Reveal Animation")]
    [SerializeField, Min(0.01f)]
    private float _revealDuration = 0.2f;

    [SerializeField]
    private Ease _revealEase = Ease.OutCubic;

    [Header("Unavailable")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField, Range(0f, 1f)]
    private float _unavailableAlpha = 0.4f;

    private ItemListController _owner;

    private PotionItemData _itemData;

    /*
     * ItemListController가 관리하는
     * 전체 포션 목록에서의 실제 인덱스.
     */
    private int _itemIndex = -1;

    /*
     * 현재 인벤토리에 보유한 개수.
     */
    private int _amount;

    private Material[] _backParticleMaterials;
    private Material _backFrameMaterial;
    private Material _frontParticleMaterial;

    private Tween _revealTween;
    private Sequence _idleSequence;

    private float _revealValue;

    private bool _canUse;
    private bool _isSelected;
    private bool _isBound;

    public PotionItemData ItemData =>
        _itemData;

    public int ItemIndex =>
        _itemIndex;

    public int Amount =>
        _amount;

    public bool CanUse =>
        _canUse;

    public bool IsSelected =>
        _isSelected;

    public bool IsBound =>
        _isBound;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (_frameImage != null)
        {
            _frameImage.raycastTarget = true;
        }

        InitializeRuntimeMaterials();
        SetSelectedImmediate(false);
    }

    /// <summary>
    /// 이 고정 슬롯에 아이템 데이터를 연결한다.
    /// </summary>
    public void Bind(
        PotionItemData itemData,
        int amount,
        int itemIndex,
        bool canUse,
        ItemListController owner)
    {
        _itemData = itemData;
        _amount = Mathf.Max(0, amount);
        _itemIndex = itemIndex;
        _canUse = canUse;
        _owner = owner;
        _isBound = itemData != null;

        gameObject.SetActive(_isBound);

        if (!_isBound)
        {
            return;
        }

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

        StopIdleEffect();

        _itemData = null;
        _amount = 0;
        _itemIndex = -1;

        _canUse = false;
        _isSelected = false;
        _isBound = false;

        _owner = null;

        ClearTexts();
        ApplyReveal(0f);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        gameObject.SetActive(false);
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (!_isBound)
        {
            return;
        }

        _owner?.SelectItemByIndex(_itemIndex);
    }

    public void OnPointerClick(
        PointerEventData eventData)
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

        _owner?.SelectItemByIndex(_itemIndex);
        _owner?.SubmitSelected();
    }

    /// <summary>
    /// 선택 상태를 Reveal 애니메이션과 함께 변경한다.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
        {
            return;
        }

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

    /// <summary>
    /// 애니메이션 없이 선택 상태를 즉시 적용한다.
    /// 목록을 새로 바인딩할 때 사용한다.
    /// </summary>
    public void SetSelectedImmediate(bool selected)
    {
        _isSelected = selected;

        _revealTween?.Kill();
        _revealTween = null;

        StopIdleEffect();

        _revealValue =
            selected ? 1f : 0f;

        ApplyReveal(_revealValue);

        if (selected)
        {
            StartIdleEffect();
        }
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
                    value =>
                    {
                        _revealValue = value;
                        ApplyReveal(value);
                    },
                    1f,
                    _revealDuration
                )
                .SetEase(_revealEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _revealTween = null;

                    if (_isSelected &&
                        _isBound)
                    {
                        StartIdleEffect();
                    }
                });
    }

    private void HideReveal()
    {
        _revealTween?.Kill();
        _revealTween = null;

        _revealValue = 0f;
        ApplyReveal(0f);
    }

    private void StartIdleEffect()
    {
        StopIdleEffect();

        if (!_isSelected ||
            !_isBound)
        {
            return;
        }

        _idleSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        //----------------------------------------
        // BackParticle1
        //----------------------------------------

        if (_backParticleImages != null &&
            _backParticleImages.Length > 0 &&
            _backParticleImages[0] != null)
        {
            RectTransform rectTransform =
                _backParticleImages[0]
                    .rectTransform;

            rectTransform.localPosition =
                Vector3.zero;

            rectTransform.localRotation =
                Quaternion.identity;

            _idleSequence.Join(
                rectTransform
                    .DOLocalMoveX(10f, 1.8f)
                    .SetLoops(
                        -1,
                        LoopType.Yoyo
                    )
                    .SetEase(Ease.InOutSine)
            );

            _idleSequence.Join(
                rectTransform
                    .DOLocalRotate(
                        new Vector3(
                            0f,
                            0f,
                            6f
                        ),
                        2.5f
                    )
                    .SetLoops(
                        -1,
                        LoopType.Yoyo
                    )
                    .SetEase(Ease.InOutSine)
            );
        }

        //----------------------------------------
        // BackParticle2
        //----------------------------------------

        if (_backParticleImages != null &&
            _backParticleImages.Length > 1 &&
            _backParticleImages[1] != null)
        {
            RectTransform rectTransform =
                _backParticleImages[1]
                    .rectTransform;

            rectTransform.localPosition =
                Vector3.zero;

            rectTransform.localRotation =
                Quaternion.identity;

            _idleSequence.Join(
                rectTransform
                    .DOLocalMoveX(-8f, 2.3f)
                    .SetLoops(
                        -1,
                        LoopType.Yoyo
                    )
                    .SetEase(Ease.InOutSine)
            );

            _idleSequence.Join(
                rectTransform
                    .DOLocalRotate(
                        new Vector3(
                            0f,
                            0f,
                            -8f
                        ),
                        3f
                    )
                    .SetLoops(
                        -1,
                        LoopType.Yoyo
                    )
                    .SetEase(Ease.InOutSine)
            );
        }

        //----------------------------------------
        // FrontParticle
        //----------------------------------------

        if (_frontParticleImage != null)
        {
            RectTransform rectTransform =
                _frontParticleImage.rectTransform;

            rectTransform.localScale =
                Vector3.one;

            _idleSequence.Join(
                rectTransform
                    .DOScale(1.05f, 0.8f)
                    .SetLoops(
                        -1,
                        LoopType.Yoyo
                    )
                    .SetEase(Ease.InOutSine)
            );
        }
    }

    private void StopIdleEffect()
    {
        if (_idleSequence != null)
        {
            _idleSequence.Kill();
            _idleSequence = null;
        }

        ResetParticleTransforms();
    }

    private void ResetParticleTransforms()
    {
        if (_backParticleImages != null)
        {
            foreach (Image image
                     in _backParticleImages)
            {
                if (image == null)
                {
                    continue;
                }

                RectTransform rectTransform =
                    image.rectTransform;

                rectTransform.localPosition =
                    Vector3.zero;

                rectTransform.localRotation =
                    Quaternion.identity;

                rectTransform.localScale =
                    Vector3.one;
            }
        }

        if (_frontParticleImage != null)
        {
            RectTransform rectTransform =
                _frontParticleImage.rectTransform;

            rectTransform.localPosition =
                Vector3.zero;

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;
        }
    }

    private void RefreshTexts()
    {
        if (_itemData == null)
        {
            return;
        }

        //----------------------------------------
        // 아이템 이름
        //----------------------------------------

        if (_itemNameText != null)
        {
            _itemNameText.text =
                _itemData.itemName;
        }

        //----------------------------------------
        // 아이템 설명
        //----------------------------------------

        if (_descriptionText != null)
        {
            /*
             * ItemData의 실제 설명 필드명에 맞춰서
             * 이 부분만 변경하면 된다.
             *
             * 예:
             * _itemData.description
             * _itemData.itemDescription
             * _itemData.Description
             */
            _descriptionText.text =
                _itemData.description;
        }

        //----------------------------------------
        // 보유 수량
        //----------------------------------------

        if (_amountText != null)
        {
            _amountText.text =
                $"보유 : {_amount}";
        }
    }

    private void ClearTexts()
    {
        if (_itemNameText != null)
        {
            _itemNameText.text =
                string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text =
                string.Empty;
        }

        if (_amountText != null)
        {
            _amountText.text =
                string.Empty;
        }
    }

    private void RefreshUsableState()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha =
                _canUse
                    ? 1f
                    : _unavailableAlpha;
        }

        /*
         * 사용 불가능한 아이템도 Hover 선택은 가능하게 유지한다.
         * 실제 사용은 ItemListController.SubmitSelected()에서 차단한다.
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
                new Material[
                    _backParticleImages.Length
                ];

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
            CreateRuntimeMaterial(
                _backFrameImage
            );

        _frontParticleMaterial =
            CreateRuntimeMaterial(
                _frontParticleImage
            );
    }

    private Material CreateRuntimeMaterial(
        Image image)
    {
        if (image == null)
        {
            return null;
        }

        image.raycastTarget = false;

        if (image.material == null)
        {
            Debug.LogWarning(
                $"[{nameof(BattleItemListEntry)}] " +
                $"{name}/{image.name}에 " +
                "Material이 없습니다.",
                image
            );

            return null;
        }

        Material runtimeMaterial =
            new Material(image.material);

        runtimeMaterial.name =
            $"{image.material.name}_" +
            $"{name}_Runtime";

        image.material = runtimeMaterial;

        if (!runtimeMaterial.HasProperty(
                RevealId))
        {
            Debug.LogWarning(
                $"[{nameof(BattleItemListEntry)}] " +
                $"{runtimeMaterial.name}에 " +
                "_Reveal 프로퍼티가 없습니다.",
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

        SetMaterialReveal(
            _backFrameMaterial,
            value
        );

        SetMaterialReveal(
            _frontParticleMaterial,
            value
        );
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

        material.SetFloat(
            RevealId,
            value
        );
    }

    private void OnDestroy()
    {
        _revealTween?.Kill();
        _revealTween = null;

        StopIdleEffect();

        DestroyMaterials(
            _backParticleMaterials
        );

        DestroyMaterial(
            _backFrameMaterial
        );

        DestroyMaterial(
            _frontParticleMaterial
        );
    }

    private static void DestroyMaterials(
        Material[] materials)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0;
             i < materials.Length;
             i++)
        {
            DestroyMaterial(
                materials[i]
            );
        }
    }

    private static void DestroyMaterial(
        Material material)
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}