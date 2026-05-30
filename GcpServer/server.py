import http.server
import json
import re
import time
import random
import threading
import subprocess
import urllib.request
import urllib.parse

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

def fetch_pubchem_3d(compound_name):
    """Fetches 3D atomic coordinates from PubChem PUG REST API for a given compound name."""
    print(f"[PubChem API] Querying 3D coordinates for: '{compound_name}'...")
    try:
        safe_name = urllib.parse.quote(compound_name.strip())
        url = f"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/{safe_name}/JSON?record_type=3d"
        
        req = urllib.request.Request(
            url, 
            headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) BioVR/1.0'}
        )
        
        with urllib.request.urlopen(req, timeout=8.0) as response:
            data = json.loads(response.read().decode('utf-8'))
            
        if "PC_Compounds" not in data or not data["PC_Compounds"]:
            print(f"[PubChem Warning] No compound data found in response for '{compound_name}'")
            return None
            
        compound = data["PC_Compounds"][0]
        
        # Element mapping: 1=H, 6=C, 7=N, 8=O, 9=F, 15=P, 16=S, 17=Cl, 35=Br, 53=I
        element_map = {
            1: "H", 6: "C", 7: "N", 8: "O", 9: "F", 15: "P", 16: "S", 17: "Cl", 35: "Br", 53: "I"
        }
        
        aid_to_element = {}
        aid_list = compound.get("atoms", {}).get("aid", [])
        element_list = compound.get("atoms", {}).get("element", [])
        
        for aid, el_num in zip(aid_list, element_list):
            aid_to_element[aid] = element_map.get(el_num, "C") # Fallback to Carbon if exotic
            
        # Get coordinates
        atoms_out = []
        coords_list = compound.get("coords", [])
        if not coords_list:
            return None
            
        conformer = coords_list[0].get("conformers", [])
        if not conformer:
            return None
            
        x_coords = conformer[0].get("x", [])
        y_coords = conformer[0].get("y", [])
        z_coords = conformer[0].get("z", [])
        coord_aids = coords_list[0].get("aid", [])
        
        # Build mapping index for bonds
        aid_to_idx = {}
        for idx, (aid, x, y, z) in enumerate(zip(coord_aids, x_coords, y_coords, z_coords)):
            aid_to_idx[aid] = idx
            atoms_out.append({
                "element": aid_to_element.get(aid, "C"),
                "x": float(x),
                "y": float(y),
                "z": float(z)
            })
            
        # Parse bonds
        bonds_out = []
        bonds_data = compound.get("bonds", {})
        aid1_list = bonds_data.get("aid1", [])
        aid2_list = bonds_data.get("aid2", [])
        order_list = bonds_data.get("order", [])
        
        for aid1, aid2, order in zip(aid1_list, aid2_list, order_list):
            if aid1 in aid_to_idx and aid2 in aid_to_idx:
                bonds_out.append({
                    "atom1": aid_to_idx[aid1],
                    "atom2": aid_to_idx[aid2],
                    "order": int(order)
                })
                
        print(f"[PubChem Success] Retrieved {len(atoms_out)} atoms and {len(bonds_out)} bonds for '{compound_name}'.")
        return {
            "atoms": atoms_out,
            "bonds": bonds_out
        }
    except Exception as e:
        print(f"[PubChem Error] Failed to fetch conformer for '{compound_name}': {e}")
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

                # Fetch structures synchronously during initial run or offload to thread
                # Fetching is lightweight enough to do inline here
                structure = fetch_pubchem_3d(entity_name)

                # Initialize a running job record
                jobs[job_id] = {
                    'job_id': job_id,
                    'entity_name': entity_name,
                    'scale_level': scale_level,
                    'target_steps': target_steps,
                    'current_step': 0,
                    'status': 'RUNNING',
                    'start_time': time.time(),
                    'checkpoints': [],
                    'structure': structure
                }

                print(f"[GCP Server] Initialized synthesis job {job_id} for entity '{entity_name}'.")

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

                step = job['current_step']
                history = []
                for s in range(60, step + 1, 60):
                    history.append({
                        "step": s,
                        "metadata_uri": f"gs://biovr-checkpoints/{job_id}/step_{s}_meta.json",
                        "coordinates_uri": f"gs://biovr-checkpoints/{job_id}/step_{s}_coords.npy"
                    })

                gpu_stats = get_actual_gpu_telemetry()
                if gpu_stats:
                    vram_bytes, temp_celsius = gpu_stats
                else:
                    progress = step / float(job['target_steps'])
                    vram_bytes = int((4.0 + progress * 6.5) * 1024 * 1024 * 1024)
                    temp_celsius = int(45 + progress * 28 + random.randint(-1, 1))

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
                    "compiled_asset_url": f"gs://biovr-assets/{job['entity_name'].lower()}_structure.pdb" if job['status'] == 'COMPLETED' else "",
                    "atoms": job['structure']['atoms'] if job.get('structure') else [],
                    "bonds": job['structure']['bonds'] if job.get('structure') else []
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
        print(f"Integrated with live PubChem API bioinformatics querying.")
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
