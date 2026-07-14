# 메인 캔버스(Canvas) 공통 설정 규칙

모든 씬의 **메인 캔버스**는 아래 설정을 따른다. 새 씬 생성 또는 기존 씬의 캔버스 수정 시 이 값과 일치하는지 확인한다.

## Canvas

| 항목 | 값 |
|---|---|
| Render Mode | Screen Space - Overlay |
| Pixel Perfect | 체크 해제 |
| Sort Order | 0 |
| Target Display | Display 1 |
| Additional Shader Channels | Nothing |
| Vertex Color Always In Gamma Color Space | 체크 해제 |

## Canvas Scaler

| 항목 | 값 |
|---|---|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | X: 1920, Y: 1080 |
| Screen Match Mode | Match Width Or Height |
| Match | 0.5 (Width/Height 중간) |
| Reference Pixels Per Unit | 100 |

## Graphic Raycaster

- 메인 캔버스에 기본 포함 (기본값 유지)

## 비고

- 기준 해상도는 FHD(1920x1080) 고정. UI 배치는 이 해상도 기준으로 작업한다
- Match 0.5는 가로/세로 어느 쪽으로 화면비가 변해도 UI가 균형 있게 스케일되도록 하기 위함
- 특수 목적 캔버스(월드 스페이스 UI 등)는 예외지만, 씬의 기본 UI를 담는 메인 캔버스는 반드시 이 규칙을 따른다
