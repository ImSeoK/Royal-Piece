# Royal Piece

Unity 기반으로 개발한 전략 카드 게임 프로젝트입니다.  
체스의 전략성과 가챠 수집, 덱빌딩 메커니즘을 결합한 게임을 목표로 제작했습니다.

---

# 👤 About Me

Unity 기반 게임 개발을 진행하고 있는 개발자입니다.  
게임 시스템 설계와 UI 구조 구현을 중심으로 프로젝트를 개발하고 있습니다.

---

# 🛠 Skills

### Engine
- Unity

### Language
- C#

### Tools
- Git
- Aseprite

---

# 📖 Overview

**Royal Piece**는 체스의 전략적 깊이에 **가챠 수집 시스템**과  
**덱 빌딩 메커니즘**을 결합한 Unity 기반 게임입니다.

플레이어는 다양한 희귀도의 체스 유닛 카드를 수집하고  
7장의 자유 유닛으로 덱을 구성하여 전투에 참여합니다.

덱 구성에 따라 사용할 수 있는 능력과 전략이 달라지며  
수집과 전략 요소를 동시에 즐길 수 있도록 설계했습니다.

---

# 🎮 Core Features

## 🎰 Gacha System

- 단일 뽑기 / 10연 뽑기 지원
- 희귀도 기반 확률 드롭 시스템  
  - Common  
  - Rare  
  - Epic  
  - Legendary
- 획득한 유닛은 인벤토리에 자동 저장

---

## 🃏 Deck Builder

- 슬롯 기반 **7유닛 덱 구성**
- 희귀도에 따른 **컬러 피드백 UI**
- 덱 저장 / 불러오기 기능 지원

---

## 📦 Inventory System

- 보유 유닛 카드 목록 열람
- 희귀도 기준 필터링 UI
- PlayerPrefs 기반 로컬 데이터 저장

---

# ⚡ Active Skill System

덱에 편성된 유닛 중 **Active Skill을 보유한 유닛**의 스킬이  
전투 진입 전 선택지로 제공됩니다.

플레이어는 이 중 **2개의 Active Skill**을 선택하여 전투에 활용할 수 있습니다.

특징

- 모든 유닛이 Active Skill을 보유하지는 않음
- 덱 구성에 따라 선택 가능한 스킬 풀이 달라짐
- 덱 빌딩 전략과 직접적으로 연결되는 시스템

---

# 🎨 UI Design

- 다크 컬러 팔레트를 기반으로 한 UI 스타일
- 희귀도별 컬러 코드를 사용하여 카드 등급 시각화
- 시스템별 UI 구조 분리

---

# 🧱 Project Architecture

프로젝트는 기능 단위로 네임스페이스를 분리하여 구성했습니다.

### Namespace Structure

Chess.Core
게임 데이터, 유닛 정의, 희귀도 시스템

Chess.Simulation
전투 로직 및 게임 규칙 처리

Chess.Presentation
UI, HUD, 카드 렌더링

---

# 🗺 Scene Structure

프로젝트는 기능별 씬 분리를 통해 구성했습니다.

### MainMenu
게임 시작 화면 및 각 씬으로 이동하는 허브

### GachaScene
단일 뽑기 / 10연 뽑기 수행  
획득 유닛 결과 확인

### InventoryScene
보유 유닛 목록 열람  
유닛 상세 정보 확인 및 강화

### DeckBuilderScene
슬롯 기반 **7유닛 덱 구성**  
덱 저장 / 불러오기

### InGameScene
전투 진행 및 HUD 표시

---

- 현재 프로젝트는 **게임 시스템 프로토타입 단계**로 개발되었습니다.

다음 기능들이 구현되어 있습니다.

- 가챠 시스템
- 유닛 인벤토리
- 덱 빌딩 시스템
- Active Skill 선택 시스템
- AI 기반 전투 프로토타입
- 주요 UI 구조

전투 시스템은 기본적인 로직과 AI 대전이 구현되어 있으며  
현재는 다음 요소들을 추가 개발 예정입니다.

- 유닛 공격 애니메이션
- 스킬 사용 시스템
- 스킬 이펙트
- 전투 연출 개선
