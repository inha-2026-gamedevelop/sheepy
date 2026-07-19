<#
.SYNOPSIS
    Assets/05.Sounds 밑의 사운드용 .mp4 파일을 .ogg로 변환한다.

.DESCRIPTION
    Unity는 .mp4를 AudioClip이 아니라 VideoClip으로 임포트하기 때문에
    SoundManager/SoundData(AudioClip[] 기반)에 그대로 쓸 수 없다.
    이 스크립트는 원본 .mp4는 그대로 둔 채, 같은 폴더에 같은 이름의 .ogg를
    추출해서 Unity가 AudioImporter로 임포트하도록 만든다.

.PARAMETER SourcePath
    변환 대상 루트 폴더. 기본값은 Assets/05.Sounds 전체.

.PARAMETER Force
    이미 있는 .ogg도 다시 추출한다. 기본은 없는 것만 변환(멱등 실행).

.EXAMPLE
    Tools\Convert-Mp4ToOgg.ps1
    Assets/05.Sounds 전체를 변환한다.

.EXAMPLE
    Tools\Convert-Mp4ToOgg.ps1 -SourcePath "Assets\05.Sounds\SFX\Player"
    Player SFX 폴더만 변환한다.
#>

param(
    [string]$SourcePath = (Join-Path $PSScriptRoot "..\Assets\05.Sounds"),
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# native exe(ffmpeg) 호출은 이 preference를 따르지 않게 별도 변수로 분리해 사용한다
$nativeErrorAction = "Continue"

# ffmpeg가 PATH에 없으면(방금 winget으로 설치해 이번 세션 PATH가 아직 안 갱신된 경우 등)
# winget 설치 경로에서 직접 찾는다.
function Get-FfmpegPath
{
    $cmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($cmd)
    {
        return $cmd.Source
    }

    $wingetRoot = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
    if (Test-Path $wingetRoot)
    {
        $found = Get-ChildItem $wingetRoot -Recurse -Filter "ffmpeg.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found)
        {
            return $found.FullName
        }
    }

    throw "ffmpeg.exe를 찾을 수 없습니다. 'winget install -e --id Gyan.FFmpeg'로 설치 후 새 셸에서 실행하세요."
}

$ffmpeg = Get-FfmpegPath
$sourceFull = Resolve-Path $SourcePath
Write-Host "ffmpeg: $ffmpeg"
Write-Host "대상 폴더: $sourceFull"

$mp4Files = Get-ChildItem -Path $sourceFull -Recurse -Filter "*.mp4"
Write-Host "발견된 mp4: $($mp4Files.Count)개"

$converted = 0
$skipped   = 0
$failed    = 0

foreach ($file in $mp4Files)
{
    $oggPath = [System.IO.Path]::ChangeExtension($file.FullName, ".ogg")

    if ((Test-Path $oggPath) -and (-not $Force))
    {
        ++$skipped
        continue
    }

    # -vn: 비디오 스트림 제외(오디오만 추출), libvorbis q5 ~ 160kbps 상당 가변비트레이트
    # -loglevel error: stderr에 배너/진행률을 안 찍어야 PowerShell이 native stderr를 에러로 오인하지 않는다
    # native exe 호출 동안은 Stop을 풀어서, stderr에 뭔가 찍혀도 스크립트 전체가 죽지 않고 아래 Test-Path로 성패를 직접 판정한다
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = $nativeErrorAction
    & $ffmpeg -y -hide_banner -loglevel error -i $file.FullName -vn -c:a libvorbis -q:a 5 $oggPath
    $ErrorActionPreference = $prevEap

    if ((Test-Path $oggPath) -and ((Get-Item $oggPath).Length -gt 0))
    {
        ++$converted
        Write-Host "  변환: $($file.Name) -> $(Split-Path $oggPath -Leaf)"
    }
    else
    {
        ++$failed
        Write-Warning "  실패: $($file.Name)"
    }
}

Write-Host ""
Write-Host "완료 - 변환 $converted / 스킵 $skipped / 실패 $failed"
Write-Host "Unity로 돌아가 해당 폴더를 Reimport하면 .ogg가 AudioClip으로 잡힙니다."
