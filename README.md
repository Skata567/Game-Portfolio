# 남윤호 — Unity 클라이언트 / 게임플레이 프로그래머

팀 프로젝트 두 개에서 제가 맡은 `NYH` 파트 코드입니다.
주로 상점, 인벤토리, 도감, 튜토리얼, 장비 같은 UI랑 그 뒤의 데이터를 담당했습니다. 버튼 하나가 제대로 동작하려면 자원이나 진행도, 편성 데이터가 서로 어긋나면 안 되는데, 그 부분을 맞추는 데 신경을 많이 썼습니다.

- 이메일 : rornfl525@gmail.com
- 사용 기술 : Unity 6, C#, Git
- 팀 프로젝트라서 게임 전체 코드는 팀 저장소에 있고, 여기엔 제가 작성한 `Assets/NYH` 폴더만 옮겨서 정리했습니다.

## 시간 없으면 이 5개만 보셔도 됩니다

1. **`StolenTime_스톨른타임/02. Scripts/Inventory/ItemGrid.cs`**
   아이템 크기대로 여러 칸을 차지하는 격자 인벤토리입니다. 한 아이템이 차지한 모든 칸에 같은 참조를 넣어서, 어느 칸을 눌러도 같은 아이템이 잡히게 했습니다.

2. **`StolenTime_스톨른타임/02. Scripts/Equipment/NYHEquipmentController.cs`**
   장비를 갈아끼우거나 강화할 때 스탯이 중복으로 쌓이는 문제가 있었는데, 전체 총합을 다시 계산해서 이전 값과의 차이만 반영하는 식으로 고쳤습니다.

3. **`StolenTime_스톨른타임/02. Scripts/Traps/ITrapEffect.cs`, `GridTrap.cs`**
   함정 효과를 인터페이스로 쪼개서 조합하는 구조입니다. 피해, 상태이상, 텔레포트 같은 효과를 컴포넌트로 붙이기만 하면 새 함정이 됩니다.

4. **`DemonKing_마왕의역습/Script/ShopData/MonsterPurchaseService.cs`**
   유닛을 팔 때 인벤토리 수량만 줄이면 편성 데이터랑 어긋나서, 편성까지 같이 정리하도록 처리했습니다.

5. **`DemonKing_마왕의역습/Script/Tutorial/ClickDebugger.cs`**
   튜토리얼 클릭이 갑자기 안 먹던 적이 있었는데, 마우스가 뭘 누르고 있는지 화면에 전부 뿌려주는 디버그 툴을 만들어서 원인(투명 Canvas가 클릭을 가로채고 있었음)을 찾아 해결했습니다.

## 마왕의 역습 (Demon King's Counterattack)

로그라이크 턴제 전략 RPG · PC · 팀 4명 · 2025.08~11 (2025 G-STAR 출품)
마왕군을 모으고 종족별로 강화해서 인간 진영이랑 싸우는 게임입니다.

플레이 영상 : https://youtu.be/cemIf2itGcE

제가 한 부분 (`DemonKing_마왕의역습/Script/`)
- 유닛 데이터 관리, 상점/경제 (`ShopData/`)
- 인벤토리, 도감 (`Inventory/`)
- 스킬 강화 (`Skill/`)
- 튜토리얼 (`Tutorial/`)
- 전투 로그, 사운드, 로딩 UI (`BattleLog/`, `UI/`, `SceneMove/`)

전투 로직 자체는 다른 팀원이 맡았고, 저는 유닛 데이터와 UI 쪽을 했습니다.

## 스톨른 타임 (Stolen Time)

2D 그리드 로그라이크 액션 RPG · PC · 팀 5명 · 2025.04 ~ 현재 (개발 중)
제한 시간 안에 던전을 돌파하는 게임입니다.

플레이 영상 : https://youtu.be/kZlq0dAkhEo

제가 한 부분 (`StolenTime_스톨른타임/02. Scripts/`)
- 격자 인벤토리, 아이템 사용/타겟팅 (`Inventory/`)
- 장비, 스탯 연동 (`Equipment/`)
- 함정 시스템 (`Traps/`)
- 오디오 (`Audio/`)
- 플레이어 이동, 전투 피드백 (`Player/`)

## 폴더 구성

```
DemonKing_마왕의역습/Script/       마왕의 역습 - 제 담당 코드
StolenTime_스톨른타임/02. Scripts/   스톨른 타임 - 제 담당 코드
```

위 코드는 팀 프로젝트의 일부이며, 제가 작성한 NYH 파트입니다. 게임 전체 저작권은 각 팀에 있습니다.
