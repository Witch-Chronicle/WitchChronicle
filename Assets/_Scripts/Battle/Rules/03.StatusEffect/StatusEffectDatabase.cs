using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 6종 상태이상 데이터를 통합 관리하는 데이터베이스
    /// SkillEffectExecutor, BattleItemExecutor 등에서 StatusEffectType으로 SO 조회
    /// </summary>
    [CreateAssetMenu(menuName = "WitchChronicle/StatusEffectDatabase")]
    public class StatusEffectDatabase : ScriptableObject
    {
        [Header("Status Effect Data List")]
        [SerializeField] private StatusEffectData[] _statusEffectDataList;

        // 런타임 조회용 딕셔너리 (StatusEffectType → StatusEffectData)
        private Dictionary<StatusEffectType, StatusEffectData> _dataMap;

        /// <summary>
        /// 등록된 상태이상 SO 목록
        /// </summary>
        public IReadOnlyList<StatusEffectData> StatusEffectDataList => _statusEffectDataList;

        /// <summary>
        /// StatusEffectType으로 해당 SO 조회
        /// </summary>
        /// <param name="type">조회할 상태이상 종류</param>
        /// <returns>SO 또는 null</returns>
        public StatusEffectData GetData(StatusEffectType type)
        {
            EnsureMapInitialized();

            if (_dataMap.TryGetValue(type, out StatusEffectData data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// 딕셔너리 초기화 (지연 초기화)
        /// </summary>
        private void EnsureMapInitialized()
        {
            if (_dataMap != null)
            {
                return;
            }

            _dataMap = new Dictionary<StatusEffectType, StatusEffectData>();

            if (_statusEffectDataList == null)
            {
                return;
            }

            for (int i = 0; i < _statusEffectDataList.Length; i++)
            {
                StatusEffectData data = _statusEffectDataList[i];

                if (data == null)
                {
                    continue;
                }

                if (_dataMap.ContainsKey(data.StatusEffectType))
                {
                    Debug.LogWarning($"[StatusEffectDatabase] 중복 상태이상: {data.StatusEffectType}");
                    continue;
                }

                _dataMap[data.StatusEffectType] = data;
            }
        }

        /// <summary>
        /// 인스펙터 값 변경 시 딕셔너리 초기화
        /// </summary>
        private void OnValidate()
        {
            _dataMap = null;
        }
    }
}