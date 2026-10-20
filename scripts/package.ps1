param([Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version, [string]$SigningMetadata)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$config = Get-Content release-config.json -Raw | ConvertFrom-Json
function Check-Exit { if ($LASTEXITCODE -ne 0) { throw "Build command failed ($LASTEXITCODE)." } }
dotnet tool restore
Check-Exit
dotnet test AgeCheck.slnx -c Release
Check-Exit
dotnet publish src/AgeCheck/AgeCheck.csproj -c Release -r $config.runtime --self-contained true -p:Version=$Version -o artifacts/publish
Check-Exit
$packArgs = @('pack', '--packId', $config.packId, '--packTitle', 'AgeCheck', '--packAuthors', 'AgeCheck', '--packVersion', $Version, '--packDir', 'artifacts/publish', '--mainExe', 'AgeCheck.exe', '--channel', $config.channel, '--runtime', $config.runtime, '--outputDir', 'artifacts/releases', '--releaseNotes', 'RELEASE_NOTES.md')
if ($SigningMetadata) { $packArgs += @('--azureTrustedSignFile', $SigningMetadata) }
$packArgs += @('--icon', 'src/AgeCheck/Assets/AgeCheck.ico')
dotnet vpk @packArgs
Check-Exit
