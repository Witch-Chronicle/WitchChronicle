using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 속성 상성 판정 결과
    /// </summary>
    public enum ElementAffinity
    {
        Normal,     // 일반 (배율 1.0)
        Weak,       // 약점 (배율 증가)
        Resist,     // 저항 (배율 감소)
        Null        // 무효 (배율 0)
    }

    /// <summary>
    /// 속성 상성 판정 결과 데이터
    /// </summary>
    public struct ElementResolveResult
    {
        public ElementAffinity Affinity;
        public float Multiplier;

        public ElementResolveResult(ElementAffinity affinity, float multiplier)
        {
            Affinity = affinity;
            Multiplier = multiplier;
        }
    }

    /// <summary>
    /// 대상의 속성 상성을 판정하여 데미지 배율을 반환
    /// 우선순위: Null > Weak > Resist > Normal
    /// </summary>
    public static class ElementResolver
    {
        // 상성 배율 상수
        private const float NormalMultiplier = 1.0f;
        private const float WeakMultiplier = 1.5f;
        private const float ResistMultiplier = 0.5f;
        private const float NullMultiplier = 0.0f;

        /// <summary>
        /// 대상에 대한 속성 상성 결과 반환
        /// </summary>
        /// <param name="target">공격 대상</param>
        /// <param name="elementType">공격 속성</param>
        /// <returns>상성 결과 (Affinity + Multiplier)</returns>
        public static ElementResolveResult Resolve(BattleUnit target, ElementType elementType)
        {
            if (target == null)
            {
                Debug.LogWarning("[ElementResolver] target이 null입니다");
                return new ElementResolveResult(ElementAffinity.Normal, NormalMultiplier);
            }

            // 무속성은 상성 무시
            if (elementType == ElementType.None)
            {
                return new ElementResolveResult(ElementAffinity.Normal, NormalMultiplier);
            }

            // 우선순위: 무효 > 약점 > 저항 > 일반
            if (target.IsNullTo(elementType))
            {
                return new ElementResolveResult(ElementAffinity.Null, NullMultiplier);
            }

            if (target.IsWeakTo(elementType))
            {
                return new ElementResolveResult(ElementAffinity.Weak, WeakMultiplier);
            }

            if (target.IsResistTo(elementType))
            {
                return new ElementResolveResult(ElementAffinity.Resist, ResistMultiplier);
            }

            return new ElementResolveResult(ElementAffinity.Normal, NormalMultiplier);
        }

        /// <summary>
        /// 배율만 필요할 때 사용하는 간편 메서드
        /// </summary>
        /// <param name="target">공격 대상</param>
        /// <param name="elementType">공격 속성</param>
        /// <returns>속성 배율</returns>
        public static float GetMultiplier(BattleUnit target, ElementType elementType)
        {
            return Resolve(target, elementType).Multiplier;
        }
    }
}
