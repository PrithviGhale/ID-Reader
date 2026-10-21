$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$probe = Join-Path $PSScriptRoot 'WpfEditorProbe.targets'
$intermediate = 'obj/EditorProbe/' + [Guid]::NewGuid().ToString('N') + '/'
$output = & dotnet msbuild src/AgeCheck/AgeCheck.csproj -t:Compile `
    -p:DesignTimeBuild=true -p:BuildingInsideVisualStudio=true -p:BuildingProject=false `
    -p:SkipCompilerExecution=true -p:ProvideCommandLineArgs=true -p:BuildProjectReferences=false `
    "-p:IntermediateOutputPath=$intermediate" "-p:CustomAfterMicrosoftCommonTargets=$probe" `
    -verbosity:minimal -getItem:Compile
if ($LASTEXITCODE -ne 0) { throw 'Editor markup generation failed.' }
$text = ($output | ForEach-Object { $_.ToString() }) -join "`n"
$start = $text.IndexOf('{')
if ($start -lt 0) { throw 'Editor build did not return its source files.' }
if ($text.Substring(0, $start) -match '\b(warning|error)\b') { throw $text.Substring(0, $start) }
$result = $text.Substring($start) | ConvertFrom-Json
$generated = @($result.Items.Compile | Where-Object { $_.Identity -match 'MainWindow\.g(?:\.i)?\.cs$' })
if ($generated.Count -ne 1) { throw 'Editor build must provide exactly one generated MainWindow definition.' }
$code = [IO.File]::ReadAllText($generated[0].FullPath)
$namedControls = Select-Xml -LiteralPath src/AgeCheck/MainWindow.xaml -XPath '//*[@x:Name]' `
    -Namespace @{ x = 'http://schemas.microsoft.com/winfx/2006/xaml' }
foreach ($control in $namedControls) {
    $name = $control.Node.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml')
    if ($code -notmatch ('\b' + [Regex]::Escape($name) + '\s*;')) { throw "Missing generated editor control: $name" }
}
Write-Output "PASS: editor markup generated all $($namedControls.Count) named controls without the referenced project assembly."
