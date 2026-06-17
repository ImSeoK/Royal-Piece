using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Balance
{
    // SampleScene의 빈 GameObject에 부착. 나머지는 자동 실행.
    public class BattleRunner : MonoBehaviour
    {
        [Header("시뮬레이션 설정")]
        [Tooltip("실행할 총 판 수")]
        public int battlesToRun = 100;

        [Tooltip("프레임당 실행할 판 수. 높을수록 빠르지만 에디터 반응성 저하.")]
        public int batchSize = 20;

        [Tooltip("Start() 시 자동 실행 여부. false면 Inspector의 컨텍스트 메뉴로 수동 시작.")]
        public bool runOnStart = true;

        [Header("덱 설정")]
        [Tooltip("이 희귀도 이상의 유닛만 덱 풀에 포함. Common = 전체 허용.")]
        public UnitRarity minimumRarity = UnitRarity.Common;

        [Tooltip("비워두면 PlayerInventory에서 자동 수집. 수동 테스트 시 여기에 유닛 목록 입력.")]
        public List<UnitDefinition> manualUnitDatabase = new List<UnitDefinition>();

        [Header("결과 출력")]
        public bool exportCSV  = true;
        public bool exportJSON = false;
        public bool exportHTML = false;

        [Header("씬 설정")]
        [Tooltip("true = 실행 시 GameManager를 자동으로 비활성화 (시뮬레이션 전용 모드)")]
        public bool disableGameManager = true;

        private bool isRunning;

        void Start()
        {
            if (disableGameManager)
            {
                // GameManager가 게임을 시작하기 전에 비활성화
                var gm = FindObjectOfType<Chess.Presentation.GameManager>();
                if (gm != null)
                {
                    gm.enabled = false;
                    Debug.Log("[BattleRunner] GameManager 비활성화 — 시뮬레이션 전용 모드");
                }
            }

            if (runOnStart)
                StartSimulation();
        }

        // Inspector 우클릭 → "시뮬레이션 시작"으로 수동 실행 가능
        [ContextMenu("시뮬레이션 시작")]
        public void StartSimulation()
        {
            if (isRunning)
            {
                Debug.LogWarning("[BattleRunner] 이미 실행 중입니다.");
                return;
            }
            StartCoroutine(RunSimulation());
        }

        private IEnumerator RunSimulation()
        {
            isRunning = true;

            var db = GetUnitDatabase();
            if (db == null || db.Count == 0)
            {
                Debug.LogError("[BattleRunner] 유닛 DB가 비어있습니다. " +
                               "PlayerInventory를 포함한 씬에서 실행하거나 manualUnitDatabase를 설정하세요.");
                isRunning = false;
                yield break;
            }

            // King, Pawn, 커스텀 유닛이 최소 1개씩 있는지 확인
            if (!db.Any(u => u.isKing) || !db.Any(u => u.isPawn) || !db.Any(u => !u.isKing && !u.isPawn))
            {
                Debug.LogError("[BattleRunner] King, Pawn, 커스텀 유닛이 모두 있어야 합니다.");
                isRunning = false;
                yield break;
            }

            var collector  = new BattleStatsCollector();
            var simulator  = new BattleSimulator();
            float startTime = Time.realtimeSinceStartup;
            int completed  = 0;

            Debug.Log($"[BattleRunner] 시작 — {battlesToRun}판 / 배치크기: {batchSize} / DB: {db.Count}개 유닛");

            while (completed < battlesToRun)
            {
                int thisBatch = Mathf.Min(batchSize, battlesToRun - completed);

                for (int i = 0; i < thisBatch; i++)
                {
                    var deck0 = GenerateRandomDeck(db);
                    var deck1 = GenerateRandomDeck(db);

                    if (deck0 == null || deck1 == null)
                    {
                        Debug.LogError("[BattleRunner] 덱 생성 실패. 중단합니다.");
                        isRunning = false;
                        yield break;
                    }

                    var result = simulator.Run(deck0, deck1);
                    collector.Record(result);

                    // 런타임에 생성한 ScriptableObject는 Destroy 필요
                    Destroy(deck0);
                    Destroy(deck1);

                    completed++;
                }

                // 50판마다 또는 완료 시 중간 집계 출력
                if (completed % 50 == 0 || completed == battlesToRun)
                    Debug.Log($"[BattleRunner] {completed}/{battlesToRun} — {collector.GetSummary()}");

                yield return null; // 프레임 양보 (에디터 반응성 유지)
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[BattleRunner] 완료! {battlesToRun}판 / {elapsed:F1}초 ({elapsed / battlesToRun * 1000:F1}ms/판)");
            Debug.Log($"[BattleRunner] 최종: {collector.GetSummary()}");

            ExportResults(collector);
            isRunning = false;
        }

        private void ExportResults(BattleStatsCollector collector)
        {
            string timestamp  = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // 프로젝트 루트 폴더 바로 아래 BalanceResults 폴더에 저장 (찾기 쉬움)
            string outputDir  = Path.Combine(Application.dataPath, "..", "BalanceResults");
            Directory.CreateDirectory(outputDir);
            string basePath   = Path.Combine(outputDir, $"balance_{timestamp}");

            string savedPath = outputDir;

            if (exportCSV)
            {
                string path = basePath + ".csv";
                collector.ExportCSV(path);
                Debug.Log($"[BattleRunner] CSV 저장: {Path.GetFullPath(path)}");
            }

            if (exportJSON)
            {
                string path = basePath + ".json";
                collector.ExportJSON(path);
                Debug.Log($"[BattleRunner] JSON 저장: {Path.GetFullPath(path)}");
            }

            if (exportHTML)
            {
                string path = basePath + ".html";
                collector.ExportHTML(path);
                string fullPath = Path.GetFullPath(path);
                Debug.Log($"[BattleRunner] HTML 저장: {fullPath}");
                // 브라우저로 자동 열기
                System.Diagnostics.Process.Start(fullPath);
                return;
            }

            if (!exportCSV && !exportJSON && !exportHTML)
            {
                Debug.LogWarning("[BattleRunner] 출력 옵션이 모두 false — 파일 없음.");
                return;
            }

            // HTML 미사용 시 탐색기로 폴더 열기
            string fullDir = Path.GetFullPath(savedPath);
            Debug.Log($"[BattleRunner] 결과 폴더: {fullDir}");
            System.Diagnostics.Process.Start("explorer.exe", fullDir);
        }

        private List<UnitDefinition> GetUnitDatabase()
        {
            if (manualUnitDatabase != null && manualUnitDatabase.Count > 0)
                return manualUnitDatabase;

            if (PlayerInventory.Instance != null)
                return PlayerInventory.Instance.allUnitsDatabase;

            return null;
        }

        // 실제 덱빌더 규칙을 지키는 랜덤 덱 생성
        // 규칙: 퀸 최대 1명, 커스텀 슬롯 7개
        private PlayerDeck GenerateRandomDeck(List<UnitDefinition> db)
        {
            var king = db.FirstOrDefault(u => u.isKing);
            var pawn = db.FirstOrDefault(u => u.isPawn);

            IEnumerable<UnitDefinition> pool = db.Where(u => !u.isKing && !u.isPawn && u.rarity >= minimumRarity);

            var poolList = pool.ToList();

            if (king == null || pawn == null || poolList.Count == 0)
            {
                Debug.LogError("[BattleRunner] 덱 구성에 필요한 유닛이 부족합니다.");
                return null;
            }

            var deck = ScriptableObject.CreateInstance<PlayerDeck>();
            deck.king                    = king;
            deck.pawn                    = pawn;
            deck.customUnits             = new List<UnitDefinition>();
            deck.customUnitEnhanceLevels = new List<int>();

            bool queenAdded = false;

            for (int i = 0; i < 7; i++)
            {
                // 퀸이 이미 있으면 퀸을 제외한 풀에서 뽑기
                var candidates = queenAdded
                    ? poolList.Where(u => !u.isQueen).ToList()
                    : poolList;

                if (candidates.Count == 0)
                    candidates = poolList; // 퀸 외 유닛이 없는 극단적 케이스 fallback

                var picked = candidates[Random.Range(0, candidates.Count)];

                if (picked.isQueen) queenAdded = true;

                deck.customUnits.Add(picked);
                deck.customUnitEnhanceLevels.Add(0);
            }

            return deck;
        }
    }
}
