"""Revision 3: two-second forward glide between fibers before the full-rug pullout.

python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py
python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py --qa
"""
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import compose

compose.EDL = HERE / "edl.json"
compose.OUT = compose.ROOT / "SteamStore/trailer/macro_20261025/revision3/refluff_macro_trailer_v3.mp4"

if __name__ == "__main__":
    sys.exit(compose.main())
