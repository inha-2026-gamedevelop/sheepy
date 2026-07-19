<#
.SYNOPSIS
    Assets/05.Sounds 밑에서, 같은 이름의 .ogg로 이미 변환된 .mp4(+.meta)를 삭제한다.

.DESCRIPTION
    Convert-Mp4ToOgg.ps1로 오디오를 추출한 뒤, 더는 쓸 일 없는 원본 .mp4를 정리한다.
    안전장치로 같은 폴더에 "같은 이름 + .ogg"가 실제로 존재하고 크기가 0보다 클 때만 삭제 대상으로 잡는다.
    .ogg가 없는 mp4(아직 변환 안 했거나 변환 실패)는 건너뛰고 경고만 남긴다.
    Unity가 나중에 헤매지 않도록 .mp4와 짝인 .mp4.meta도 함께 지운다.

.PARAMETER SourcePath
    정리 대상 루트 폴더. 기본값은 Assets/05.Sounds 전체.

.PARAMETER WhatIf
    실제로 지우지 않고 무엇을 지울지만 보여준다.

.EXAMPLE
    Tools\Remove-ConvertedMp4.ps1 -WhatIf
    삭제 대상만 미리 확인한다.

.EXAMPLE
    Tools\Remove-ConvertedMp4.ps1
    Assets/05.Sounds 전체에서 변환 완료된 mp4를 실제로 삭제한다.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SourcePath = ""
)

# CmdletBinding(SupportsShouldProcess) 조합에서는 param 기본값 평가 시점에 $PSScriptRoot가 비어 있을 수 있어
# 본문에서 별도로 처리한다.
if ([string]::IsNullOrEmpty($SourcePath))
{
    $SourcePath = Join-Path $PSScriptRoot "..\Assets\05.Sounds"
}

$sourceFull = Resolve-Path $SourcePath
Write-Host "대상 폴더: $sourceFull"

$mp4Files = Get-ChildItem -Path $sourceFull -Recurse -Filter "*.mp4"
Write-Host "발견된 mp4: $($mp4Files.Count)개"

$removed        = 0
$skippedNoOgg   = 0

foreach ($file in $mp4Files)
{
    $oggPath = [System.IO.Path]::ChangeExtension($file.FullName, ".ogg")

    if (-not (Test-Path $oggPath) -or ((Get-Item $oggPath).Length -le 0))
    {
        ++$skippedNoOgg
        Write-Warning "  건너뜀(ogg 없음/비어있음): $($file.Name)"
        continue
    }

    $metaPath = "$($file.FullName).meta"

    if ($PSCmdlet.ShouldProcess($file.FullName, "Remove mp4 (+ .meta)"))
    {
        Remove-Item -LiteralPath $file.FullName -Force
        if (Test-Path $metaPath)
        {
            Remove-Item -LiteralPath $metaPath -Force
        }
        ++$removed
        Write-Host "  삭제: $($file.Name)"
    }
}

Write-Host ""
Write-Host "완료 - 삭제 $removed / ogg 없어서 보존 $skippedNoOgg"
Write-Host "Unity 에디터로 돌아가면 사라진 mp4에 대한 누락 참조 경고가 뜰 수 있으니, SoundDB 등에서 mp4를 직접 참조하던 곳이 없었는지 확인하세요."
