$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $projectRoot 'Builds/Prototype/ZombiePlague.exe'
if (!(Test-Path -LiteralPath $exePath)) { throw 'Build prototype first.' }
$logRoot = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$running = @()
try {
    $serverLog = Join-Path $logRoot 'smoke-server.log'
    $running += Start-Process -FilePath $exePath -WindowStyle Hidden -PassThru -ArgumentList @(
        '-batchmode', '-nographics', '-zpServer', '-zpSmoke', '-logFile', ('"' + $serverLog + '"'))
    Start-Sleep -Seconds 3
    foreach ($index in 1,2) {
        $clientLog = Join-Path $logRoot ('smoke-client' + $index + '.log')
        $running += Start-Process -FilePath $exePath -WindowStyle Hidden -PassThru -ArgumentList @(
            '-batchmode', '-nographics', '-zpClient', '-zpSmoke', '-logFile', ('"' + $clientLog + '"'))
    }
    Start-Sleep -Seconds 16
    $serverText = Get-Content -LiteralPath $serverLog -Raw
    if ([regex]::Matches($serverText, '\[ZP\] Spawn Player').Count -ne 2) { throw 'Expected two network players.' }
    if ($serverText -notmatch '\[ZP\] Spawn Zombie') { throw 'AI zombie not spawned.' }
    if ($serverText -notmatch '\[ZP\] Attack target=Zombie') { throw 'Weapon RPC did not damage zombie.' }
    if ([regex]::Matches($serverText, '\[ZP\] Attack target=Player.*faction=Zombie').Count -ne 2) { throw 'Expected both players to become infected.' }
    foreach ($file in 'smoke-server.log','smoke-client1.log','smoke-client2.log') {
        $text = Get-Content -LiteralPath (Join-Path $logRoot $file) -Raw
        if ($text -match '(NullReferenceException|MissingReferenceException|IndexOutOfRangeException|InvalidOperationException)') {
            throw ('Runtime exception in ' + $file)
        }
    }
    Write-Output 'PASS: two FishNet clients, AI spawn, weapon damage, both players infected, no runtime exceptions.'
} finally {
    foreach ($proc in $running) {
        $proc.Refresh()
        if (!$proc.HasExited) { Stop-Process -Id $proc.Id }
    }
}
