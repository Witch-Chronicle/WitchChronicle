using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 전투 승리 시 보상 계산
    /// 처치한 적들의 경험치·골드·드롭 아이템을 집계하여 결과 데이터 생성
    /// 실제 성장 반영은 외부 시스템(캐릭터/파티) 담당
    /// </summary>
    public static class BattleRewardCalculator
    {
        /// <summary>
        /// 처치한 적 목록으로부터 보상 계산
        /// </summary>
        /// <param name="defeatedEnemies">처치한 적의 EnemyBattleData 목록</param>
        /// <returns>보상 결과</returns>
        public static BattleRewardResult Calculate(IReadOnlyList<EnemyBattleData> defeatedEnemies)
        {
            BattleRewardResult result = new BattleRewardResult();
            result.DefeatedEnemies = defeatedEnemies;

            if (defeatedEnemies == null || defeatedEnemies.Count == 0)
            {
                Debug.LogWarning("[BattleRewardCalculator] 처치한 적이 없습니다");
                return result;
            }

            int totalExp = 0;
            int totalGold = 0;

            for (int i = 0; i < defeatedEnemies.Count; i++)
            {
                EnemyBattleData enemy = defeatedEnemies[i];

                if (enemy == null)
                {
                    continue;
                }

                totalExp += enemy.ExpReward;
                totalGold += enemy.GoldReward;
            }

            result.TotalExp = totalExp;
            result.TotalGold = totalGold;
            result.DroppedItems = null;         // TODO: EnemyBattleData에 아이템 드롭 리스트 추가 시 구현

            Debug.Log($"[Reward] 처치 {defeatedEnemies.Count}마리 / EXP {totalExp} / Gold {totalGold}");

            return result;
        }

        /// <summary>
        /// 도망/패배 등 승리하지 않은 종료 시 빈 보상 결과 생성
        /// </summary>
        public static BattleRewardResult CreateEmpty()
        {
            return new BattleRewardResult
            {
                TotalExp = 0,
                TotalGold = 0,
                DefeatedEnemies = null,
                DroppedItems = null
            };
        }
    }

    /// <summary>
    /// 전투 보상 결과 데이터
    /// UI(결과 화면), 캐릭터 성장 시스템이 이 결과를 받아 처리
    /// </summary>
    public struct BattleRewardResult
    {
        public int TotalExp;
        public int TotalGold;
        public IReadOnlyList<EnemyBattleData> DefeatedEnemies;
        public IReadOnlyList<ItemData> DroppedItems;
    }
}
