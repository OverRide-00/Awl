param(
 [Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
 [Parameter(Mandatory=$true)][string]$HtmlPath
)
$ErrorActionPreference='Stop'
$tag='v'+$Version
if(-not(Test-Path -LiteralPath $HtmlPath)){throw 'The HTML release notes were not found.'}
$credentialInput="protocol=https`nhost=github.com`nusername=OverRide-00`n`n"
$credentialOutput=$credentialInput|git credential fill
$credential=@{}
foreach($line in $credentialOutput){$parts=$line -split '=',2;if($parts.Count -eq 2){$credential[$parts[0]]=$parts[1]}}
if([String]::IsNullOrWhiteSpace($credential.password)){throw 'GitHub credentials were not available.'}
$headers=@{Authorization='Bearer '+$credential.password;Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='Awl-DevMode'}
$release=Invoke-RestMethod -Method Get -Uri ('https://api.github.com/repos/OverRide-00/Awl/releases/tags/'+$tag) -Headers $headers
if($release.tag_name -ne $tag){throw 'GitHub returned a release with the wrong tag.'}
$old=@($release.assets|Where-Object {$_.name -eq 'index.html'})
if($old.Count -gt 1){throw 'The release has more than one index.html asset.'}
$stagedName='index-'+$Version+'-staged-'+[Guid]::NewGuid().ToString('N')+'.html'
$uploadUri=($release.upload_url -replace '\{.*$','')+'?name='+[Uri]::EscapeDataString($stagedName)
Add-Type -AssemblyName System.Net.Http
$client=[Net.Http.HttpClient]::new()
$client.Timeout=[TimeSpan]::FromMinutes(5)
$client.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$credential.password)
$client.DefaultRequestHeaders.Accept.ParseAdd('application/vnd.github+json')
$client.DefaultRequestHeaders.UserAgent.ParseAdd('Awl-DevMode')
$client.DefaultRequestHeaders.Add('X-GitHub-Api-Version','2022-11-28')
$stream=[IO.File]::OpenRead($HtmlPath)
try{
 $content=[Net.Http.StreamContent]::new($stream)
 try{
  $content.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new('text/html')
  $response=$client.PostAsync($uploadUri,$content).GetAwaiter().GetResult()
  try{
   $responseText=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
   if(-not $response.IsSuccessStatusCode){throw ('Staging release notes failed: '+[int]$response.StatusCode+' '+$responseText)}
   $staged=$responseText|ConvertFrom-Json
  }finally{$response.Dispose()}
 }finally{$content.Dispose()}
}finally{$stream.Dispose();$client.Dispose()}
if($staged.state -ne 'uploaded' -or $staged.size -ne (Get-Item -LiteralPath $HtmlPath).Length){throw 'The staged HTML asset failed verification; the old asset is unchanged.'}
if($old.Count -eq 1){Invoke-RestMethod -Method Delete -Uri ('https://api.github.com/repos/OverRide-00/Awl/releases/assets/'+$old[0].id) -Headers $headers|Out-Null}
$rename=@{name='index.html';label="Awl $Version - What's New"}|ConvertTo-Json
$newAsset=Invoke-RestMethod -Method Patch -Uri ('https://api.github.com/repos/OverRide-00/Awl/releases/assets/'+$staged.id) -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($rename))
if($newAsset.name -ne 'index.html'){throw 'The new release asset did not receive the index.html name.'}
. (Join-Path $PSScriptRoot 'Release-Description.ps1')
$description=Get-AwlReleaseDescription -HtmlPath $HtmlPath -Tag $tag
$update=@{body=$description}|ConvertTo-Json -Depth 3
Invoke-RestMethod -Method Patch -Uri ('https://api.github.com/repos/OverRide-00/Awl/releases/'+$release.id) -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($update))|Out-Null
Write-Output ('Updated '+$release.html_url)
