# server.py
import json
from http.server import HTTPServer, BaseHTTPRequestHandler
from actions import launch_spotify, play_pause, bluetooth, test_sound, set_volume, ensure_play
import time
import socket
import asyncio
import sys, os, yaml

def resource_path(rel):
    base = getattr(sys, "_MEIPASS", os.path.dirname(os.path.abspath(__file__)))
    return os.path.join(base, rel)

# load config if present, else use defaults
cfg_path = resource_path("config.yaml")
DEFAULT_CONFIG = {
    "server": {"port": 8765, "cooldown_sec": 8},
    "bluetooth": {"device_name": "Echo Show 5-1MM", "wait_connect_sec": 10},
    "spotify": {"playlist_uri": None},
    "security": {}
}
if os.path.exists(cfg_path):
    try:
        with open(cfg_path, "r", encoding="utf-8") as f:
            cfg = yaml.safe_load(f) or DEFAULT_CONFIG
    except Exception:
        cfg = DEFAULT_CONFIG
else:
    cfg = DEFAULT_CONFIG
PORT = cfg["server"].get("port", 8765)


def perform_action(action: str):
    steps = []
    try:
        if action in ("launch_and_play",):
            bluetooth.run()
            steps.append("Bluetooth connected")
            set_volume.run()
            steps.append("Setting Volume")
            test_sound.run()
            steps.append("Testing sound")
            asyncio.run(ensure_play.run())
            steps.append("Spotify launched and playback ensured")

            # launch_spotify.run()
            # steps.append("Launched Spotify")
            # play_pause.run()
            # steps.append("Sent Play/Pause")
        return {"status": "success", "steps": steps}
    except Exception as e:
        return {"status": "error", "steps": steps + [f"ERROR: {e}"]}

class Handler(BaseHTTPRequestHandler):
    def _send(self, code, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_POST(self):
        if self.path != "/trigger":
            self._send(404, {"error": "not found"})
            return

        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length).decode("utf-8") if length else "{}"
        try:
            data = json.loads(body)
            action = data.get("action", "launch_and_play")
        except Exception:
            self._send(400, {"error": "invalid json"})
            return

        result = perform_action(action)
        self._send(200, result)

    def do_GET(self):
        if self.path == "/ping":
            self._send(200, {"status": "ok"})
        else:
            self._send(404, {"error": "not found"})

    def log_message(self, *args, **kwargs):
        # silence default logging
        return

if __name__ == "__main__":
    srv = HTTPServer(("0.0.0.0", PORT), Handler)
    print(f"Listening on http://0.0.0.0:{PORT}")
    hostname = socket.gethostname()
    ip_local = socket.gethostbyname(hostname)
    print(f"Listening on http://{ip_local}:{PORT}")
    srv.serve_forever()
