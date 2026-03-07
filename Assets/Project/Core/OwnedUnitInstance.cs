using System;

namespace Chess.Core
{
    [Serializable]
    public class OwnedUnitInstance
    {
        public string instanceId;   // 개별 고유 ID
        public string unitName;     // UnitDefinition 참조용
        public int enhanceLevel;    // 0~10

        public OwnedUnitInstance(string unitName)
        {
            this.instanceId = Guid.NewGuid().ToString();
            this.unitName = unitName;
            this.enhanceLevel = 0;
        }
    }
}