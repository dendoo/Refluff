"""
Checks the icon a player build carries (Docs/STEAM.md 5.1): reads the exe's icon group straight from its resources
(Win32 LoadLibraryEx as a data file, no extra packages) and compares every frame that has an art file of its size in
Assets/CarpetCleaning/AppIcon (refluff_icon_<n>.png, python Tools/steam_art/export_app_icon.py) pixel by pixel,
premultiplied. Unity scales the frames it has no slot for (24, 96, 192) itself; those are listed, not compared.

    python Tools/Steam/check_exe_icon.py [path/to/Refluff.exe] [--out folder]   (default: the release build)

--out also writes the group as an .ico and a light / dark preview sheet. Exit code 1 when a frame differs or the exe
has no icon group. Unity's default icon (Player Settings icons left empty, as in v0.9.0) fails on every frame.
"""
import ctypes
import ctypes.wintypes as wt
import io
import os
import struct
import sys

import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
ICON_DIR = os.path.join(ROOT, "Assets", "CarpetCleaning", "AppIcon")
RT_ICON, RT_GROUP_ICON = 3, 14

k32 = ctypes.WinDLL("kernel32", use_last_error=True)
k32.LoadLibraryExW.restype = wt.HMODULE
k32.LoadLibraryExW.argtypes = [wt.LPCWSTR, wt.HANDLE, wt.DWORD]
k32.FreeLibrary.argtypes = [wt.HMODULE]
k32.FindResourceW.restype = wt.HANDLE
k32.FindResourceW.argtypes = [wt.HMODULE, wt.LPVOID, wt.LPVOID]
k32.LoadResource.restype = wt.HANDLE
k32.LoadResource.argtypes = [wt.HMODULE, wt.HANDLE]
k32.LockResource.restype = ctypes.c_void_p
k32.LockResource.argtypes = [wt.HANDLE]
k32.SizeofResource.restype = wt.DWORD
k32.SizeofResource.argtypes = [wt.HMODULE, wt.HANDLE]
ENUMPROC = ctypes.WINFUNCTYPE(wt.BOOL, wt.HMODULE, wt.LPVOID, wt.LPVOID, ctypes.c_void_p)
k32.EnumResourceNamesW.argtypes = [wt.HMODULE, wt.LPVOID, ENUMPROC, ctypes.c_void_p]


def icon_group(exe):
    """The exe's first icon group as .ico bytes (the one Explorer and the window class use)."""
    h = k32.LoadLibraryExW(exe, None, 0x2 | 0x20)  # LOAD_LIBRARY_AS_DATAFILE | AS_IMAGE_RESOURCE
    if not h:
        sys.exit("cannot open %s (error %d)" % (exe, ctypes.get_last_error()))
    try:
        def res(name, typ):
            key = ctypes.c_wchar_p(name) if isinstance(name, str) else name
            r = k32.FindResourceW(h, ctypes.cast(key, ctypes.c_void_p) if isinstance(name, str) else key, typ)
            return ctypes.string_at(k32.LockResource(k32.LoadResource(h, r)), k32.SizeofResource(h, r)) if r else None

        names = []
        # an integer id, or a string name that only lives during the callback; the first group is the exe's icon
        cb = ENUMPROC(lambda hm, typ, name, lp: names.append(name if name >> 16 == 0 else ctypes.wstring_at(name)) or False)
        k32.EnumResourceNamesW(h, RT_GROUP_ICON, cb, None)
        if not names:
            return None
        grp = res(names[0], RT_GROUP_ICON)
        count = struct.unpack_from("<HHH", grp, 0)[2]
        entries = [struct.unpack_from("<BBBBHHIH", grp, 6 + i * 14) for i in range(count)]
        images = [res(e[7], RT_ICON) for e in entries]
    finally:
        k32.FreeLibrary(h)
    ico = io.BytesIO()
    ico.write(struct.pack("<HHH", 0, 1, count))
    off = 6 + 16 * count
    for e, d in zip(entries, images):
        ico.write(struct.pack("<BBBBHHII", e[0], e[1], e[2], e[3], e[4], e[5], len(d), off))
        off += len(d)
    for d in images:
        ico.write(d)
    return ico.getvalue()


def premultiplied(im):
    a = np.asarray(im.convert("RGBA"), np.float64) / 255.0
    return np.concatenate([a[..., :3] * a[..., 3:], a[..., 3:]], axis=2)


def main():
    args = [a for a in sys.argv[1:]]
    out = None
    if "--out" in args:
        i = args.index("--out")
        out = args[i + 1]
        del args[i:i + 2]
    exe = args[0] if args else os.path.join(ROOT, "Builds", "Steam", "content", "windows", "Refluff.exe")
    data = icon_group(exe)
    if data is None:
        print("FAIL: %s has no icon group" % exe)
        return 1
    ico = Image.open(io.BytesIO(data))
    sizes = sorted(ico.info["sizes"])
    print("%s: %d frames %s" % (os.path.relpath(exe, ROOT), len(sizes), ", ".join("%d" % s[0] for s in sizes)))
    bad = 0
    for s in sizes:
        art = os.path.join(ICON_DIR, "refluff_icon_%d.png" % s[0])
        if not os.path.exists(art):
            print("  %4d  scaled by Unity (no slot)" % s[0])
            continue
        d = np.abs(premultiplied(ico.ico.getimage(s)) - premultiplied(Image.open(art))).max() * 255.0
        bad += d > 1.0
        print("  %4d  %s  (max premultiplied diff %.1f)" % (s[0], "same as the art" if d <= 1.0 else "DIFFERS", d))
    if out:
        os.makedirs(out, exist_ok=True)
        open(os.path.join(out, "exe_icon.ico"), "wb").write(data)
        frames = [ico.ico.getimage(s).convert("RGBA") for s in reversed(sizes)]
        w, h = 20 + sum(f.width + 20 for f in frames), max(f.height for f in frames) + 40
        sheet = Image.new("RGBA", (w, 2 * h), (236, 238, 242, 255))
        sheet.paste(Image.new("RGBA", (w, h), (28, 38, 56, 255)), (0, h))
        for row in range(2):
            x = 20
            for f in frames:
                sheet.alpha_composite(f, (x, row * h + 20))
                x += f.width + 20
        sheet.save(os.path.join(out, "exe_icon_sheet.png"))
        print("  wrote %s" % os.path.join(out, "exe_icon.ico / exe_icon_sheet.png"))
    print("OK" if bad == 0 else "FAIL: %d frame(s) differ from the art" % bad)
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
