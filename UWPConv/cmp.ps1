$Env:Path += ";C:\Program Files (x86)\Windows Kits\10\bin\x64"
cd D:\MO\F#\DumbPipeBench\UWPConv
MakeAppx.exe pack /f map.txt /p DumbPipeBench.appx

MakeCert.exe -r -h 0 -n "CN=nabiki_t" -eku 1.3.6.1.5.5.7.3.3 -pe -sv DumbPipeBench.pvk DumbPipeBench.cer
pvk2pfx.exe -pvk DumbPipeBench.pvk -spc DumbPipeBench.cer -pfx DumbPipeBench.pfx
signtool.exe sign -f DumbPipeBench.pfx -fd SHA256 -v .\DumbPipeBench.appx
