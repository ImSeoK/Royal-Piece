using UnityEngine;
using Chess.Core;

namespace Chess.Presentation
{
    [CreateAssetMenu(fileName = "BuffIconConfig", menuName = "Chess/Buff Icon Config")]
    public class BuffIconConfig : ScriptableObject
    {
        [Header("스탯 아이콘")]
        public Sprite atkIcon;
        public Sprite defIcon;
        public Sprite spdIcon;
        public Sprite damageReceivedPlusIcon;   // +r (피해 증가 / 빨강)
        public Sprite damageReceivedMinusIcon;  // -r (피해 감소 / 초록)
        public Sprite hpIcon;

        [Header("상태이상 아이콘")]
        public Sprite movementLockIcon;
        public Sprite dotIcon;

        public Sprite GetIcon(StatType type)
        {
            switch (type)
            {
                case StatType.ATK:            return atkIcon;
                case StatType.DEF:            return defIcon;
                case StatType.SPD:            return spdIcon;
                case StatType.DamageReceived: return damageReceivedPlusIcon;
                case StatType.HP:             return hpIcon;
                case StatType.MovementLock:   return movementLockIcon;
                default:                      return null;
            }
        }

        public Sprite GetDamageReceivedIcon(bool isPositive)
            => isPositive ? damageReceivedPlusIcon : damageReceivedMinusIcon;
    }
}
