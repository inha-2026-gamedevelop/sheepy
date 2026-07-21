alter table public.players
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
