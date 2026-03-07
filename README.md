## About Me
Unity 기반 게임을 개발하고 있는 개발자입니다.

## Skills

Engine
- Unity

Language
- C#

Tools
- Git
- Aseprite

📖Overview
Royal Piece는 체스의 전략적 깊이에 가챠 수집 요소와 덱빌딩 메커니즘을 결합한 Unity 기반 게임입니다.
플레이어는 다양한 희귀도의 체스 유닛 카드를 수집하고, 7장의 자유 유닛으로 덱을 구성해 전투에 임합니다.


🎮 Core Features

🎰 가챠 시스템

단일 뽑기 / 10연 뽑기 지원
희귀도별 확률 기반 드롭 시스템 (common, rare, epic, legendary) 
인벤토리에 자동 저장

🃏 덱 빌더

슬롯 기반 7유닛 덱 구성
희귀도에 따른 컬러 피드백 (시각적 구분)
덱 저장 / 불러오기 기능

📦 인벤토리

보유 유닛 카드 목록 열람
필터링 UI (희귀도 등 기준 정렬)
PlayerPrefs 기반 로컬 저장

🎨 Design

다크 컬러 팔레트 기반 UI 일관성 유지
희귀도별 컬러 코드로 카드 등급 시각화

⚡ Active Skill 시스템
덱에 편성된 유닛 중 Active Skill을 보유한 유닛의 스킬이 전투 진입 전 선택지로 제공됩니다.
플레이어는 이 중 2개의 Active Skill을 선택해 전투에 차용할 수 있습니다.

모든 유닛이 Active Skill을 보유하는 것은 아님
덱 구성에 따라 선택 가능한 스킬 풀이 달라지므로, 덱 빌딩 전략과 긴밀하게 연결됨


네임스페이스 구조

Chess.Core           - 게임 데이터, 유닛 정의, 희귀도 시스템
Chess.Simulation     - 전투 로직, 게임 규칙
Chess.Presentation   - UI, HUD, 카드 렌더링

씬 구성

MainMenu  -  게임 시작 화면, 각 씬으로의 진입 허브
GachaScene  -  단일 / 10연 뽑기 수행, 획득 유닛 확인
InventoryScene  -  보유 유닛 목록 열람, 유닛 상세 정보 확인 및 강화 진행
DeckBuilderScene  -  슬롯 기반 7유닛 덱 구성, 저장 / 불러오기
InGameScene  -  전투 진행, HUD 표시
