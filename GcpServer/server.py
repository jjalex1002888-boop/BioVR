import http.server
import json
import re
import time
import random
import threading
import subprocess

PORT = 3000
jobs = {}

def get_actual_gpu_telemetry():
    """Attempts to query the local machine's actual NVIDIA GPU details using nvidia-smi."""
    try:
        result = subprocess.run(
            ['nvidia-smi', '--query-gpu=memory.used,temperature.gpu', '--format=csv,noheader,nounits'],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, timeout=1.0
        )
        if result.returncode == 0:
            parts = result.stdout.strip().split(',')
            if len(parts) >= 2:
                vram_mb = int(parts[0].strip())
                temp_c = int(parts[1].strip())
                return vram_mb * 1024 * 1024, temp_c  # bytes, celsius
    except Exception:
        pass
    return None

class GcpMockServerHandler(http.server.BaseHTTPRequestHandler):
    def _set_headers(self, status_code=200):
        self.send_response(status_code)
        self.send_header('Content-Type', 'application/json')
        # Setup perfect CORS headers to prevent cross-origin blocks
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()

    def do_OPTIONS(self):
        self._set_headers(200)

    def do_POST(self):
        if self.path == '/v1/generation/run':
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length)
            try:
                payload = json.loads(post_data.decode('utf-8'))
                job_id = payload.get('job_id')
                scale_level = payload.get('scale_level')
                entity_name = payload.get('simulation_parameters', {}).get('entity_identifiers', ['Dopamine'])[0]
                target_steps = payload.get('simulation_parameters', {}).get('target_steps', 200)

                # Initialize a running job record
                jobs[job_id] = {
                    'job_id': job_id,
                    'entity_name': entity_name,
                    'scale_level': scale_level,
                    'target_steps': target_steps,
                    'current_step': 0,
                    'status': 'RUNNING',
                    'start_time': time.time(),
                    'checkpoints': []
                }

                print(f"[GCP Mock Node] Initialized synthesis job {job_id} for entity '{entity_name}'.")

                # Response JSON
                res = {
                    "job_id": job_id,
                    "status": "RUNNING",
                    "message": f"AlphaFold prediction for '{entity_name}' successfully launched on GCP cluster."
                }
                self._set_headers(200)
                self.wfile.write(json.dumps(res).encode('utf-8'))
            except Exception as e:
                self._set_headers(400)
                self.wfile.write(json.dumps({"error": str(e)}).encode('utf-8'))
        else:
            self._set_headers(404)
            self.wfile.write(json.dumps({"error": "Endpoint not found"}).encode('utf-8'))

    def do_GET(self):
        # Match GET /v1/generation/status/{job_id}
        match = re.match(r'^/v1/generation/status/([^/]+)$', self.path)
        if match:
            job_id = match.group(1)
            if job_id in jobs:
                job = jobs[job_id]
                elapsed = time.time() - job['start_time']
                
                # Advance simulated steps over 8 seconds (25 steps per second)
                step_speed = 25.0
                job['current_step'] = min(job['target_steps'], int(elapsed * step_speed))

                if job['current_step'] >= job['target_steps']:
                    job['status'] = 'COMPLETED'

                # Calculate checkpoint logs
                step = job['current_step']
                history = []
                for s in range(60, step + 1, 60):
                    history.append({
                        "step": s,
                        "metadata_uri": f"gs://biovr-checkpoints/{job_id}/step_{s}_meta.json",
                        "coordinates_uri": f"gs://biovr-checkpoints/{job_id}/step_{s}_coords.npy"
                    })

                # Retrieve GPU Telemetry
                gpu_stats = get_actual_gpu_telemetry()
                if gpu_stats:
                    vram_bytes, temp_celsius = gpu_stats
                    # Print live status using actual local GPU readings!
                    gpu_name = "LOCAL NVIDIA GPU CLUSTER"
                else:
                    # Ramping simulated stats
                    progress = step / float(job['target_steps'])
                    vram_bytes = int((4.0 + progress * 6.5) * 1024 * 1024 * 1024)
                    temp_celsius = int(45 + progress * 28 + random.randint(-1, 1))
                    gpu_name = "GCP NODE: NVIDIA Tesla T4 Cluster"

                res = {
                    "job_id": job_id,
                    "status": job['status'],
                    "current_step": step,
                    "total_steps": job['target_steps'],
                    "performance_metrics": {
                        "elapsed_time_seconds": int(elapsed),
                        "estimated_remaining_seconds": max(0, int((job['target_steps'] - step) / step_speed)),
                        "gpu_vram_usage_bytes": vram_bytes,
                        "gpu_temperature_celsius": temp_celsius,
                        "quota_session_limit_seconds": 5400
                    },
                    "checkpoint_history": history,
                    "compiled_asset_url": f"gs://biovr-assets/{job['entity_name'].lower()}_structure.pdb" if job['status'] == 'COMPLETED' else ""
                }
                
                self._set_headers(200)
                self.wfile.write(json.dumps(res).encode('utf-8'))
            else:
                self._set_headers(404)
                self.wfile.write(json.dumps({"error": "Job not found"}).encode('utf-8'))
        else:
            self._set_headers(404)
            self.wfile.write(json.dumps({"error": "Endpoint not found"}).encode('utf-8'))

class ThreadedHTTPServer(threading.Thread):
    def __init__(self):
        super().__init__()
        self.server = http.server.HTTPServer(('0.0.0.0', PORT), GcpMockServerHandler)
        self.daemon = True

    def run(self):
        print(f"\n=======================================================")
        print(f"[GCP CLUSTER ACTIVE] Local mock cluster server running at:")
        print(f" http://localhost:{PORT}")
        print(f"Integrated with live local NVIDIA GPU telemetry query support.")
        print(f"=======================================================\n")
        self.server.serve_forever()

if __name__ == '__main__':
    server = ThreadedHTTPServer()
    server.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nShutting down GCP mock cluster server.")
