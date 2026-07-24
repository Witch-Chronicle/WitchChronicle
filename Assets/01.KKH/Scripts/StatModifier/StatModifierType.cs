/// <summary>
/// 스탯 보정값이 스탯에 적용되는 방식
/// </summary>
public enum StatModifierType
{
    Flat,               // 단순 더하기
    PercentAdd,         // 스탯 + 퍼센트 (+20% 같은 식)
    PercentMultiply     // 최종 스탯 x 비율 (x2배 같은 식)
}