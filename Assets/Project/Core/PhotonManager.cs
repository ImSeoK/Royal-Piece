using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;

namespace Chess.Core
{
    /// <summary>
    /// Photon Fusion 매칭 관리자.
    /// Normal: AutoHostOrClient 단순 매칭.
    /// Ranked: 점수 기반 매칭 — 100점 버킷, 1분마다 ±N 확장.
    /// </summary>
    public class PhotonManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static PhotonManager Instance { get; private set; }

        public event Action OnMatchReady;
        public event Action<string> OnMatchFailed;

        public bool IsHost { get; private set; }
        public NetworkRunner Runner => _runner;

        NetworkRunner _runner;
        bool _matchReadyFired;

        // ─── 랭크 매칭 전용 ────────────────────────────────────────
        bool _isRanked;
        int  _myScore;
        int  _tolerance;          // 현재 허용 점수 차 (0 → 100 → 200 → ...)
        bool _migrating;          // 세션 전환 중 플래그 (중복 방지)
        Coroutine _expandRoutine;

        const int BUCKET_SIZE    = 100;  // 버킷 간격
        const int EXPAND_SECONDS = 60;   // 확장 주기

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ─── 외부 진입점 ───────────────────────────────────────────

        public async void StartMatchmaking(GameMode mode)
        {
            _matchReadyFired = false;
            _migrating       = false;
            _isRanked        = (mode == GameMode.Ranked);
            _myScore         = _isRanked ? RankSystem.GetPoints() : 0;
            _tolerance       = 0;

            if (_expandRoutine != null) { StopCoroutine(_expandRoutine); _expandRoutine = null; }

            await ShutdownRunner();
            await CreateRunner();

            string modeKey = _isRanked ? "ranked" : "normal";
            var props = new Dictionary<string, SessionProperty> { ["gameMode"] = modeKey };
            if (_isRanked) props["rankScore"] = _myScore;

            var args = new StartGameArgs
            {
                GameMode          = Fusion.GameMode.AutoHostOrClient,
                PlayerCount       = 2,
                SceneManager      = _runner.GetComponent<NetworkSceneManagerDefault>(),
                SessionProperties = props,
            };

            Debug.Log($"[Photon] 매칭 시작: {modeKey}" + (_isRanked ? $" (점수={_myScore})" : ""));
            var result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                Debug.LogError($"[Photon] StartGame 실패: {result.ShutdownReason}");
                OnMatchFailed?.Invoke(result.ShutdownReason.ToString());
                return;
            }

            if (_isRanked)
                _expandRoutine = StartCoroutine(ExpandTolerance());
        }

        public void LoadGameScene()
        {
            if (_runner == null || !_runner.IsServer) return;
            _runner.LoadScene(SceneRef.FromIndex(4));
        }

        public async void Disconnect()
        {
            if (_expandRoutine != null) { StopCoroutine(_expandRoutine); _expandRoutine = null; }
            await ShutdownRunner();
        }

        // ─── 랭크 매칭 — 버킷 확장 코루틴 ────────────────────────

        IEnumerator ExpandTolerance()
        {
            while (!_matchReadyFired)
            {
                yield return new WaitForSeconds(EXPAND_SECONDS);
                if (_matchReadyFired) yield break;

                _tolerance += BUCKET_SIZE;
                Debug.Log($"[Photon] 랭크 범위 확장: ±{_tolerance}점");
                // OnSessionListUpdated가 다음 업데이트에서 자동으로 새 세션 탐색
            }
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
                if (_expandRoutine != null) { StopCoroutine(_expandRoutine); _expandRoutine = null; }
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

        /// <summary>
        /// 랭크 매칭 핵심: 세션 목록에서 ±tolerance 범위 안 세션 탐색.
        /// 두 호스트가 서로 발견했을 때 낮은 점수가 높은 점수 쪽으로 이동
        /// (동점 시 세션 이름 사전순 — 결정론적 충돌 해소).
        /// </summary>
        public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (!_isRanked || _matchReadyFired || _migrating || _tolerance == 0) return;
            // 호스트로서 대기 중일 때만 탐색 (클라이언트로 이미 세션에 붙었으면 불필요)
            if (!runner.IsServer) return;

            foreach (var session in sessionList)
            {
                if (!session.IsOpen || session.PlayerCount >= 2) continue;
                if (session.Name == runner.SessionInfo?.Name) continue;  // 내 세션 제외
                if (!session.Properties.TryGetValue("rankScore", out var prop)) continue;

                int hostScore = (int)prop;
                if (Mathf.Abs(hostScore - _myScore) > _tolerance) continue;

                // 충돌 해소: 내 점수가 더 낮거나, 동점일 때 내 세션명이 사전순으로 앞이면 내가 이동
                bool iShouldMigrate = _myScore < hostScore ||
                    (_myScore == hostScore &&
                     string.Compare(runner.SessionInfo?.Name, session.Name, StringComparison.Ordinal) < 0);

                if (!iShouldMigrate) continue;

                _migrating = true;
                Debug.Log($"[Photon] 적합한 세션 발견 → 이동: 상대점수={hostScore}, 내점수={_myScore}, 허용범위=±{_tolerance}");
                await MigrateToSession(session.Name);
                return;
            }
        }

        // ─── 내부 유틸 ─────────────────────────────────────────────

        async Task MigrateToSession(string sessionName)
        {
            await ShutdownRunner();
            await CreateRunner();

            var args = new StartGameArgs
            {
                GameMode     = Fusion.GameMode.Client,
                SessionName  = sessionName,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
            };

            Debug.Log($"[Photon] 세션 합류: {sessionName}");
            var result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                Debug.LogError($"[Photon] 세션 합류 실패: {result.ShutdownReason}");
                _migrating = false;
                // 실패 시 일반 매칭으로 복귀
                StartMatchmaking(GameMode.Ranked);
            }
        }

        async Task ShutdownRunner()
        {
            if (_runner == null) return;
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown();
            _runner = null;
        }

        async Task CreateRunner()
        {
            // 이전 NetworkRunner 오브젝트 정리
            var old = GameObject.Find("NetworkRunner");
            if (old != null) Destroy(old);

            await Task.Yield();  // 프레임 넘겨서 Destroy 반영

            var runnerGO = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGO);
            _runner = runnerGO.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            runnerGO.AddComponent<NetworkSceneManagerDefault>();
        }

        // 사용하지 않는 콜백
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
    }
}
