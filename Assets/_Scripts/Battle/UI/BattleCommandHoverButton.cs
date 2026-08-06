using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleCommandHoverButton : MonoBehaviour, IPointerEnterHandler
{
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");

    [Header("Hovered UI")]
    [SerializeField] private GameObject hoveredObject;
    [SerializeField] private Image hoveredImage;

    [Header("Reveal Animation")]
    [SerializeField, Min(0.01f)]
    private float revealDuration = 0.35f;

    [SerializeField]
    private Ease revealEase = Ease.OutCubic;

    private Material runtimeMaterial;
    private Tween revealTween;

    private BattleCommandUIController _commandUI;

    private void Awake()
    {
        _commandUI = GetComponentInParent<BattleCommandUIController>();

        InitializeMaterial();
        SetHoveredImmediate(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _commandUI?.SelectByHoverButton(this);
    }

    private void InitializeMaterial()
    {
        if (hoveredImage == null && hoveredObject != null)
        {
            hoveredImage = hoveredObject.GetComponent<Image>();
        }

        if (hoveredImage == null)
        {
            Debug.LogError(
                $"{name}: Hovered Image가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (hoveredImage.material == null)
        {
            Debug.LogError(
                $"{name}: Hovered Image에 Reveal Material이 없습니다.",
                this
            );
            return;
        }

        // 공유 Material을 직접 변경하지 않도록 버튼 전용 Material 생성
        runtimeMaterial = new Material(hoveredImage.material);
        runtimeMaterial.name = $"{hoveredImage.material.name}_{name}_Instance";

        hoveredImage.material = runtimeMaterial;
    }

    public void SetHovered(bool isHovered)
    {
        if (isHovered)
        {
            PlayReveal();
        }
        else
        {
            HideImmediate();
        }
    }

    private void PlayReveal()
    {
        if (hoveredObject != null)
        {
            hoveredObject.SetActive(true);
        }

        if (runtimeMaterial == null)
            return;

        revealTween?.Kill();

        // 새로운 버튼에 들어올 때마다 왼쪽부터 다시 재생
        runtimeMaterial.SetFloat(RevealId, 0f);

        revealTween = DOTween.To(
                () => runtimeMaterial.GetFloat(RevealId),
                value => runtimeMaterial.SetFloat(RevealId, value),
                1f,
                revealDuration
            )
            .SetEase(revealEase)
            .SetUpdate(true);
    }

    private void HideImmediate()
    {
        revealTween?.Kill();
        revealTween = null;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(RevealId, 0f);
        }

        if (hoveredObject != null)
        {
            hoveredObject.SetActive(false);
        }
    }

    private void SetHoveredImmediate(bool isHovered)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(RevealId, isHovered ? 1f : 0f);
        }

        if (hoveredObject != null)
        {
            hoveredObject.SetActive(isHovered);
        }
    }

    private void OnDisable()
    {
        revealTween?.Kill();
        revealTween = null;

        if (BattleCommandHoverManager.HasInstance)
        {
            BattleCommandHoverManager.Instance.ClearIfCurrent(this);
        }
    }

    private void OnDestroy()
    {
        revealTween?.Kill();

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}