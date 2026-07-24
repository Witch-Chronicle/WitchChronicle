using UnityEngine;

/// NPC·오브젝트가 구현하는 상호작용 인터페이스.
/// 상호작용 가능한 대상은 이 인터페이스만 구현하면 됨.
public interface ITFInteractable
{
    string Prompt { get; }              // UI 프롬프트 텍스트 ("대화하기" 등)
    void Interact(GameObject interactor);
}
