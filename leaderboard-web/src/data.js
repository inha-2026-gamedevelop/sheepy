export const ACHIEVEMENTS = [
  ['first_rewind', '시간을 거스른 자', '처음으로 시간을 되감았다.'],
  ['clone_full_squad', '하나를 위한 모두, 모두를 위한', '분신 3개를 동시에 유지했다.'],
  ['boss_phase1_clear', '도플갱어', '보스 분신 두개 클리어'],
  ['boss_defeated', 'The Last Rewind', '아자토스를 쓰러뜨렸다.'],
  ['death_100', '이러시는 이유가 있을것 아니에요', '플레이어 100번 죽기'],
  ['rewind_100', '타임 루퍼', '되감기 100번 사용'],
  ['first_slow', '느려', '처음으로 슬로우 사용'],
  ['no_rewind', '도대체 왜이러는 걸까요?', '리와인드 없이 보스 클리어'],
  ['domain_expansion', '료이키텐카이', '아자토스의 영역전개 맞기'],
  ['jumpking', '당신은 점프킹', '맵의 가장 높은곳에 있는 아이템 획득'],
  ['afk_5min', '잠만보', '5분 이상 가만히 있기'],
  ['boss_first_death', '어서와, 이런 보스는 처음이지?', '보스에게 첫 죽음'],
  ['hidden_area_found', '진정한 탐험가', '숨겨진 공간을 발견했다'],
  ['radio_all_heard', '주파수 고정', '라디오 5개를 모두 들었다'],
  ['first_fall', '이걸 떨어져?', '처음으로 떨어졌다'],
  ['ending_credits', '아름다운 이별', '엔딩 크레딧을 끝까지 봤다'],
  ['lever_sync', '싱크로나이즈드 올림픽', '레버 2개를 동시에 작동시켰다'],
  ['clone_finisher', '너한테 모든걸 맡긴다', '분신이 보스에게 막타를 넣었다'],
  ['reflect_100', '눈을 뜨세요', '공격 반사에 100번 넘게 공격했다'],
  ['boss_death_500_clear', '중꺾마', '보스에게 500번 이상 죽으면서도 결국 클리어했다'],
  ['boss_near_death_loss', '어라? 왜 눈물이 나지?', '보스 체력을 10% 아래로 남기고 죽었다'],
].map(([id, title, description]) => ({ id, title, description }))

const demoIds = ACHIEVEMENTS.map(({ id }) => id)

export const DEMO_PLAYERS = [
  { username: 'Memento', createdAt: '2026-06-03T10:20:00Z', clearTimeMs: 163840, achievements: demoIds },
  { username: 'Re:Turn', createdAt: '2026-06-08T06:45:00Z', clearTimeMs: 169210, achievements: demoIds.slice(0, 19) },
  { username: 'sheepy', createdAt: '2026-06-01T12:00:00Z', clearTimeMs: 174605, achievements: demoIds.slice(0, 16) },
  { username: 'Astra', createdAt: '2026-06-12T03:32:00Z', clearTimeMs: 181892, achievements: demoIds.slice(0, 14) },
  { username: 'Fable', createdAt: '2026-06-17T09:15:00Z', clearTimeMs: 186476, achievements: demoIds.slice(0, 12) },
  { username: 'Lumi', createdAt: '2026-06-20T01:10:00Z', clearTimeMs: 193501, achievements: demoIds.slice(0, 11) },
  { username: 'Sora', createdAt: '2026-06-22T08:05:00Z', clearTimeMs: 201928, achievements: demoIds.slice(0, 9) },
]

const getEnv = (name) => import.meta.env[name]?.trim()

async function request(path) {
  const baseUrl = getEnv('VITE_SUPABASE_URL')
  const anonKey = getEnv('VITE_SUPABASE_ANON_KEY')
  if (!baseUrl || !anonKey) {
    throw new Error('Supabase 환경 변수가 설정되지 않았습니다.')
  }

  const response = await fetch(`${baseUrl}/rest/v1/${path}`, {
    headers: { apikey: anonKey, Authorization: `Bearer ${anonKey}` },
  })
  if (!response.ok) {
    throw new Error(`데이터를 불러오지 못했습니다. (${response.status})`)
  }
  return response.json()
}

export async function loadLeaderboard() {
  const [scoresResult, playersResult, achievementsResult] = await Promise.allSettled([
    request('scores?select=username,duration_ms,created_at&duration_ms=not.is.null&order=duration_ms.asc'),
    request('players?select=username,created_at'),
    request('player_achievements?select=username,achievement_id'),
  ])

  if (scoresResult.status !== 'fulfilled') {
    throw scoresResult.reason
  }

  const playersByName = new Map(
    (playersResult.status === 'fulfilled' ? playersResult.value : []).map((player) => [player.username, player]),
  )
  const achievementsByName = new Map()
  if (achievementsResult.status === 'fulfilled') {
    achievementsResult.value.forEach(({ username, achievement_id: id }) => {
      const unlocked = achievementsByName.get(username) ?? []
      unlocked.push(id)
      achievementsByName.set(username, unlocked)
    })
  }

  const bestScoreByName = new Map()
  scoresResult.value.forEach((score) => {
    if (!bestScoreByName.has(score.username)) bestScoreByName.set(score.username, score)
  })

  return [...bestScoreByName.values()]
    .map((score) => ({
      username: score.username,
      createdAt: playersByName.get(score.username)?.created_at ?? score.created_at,
      clearTimeMs: Number(score.duration_ms),
      achievements: achievementsByName.get(score.username) ?? [],
    }))
    .sort((first, second) => first.clearTimeMs - second.clearTimeMs)
}
