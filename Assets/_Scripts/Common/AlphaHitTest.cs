using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlphaHitTest : MonoBehaviour
{
    [Range(0.01f, 1f)]
    [SerializeField] private float alphaThreshold = 0.1f;

    private void Awake()
    {
        ApplyThreshold();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyThreshold();
    }
#endif

    private void ApplyThreshold()
    {
        Image image = GetComponent<Image>();

        if (image != null)
        {
            image.alphaHitTestMinimumThreshold = alphaThreshold;
        }
    }
}