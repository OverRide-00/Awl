param(
 [Parameter(Mandatory=$true)][ValidateSet('Release','Beta','Alpha')][string]$Channel,
 [Parameter(Mandatory=$true)][string]$Version,
 [Parameter(Mandatory=$true)][string]$WhatsNew,
 [Parameter(Mandatory=$true)][string]$ProfileRoot,
 [Parameter(Mandatory=$true)][string]$InbuiltRoot
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Version must use major.minor.patch, for example 1.2.0.'}
if(-not(Test-Path -LiteralPath $WhatsNew)){throw 'The What''s New HTML file was not found.'}
if([IO.Path]::GetExtension($WhatsNew) -notin '.html','.htm'){throw 'What''s New must be an HTML file.'}
$tag='v'+$Version+($(if($Channel -eq 'Release'){''}else{'-'+$Channel.ToLowerInvariant()}))
& (Join-Path $PSScriptRoot 'Build.ps1') -ProfileRoot $ProfileRoot -InbuiltRoot $InbuiltRoot -Version $Version -Channel $Channel -AwlOnly
if($LASTEXITCODE -ne 0){throw 'Awl build failed.'}
$exe=Join-Path $projectRoot 'dist\Awl.exe'
$archive=Join-Path $projectRoot ('releases\'+$tag)
New-Item -ItemType Directory -Force -Path $archive|Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $archive 'Awl.exe') -Force
Copy-Item -LiteralPath $WhatsNew -Destination (Join-Path $archive 'index.html') -Force
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $archive 'Awl.exe.sha256') -Value ($hash+'  Awl.exe') -Encoding ASCII
$credentialInput="protocol=https`nhost=github.com`nusername=OverRide-00`n`n"
$credentialOutput=$credentialInput|git credential fill
$credential=@{}
foreach($line in $credentialOutput){$parts=$line -split '=',2;if($parts.Count -eq 2){$credential[$parts[0]]=$parts[1]}}
if([String]::IsNullOrWhiteSpace($credential.password)){throw 'GitHub credentials were not available. Sign in to GitHub Desktop and try again.'}
$headers=@{Authorization='Bearer '+$credential.password;Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='Awl-DevMode'}
$html=Get-Content -LiteralPath $WhatsNew -Raw
$plain=[Net.WebUtility]::HtmlDecode([regex]::Replace($html,'<[^>]+>',' '))
$plain=[regex]::Replace($plain,'\s+',' ').Trim()
if($plain.Length -gt 4000){$plain=$plain.Substring(0,4000)}
$payload=@{tag_name=$tag;target_commitish='main';name=('Awl '+$Version+' · '+$Channel);body=$plain;draft=$false;prerelease=($Channel -ne 'Release');generate_release_notes=$false}|ConvertTo-Json
$release=Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/OverRide-00/Awl/releases' -Headers $headers -ContentType 'application/json' -Body $payload
$uploadBase=($release.upload_url -replace '\{.*$','')
foreach($asset in @(@{Path=$exe;Name='Awl.exe';Type='application/vnd.microsoft.portable-executable'},@{Path=(Join-Path $archive 'Awl.exe.sha256');Name='Awl.exe.sha256';Type='text/plain'},@{Path=(Join-Path $archive 'index.html');Name='index.html';Type='text/html'})){
 $uri=$uploadBase+'?name='+[Uri]::EscapeDataString($asset.Name)
 Invoke-WebRequest -Method Post -Uri $uri -Headers $headers -ContentType $asset.Type -InFile $asset.Path|Out-Null
}
$versionFile=Join-Path $projectRoot 'VERSION'
$currentVersion=if(Test-Path -LiteralPath $versionFile){(Get-Content -LiteralPath $versionFile -Raw).Trim()}else{''}
if($currentVersion -ne $Version){[IO.File]::WriteAllText($versionFile,$Version+"`n",[Text.UTF8Encoding]::new($false))}
Push-Location $projectRoot
try{
 git add VERSION
 git diff --cached --quiet
 if($LASTEXITCODE -ne 0){git commit -m ('Release '+$tag)|Out-Null}
 git -c credential.username=OverRide-00 push origin main|Out-Null
 if($LASTEXITCODE -ne 0){throw 'The release was published, but pushing VERSION to the repository failed.'}
}finally{Pop-Location}
Write-Output ('Published '+$release.html_url)
