#!/usr/bin/env python3
"""Serve the Unity export locally with the same gzip headers used on Vercel."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse

class UnityHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith('.wasm.gz'):
            return 'application/wasm'
        if path.endswith('.js.gz'):
            return 'application/javascript'
        if path.endswith('.data.gz'):
            return 'application/octet-stream'
        return super().guess_type(path)

    def end_headers(self):
        if self.path.split('?')[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / 'Builds' / 'BeaverCreekWeb'
    if not (root / 'index.html').exists():
        raise SystemExit('Build the WebGL player before starting the preview server.')
    print(f'Beaver Creek: http://127.0.0.1:{args.port}', flush=True)
    ThreadingHTTPServer(('127.0.0.1', args.port), partial(UnityHandler, directory=str(root))).serve_forever()
