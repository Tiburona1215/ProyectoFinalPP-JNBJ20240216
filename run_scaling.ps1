$ErrorActionPreference = "Stop"

$parallelExe  = ".\parallel\bin\Release\net9.0\parallel.exe"
$seqExe       = ".\sequential\bin\Release\net9.0\sequential.exe"
$outputCsv    = "scaling_results.csv"
$threads      = @(1, 2, 4, 8)
$repetitions  = 3         

Write-Host "Compilando proyectos..." -ForegroundColor Cyan
dotnet build .\sequential\sequential.csproj -c Release --nologo -v q
dotnet build .\parallel\parallel.csproj     -c Release --nologo -v q

Write-Host "`nMidiendo version secuencial..." -ForegroundColor Cyan
$seqTimes = @()
for ($r = 0; $r -lt $repetitions; $r++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $seqExe | Out-Null
    $sw.Stop()
    $seqTimes += $sw.Elapsed.TotalSeconds
    Write-Host "  Rep $($r+1): $($sw.Elapsed.TotalSeconds.ToString('F3')) s"
}
$seqAvg = ($seqTimes | Measure-Object -Average).Average

"threads,time_s,speedup,efficiency" | Set-Content $outputCsv

"1_seq,$($seqAvg.ToString('F3')),1.0000,1.0000" | Add-Content $outputCsv

foreach ($t in $threads) {
    Write-Host "`nMidiendo paralelo con $t thread(s)..." -ForegroundColor Cyan
    $times = @()
    for ($r = 0; $r -lt $repetitions; $r++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        & $parallelExe $t | Out-Null
        $sw.Stop()
        $times += $sw.Elapsed.TotalSeconds
        Write-Host "  Rep $($r+1): $($sw.Elapsed.TotalSeconds.ToString('F3')) s"
    }
    $avg       = ($times | Measure-Object -Average).Average
    $speedup   = $seqAvg / $avg
    $efficiency = $speedup / $t
    "$t,$($avg.ToString('F3')),$($speedup.ToString('F4')),$($efficiency.ToString('F4'))" |
        Add-Content $outputCsv
    Write-Host "  Promedio: $($avg.ToString('F3')) s | Speed-up: $($speedup.ToString('F2'))x | Eficiencia: $($efficiency.ToString('F2'))"
}

Write-Host "`nResultados guardados en $outputCsv" -ForegroundColor Green

if (Get-Command python -ErrorAction SilentlyContinue) {
    Write-Host "Generando grafica de speed-up..." -ForegroundColor Cyan
    python plot_speedup.py
} else {
    Write-Host "Python no encontrado; ejecuta plot_speedup.py manualmente." -ForegroundColor Yellow
}