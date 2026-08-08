using UnityEngine;

/// <summary>
/// 마을의 빠른 이동 포탈.
///
/// TeleportPortal 모델 오브젝트에 붙이고, 콜라이더를 함께 둔다.
/// (PlayerInteractor가 콜라이더와 같은 오브젝트에서 스크립트를 찾으므로
///  콜라이더는 반드시 이 컴포넌트와 같은 오브젝트에 있어야 한다.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour, ITFInteractable
{
    [Header("빠른 이동 UI")]
    [SerializeField] private TeleportPanel _teleportPanel;

    // 안내 문구는 쓰지 않는다. 인터페이스 요구사항이라 빈 문자열만 반환.
    public string Prompt => string.Empty;

    public void Interact(GameObject interactor)
    {
        if (_teleportPanel == null)
        {
            Debug.LogWarning($"[{name}] TeleportPanel이 연결되지 않았습니다.");
            return;
        }

        _teleportPanel.Open();

    }
}
