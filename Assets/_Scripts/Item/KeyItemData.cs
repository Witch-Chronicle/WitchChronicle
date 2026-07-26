using UnityEngine;

/// <summary>
/// 스토리 및 퀘스트 전용 아이템.
/// 예: 열쇠, 퀘스트 아이템
/// </summary>
[CreateAssetMenu(fileName = "NewKeyItem", menuName = "Witch Chronicle/Item/KeyItemData")]
public class KeyItemData : ItemData
{
    // 퀘스트 진행 조건, 사용 가능 위치 등 추가 데이터가 필요하면 여기에 작성
    // 예: public string relatedQuestId;
}