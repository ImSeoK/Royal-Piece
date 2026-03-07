using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Chess.Core
{
    [CreateAssetMenu(fileName = "PlayerDeck", menuName = "Chess/PlayerDeck")]
    public class PlayerDeck : ScriptableObject
    {
        [Header("덱 구성")]
        [Tooltip("커스텀 유닛 7개 (King 제외)")]
        public List<UnitDefinition> customUnits = new List<UnitDefinition>();

        [Header("킹")]
        [Tooltip("필수: King 유닛")]
        public UnitDefinition king;

        [Header("폰")]
        [Tooltip("고정: 2번째 줄에 배치될 Pawn")]
        public UnitDefinition pawn;

        [Header("덱 규칙")]
        public int maxCustomUnits = 7;  // ← 7개로 수정!
        public int fixedPawns = 8;       // ← 고정 8개

        /// <summary>
        /// 덱 유효성 검사
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            // King 체크
            if (king == null)
            {
                errorMessage = "King이 설정되지 않았습니다.";
                return false;
            }

            if (!king.isKing)
            {
                errorMessage = "King 슬롯에 King이 아닌 유닛이 설정되었습니다.";
                return false;
            }

            // Pawn 체크
            if (pawn == null)
            {
                errorMessage = "Pawn이 설정되지 않았습니다.";
                return false;
            }

            // 커스텀 유닛 개수 체크
            if (customUnits.Count != maxCustomUnits)
            {
                errorMessage = $"커스텀 유닛 개수가 {maxCustomUnits}개가 아닙니다. (현재: {customUnits.Count}개)";
                return false;
            }

            // null 체크
            if (customUnits.Any(u => u == null))
            {
                errorMessage = "덱에 null 유닛이 포함되어 있습니다.";
                return false;
            }

            errorMessage = "";
            return true;
        }

        /// <summary>
        /// 덱 정보 출력
        /// </summary>
        public string GetDeckInfo()
        {
            return $"Deck: King + 커스텀 {customUnits.Count}개 + Pawn {fixedPawns}개";
        }
    }
}