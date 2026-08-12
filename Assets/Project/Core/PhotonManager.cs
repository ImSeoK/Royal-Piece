using UnityEngine;
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

namespace Chess.Core
{
    /// <summary>
    /// Photon Fusion 매칭 관리자 (Phase 3).
    /// StartMatchmaking(mode) 호출 → 2인 매칭 완료 시 OnMatchReady 이벤트 발생.
    /// AI 모드는 이 클래스를 거치지 않음.
    /// </summary>
    public class PhotonManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static PhotonManager Instance { get; private set; }

        /// <summary>2명 매칭 완료 — 호출 측에서 씬 전환.</summary>
        public event Action OnMatchReady;
        /// <summary>연결/매칭 실패 (사유 문자열 포함).</summary>
        public event Action<string> OnMatchFailed;

        /// <summary>이 클라이언트가 Host(서버)인지 여부.</summary>
        public bool IsHost { get; private set; }

        /// <summary>현재 활성 NetworkRunner. null이면 미연결.</summary>
        public NetworkRunner Runner => _runner;

        NetworkRunner _runner;
        bool _matchReadyFired;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ─── 외부 진입점 ───────────────────────────────────────────

        /// <summary>
        /// 온라인 매칭 시작. Normal/Ranked 모드만 호출.
        /// 결과는 OnMatchReady 또는 OnMatchFailed 이벤트로 수신.
        /// </summary>
        public async void StartMatchmaking(GameMode mode)
        {
            _matchReadyFired = false;

            // 기존 연결 정리
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                await _runner.Shutdown();
                _runner = null;
            }

            var runnerGO = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGO);
            _runner = runnerGO.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            runnerGO.AddComponent<NetworkSceneManagerDefault>();

            string modeKey = mode == GameMode.Ranked ? "ranked" : "normal";

            var args = new StartGameArgs
            {
                GameMode    = Fusion.GameMode.AutoHostOrClient,
                PlayerCount = 2,
                Scene       = SceneRef.None,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["gameMode"] = modeKey,
                },
            };

            Debug.Log($"[Photon] 매칭 시작: {modeKey}");
            var result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                Debug.LogError($"[Photon] StartGame 실패: {result.ShutdownReason}");
                OnMatchFailed?.Invoke(result.ShutdownReason.ToString());
            }
        }

        /// <summary>매칭 취소 또는 연결 해제.</summary>
        public async void Disconnect()
        {
            if (_runner == null) return;
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown();
            _runner = null;
        }

        // ─── INetworkRunnerCallbacks ────────────────────────────────

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            IsHost = runner.IsServer;

            int count = 0;
            foreach (var _ in runner.ActivePlayers) count++;
            Debug.Log($"[Photon] 플레이어 입장 (총 {count}명, IsHost={IsHost})");

            if (count >= 2 && !_matchReadyFired)
            {
                _matchReadyFired = true;
                Debug.Log("[Photon] 2인 매칭 완료 → OnMatchReady");
                OnMatchReady?.Invoke();
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
            => Debug.Log($"[Photon] 플레이어 퇴장: {player}");

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[Photon] Shutdown: {shutdownReason}");
            if (shutdownReason != ShutdownReason.Ok &&
                shutdownReason != ShutdownReason.OperationCanceled)
                OnMatchFailed?.Invoke(shutdownReason.ToString());
        }

        public void OnConnectedToServer(NetworkRunner runner)
            => Debug.Log("[Photon] 서버 연결됨");

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
            => Debug.Log($"[Photon] 서버 연결 끊김: {reason}");

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[Photon] 연결 실패: {reason}");
            OnMatchFailed?.Invoke(reason.ToString());
        }

        // 사용하지 않는 콜백 (빈 구현)
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    }
}
