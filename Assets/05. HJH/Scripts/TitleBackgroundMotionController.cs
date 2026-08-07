using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 타이틀 화면의 구름 무한 스크롤과 마우스 패럴랙스를 함께 제어합니다.
/// Time.timeScale이 0이어도 움직이도록 unscaledDeltaTime을 사용합니다.
/// </summary>
public sealed class TitleBackgroundMotionController : MonoBehaviour
{
    [Serializable]
    private sealed class CloudLayer
    {
        [Tooltip("패럴랙스로 움직일 구름 부모 RectTransform")]
        public RectTransform root;

        [Tooltip("같은 구름 Sprite를 사용하는 첫 번째 Image")]
        public RectTransform first;

        [Tooltip("첫 번째 Image의 복제본")]
        public RectTransform second;

        [Min(0f)]
        [Tooltip("초당 왼쪽으로 이동할 UI 픽셀")]
        public float speed = 6f;

        [Tooltip("두 이미지가 만나는 간격. 경계가 보이면 -50~-150을 권장")]
        public float gap = -80f;

        [Tooltip("마우스가 화면 끝에 있을 때 부모가 움직일 최대 거리")]
        public Vector2 parallax = new Vector2(6f, 3f);

        [NonSerialized] public Vector2 BaseRootPosition;
        [NonSerialized] public float Width;
    }

    [Header("구름 레이어")]
    [SerializeField] private CloudLayer _farClouds = new CloudLayer();
    [SerializeField] private CloudLayer _nearClouds = new CloudLayer();

    [Header("추가 패럴랙스 레이어")]
    [SerializeField] private RectTransform _backgroundLayer;
    [SerializeField] private Vector2 _backgroundParallax = new Vector2(2f, 1f);

    [SerializeField] private RectTransform _characterLayer;
    [SerializeField] private Vector2 _characterParallax = new Vector2(14f, 6f);

    [Header("움직임")]
    [Min(0.01f)]
    [SerializeField] private float _parallaxFollowSpeed = 3.5f;

    [Tooltip("캐릭터가 아주 약하게 숨 쉬듯 위아래로 움직이는 거리")]
    [Min(0f)]
    [SerializeField] private float _characterBreathAmount = 2f;

    [Min(0.01f)]
    [SerializeField] private float _characterBreathSpeed = 0.75f;

    private Vector2 _backgroundBasePosition;
    private Vector2 _characterBasePosition;

    private void Awake()
    {
        CacheCloudLayer(_farClouds);
        CacheCloudLayer(_nearClouds);

        if (_backgroundLayer != null)
        {
            _backgroundBasePosition = _backgroundLayer.anchoredPosition;
        }

        if (_characterLayer != null)
        {
            _characterBasePosition = _characterLayer.anchoredPosition;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        ScrollCloudLayer(_farClouds, deltaTime);
        ScrollCloudLayer(_nearClouds, deltaTime);

        Vector2 pointer = GetNormalizedPointerPosition();
        float follow = 1f - Mathf.Exp(-_parallaxFollowSpeed * deltaTime);

        MoveLayer(_backgroundLayer, _backgroundBasePosition,
            Vector2.Scale(pointer, _backgroundParallax), follow);

        MoveCloudRoot(_farClouds, pointer, follow);
        MoveCloudRoot(_nearClouds, pointer, follow);

        if (_characterLayer != null)
        {
            float breath = Mathf.Sin(Time.unscaledTime * _characterBreathSpeed * Mathf.PI * 2f)
                * _characterBreathAmount;

            Vector2 target = _characterBasePosition
                + Vector2.Scale(pointer, _characterParallax)
                + Vector2.up * breath;

            _characterLayer.anchoredPosition = Vector2.Lerp(
                _characterLayer.anchoredPosition,
                target,
                follow);
        }
    }

    private static void CacheCloudLayer(CloudLayer layer)
    {
        if (layer == null || layer.first == null || layer.second == null)
        {
            return;
        }

        if (layer.root != null)
        {
            layer.BaseRootPosition = layer.root.anchoredPosition;
        }

        layer.Width = Mathf.Max(1f, layer.first.rect.width);

        Vector2 firstPosition = layer.first.anchoredPosition;
        firstPosition.x = 0f;
        layer.first.anchoredPosition = firstPosition;

        Vector2 secondPosition = layer.second.anchoredPosition;
        secondPosition.x = layer.Width + layer.gap;
        layer.second.anchoredPosition = secondPosition;
    }

    private static void ScrollCloudLayer(CloudLayer layer, float deltaTime)
    {
        if (layer == null || layer.first == null || layer.second == null)
        {
            return;
        }

        float distance = layer.speed * deltaTime;
        MoveLeft(layer.first, distance);
        MoveLeft(layer.second, distance);

        RecycleIfOffscreen(layer.first, layer.second, layer.Width, layer.gap);
        RecycleIfOffscreen(layer.second, layer.first, layer.Width, layer.gap);
    }

    private static void MoveLeft(RectTransform target, float distance)
    {
        Vector2 position = target.anchoredPosition;
        position.x -= distance;
        target.anchoredPosition = position;
    }

    private static void RecycleIfOffscreen(
        RectTransform target,
        RectTransform other,
        float width,
        float gap)
    {
        Vector2 position = target.anchoredPosition;

        if (position.x + width >= 0f)
        {
            return;
        }

        position.x = other.anchoredPosition.x + width + gap;
        target.anchoredPosition = position;
    }

    private void MoveCloudRoot(CloudLayer layer, Vector2 pointer, float follow)
    {
        if (layer == null || layer.root == null)
        {
            return;
        }

        Vector2 target = layer.BaseRootPosition + Vector2.Scale(pointer, layer.parallax);
        layer.root.anchoredPosition = Vector2.Lerp(layer.root.anchoredPosition, target, follow);
    }

    private static void MoveLayer(
        RectTransform layer,
        Vector2 basePosition,
        Vector2 offset,
        float follow)
    {
        if (layer == null)
        {
            return;
        }

        layer.anchoredPosition = Vector2.Lerp(
            layer.anchoredPosition,
            basePosition + offset,
            follow);
    }

    private static Vector2 GetNormalizedPointerPosition()
    {
        Vector2 pointerPosition;

#if ENABLE_INPUT_SYSTEM
        pointerPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
        pointerPosition = Input.mousePosition;
#endif

        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);

        return new Vector2(
            Mathf.Clamp(pointerPosition.x / width * 2f - 1f, -1f, 1f),
            Mathf.Clamp(pointerPosition.y / height * 2f - 1f, -1f, 1f));
    }
}
