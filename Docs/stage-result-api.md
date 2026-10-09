# 판 결과 제출 API 명세 (리더보드)

클라이언트가 스테이지 한 판을 끝낼 때마다 리더보드 서버로 보내는 데이터의 계약입니다.

> 이 문서는 클라이언트 코드와 같이 고칩니다. 아래 파일을 고치면 이 문서도 함께 고칩니다.
> - 요청 DTO: `Assets/Runtime/Scripts/HTTP/StageResultDTO.cs`
> - 키 목록: `Assets/Runtime/Scripts/Stage/StageStatKeys.cs`
> - 변환: `Assets/Runtime/Scripts/HTTP/StageResultMapper.cs`

## 1. 엔드포인트 (제안)

| 항목 | 값 |
|---|---|
| 메서드 | `POST` |
| 경로 | `/rank/stage-results` (제안, 서버에서 확정해 주세요) |
| 인증 | `Authorization: Bearer {accessToken}` (기존 `/auth/login`의 토큰) |
| 본문 | `application/json`, 아래 `StageResultRequest` |

클라이언트는 경로가 정해지면 `ApiClient.STAGE_RESULT_PATH` 한 줄만 채웁니다. 그 전까지는 결과를 로컬에 쌓아 둡니다.

## 2. 클라이언트 동작 (서버가 알아야 할 것)

- 판이 끝나면 결과를 **먼저 로컬 파일에 저장**하고 전송합니다. 실패하면 남겨 두었다가 다음 판 종료나 다음 실행 때 다시 보냅니다. 그래서 **같은 판이 여러 번 올 수 있습니다.**
- `runId`(GUID)는 판마다 하나이며 재전송해도 바뀌지 않습니다. 서버는 `runId`로 중복을 걸러 주세요(멱등 키). 이미 받은 `runId`가 다시 오면 `2xx`를 돌려주면 됩니다.
- 응답 코드별 클라이언트 처리:

| 응답 | 클라이언트 처리 |
|---|---|
| `2xx` | 로컬 보관본 삭제 |
| `400`, `422` | 계약 오류로 보고 재전송하지 않음(별도 폴더로 이동) |
| `401` | 보관하고 다음에 재전송 (토큰 갱신 후) |
| `5xx`, 네트워크 오류 | 보관하고 다음에 재전송 |

- 계정은 토큰으로 판단합니다. 본문에는 계정 id가 없습니다.
- 클리어(`cleared: true`)와 실패(`cleared: false`)를 모두 보냅니다. 랭킹에 무엇을 반영할지는 서버가 정합니다.
- **점수는 보내지 않습니다.** 원본 수치만 보내고, 점수와 순위 계산은 서버 책임입니다. DPS와 비율도 아래 원본 값으로 계산할 수 있습니다.

## 3. 요청 본문 `StageResultRequest`

| 필드 | 타입 | 필수 | 설명 |
|---|---|---|---|
| `runId` | string (GUID) | O | 판 고유 id, 멱등 키 |
| `schemaVersion` | int | O | 계약 버전. 현재 `1` |
| `clientVersion` | string | O | 게임 빌드 버전 (`Application.version`) |
| `clearedAtUtc` | string | O | 판이 끝난 시각, ISO-8601 UTC (`2026-10-09T12:34:56.789Z`) |
| `playerSlot` | int | O | 판 안의 플레이어 번호. 지금은 항상 `1`, 협동 대비 필드. 계정 id 아님 |
| `stageId` | string | O | 스테이지 키. 스테이지별 리더보드 구분에 사용 |
| `cleared` | bool | O | 클리어면 `true`, 사망이면 `false` |
| `elapsedSeconds` | float | O | 생존(진행) 시간, 초 |
| `killCount` | int | O | 몬스터 처치 수 |
| `reachedLevel` | int | O | 판 끝 플레이어 레벨 |
| `playableId` | string | O | 고른 트레이너 데이터 id. 없으면 `""` |
| `playerStats` | `StatEntry[]` | O | 판 끝 플레이어 스탯 (5장) |
| `counters` | `StatEntry[]` | O | 판 카운터 (4장). 모르는 키는 무시해 주세요 |
| `currenciesGained` | `Currency[]` | O | 이번 판에 들어온 재화 총량 (쓴 것 포함) |
| `currenciesHeld` | `Currency[]` | O | 판 끝에 남은 스테이지 재화 |
| `party` | `PartyMember[]` | O | 판 끝 장착 포켓몬 (최대 6) |
| `unitDamages` | `UnitDamage[]` | O | 포켓몬 개체별 피해 통계 |

배열은 비어 있어도 `null`이 아니라 `[]`로 옵니다.

### `StatEntry`

| 필드 | 타입 | 설명 |
|---|---|---|
| `key` | string | 키 (4장, 5장) |
| `value` | float | 값 |

### `Currency`

| 필드 | 타입 | 설명 |
|---|---|---|
| `currencyId` | string | `monster_ball`, `super_ball`, `hyper_ball`, `master_ball`, `pocket_dollar` |
| `amount` | int | 수량 |

### `PartyMember`

| 필드 | 타입 | 설명 |
|---|---|---|
| `slot` | int | 장착 칸 0~5 |
| `memberId` | int | 판 안의 포켓몬 개체 번호. `unitDamages.memberId`와 이어짐 |
| `monsterId` | string | 포켓몬 데이터 id (에셋 이름). 판마다 바뀌지 않음 |
| `dexNumber` | int | 도감 번호. 메가진화 등은 같은 번호일 수 있음 |
| `upgradeLevel` | int | 성 1~3 |

### `UnitDamage`

포켓몬 개체 하나가 한 줄입니다. 판 중간에 교체하면 새 줄이 생기고, 진화와 성 올리기는 같은 줄로 이어집니다. 판 중간에 빠진 포켓몬도 남습니다.

| 필드 | 타입 | 설명 |
|---|---|---|
| `memberId` | int | 개체 번호. `0`이면 포켓몬에 귀속되지 않은 피해(능력 종류별 한 줄) |
| `monsterId` | string | 마지막 포켓몬 데이터 id. `memberId`가 0이면 `""` |
| `dexNumber` | int | 마지막 도감 번호. `memberId`가 0이면 `0` |
| `upgradeLevel` | int | 마지막 성 |
| `abilityType` | string | 공격 능력 이름. 예: `FireAttack`, `WaterAttackEvolution` |
| `totalDamage` | float | 판 전체 누적 피해 (넘친 피해 제외) |
| `secondsInParty` | float | 파티에 있던 시간, 초 |
| `recentDamage` | float | 마지막 60초 피해 |
| `recentSeconds` | float | 마지막 60초 중 파티에 있던 시간 |
| `inPartyAtEnd` | bool | 판이 끝날 때 파티에 있었는지 |

계산 예: 판 전체 DPS = `totalDamage / secondsInParty`, 파티 비율 = `secondsInParty / elapsedSeconds`.

## 4. 카운터 키 (`counters`)

| 키 | 뜻 |
|---|---|
| `kills` | 몬스터 처치 수 (`killCount`와 같음) |
| `reachedLevel` | 도달 레벨 (`reachedLevel`과 같음) |
| `bossKills` | 보스 처치 수 |
| `expGained` | 얻은 경험치 (배율 적용 후) |
| `damageTaken` | 받은 피해 |
| `healed` | 회복량 |
| `shopPurchases` | 상점 구매 횟수 |
| `syntheses` | 합성 횟수 |
| `sells` | 판매한 마릿수 |

카운터는 앞으로 늘어납니다(예: 상자 열기). 서버에는 모르는 키를 오류로 처리하지 말고 저장하거나 무시해 주세요. 값이 0인 카운터는 빠질 수 있습니다.

## 5. 플레이어 스탯 키 (`playerStats`)

| 키 | 뜻 | 단위 |
|---|---|---|
| `maxHp` | 최대 체력 | 체력 |
| `hpRegen` | 초당 체력 회복 | 체력/초 |
| `damageMultiplier` | 주는 피해 배율 | 1.0 = 100% |
| `cooldownMultiplier` | 쿨다운 배율 | 1.0 = 100% |
| `projectileMultiplier` | 투사체 수 배율 | 1.0 = 100% |
| `projectileSpeedMultiplier` | 투사체 속도 배율 | 1.0 = 100% |
| `sizeMultiplier` | 크기 배율 | 1.0 = 100% |
| `durationMultiplier` | 지속 시간 배율 | 1.0 = 100% |
| `moveSpeed` | 이동 속도 | 유닛/초 |
| `magnetRadius` | 자석 범위 | 유닛 |
| `xpMultiplier` | 경험치 배율 | 1.0 = 100% |
| `damageReductionPercent` | 피해 감소 | 0~100 (%) |
| `receivedDamageMultiplier` | 받는 피해 배율 | 1.0 = 100% |

## 6. 예시

요청:

```json
{
  "runId": "0f8b3c2e-7a41-4b8e-9d0a-5c1e2f3a4b5c",
  "schemaVersion": 1,
  "clientVersion": "0.1.0",
  "clearedAtUtc": "2026-10-09T12:34:56.789Z",
  "playerSlot": 1,
  "stageId": "stage_01",
  "cleared": true,
  "elapsedSeconds": 1054.2,
  "killCount": 8979,
  "reachedLevel": 39,
  "playableId": "Trainer_Red",
  "playerStats": [
    { "key": "maxHp", "value": 1032.0 },
    { "key": "damageMultiplier", "value": 1.5 }
  ],
  "counters": [
    { "key": "expGained", "value": 3005.0 },
    { "key": "shopPurchases", "value": 12.0 },
    { "key": "bossKills", "value": 2.0 },
    { "key": "kills", "value": 8979.0 },
    { "key": "reachedLevel", "value": 39.0 }
  ],
  "currenciesGained": [
    { "currencyId": "monster_ball", "amount": 433 }
  ],
  "currenciesHeld": [
    { "currencyId": "monster_ball", "amount": 21 },
    { "currencyId": "pocket_dollar", "amount": 0 }
  ],
  "party": [
    { "slot": 1, "memberId": 1, "monsterId": "Charizard", "dexNumber": 6, "upgradeLevel": 2 }
  ],
  "unitDamages": [
    {
      "memberId": 1,
      "monsterId": "Charizard",
      "dexNumber": 6,
      "upgradeLevel": 2,
      "abilityType": "FireAttack",
      "totalDamage": 813300.0,
      "secondsInParty": 1054.2,
      "recentDamage": 52000.0,
      "recentSeconds": 60.0,
      "inPartyAtEnd": true
    }
  ]
}
```

응답 (제안, 서버에서 확정해 주세요):

```json
{ "runId": "0f8b3c2e-7a41-4b8e-9d0a-5c1e2f3a4b5c", "accepted": true }
```

## 7. 버전 규칙

- 필드를 **추가**하는 것은 버전을 올리지 않습니다. 서버는 모르는 필드를 무시해 주세요.
- 필드를 빼거나, 이름이나 뜻을 바꾸면 `schemaVersion`을 올리고 이 문서에 변경 내역을 적습니다.

| 버전 | 변경 |
|---|---|
| 1 | 최초 |
