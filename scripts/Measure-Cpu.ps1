param([ValidateRange(5,60)][int]$Seconds=30)
$ErrorActionPreference='Stop'
$processors=[Environment]::ProcessorCount
$before=@{}
Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -in @('Awl','Awl.DevMode','ffmpeg') } | ForEach-Object {
 $before[$_.Id]=[pscustomobject]@{Cpu=$_.TotalProcessorTime.TotalSeconds;Start=$_.StartTime}
}
$watch=[Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Seconds $Seconds
$results=Get-Process -ErrorAction SilentlyContinue | Where-Object { $before.ContainsKey($_.Id) } | ForEach-Object {
 if($_.StartTime -eq $before[$_.Id].Start){
  [pscustomobject]@{Process=$_.ProcessName;PID=$_.Id;LogicalProcessors=$processors;SampleSeconds=[Math]::Round($watch.Elapsed.TotalSeconds,2);TotalCpuPercent=[Math]::Round(100*($_.TotalProcessorTime.TotalSeconds-$before[$_.Id].Cpu)/$watch.Elapsed.TotalSeconds/$processors,2)}
 }
}
$results | ConvertTo-Json
