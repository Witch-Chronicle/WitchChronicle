using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 전투 중 아이템(포션) 사용 실행부
    /// PotionItemData를 받아 회복·상태이상 해제·전체 상태이상 해제 효과를 적용하고 인벤토리에서 차감
    /// </summary>
    public class BattleItemExecutor
    {
        private readonly StatusEffectController _statusEffectController;

        public BattleItemExecutor(StatusEffectController statusEffectController)
        {
            _statusEffectController = statusEffectController;
        }

        public BattleItemResult UsePotion(BattleUnit user, PotionItemData potionData)
        {
            BattleItemResult result = new BattleItemResult();

            if (user == null || potionData == null)
            {
                Debug.LogWarning("[BattleItemExecutor] 인자가 null입니다");
                result.Success = false;
                return result;
            }

            if (user.IsAlive == false)
            {
                Debug.LogWarning($"[BattleItemExecutor] {user.UnitName}은 사망 상태입니다");
                result.Success = false;
                return result;
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[BattleItemExecutor] PlayerInventory.Instance가 null입니다");
                result.Success = false;
                return result;
            }

            bool consumed = PlayerInventory.Instance.TryConsumeItem(potionData, 1);

            if (consumed == false)
            {
                Debug.Log($"[BattleItemExecutor] {potionData.itemName} 보유 수량 부족");
                result.Success = false;
                return result;
            }

            switch (potionData.PotionEffect)
            {
                case PotionEffect.HealHp:
                    ApplyHealHp(user, potionData, ref result);
                    break;

                case PotionEffect.HealMp:
                    ApplyHealMp(user, potionData, ref result);
                    break;

                case PotionEffect.CureStatusEffect:
                    ApplyCureStatusEffect(user, potionData, ref result);
                    break;

                case PotionEffect.CureAllStatusEffects:      // ⭐ 신규
                    ApplyCureAllStatusEffects(user, potionData, ref result);
                    break;

                default:
                    Debug.LogWarning($"[BattleItemExecutor] 알 수 없는 PotionEffect: {potionData.PotionEffect}");
                    break;
            }

            PlayerInventory.Instance.RaiseInventoryChanged();

            result.Success = true;
            result.UsedItem = potionData;
            return result;
        }

        private void ApplyHealHp(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            int healAmount = Mathf.RoundToInt(user.MaxHp * potionData.HealRatio);
            healAmount = Mathf.Max(1, healAmount);

            user.Heal(healAmount);
            result.HealHpAmount = healAmount;

            Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용 (HP +{healAmount})");
        }

        private void ApplyHealMp(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            if (user.UsesMp == false)
            {
                Debug.Log($"[Item] {user.UnitName}은 MP를 사용하지 않아 마나 포션 효과 없음");
                return;
            }

            int healAmount = Mathf.RoundToInt(user.MaxMp * potionData.HealRatio);
            healAmount = Mathf.Max(1, healAmount);

            user.RestoreMp(healAmount);
            result.HealMpAmount = healAmount;

            Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용 (MP +{healAmount})");
        }

        private void ApplyCureStatusEffect(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            if (potionData.CureStatusEffectType == StatusEffectType.None)
            {
                Debug.LogWarning($"[BattleItemExecutor] {potionData.itemName}의 해제 대상이 None입니다");
                return;
            }

            if (_statusEffectController == null)
            {
                Debug.LogWarning("[BattleItemExecutor] StatusEffectController가 null입니다");
                return;
            }

            bool removed = _statusEffectController.RemoveStatusEffect(user, potionData.CureStatusEffectType);
            result.CuredStatusEffect = potionData.CureStatusEffectType;
            result.CureSuccess = removed;

            if (removed)
            {
                Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용, {potionData.CureStatusEffectType} 해제");
            }
            else
            {
                Debug.Log($"[Item] {user.UnitName}은 {potionData.CureStatusEffectType} 상태가 아님 (포션은 소모됨)");
            }
        }

        /// <summary>
        /// 만능 치료제 실행 - 대상의 모든 상태이상 해제
        /// </summary>
        private void ApplyCureAllStatusEffects(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            if (_statusEffectController == null)
            {
                Debug.LogWarning("[BattleItemExecutor] StatusEffectController가 null입니다");
                return;
            }

            _statusEffectController.RemoveAllStatusEffects(user);
            result.CureAllSuccess = true;

            Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용, 모든 상태이상 해제");
        }
    }

    public struct BattleItemResult
    {
        public bool Success;
        public ItemData UsedItem;
        public int HealHpAmount;
        public int HealMpAmount;
        public StatusEffectType CuredStatusEffect;
        public bool CureSuccess;
        public bool CureAllSuccess;         // ⭐ 신규
    }
}