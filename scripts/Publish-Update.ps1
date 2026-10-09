param(
 [Parameter(Mandatory=$true)][ValidateSet('Release','Beta','Alpha')][string]$Channel,
 [Parameter(Mandatory=$true)][string]$Version,
 [Parameter(Mandatory=$true)][string]$WhatsNew,
 [Parameter(Mandatory=$true)][string]$ProfileRoot,
 [string]$InbuiltRoot = "",
 [string]$PluginRoot = "",
 [string]$IconPackRoot = "",
 [string]$DistRoot = ""
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Version must use major.minor.patch, for example 1.2.0.'}
if(-not(Test-Path -LiteralPath $WhatsNew)){throw 'The What''s New HTML file was not found.'}
if([IO.Path]::GetExtension($WhatsNew) -notin '.html','.htm'){throw 'What''s New must be an HTML file.'}
$tag='v'+$Version+($(if($Channel -eq 'Release'){''}else{'-'+$Channel.ToLowerInvariant()}))
& (Join-Path $PSScriptRoot 'Build.ps1') -ProfileRoot $ProfileRoot -InbuiltRoot $InbuiltRoot -PluginRoot $PluginRoot -IconPackRoot $IconPackRoot -DistRoot $DistRoot -Version $Version -Channel $Channel -AwlOnly
if($LASTEXITCODE -ne 0){throw 'Awl build failed.'}
$exe=Join-Path $(if([String]::IsNullOrWhiteSpace($DistRoot)){Join-Path $projectRoot 'dist'}else{[IO.Path]::GetFullPath($DistRoot)}) 'Awl.exe'
$archive=Join-Path $projectRoot ('releases\'+$tag)
New-Item -ItemType Directory -Force -Path $archive|Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $archive 'Awl.exe') -Force
$archivedWhatsNew=Join-Path $archive 'index.html'
if([IO.Path]::GetFullPath($WhatsNew) -ne [IO.Path]::GetFullPath($archivedWhatsNew)){
 Copy-Item -LiteralPath $WhatsNew -Destination $archivedWhatsNew -Force
}
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $archive 'Awl.exe.sha256') -Value ($hash+'  Awl.exe') -Encoding ASCII
$credentialInput="protocol=https`nhost=github.com`nusername=OverRide-00`n`n"
$credentialOutput=$credentialInput|git credential fill
$credential=@{}
foreach($line in $credentialOutput){$parts=$line -split '=',2;if($parts.Count -eq 2){$credential[$parts[0]]=$parts[1]}}
if([String]::IsNullOrWhiteSpace($credential.password)){throw 'GitHub credentials were not available. Sign in to GitHub Desktop and try again.'}
$headers=@{Authorization='Bearer '+$credential.password;Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='Awl-DevMode'}
. (Join-Path $PSScriptRoot 'Release-Description.ps1')
$description=Get-AwlReleaseDescription -HtmlPath $archivedWhatsNew -Tag $tag
$payload=@{tag_name=$tag;target_commitish='main';name=('Awl '+$Version+' - '+$Channel);body=$description;draft=$false;prerelease=($Channel -ne 'Release');generate_release_notes=$false}|ConvertTo-Json
$payloadBytes=[Text.Encoding]::UTF8.GetBytes($payload)
$releaseUri='https://api.github.com/repos/OverRide-00/Awl/releases'
try{
 $release=Invoke-RestMethod -Method Get -Uri ($releaseUri+'/tags/'+[Uri]::EscapeDataString($tag)) -Headers $headers
}catch{
 if($_.Exception.Response -eq $null -or [int]$_.Exception.Response.StatusCode -ne 404){throw}
 $release=$null
}
if($release -eq $null){
 $release=Invoke-RestMethod -Method Post -Uri $releaseUri -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $payloadBytes
}
if($release.tag_name -ne $tag){throw 'GitHub returned a release with the wrong tag.'}
$uploadBase=($release.upload_url -replace '\{.*$','')
Add-Type -AssemblyName System.Net.Http
$client=[Net.Http.HttpClient]::new()
$client.Timeout=[TimeSpan]::FromMinutes(10)
$client.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$credential.password)
$client.DefaultRequestHeaders.Accept.ParseAdd('application/vnd.github+json')
$client.DefaultRequestHeaders.UserAgent.ParseAdd('Awl-DevMode')
$client.DefaultRequestHeaders.Add('X-GitHub-Api-Version','2022-11-28')
foreach($asset in @(@{Path=$exe;Name='Awl.exe';Type='application/vnd.microsoft.portable-executable'},@{Path=(Join-Path $archive 'Awl.exe.sha256');Name='Awl.exe.sha256';Type='text/plain'},@{Path=(Join-Path $archive 'index.html');Name='index.html';Type='text/html'})){
 $existing=@($release.assets|Where-Object {$_.name -eq $asset.Name})
 if($existing.Count -gt 0){
  if($existing.Count -ne 1 -or $existing[0].state -ne 'uploaded' -or $existing[0].size -ne (Get-Item -LiteralPath $asset.Path).Length){throw ('Release asset already exists with different contents or is incomplete: '+$asset.Name)}
  continue
 }
 $uri=$uploadBase+'?name='+[Uri]::EscapeDataString($asset.Name)
 $stream=[IO.File]::OpenRead($asset.Path)
 try{
  $content=[Net.Http.StreamContent]::new($stream)
  try{
   $content.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new($asset.Type)
   $response=$client.PostAsync($uri,$content).GetAwaiter().GetResult()
   try{
    if(-not $response.IsSuccessStatusCode){throw ('Upload failed for '+$asset.Name+': '+[int]$response.StatusCode+' '+$response.Content.ReadAsStringAsync().GetAwaiter().GetResult())}
   }finally{$response.Dispose()}
  }finally{$content.Dispose()}
 }finally{$stream.Dispose()}
}
$client.Dispose()
$versionFile=Join-Path $projectRoot 'VERSION'
$currentVersion=if(Test-Path -LiteralPath $versionFile){(Get-Content -LiteralPath $versionFile -Raw).Trim()}else{''}
if($currentVersion -ne $Version){[IO.File]::WriteAllText($versionFile,$Version+"`n",[Text.UTF8Encoding]::new($false))}
$versionUri='https://api.github.com/repos/OverRide-00/Awl/contents/VERSION'
$remoteVersion=Invoke-RestMethod -Method Get -Uri ($versionUri+'?ref=main') -Headers $headers
$remoteText=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(($remoteVersion.content -replace '\s',''))).Trim()
if($remoteText -ne $Version){
 $versionUpdate=@{message=('Release '+$tag);content=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Version+"`n"));sha=$remoteVersion.sha;branch='main'}|ConvertTo-Json
 Invoke-RestMethod -Method Put -Uri $versionUri -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($versionUpdate))|Out-Null
}
Write-Output ('Published '+$release.html_url)
