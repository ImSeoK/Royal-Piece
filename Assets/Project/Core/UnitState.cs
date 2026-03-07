using UnityEngine;

namespace Chess.Core
{
    public class UnitState
    {
        public int id;                          // 고유 ID
        public int currentHP;                   // 현재 체력
        public Vector2Int position;             // 보드 위치 (0~7, 0~7)
        public int ownerID;                     // 플레이어 ID (0 or 1)
        public UnitDefinition definition;       // 유닛 정의

        public bool IsAlive => currentHP > 0;

        // 생성자
        public UnitState(int id, UnitDefinition def, Vector2Int pos, int owner)
        {
            this.id = id;
            this.definition = def;
            this.currentHP = def.maxHP;
            this.position = pos;
            this.ownerID = owner;
        }

        // 데미지 받기
        public void TakeDamage(int damage)
        {
            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;
        }
    }
}