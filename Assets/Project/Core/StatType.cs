using UnityEngine;

namespace Chess.Core
{
    public enum StatType
    {
        HP,
        ATK,
        SPD,
        DEF,
        // 받는 데미지 증감 (양수 = 취약, 음수 = 저항)
        DamageReceived,
        // 이동 봉쇄 — ActiveBuff로 관리, amount=0, remainingTurns=지속턴
        MovementLock,
    }
}