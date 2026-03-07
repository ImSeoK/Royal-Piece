using System;
using System.Collections.Generic;

namespace Chess.Core
{
    [Serializable]
    public class SlotEntry
    {
        public int slotIndex;
        public string unitName;
    }

    [Serializable]
    public class SavedDeckData
    {
        public List<SlotEntry> slots = new List<SlotEntry>();
        public List<string> customUnitNames = new List<string>();
        public string kingName;
        public string pawnName;

        // 선택된 액티브 스킬 (최대 2개, 스킬 이름으로 저장)
        public List<string> selectedSkillNames = new List<string>();
    }
}