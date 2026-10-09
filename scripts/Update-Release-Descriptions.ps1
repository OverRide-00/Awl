param([switch]$Apply)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'Release-Description.ps1')
$api='https://api.github.com/repos/OverRide-00/Awl/releases'
$publicHeaders=@{Accept='application/vnd.github+json';'User-Agent'='Awl-Release-Descriptions'}
$releases=Invoke-RestMethod -Method Get -Uri ($api+'?per_page=100') -Headers $publicHeaders
if($releases.Count -eq 0){throw 'No published releases were found.'}
$plans=foreach($release in $releases){
 if($release.draft){continue}
 if($release.tag_name -notmatch '^v\d+\.\d+\.\d+(?:-(?:alpha|beta))?$'){throw ('Unexpected release tag: '+$release.tag_name)}
 if(-not @($release.assets|Where-Object {$_.name -eq 'index.html'}).Count){throw ('index.html is missing from '+$release.tag_name)}
 $archived=Join-Path $projectRoot (Join-Path 'releases' (Join-Path $release.tag_name 'index.html'))
 $documented=Join-Path $projectRoot (Join-Path 'docs\releases' (Join-Path $release.tag_name.Substring(1) 'index.html'))
 $html=if(Test-Path -LiteralPath $archived){$archived}elseif(Test-Path -LiteralPath $documented){$documented}else{throw ('Local index.html is missing for '+$release.tag_name)}
 [pscustomobject]@{Id=$release.id;Tag=$release.tag_name;Description=(Get-AwlReleaseDescription -HtmlPath $html -Tag $release.tag_name)}
}
if(-not $Apply){foreach($plan in $plans){Write-Output ($plan.Tag+"`n"+$plan.Description+"`n")};return}
$credentialInput="protocol=https`nhost=github.com`nusername=OverRide-00`n`n"
$credentialOutput=$credentialInput|git credential fill
$credential=@{}
foreach($line in $credentialOutput){$parts=$line -split '=',2;if($parts.Count -eq 2){$credential[$parts[0]]=$parts[1]}}
if([String]::IsNullOrWhiteSpace($credential.password)){throw 'GitHub credentials were not available.'}
$headers=@{Authorization='Bearer '+$credential.password;Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='Awl-Release-Descriptions'}
foreach($plan in $plans){
 $payload=@{body=[string]$plan.Description}|ConvertTo-Json -Depth 3
 $updated=Invoke-RestMethod -Method Patch -Uri ($api+'/'+$plan.Id) -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($payload))
 if([string]$updated.body -ne [string]$plan.Description){throw ('GitHub did not retain the expected description for '+$plan.Tag)}
 Write-Output ('Updated '+$plan.Tag)
}
