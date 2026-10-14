$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logRoot = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
try {
    $response = Invoke-WebRequest 'http://127.0.0.1:8080/health' -TimeoutSec 2
    if ($response.StatusCode -eq 200) { Write-Output 'Unity MCP is already running.'; return }
} catch {}
$uvxPath = (Get-Command uvx -ErrorAction Stop).Source
Start-Process -FilePath $uvxPath -WindowStyle Hidden -ArgumentList @(
    '--from', 'mcpforunityserver==10.3.0', 'mcp-for-unity',
    '--transport', 'http', '--http-url', 'http://127.0.0.1:8080'
) -RedirectStandardOutput (Join-Path $logRoot 'unity-mcp-server.log') `
  -RedirectStandardError (Join-Path $logRoot 'unity-mcp-server-error.log')
Write-Output 'Unity MCP starting. In Unity: Window > MCP for Unity > Connect.'
