using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Chess.Core;

namespace Chess.Presentation
{
    [Serializable]
    public class DeckNetworkData
    {
        public string kingName;
        public string pawnName;
        public int    kingEnhanceLevel;
        public int    pawnEnhanceLevel;
        public List<string> customUnitNames        = new List<string>();
        public List<int>    customUnitEnhanceLevels = new List<int>();
        public List<string> activeSkillNames        = new List<string>();
        public string firebaseUID;
    }

    /// <summary>
    /// Photon Fusion NetworkBehaviour — 덱 교환 + 이동 RPC (Phase 4).
    /// SampleScene의 NetworkObject 오브젝트에 부착.
    /// Spawned() → 양쪽 덱 교환 완료 → GameManager.OnDecksReady() 호출.
    /// </summary>
    public class NetworkGameBridge : NetworkBehaviour
    {
        public static NetworkGameBridge Instance { get; private set; }

        /// <summary>Host=0, Client=1. Spawned() 이후 확정.</summary>
        public int LocalPlayerID { get; private set; }

        /// <summary>세션 이름 (게임 로그 ID로 사용).</summary>
        public string SessionId => Runner?.SessionInfo.Name;

        /// <summary>플레이어별 Firebase UID. 덱 교환 완료 후 확정.</summary>
        public string GetPlayerUID(int playerID) => playerID == 0 ? _player0UID : _player1UID;

        PlayerDeck _deck0;
        PlayerDeck _deck1;
        bool _gameStarted;
        string _deck0Json;  // 클라이언트 재전송용 Host 덱 캐시
        string _player0UID;
        string _player1UID;

        // ─── Fusion 생명주기 ──────────────────────────────────────────

        public override void Spawned()
        {
            Instance = this;
            LocalPlayerID = Runner.IsServer ? 0 : 1;
            Debug.Log($"[Bridge] Spawned LocalPlayerID={LocalPlayerID}");

            var myDeck = DeckTransfer.Instance?.Player0Deck;
            if (myDeck == null)
            {
                Debug.LogError("[Bridge] DeckTransfer에 덱 없음!");
                return;
            }

            var data = new DeckNetworkData
            {
                kingName          = myDeck.king?.unitName ?? "",
                pawnName          = myDeck.pawn?.unitName ?? "",
                kingEnhanceLevel  = myDeck.kingEnhanceLevel,
                pawnEnhanceLevel  = myDeck.pawnEnhanceLevel,
                customUnitNames        = myDeck.customUnits.Select(u => u.unitName).ToList(),
                customUnitEnhanceLevels = myDeck.customUnitEnhanceLevels.ToList(),
                activeSkillNames       = myDeck.selectedActiveSkills.Select(s => s.skillName).ToList(),
                firebaseUID            = Chess.Core.FirebaseManager.Instance?.UID ?? "",
            };

            RPC_SendDeck(LocalPlayerID, JsonUtility.ToJson(data));
        }

        // ─── RPC ──────────────────────────────────────────────────────

        /// <summary>각 플레이어가 자신의 덱을 전체 브로드캐스트.</summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SendDeck(int playerID, string deckJson)
        {
            Debug.Log($"[Bridge] 덱 수신: Player{playerID}");
            var data = JsonUtility.FromJson<DeckNetworkData>(deckJson);
            var deck = BuildDeck(data);

            if (playerID == 0) { _deck0 = deck; _deck0Json = deckJson; _player0UID = data.firebaseUID; }
            else               { _deck1 = deck;                        _player1UID = data.firebaseUID; }

            // Host가 클라이언트 덱을 수신했을 때, 자신의 덱을 재전송
            // (클라이언트가 늦게 접속해서 최초 RPC를 못 받은 경우 대비)
            if (playerID == 1 && Runner.IsServer && _deck0Json != null)
            {
                Debug.Log("[Bridge] Host → 덱0 재전송 (클라이언트 재접속 대비)");
                RPC_SendDeck(0, _deck0Json);
            }

            TryStartGame();
        }

        /// <summary>이동 실행을 전체 브로드캐스트. 자기 자신이 보낸 것은 이미 로컬 실행되었으므로 수신 시 스킵.</summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SendMove(int senderPlayerID, int unitId, int targetX, int targetY)
        {
            if (senderPlayerID == LocalPlayerID) return;

            Debug.Log($"[Bridge] 원격 이동 수신: Player{senderPlayerID} unitId={unitId} → ({targetX},{targetY})");
            FindObjectOfType<GameManager>()?.ExecuteRemoteMove(unitId, new Vector2Int(targetX, targetY));
        }

        /// <summary>패시브 스킬 타겟 선택을 전체 브로드캐스트. 자신이 보낸 건 이미 처리했으므로 스킵.</summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SendPassiveTarget(int senderPlayerID, int targetUnitId)
        {
            if (senderPlayerID == LocalPlayerID) return;

            Debug.Log($"[Bridge] 패시브 타겟 수신: Player{senderPlayerID} unitId={targetUnitId}");
            FindObjectOfType<GameManager>()?.ReceivePassiveTarget(targetUnitId);
        }

        /// <summary>액티브 스킬 사용을 전체 브로드캐스트. 자신이 보낸 건 이미 로컬 실행되었으므로 스킵.</summary>
        /// targetUnitId == -1이면 range 스킬 (타겟 없음).
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SendActiveSkill(int senderPlayerID, string skillName, int targetUnitId)
        {
            if (senderPlayerID == LocalPlayerID) return;

            Debug.Log($"[Bridge] 액티브 스킬 수신: Player{senderPlayerID} skill={skillName} target={targetUnitId}");
            FindObjectOfType<GameManager>()?.ReceiveActiveSkill(senderPlayerID, skillName, targetUnitId);
        }

        /// <summary>항복을 전체 브로드캐스트. 상대방은 승리 처리.</summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_Surrender(int surrenderingPlayerID)
        {
            Debug.Log($"[Bridge] 항복 수신: Player{surrenderingPlayerID}");
            FindObjectOfType<GameManager>()?.ReceiveSurrender(surrenderingPlayerID);
        }

        // ─── 내부 ─────────────────────────────────────────────────────

        void TryStartGame()
        {
            if (_gameStarted || _deck0 == null || _deck1 == null) return;
            _gameStarted = true;

            Debug.Log("[Bridge] 양쪽 덱 준비 완료 → 게임 시작");
            FindObjectOfType<GameManager>()?.OnDecksReady(_deck0, _deck1, LocalPlayerID);
        }

        PlayerDeck BuildDeck(DeckNetworkData data)
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[Bridge] PlayerInventory 없음!");
                return null;
            }

            var db   = PlayerInventory.Instance.allUnitsDatabase;
            var deck = ScriptableObject.CreateInstance<PlayerDeck>();

            deck.king            = db.Find(u => u.unitName == data.kingName);
            deck.pawn            = db.Find(u => u.unitName == data.pawnName);
            deck.kingEnhanceLevel = data.kingEnhanceLevel;
            deck.pawnEnhanceLevel = data.pawnEnhanceLevel;
            deck.customUnits              = new List<UnitDefinition>();
            deck.customUnitEnhanceLevels  = new List<int>();
            deck.selectedActiveSkills     = new List<SkillDefinition>();

            for (int i = 0; i < data.customUnitNames.Count; i++)
            {
                var def = db.Find(u => u.unitName == data.customUnitNames[i]);
                if (def != null)
                {
                    deck.customUnits.Add(def);
                    int lv = i < data.customUnitEnhanceLevels.Count ? data.customUnitEnhanceLevels[i] : 0;
                    deck.customUnitEnhanceLevels.Add(lv);
                }
            }

            foreach (var skillName in data.activeSkillNames)
            {
                foreach (var unit in db)
                {
                    if (unit.activeSkill?.skillName == skillName)
                    {
                        deck.selectedActiveSkills.Add(unit.activeSkill);
                        break;
                    }
                }
            }

            return deck;
        }
    }
}
