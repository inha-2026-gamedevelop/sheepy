# TheLastRewind - 팀 공용 프로젝트 메모리

이 파일은 **프로젝트 루트**에 있어, 저장소를 여는 모든 팀원의 Claude Code 에이전트가 매 세션 자동으로 읽는다.
프로젝트 상세 문서는 팀 공용 폴더 `claude/`에 있으며, 아래에서 로드/참조한다.
개인 대화/설정(`.claude/`)은 커밋하지 않으며 공유되지 않는다.

## 자동 로드 (import)

아래 문서는 `@` import로 이 파일과 함께 매 세션 자동 로드된다.

@claude/CLAUDE.md

## 팀 공용 문서 (`claude/`, 관련 작업 시 열람)

- `claude/coding-convention.md` - 코딩 컨벤션 전문. 코드 작성/리뷰 전 필독 (변경은 CODEOWNERS @aurenixs 승인 필요)
- `claude/PLAN.md` - 구현 현황 / 우선순위별 남은 작업 / 리팩토링 이력
- `claude/UML.md` - 전체 클래스 구조 UML (Mermaid, 서브시스템별)
- `claude/README.md` - 게임 소개 / 시스템 현황 / 폴더 구조
- `claude/canvas-convention.md` - 모든 씬 메인 캔버스 공통 설정 (Screen Space Overlay, 1920x1080 기준)
- `claude/gamedb.md` - GameDB(SO DB) 데이터 시스템 인수인계 - 구조/사용법/확장 절차/AI 에이전트 규칙
