alter table public.players
    add column if not exists device_id    text,
    add column if not exists last_scene   text,
    add column if not exists pos_x        real,
    add column if not exists pos_y        real,
    add column if not exists pos_z        real,
    add column if not exists facing_dir        smallint,
    add column if not exists use_default_spawn boolean     not null default false,
    add column if not exists boss_cleared       boolean     not null default false,
    add column if not exists updated_at         timestamptz not null default now();

create or replace function public.touch_players_updated_at()
returns trigger as $$
begin
    new.updated_at = now();
    return new;
end;
$$ language plpgsql;

drop trigger if exists trg_players_updated_at on public.players;
create trigger trg_players_updated_at
    before update on public.players
    for each row execute function public.touch_players_updated_at();

-- 기기당 계정 1개: device_id 유니크 (null 기존 행은 제외하는 부분 유니크 인덱스)
create unique index if not exists players_device_id_unique
    on public.players (device_id)
    where device_id is not null;

create table if not exists public.player_achievements (
    username       text        not null references public.players(username) on delete cascade,
    achievement_id text        not null,
    unlocked_at    timestamptz not null default now(),
    primary key (username, achievement_id)
);

create index if not exists idx_player_achievements_username
    on public.player_achievements (username);

alter table public.player_achievements enable row level security;

drop policy if exists player_achievements_select on public.player_achievements;
create policy player_achievements_select
    on public.player_achievements for select using (true);

drop policy if exists player_achievements_upsert on public.player_achievements;
create policy player_achievements_upsert
    on public.player_achievements for insert with check (true);

-- 설정 - "업적 기록 제거" 기능(BackendMirror.MirrorClearAchievements)이 DELETE 요청을 보낸다.
drop policy if exists player_achievements_delete on public.player_achievements;
create policy player_achievements_delete
    on public.player_achievements for delete using (true);
