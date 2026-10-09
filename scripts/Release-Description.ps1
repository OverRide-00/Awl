function Get-AwlReleaseDescription {
 param([Parameter(Mandatory=$true)][string]$HtmlPath,[Parameter(Mandatory=$true)][string]$Tag)
 $html=[IO.File]::ReadAllText($HtmlPath,[Text.Encoding]::UTF8)
 $main=[regex]::Match($html,'(?is)<main\b[^>]*>(.*?)</main>')
 if($main.Success){$html=$main.Groups[1].Value}
 $html=[regex]::Replace($html,'(?is)<(style|script)\b[^>]*>.*?</\1>',' ')
 $paragraph=[regex]::Match($html,'(?is)<p\b[^>]*>(.*?)</p>')
 if(-not $paragraph.Success){throw ('No introductory paragraph was found in '+$HtmlPath)}
 $summary=[Net.WebUtility]::HtmlDecode([regex]::Replace($paragraph.Groups[1].Value,'<[^>]+>',' '))
 $summary=[regex]::Replace($summary,'\s+',' ').Trim()
 if($summary.Length -gt 320){
  $summary=$summary.Substring(0,320)
  $lastSpace=$summary.LastIndexOf(' ')
  if($lastSpace -gt 240){$summary=$summary.Substring(0,$lastSpace)}
  $summary=$summary.TrimEnd(' ','.',',',';',':')+'.'
 }
 if([String]::IsNullOrWhiteSpace($summary)){throw ('The introductory paragraph was empty in '+$HtmlPath)}
 $url='https://github.com/OverRide-00/Awl/releases/download/'+[Uri]::EscapeDataString($Tag)+'/index.html'
 return ($summary+"`n`n"+'**Full breakdown:** [View index.html]('+$url+') (download and open it in a browser).')
}
