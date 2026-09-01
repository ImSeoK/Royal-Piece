/**
 * Royal Piece — Phase 5 Cloud Function
 * Firestore gameLogs/{gameId} 문서 생성 시 트리거.
 * 게임 이벤트 로그를 검증하고 rankPoints / currency 업데이트.
 */

const { onDocumentCreated } = require("firebase-functions/v2/firestore");
const admin = require("firebase-admin");
admin.initializeApp();

const db = admin.firestore();

// ─── 상수 ──────────────────────────────────────────────────────────
const RANK_WIN_DELTA  =  15;
const RANK_LOSS_DELTA =  -8;
const GOLD_WIN        =  40;
const GOLD_LOSS       =  15;
const MIN_MOVE_COUNT  =   3; // 최소 이동 횟수 (1수 만에 승리 방지)
const MIN_TOTAL_EVENTS =  5; // 최소 총 이벤트 수
const MAX_GAME_AGE_MS  = 30 * 60 * 1000; // 30분: 이 시간 이전 제출 거부

// ─── 트리거 ────────────────────────────────────────────────────────
exports.verifyGameResult = onDocumentCreated("gameLogs/{gameId}", async (event) => {
    const snap   = event.data;
    const gameId = event.params.gameId;
    const data   = snap.data();

    const { player0uid, player1uid, winnerPlayerID, isRanked, events, submittedBy, timestamp } = data;

    // 0. 중복 처리 방지 — verified 필드가 이미 존재하면 스킵 (엣지케이스 안전망)
    if (data.verified !== undefined) {
        console.warn(`[${gameId}] 이미 처리된 문서 — 스킵`);
        return;
    }

    // 1. 필수 필드 검증
    if (!player0uid || !player1uid || winnerPlayerID === undefined) {
        console.error(`[${gameId}] 필수 필드 누락`);
        await snap.ref.update({ verified: false, reason: "missing_fields" });
        return;
    }

    // 2. 제출자가 게임 참가자인지 확인
    if (submittedBy !== player0uid && submittedBy !== player1uid) {
        console.error(`[${gameId}] 제출자 UID 불일치`);
        await snap.ref.update({ verified: false, reason: "invalid_submitter" });
        return;
    }

    // 3. 제출 시각 유효성 확인 (30분 이상 지난 로그 거부)
    if (timestamp) {
        const gameTimeMs = timestamp.toMillis ? timestamp.toMillis() : timestamp._seconds * 1000;
        const ageMs = Date.now() - gameTimeMs;
        if (ageMs > MAX_GAME_AGE_MS) {
            console.error(`[${gameId}] 제출 시각 초과 (${Math.floor(ageMs / 60000)}분 전)`);
            await snap.ref.update({ verified: false, reason: "submission_too_old" });
            return;
        }
    }

    // 4. 이벤트 최소 수 확인 (비정상적으로 짧은 게임 차단)
    const allEvents  = events || [];
    const moveEvents = allEvents.filter(e => e.eventType === "move");
    if (allEvents.length < MIN_TOTAL_EVENTS) {
        console.error(`[${gameId}] 총 이벤트 부족 (${allEvents.length})`);
        await snap.ref.update({ verified: false, reason: "too_few_events" });
        return;
    }
    if (moveEvents.length < MIN_MOVE_COUNT) {
        console.error(`[${gameId}] 이동 횟수 부족 (${moveEvents.length})`);
        await snap.ref.update({ verified: false, reason: "too_few_moves" });
        return;
    }

    // 5. 게임 결과 시뮬레이션 검증
    const verifiedWinner = simulateGame(allEvents);
    if (verifiedWinner === null) {
        // 시뮬레이션 실패 시 제출된 결과 그대로 신뢰 (풀 시뮬레이션 미구현 구간)
        console.warn(`[${gameId}] 시뮬레이션 불확정 — 제출 결과 채택`);
    } else if (verifiedWinner !== winnerPlayerID) {
        console.error(`[${gameId}] 승자 불일치: 제출=${winnerPlayerID} 검증=${verifiedWinner}`);
        await snap.ref.update({ verified: false, reason: "winner_mismatch" });
        return;
    }

    // 6. 승패 결정
    const winnerUID = winnerPlayerID === 0 ? player0uid : player1uid;
    const loserUID  = winnerPlayerID === 0 ? player1uid : player0uid;

    // 7. Firestore 업데이트 (트랜잭션)
    await db.runTransaction(async tx => {
        const winnerRef = db.collection("users").doc(winnerUID);
        const loserRef  = db.collection("users").doc(loserUID);

        const [winnerSnap, loserSnap] = await Promise.all([
            tx.get(winnerRef),
            tx.get(loserRef),
        ]);

        const winnerData = winnerSnap.exists ? winnerSnap.data() : {};
        const loserData  = loserSnap.exists  ? loserSnap.data()  : {};

        const winnerCurrency   = (winnerData.currency   || 0) + GOLD_WIN;
        const loserCurrency    = (loserData.currency    || 0) + GOLD_LOSS;
        const winnerRankPoints = isRanked
            ? Math.max(0, (winnerData.rankPoints || 0) + RANK_WIN_DELTA)
            : (winnerData.rankPoints || 0);
        const loserRankPoints  = isRanked
            ? Math.max(0, (loserData.rankPoints  || 0) + RANK_LOSS_DELTA)
            : (loserData.rankPoints  || 0);

        tx.set(winnerRef, { currency: winnerCurrency, rankPoints: winnerRankPoints }, { merge: true });
        tx.set(loserRef,  { currency: loserCurrency,  rankPoints: loserRankPoints  }, { merge: true });
    });

    await snap.ref.update({ verified: true, verifiedWinner: winnerPlayerID });
    console.log(`[${gameId}] 검증 완료 — 승자: Player${winnerPlayerID}`);
});

// ─── 시뮬레이션 (MVP: 킹 이동 추적 기반) ─────────────────────────
/**
 * 이벤트 로그에서 킹의 생존 여부를 추적해 승자 반환.
 * 스킬 효과는 미구현 → null 반환 시 제출 결과 채택.
 *
 * @param {Array} events
 * @returns {number|null} 승자 playerID (0 or 1), 판단 불가 시 null
 */
function simulateGame(events) {
    // 이동 이벤트만 추적 — 전투 결과(킹 사망)는 로그에 없으므로 null 반환
    // Phase 5.2에서 전투 시뮬레이션 포팅 시 구현 예정
    return null;
}
