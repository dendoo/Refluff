"""Revision 2: full-rug pullout and continuous approach/soil transition, then the locked existing action takes.

python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py
python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py --qa
"""
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import compose

compose.EDL = HERE / "edl.json"
compose.OUT = compose.ROOT / "SteamStore/trailer/macro_20261025/revision2/refluff_macro_trailer_v2.mp4"

if __name__ == "__main__":
    sys.exit(compose.main())
