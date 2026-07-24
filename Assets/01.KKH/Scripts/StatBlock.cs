using System;
using UnityEngine;

// 스탯 값을 하나의 묶음으로 처리하는 데이터 박스
[Serializable]
public class StatBlock
{
    // 인스펙터에서 스탯값 설정
    public int maxHP;
    public int maxMP;
    public int magicPower;
    public int intelligence;
    public int defense;
    public int speed;
    public int luck;

    // 현재 스탯값 가져오기
    public int Get(StatType type)
    {
        return type switch
        {
            StatType.MaxHP => maxHP,
            StatType.MaxMP => maxMP,
            StatType.SpellPower => magicPower,
            StatType.Intelligence => intelligence,
            StatType.Defense => defense,
            StatType.Speed => speed,
            StatType.Luck => luck,
            _ => 0
        };
    }

    // 스탯에 값 추가
    public void Add(StatType type, int value)
    {
        switch (type)
        {
            case StatType.MaxHP:
                maxHP += value;
                break;
            case StatType.MaxMP:
                maxMP += value;
                break;
            case StatType.SpellPower:
                magicPower += value;
                break;
            case StatType.Intelligence:
                intelligence += value;
                break;
            case StatType.Defense:
                defense += value;
                break;
            case StatType.Speed:
                speed += value;
                break;
            case StatType.Luck:
                luck += value;
                break;
        }
    }

    // 스탯 복사 (초기화)
    public StatBlock Clone()
    {
        return new StatBlock
        {
            maxHP = maxHP,
            maxMP = maxMP,
            magicPower = magicPower,
            intelligence = intelligence,
            defense = defense,
            speed = speed,
            luck = luck
        };
    }
}