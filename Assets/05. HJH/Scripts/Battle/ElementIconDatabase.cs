using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// ElementType별 아이콘 스프라이트를 통합 관리하는 데이터베이스.
    /// BattleUIContext가 참조해서 스킬 UI(BattleSkillListEntry 등)에 아이콘을 제공.
    /// </summary>
    [CreateAssetMenu(menuName = "WitchChronicle/ElementIconDatabase")]
    public class ElementIconDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct ElementIconEntry
        {
            public ElementType ElementType;
            public Sprite Icon;
        }

        [Header("Element Icon List")]
        [SerializeField] private ElementIconEntry[] _entries;

        private Dictionary<ElementType, Sprite> _iconMap;

        /// <summary>
        /// ElementType에 해당하는 아이콘 조회. 등록 안 되어 있으면 null.
        /// </summary>
        public Sprite GetIcon(ElementType type)
        {
            EnsureMapInitialized();

            if (_iconMap.TryGetValue(type, out Sprite icon))
            {
                return icon;
            }

            return null;
        }

        private void EnsureMapInitialized()
        {
            if (_iconMap != null)
            {
                return;
            }

            _iconMap = new Dictionary<ElementType, Sprite>();

            if (_entries == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                ElementIconEntry entry = _entries[i];

                if (_iconMap.ContainsKey(entry.ElementType))
                {
                    Debug.LogWarning($"[ElementIconDatabase] 중복 ElementType: {entry.ElementType}");
                    continue;
                }

                _iconMap[entry.ElementType] = entry.Icon;
            }
        }

        private void OnValidate()
        {
            _iconMap = null;
        }
    }
}