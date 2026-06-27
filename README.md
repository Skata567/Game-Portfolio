# 남윤호 · Unity Client & Gameplay Programmer — 코드 포트폴리오

Unity / C# 클라이언트 프로그래머로 참여한 **두 개의 팀 프로젝트**에서, 제가 담당한 `NYH` 파트의 실제 구현 코드입니다.
UI를 단순 표시 계층이 아니라 **게임 데이터·진행 상태·플레이어 피드백을 연결하는 시스템**으로 설계·구현하는 데 집중했습니다.

- **이메일** · rornfl525@gmail.com
- **기술 스택** · Unity 6 · C# · Git
- 본 저장소는 팀 프로젝트 중 **본인 담당(`Assets/NYH`) 코드만 발췌**해 정리한 것입니다. 원본은 팀 공용 저장소에서 관리됩니다.

---

## ⭐ 먼저 볼 5개 파일 (바쁘면 이것만)

1. **`StolenTime_스톨른타임/02. Scripts/Inventory/ItemGrid.cs`** — 아이템 크기 기반 2D 격자 인벤토리. 점유 칸 참조 관리, 경계·겹침 검사.
2. **`StolenTime_스톨른타임/02. Scripts/Equipment/NYHEquipmentController.cs`** — 장비 스탯을 총합 재계산 후 **차이(diff)만 반영**해 중복·누락 방지.
3. **`StolenTime_스톨른타임/02. Scripts/Traps/ITrapEffect.cs` + `GridTrap.cs`** — `ScriptableObject` + 인터페이스 조합형 함정 효과 시스템.
4. **`DemonKing_마왕의역습/Script/ShopData/MonsterPurchaseService.cs`** — 판매 시 인벤토리·전투 편성 데이터를 함께 갱신해 일관성 유지.
5. **`DemonKing_마왕의역습/Script/Tutorial/ClickDebugger.cs`** — 클릭이 안 먹던 버그를 `EventSystem.RaycastAll`로 추적한 디버그 툴.

---

## 📦 프로젝트 1 — 마왕의 역습 (Demon King's Counterattack)

> 로그라이크 턴제 전략 RPG · PC (Windows) · Unity 6000.0.58f2 · 2025 G-STAR 출품 팀 프로젝트
> 마왕군을 모집·강화·편성해 노드형 맵을 진행하며 인간 진영과 전투하는 전략 RPG.

🎬 **게임플레이 영상** · https://youtu.be/cemIf2itGcE

| 시스템 | 핵심 파일 | 설명 |
|--------|-----------|------|
| 튜토리얼 | `Script/Tutorial/TutorialManager.cs` | 14단계 enum 상태 기반 튜토리얼. `DontDestroyOnLoad`로 씬 전환에도 상태 유지 |
| 상점/경제 | `Script/ShopData/MonsterPurchaseService.cs` | 자원·스테이지·튜토리얼 검증 후 구매. 판매 시 편성 데이터까지 동기화 |
| 스킬 강화 | `Script/Skill/SkillUpgrade.cs` | 순차 해금 + 자원 검증 + 전역 상태 저장으로 씬 전환 후 레벨 유지 |
| 도감/인벤토리 | `Script/Inventory/` | 획득/조우 여부에 따른 정보 공개, 도감 진행감 강화 |
| 전투 로그/사운드 | `Script/BattleLog/`, `Script/UI/`, `Script/SceneMove/` | 전투 로그 자동 스크롤, 사운드, 로딩/인트로 연출 |

## 📦 프로젝트 2 — 스톨른 타임 (Stolen Time)

> 2D 그리드 기반 로그라이크 액션 RPG · PC (Windows) · Unity · C#
> 제한 시간 안에 던전을 돌파하는 탐험·성장 루프.

🎬 **게임플레이 영상** · https://youtu.be/kZlq0dAkhEo

| 시스템 | 핵심 파일 | 설명 |
|--------|-----------|------|
| 격자 인벤토리 | `02. Scripts/Inventory/ItemGrid.cs` | 아이템 크기 기반 2D 격자 배치. 점유 칸에 같은 참조 기록, 경계·겹침 검사 |
| 타겟팅 UX | `02. Scripts/Inventory/InventoryTargetingController.cs` | 투척/주문서 등 "사용 후 대상 선택" 상태 머신, 중복 입력 차단 |
| 장비/스탯 동기화 | `02. Scripts/Equipment/NYHEquipmentController.cs` | 총합 재계산 후 이전 적용값과의 **차이(diff)만 반영**해 중복·누락 방지 |
| 함정 시스템 | `02. Scripts/Traps/ITrapEffect.cs`, `GridTrap.cs`, `TrapSystem.cs` | `ScriptableObject` + `ITrapEffect` 조합형 효과(피해·상태이상·지형·텔레포트·소환·지연) |
| 오디오/플레이어 | `02. Scripts/Audio/`, `02. Scripts/Player/` | AudioMixer 볼륨 저장, BGM 크로스페이드, 그리드 이동/전투 피드백 |

---

## 🛠 기술적으로 신경 쓴 점

- **데이터 일관성** — 구매·판매·편성이 얽히는 구간에서 상태 변경 기준을 명확히 두어 인벤토리/편성 데이터 불일치를 방지했습니다.
- **확장 가능한 구조** — `ScriptableObject`와 인터페이스(`ITrapEffect`) 조합으로 기획 변경과 신규 콘텐츠 추가에 유연하게 대응했습니다.
- **Unity 생명주기 관리** — 씬 전환 시 `DontDestroyOnLoad` 루트 분리, `sceneLoaded` 이벤트 구독으로 상태를 안전하게 유지했습니다.

## 📁 폴더 구조

```
DemonKing_마왕의역습/Script/   ← 마왕의 역습, 본인 담당(NYH) 코드
StolenTime_스톨른타임/02. Scripts/ ← 스톨른 타임, 본인 담당(NYH) 코드
```

> 본 저장소의 코드는 팀 프로젝트의 일부이며, 본인이 작성한 `NYH` 파트입니다. 프로젝트 전체 저작권은 각 팀에 있습니다.
