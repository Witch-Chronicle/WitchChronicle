using UnityEngine;

/// <summary>
/// 제작, 강화, 요리 등에 사용되는 재료 아이템.
/// 방치형으로 얻는 재료도 포함.
/// 예: 약초, 생선, 광물, 강화석, 몬스터 드롭 아이템 등
/// </summary>
[CreateAssetMenu(fileName = "NewMaterialItem", menuName = "Witch Chronicle/Item/MaterialItemData")]
public class MaterialItemData : ItemData
{
    [Header("재료 아이템 데이터")]
    public MaterialType materialType; // 재료 종류 (필요 없으면 삭제 가능)
}