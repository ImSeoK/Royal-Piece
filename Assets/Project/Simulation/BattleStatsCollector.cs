using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Chess.Core;

namespace Chess.Balance
{
    // 한 유닛 정의에 대한 누적 통계
    public class AggregatedUnitStats
    {
        public UnitDefinition definition;
        public int gamesPlayed;
        public int gamesWon;
        public long totalDamageDealt;
        public long totalDamageReceived;
        public long totalTurnsAlive;
        public int totalKills;
        public int totalPassiveActivations;
        public int deathCount;

        public float WinRate              => gamesPlayed > 0 ? (float)gamesWon               / gamesPlayed : 0f;
        public float AvgDamageDealt       => gamesPlayed > 0 ? (float)totalDamageDealt        / gamesPlayed : 0f;
        public float AvgDamageReceived    => gamesPlayed > 0 ? (float)totalDamageReceived     / gamesPlayed : 0f;
        public float AvgTurnsAlive        => gamesPlayed > 0 ? (float)totalTurnsAlive         / gamesPlayed : 0f;
        public float AvgKills             => gamesPlayed > 0 ? (float)totalKills              / gamesPlayed : 0f;
        public float AvgPassiveActivations=> gamesPlayed > 0 ? (float)totalPassiveActivations / gamesPlayed : 0f;
        public float SurvivalRate         => gamesPlayed > 0 ? 1f - (float)deathCount         / gamesPlayed : 0f;
    }

    // 여러 판의 BattleResult를 받아 누적 통계를 관리하고 파일로 출력
    public class BattleStatsCollector
    {
        private readonly Dictionary<UnitDefinition, AggregatedUnitStats> statsMap
            = new Dictionary<UnitDefinition, AggregatedUnitStats>();

        public int TotalGames   { get; private set; }
        public int Player0Wins  { get; private set; }
        public int Player1Wins  { get; private set; }
        public int Draws        { get; private set; }
        public long TotalTurns  { get; private set; }

        public float AvgTurnsPerGame => TotalGames > 0 ? (float)TotalTurns / TotalGames : 0f;

        public void Record(BattleResult result)
        {
            TotalGames++;
            TotalTurns += result.totalTurns;

            if      (result.winnerID == 0) Player0Wins++;
            else if (result.winnerID == 1) Player1Wins++;
            else                           Draws++;

            foreach (var rec in result.unitRecords)
            {
                if (rec.definition == null) continue;

                if (!statsMap.TryGetValue(rec.definition, out var stats))
                {
                    stats = new AggregatedUnitStats { definition = rec.definition };
                    statsMap[rec.definition] = stats;
                }

                stats.gamesPlayed++;
                if (result.winnerID == rec.ownerID) stats.gamesWon++;
                stats.totalDamageDealt        += rec.damageDealt;
                stats.totalDamageReceived     += rec.damageReceived;
                stats.totalTurnsAlive         += rec.turnsAlive;
                stats.totalKills              += rec.kills;
                stats.totalPassiveActivations += rec.passiveActivations;
                if (!rec.survived) stats.deathCount++;
            }
        }

        // 콘솔 출력용 요약 문자열
        public string GetSummary()
        {
            return $"총 {TotalGames}판 | P0승: {Player0Wins} ({Player0Wins * 100f / TotalGames:F1}%) | " +
                   $"P1승: {Player1Wins} ({Player1Wins * 100f / TotalGames:F1}%) | " +
                   $"무: {Draws} | 평균 {AvgTurnsPerGame:F1}턴";
        }

        // 유닛별 통계 목록 반환 (승률 내림차순)
        public IEnumerable<AggregatedUnitStats> GetAllStats()
            => statsMap.Values.OrderByDescending(s => s.WinRate);

        // CSV 파일로 저장
        public void ExportCSV(string filePath)
        {
            var sb = new StringBuilder();

            // 파일 헤더: 전체 요약
            sb.AppendLine($"# 총판수,P0승,P1승,무승부,평균턴");
            sb.AppendLine($"# {TotalGames},{Player0Wins},{Player1Wins},{Draws},{AvgTurnsPerGame:F1}");
            sb.AppendLine();

            // 유닛별 컬럼 헤더
            sb.AppendLine("유닛명,타입,희귀도,게임수,승률,평균딜,평균피해,평균생존턴,평균킬,패시브발동,생존율");

            foreach (var stats in GetAllStats())
            {
                var def = stats.definition;
                sb.AppendLine(
                    $"{def.unitName}," +
                    $"{def.GetTypeText()}," +
                    $"{def.rarity}," +
                    $"{stats.gamesPlayed}," +
                    $"{stats.WinRate:F3}," +
                    $"{stats.AvgDamageDealt:F1}," +
                    $"{stats.AvgDamageReceived:F1}," +
                    $"{stats.AvgTurnsAlive:F1}," +
                    $"{stats.AvgKills:F2}," +
                    $"{stats.AvgPassiveActivations:F2}," +
                    $"{stats.SurvivalRate:F3}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // HTML 보고서 저장 (브라우저에서 바로 열림)
        public void ExportHTML(string filePath)
        {
            var list = GetAllStats().ToList();
            float avgWinRate = TotalGames > 0 ? 0.5f : 0f;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang='ko'><head><meta charset='UTF-8'>");
            sb.AppendLine("<title>밸런스 테스트 결과</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#eee;padding:24px;margin:0}");
            sb.AppendLine("h1{color:#e2b96f;margin-bottom:4px}");
            sb.AppendLine(".summary{background:#16213e;border-radius:8px;padding:16px;margin-bottom:24px;display:flex;gap:32px;flex-wrap:wrap}");
            sb.AppendLine(".stat-box{text-align:center}.stat-box .val{font-size:2em;font-weight:bold;color:#e2b96f}.stat-box .lbl{font-size:0.8em;color:#aaa;margin-top:4px}");
            sb.AppendLine("table{width:100%;border-collapse:collapse;background:#16213e;border-radius:8px;overflow:hidden}");
            sb.AppendLine("th{background:#0f3460;padding:10px 12px;text-align:left;font-size:0.85em;color:#aaa;cursor:pointer;user-select:none}");
            sb.AppendLine("th:hover{background:#1a4a7a}");
            sb.AppendLine("td{padding:9px 12px;border-bottom:1px solid #0f3460;font-size:0.9em}");
            sb.AppendLine("tr:hover td{background:#1e2d50}");
            sb.AppendLine(".op{background:#3d1515!important;border-left:3px solid #ff4444}");
            sb.AppendLine(".weak{background:#151a3d!important;border-left:3px solid #4466ff}");
            sb.AppendLine(".bar-wrap{background:#0f1929;border-radius:4px;height:8px;width:80px;display:inline-block;vertical-align:middle}");
            sb.AppendLine(".bar{height:8px;border-radius:4px}");
            sb.AppendLine(".tag{font-size:0.7em;padding:2px 6px;border-radius:4px;font-weight:bold;margin-left:6px}");
            sb.AppendLine(".tag-op{background:#ff4444;color:#fff}.tag-weak{background:#4466ff;color:#fff}");
            sb.AppendLine(".rarity-Common{color:#aaa}.rarity-Rare{color:#4fc3f7}.rarity-Epic{color:#ce93d8}.rarity-Legendary{color:#ffd54f}");
            sb.AppendLine("</style></head><body>");

            // 요약 헤더
            sb.AppendLine($"<h1>밸런스 테스트 결과</h1>");
            sb.AppendLine($"<p style='color:#aaa;margin-top:0'>{System.DateTime.Now:yyyy-MM-dd HH:mm} · {TotalGames}판 시뮬레이션</p>");
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine($"<div class='stat-box'><div class='val'>{TotalGames}</div><div class='lbl'>총 판 수</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='val'>{Player0Wins}</div><div class='lbl'>P0 승 ({Player0Wins * 100f / TotalGames:F1}%)</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='val'>{Player1Wins}</div><div class='lbl'>P1 승 ({Player1Wins * 100f / TotalGames:F1}%)</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='val'>{Draws}</div><div class='lbl'>무승부</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='val'>{AvgTurnsPerGame:F1}</div><div class='lbl'>평균 턴 수</div></div>");
            sb.AppendLine("</div>");

            // 유닛 테이블
            sb.AppendLine("<table id='tbl'>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th onclick='sort(0)'>유닛명</th><th onclick='sort(1)'>타입</th><th onclick='sort(2)'>희귀도</th>");
            sb.AppendLine("<th onclick='sort(3)'>승률 ▼</th><th onclick='sort(4)'>평균 딜</th><th onclick='sort(5)'>평균 피해</th>");
            sb.AppendLine("<th onclick='sort(6)'>생존 턴</th><th onclick='sort(7)'>평균 킬</th><th onclick='sort(8)'>생존율</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var s in list)
            {
                var def = s.definition;
                bool isOp   = s.WinRate >= 0.60f;
                bool isWeak = s.WinRate <= 0.40f;
                string rowClass = isOp ? "op" : isWeak ? "weak" : "";
                string tag = isOp
                    ? "<span class='tag tag-op'>OP</span>"
                    : isWeak ? "<span class='tag tag-weak'>약함</span>" : "";

                int barPct = (int)(s.WinRate * 100);
                string barColor = isOp ? "#ff6b6b" : isWeak ? "#6b8cff" : "#50c878";

                sb.AppendLine($"<tr class='{rowClass}'>");
                sb.AppendLine($"<td><b>{def.unitName}</b>{tag}</td>");
                sb.AppendLine($"<td>{def.GetTypeText()}</td>");
                sb.AppendLine($"<td class='rarity-{def.rarity}'>{def.rarity}</td>");
                sb.AppendLine($"<td><span style='font-weight:bold;color:{barColor}'>{s.WinRate * 100:F1}%</span> " +
                              $"<span class='bar-wrap'><span class='bar' style='width:{barPct}%;background:{barColor}'></span></span></td>");
                sb.AppendLine($"<td>{s.AvgDamageDealt:F0}</td>");
                sb.AppendLine($"<td>{s.AvgDamageReceived:F0}</td>");
                sb.AppendLine($"<td>{s.AvgTurnsAlive:F1}</td>");
                sb.AppendLine($"<td>{s.AvgKills:F2}</td>");
                sb.AppendLine($"<td>{s.SurvivalRate * 100:F1}%</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            // 컬럼 클릭 정렬 스크립트
            sb.AppendLine("<script>");
            sb.AppendLine("let sortCol=3,asc=false;");
            sb.AppendLine("function sort(c){if(sortCol===c)asc=!asc;else{sortCol=c;asc=c<3;}");
            sb.AppendLine("const tb=document.querySelector('#tbl tbody');");
            sb.AppendLine("const rows=[...tb.rows].sort((a,b)=>{");
            sb.AppendLine("  let av=a.cells[c].innerText.replace('%',''),bv=b.cells[c].innerText.replace('%','');");
            sb.AppendLine("  const n=parseFloat(av),m=parseFloat(bv);");
            sb.AppendLine("  const cmp=isNaN(n)?av.localeCompare(bv):n-m;return asc?cmp:-cmp;});");
            sb.AppendLine("rows.forEach(r=>tb.appendChild(r));}");
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // JSON 파일로 저장
        public void ExportJSON(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"totalGames\": {TotalGames},");
            sb.AppendLine($"  \"player0Wins\": {Player0Wins},");
            sb.AppendLine($"  \"player1Wins\": {Player1Wins},");
            sb.AppendLine($"  \"draws\": {Draws},");
            sb.AppendLine($"  \"avgTurnsPerGame\": {AvgTurnsPerGame:F1},");
            sb.AppendLine("  \"units\": [");

            var list = GetAllStats().ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var s   = list[i];
                var def = s.definition;
                string comma = i < list.Count - 1 ? "," : "";
                sb.AppendLine(
                    $"    {{" +
                    $"\"name\": \"{def.unitName}\", " +
                    $"\"type\": \"{def.GetTypeText()}\", " +
                    $"\"rarity\": \"{def.rarity}\", " +
                    $"\"games\": {s.gamesPlayed}, " +
                    $"\"winRate\": {s.WinRate:F3}, " +
                    $"\"avgDmgDealt\": {s.AvgDamageDealt:F1}, " +
                    $"\"avgDmgReceived\": {s.AvgDamageReceived:F1}, " +
                    $"\"avgTurnsAlive\": {s.AvgTurnsAlive:F1}, " +
                    $"\"avgKills\": {s.AvgKills:F2}, " +
                    $"\"avgPassives\": {s.AvgPassiveActivations:F2}, " +
                    $"\"survivalRate\": {s.SurvivalRate:F3}" +
                    $"}}{comma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
