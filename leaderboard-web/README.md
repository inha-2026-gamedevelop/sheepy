# The Last Re:wind Records

보스 클리어 타임 순위와 플레이어별 업적 현황을 보여 주는 React/Vite 웹앱입니다. Unity 프로젝트와 분리된 폴더라 게임 에셋과 코드를 변경하지 않습니다.

## 로컬 실행

```bash
npm install
npm run dev
```

Supabase를 연결하려면 `.env.example`을 `.env`로 복사해 프로젝트 URL과 **anon key**를 채웁니다. 환경 변수가 없거나 조회가 실패하면 화면은 검증용 예시 기록을 표시합니다.

## Supabase 조회 계약

프론트엔드는 다음 공개 읽기 API를 조회합니다.

- `scores`: `username`, `duration_ms`, `created_at` — 사용자별 가장 빠른 `duration_ms`를 순위에 사용
- `players`: `username`, `created_at` — 계정 생성일
- `player_achievements`: `username`, `achievement_id` — 업적 달성 여부

현재 Unity 앱은 `duration_ms`를 보낼 수 있습니다. 배포 전에는 위 세 테이블에 필요한 컬럼이 존재하고, 익명 사용자에게 필요한 `SELECT`만 허용하는 RLS 정책이 설정되어 있는지 확인하세요. 브라우저에는 `service_role` 키를 넣으면 안 됩니다.

## Netlify 배포

1. Netlify에서 이 저장소를 연결하고 **Base directory**를 `leaderboard-web`로 지정합니다.
2. Build command는 `npm run build`, Publish directory는 `dist`입니다. (`netlify.toml`에 포함됨)
3. Site configuration > Environment variables에 `VITE_SUPABASE_URL`, `VITE_SUPABASE_ANON_KEY`를 등록합니다.
4. 배포를 실행합니다. SPA 새로고침을 위한 redirect 규칙도 `netlify.toml`에 포함되어 있습니다.
