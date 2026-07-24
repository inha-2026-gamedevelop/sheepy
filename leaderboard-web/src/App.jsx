import { useEffect, useMemo, useState } from 'react'
import { ACHIEVEMENTS, DEMO_PLAYERS, loadLeaderboard } from './data.js'

function Icon({ name, size = 20 }) {
  const paths = {
    clock: <><circle cx="12" cy="12" r="8.5" /><path d="M12 7v5l3.2 2" /></>,
    trophy: <><path d="M8 4h8v4.5A4 4 0 0 1 12 12.5 4 4 0 0 1 8 8.5V4Z" /><path d="M8 6H4.5v1a3.5 3.5 0 0 0 3.3 3.5M16 6h3.5v1a3.5 3.5 0 0 1-3.3 3.5M12 12.5V17M8.5 20h7" /></>,
    spark: <path d="m12 2 1.85 6.15L20 10l-6.15 1.85L12 18l-1.85-6.15L4 10l6.15-1.85L12 2Z" />,
    calendar: <><rect x="4" y="5.5" width="16" height="14" rx="2" /><path d="M8 3.5v4M16 3.5v4M4 10h16" /></>,
    arrow: <path d="m9 18 6-6-6-6" />,
    check: <path d="m5 12 4.2 4.2L19 6.5" />,
    refresh: <><path d="M20 11a8 8 0 0 0-14.9-3.9L3 9.5M4 13a8 8 0 0 0 14.9 3.9L21 14.5" /><path d="M3 4v5.5h5.5M21 20v-5.5h-5.5" /></>,
  }
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>
}

function formatTime(milliseconds) {
  const totalSeconds = Math.floor(milliseconds / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = String(totalSeconds % 60).padStart(2, '0')
  const centiseconds = String(Math.floor((milliseconds % 1000) / 10)).padStart(2, '0')
  return `${minutes}:${seconds}.${centiseconds}`
}

function formatDate(value) {
  if (!value) return '기록 없음'
  return new Intl.DateTimeFormat('ko-KR', { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(value))
}

function rankLabel(rank) {
  return String(rank).padStart(2, '0')
}

function App() {
  const [players, setPlayers] = useState(DEMO_PLAYERS)
  const [selectedName, setSelectedName] = useState(DEMO_PLAYERS[0].username)
  const [status, setStatus] = useState('demo')
  const [error, setError] = useState('')

  const refresh = async () => {
    setStatus('loading')
    setError('')
    try {
      const livePlayers = await loadLeaderboard()
      if (!livePlayers.length) throw new Error('등록된 클리어 기록이 없습니다.')
      setPlayers(livePlayers)
      setSelectedName((current) => livePlayers.some((player) => player.username === current) ? current : livePlayers[0].username)
      setStatus('live')
    } catch (reason) {
      setStatus('demo')
      setError(reason instanceof Error ? reason.message : '기록을 불러오지 못했습니다.')
    }
  }

  useEffect(() => { refresh() }, [])

  const rankedPlayers = useMemo(
    () => [...players].sort((first, second) => first.clearTimeMs - second.clearTimeMs),
    [players],
  )
  const selectedPlayer = rankedPlayers.find((player) => player.username === selectedName) ?? rankedPlayers[0]
  const selectedRank = rankedPlayers.findIndex((player) => player.username === selectedPlayer?.username) + 1
  const averageTime = rankedPlayers.reduce((total, player) => total + player.clearTimeMs, 0) / rankedPlayers.length

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#top" aria-label="The Last Rewind Records 홈">
          <span className="brand-mark"><span /></span>
          <span><strong>THE LAST RE:WIND</strong><small>RECORDS</small></span>
        </a>
        <button className="refresh-button" type="button" onClick={refresh} disabled={status === 'loading'} aria-label="기록 새로고침">
          <Icon name="refresh" size={17} /> <span>{status === 'loading' ? '불러오는 중' : '새로고침'}</span>
        </button>
      </header>

      <main id="top" className="content">
        <section className="intro" aria-labelledby="page-title">
          <div>
            <p className="eyebrow">BOSS CLEAR ARCHIVE</p>
            <h1 id="page-title">시간을 거스른<br /><em>기록들.</em></h1>
            <p className="intro-copy">아자토스를 쓰러뜨린 플레이어들의 가장 빠른 순간과, 그들이 남긴 여정을 확인하세요.</p>
          </div>
          <div className="hero-stat" aria-label={`현재 1위 ${rankedPlayers[0]?.username ?? ''}, ${formatTime(rankedPlayers[0]?.clearTimeMs ?? 0)}`}>
            <div className="hero-stat-icon"><Icon name="trophy" size={25} /></div>
            <div><span>FASTEST REWIND</span><strong>{formatTime(rankedPlayers[0]?.clearTimeMs ?? 0)}</strong><small>{rankedPlayers[0]?.username ?? '—'} · CURRENT #1</small></div>
          </div>
        </section>

        <section className="summary-grid" aria-label="기록 요약">
          <article className="summary-card"><span className="summary-icon"><Icon name="trophy" /></span><div><small>REGISTERED RUNS</small><strong>{rankedPlayers.length}</strong></div></article>
          <article className="summary-card"><span className="summary-icon"><Icon name="clock" /></span><div><small>AVERAGE CLEAR</small><strong>{formatTime(averageTime)}</strong></div></article>
          <article className="summary-card"><span className="summary-icon"><Icon name="spark" /></span><div><small>ACHIEVEMENTS</small><strong>{ACHIEVEMENTS.length}</strong></div></article>
        </section>

        {error && <p className="data-note" role="status">실시간 연결 전까지 예시 기록을 표시합니다. {error}</p>}

        <section className="board-layout" aria-label="보스 클리어 랭킹">
          <div className="ranking-panel">
            <div className="section-heading"><div><p className="eyebrow">LEADERBOARD</p><h2>Boss Clear Ranking</h2></div><span>{status === 'live' ? 'LIVE DATA' : 'PREVIEW DATA'}</span></div>
            <ol className="ranking-list">
              {rankedPlayers.map((player, index) => {
                const isSelected = player.username === selectedPlayer?.username
                return <li key={player.username}>
                  <button type="button" className={`rank-row ${isSelected ? 'is-selected' : ''}`} onClick={() => setSelectedName(player.username)} aria-pressed={isSelected}>
                    <span className={`rank-number rank-${index + 1}`}>{rankLabel(index + 1)}</span>
                    <span className="player-avatar" aria-hidden="true">{player.username.slice(0, 1).toUpperCase()}</span>
                    <span className="rank-player"><strong>{player.username}</strong><small><Icon name="calendar" size={13} /> {formatDate(player.createdAt)} 가입</small></span>
                    <span className="rank-achievements"><Icon name="spark" size={15} /> {player.achievements.length}/{ACHIEVEMENTS.length}</span>
                    <time className="rank-time">{formatTime(player.clearTimeMs)}</time>
                    <Icon name="arrow" size={18} />
                  </button>
                </li>
              })}
            </ol>
          </div>

          {selectedPlayer && <PlayerDetail player={selectedPlayer} rank={selectedRank} />}
        </section>
      </main>
      <footer>THE LAST RE:WIND · PLAYER RECORDS <span>·</span> 기록은 클리어 타임 기준으로 정렬됩니다.</footer>
    </div>
  )
}

function PlayerDetail({ player, rank }) {
  const unlocked = new Set(player.achievements)
  const completion = Math.round((unlocked.size / ACHIEVEMENTS.length) * 100)
  return <aside className="detail-panel" aria-labelledby="profile-title">
    <div className="profile-top">
      <span className="profile-rank">RANK #{rankLabel(rank)}</span>
      <span className="profile-avatar" aria-hidden="true">{player.username.slice(0, 1).toUpperCase()}</span>
      <h2 id="profile-title">{player.username}</h2>
      <p><Icon name="calendar" size={15} /> {formatDate(player.createdAt)}에 모험 시작</p>
    </div>
    <div className="personal-best"><span>PERSONAL BEST</span><strong>{formatTime(player.clearTimeMs)}</strong><small>AZATHOTH · BOSS CLEAR</small></div>
    <section className="achievement-section" aria-labelledby="achievement-title">
      <div className="achievement-heading"><div><p className="eyebrow">COLLECTION</p><h3 id="achievement-title">업적 진행도</h3></div><strong>{unlocked.size}<small> / {ACHIEVEMENTS.length}</small></strong></div>
      <div className="progress-track" role="progressbar" aria-label="업적 진행도" aria-valuemin="0" aria-valuemax={ACHIEVEMENTS.length} aria-valuenow={unlocked.size}><span style={{ width: `${completion}%` }} /></div>
      <p className="progress-copy">전체 업적의 {completion}%를 달성했습니다.</p>
      <ul className="achievement-list">
        {ACHIEVEMENTS.map((achievement) => {
          const isUnlocked = unlocked.has(achievement.id)
          return <li className={isUnlocked ? 'is-unlocked' : 'is-locked'} key={achievement.id}>
            <span className="achievement-state">{isUnlocked ? <Icon name="check" size={16} /> : <span />}</span>
            <span><strong>{isUnlocked ? achievement.title : '숨겨진 기억'}</strong><small>{isUnlocked ? achievement.description : '아직 발견하지 못한 업적입니다.'}</small></span>
          </li>
        })}
      </ul>
    </section>
  </aside>
}

export default App
