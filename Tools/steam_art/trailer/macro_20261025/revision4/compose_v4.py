"""Revision 4: three-times-faster first two-second forward camera glide, with later timing preserved.

python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py
python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py --qa
"""
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import compose

compose.EDL = HERE / "edl.json"
compose.OUT = compose.ROOT / "SteamStore/trailer/macro_20261025/revision4/refluff_macro_trailer_v4.mp4"

if __name__ == "__main__":
    sys.exit(compose.main())
