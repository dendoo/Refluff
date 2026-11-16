"""Writes Tools/Steam/THIRD_PARTY_NOTICES.txt: the licence texts of every third-party library that ships inside the player
(Refluff_Data/Managed). SteamBuild copies the file next to Refluff.exe, and warns when the build carries a managed dll
that is not listed in SteamBuild.KnownThirdParty (then add it here as well). Docs/STEAM.md 5.

    python Tools/Steam/make_notices.py

Deterministic: the texts come from the package cache (Library/PackageCache/<package>@<hash>/...), except MCP for Unity,
whose package ships no licence file (text from https://github.com/CoplayDev/unity-mcp/blob/main/LICENSE, read 2026-09-27),
and the UI fonts of the non-Latin languages (Docs/LOCALIZATION.md 4): subset Noto Sans, SIL Open Font License 1.1, whose
licence files (from the google/fonts repository, next to each font) are kept in Tools/loc/licenses/.
"""
import glob
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CACHE = os.path.join(ROOT, 'Library', 'PackageCache')
OUT = os.path.join(ROOT, 'Tools', 'Steam', 'THIRD_PARTY_NOTICES.txt')

MIT_BODY = """Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE."""

# (title, assemblies in Refluff_Data/Managed, package folder prefix, licence file inside it, or literal text)
ENTRIES = [
    ('Steamworks.NET', 'com.rlabrecque.steamworks.net.dll', 'com.rlabrecque.steamworks.net', 'LICENSE.md', None),
    ('Unity glTFast', 'glTFast*.dll', 'com.unity.cloud.gltfast', 'LICENSE.md', None),
    ('Json.NET (Newtonsoft.Json), as packaged by Unity (com.unity.nuget.newtonsoft-json)', 'Newtonsoft.Json.dll',
     'com.unity.nuget.newtonsoft-json', 'Third Party Notices.md', None),
    ('MCP for Unity', 'MCPForUnity.Runtime.dll', None, None, 'MIT License\n\nCopyright (c) 2025 CoplayDev\n\n' + MIT_BODY),
]

# (title, where it ships, licence file in Tools/loc/licenses): fonts are not dlls, SteamBuild's dll check does not see them
LICENCES = os.path.join(ROOT, 'Tools', 'loc', 'licenses')
FONT_ENTRIES = [
    ('Noto Sans JP, Noto Sans KR, Noto Sans SC, Noto Sans TC (subset UI fonts)', 'Refluff_Data, UI fonts',
     'OFL_NotoSansJP.txt'),
    ('Noto Sans Thai (subset UI font)', 'Refluff_Data, UI fonts', 'OFL_NotoSansThai.txt'),
    ('Noto Sans Arabic (subset UI font)', 'Refluff_Data, UI fonts', 'OFL_NotoSansArabic.txt'),
]


def package_file(prefix, name):
    hits = sorted(glob.glob(os.path.join(CACHE, prefix + '@*', name)))
    if not hits:
        sys.exit(f'missing {prefix}/{name} in {CACHE}: open the project in Unity once so the package cache exists')
    with open(hits[-1], encoding='utf-8') as f:
        return f.read().strip()


def main():
    parts = ['Third-party software in Refluff: Carpet Cleaning ASMR',
             'The game includes the libraries below. Their licences require these notices to travel with the game.',
             'Sound, picture and model credits are in CREDITS.txt.']
    for title, dlls, prefix, name, literal in ENTRIES:
        text = literal if literal is not None else package_file(prefix, name)
        parts.append('=' * 78 + '\n' + title + '\n(' + dlls + ')\n' + '=' * 78 + '\n\n' + text)
    for title, where, name in FONT_ENTRIES:
        path = os.path.join(LICENCES, name)
        if not os.path.exists(path):
            sys.exit(f'missing {path} (Docs/LOCALIZATION.md 4)')
        with open(path, encoding='utf-8') as f:
            text = f.read().strip()
        parts.append('=' * 78 + '\n' + title + '\n(' + where + ')\n' + '=' * 78 + '\n\n' + text)
    body = '\n\n'.join(parts).replace('\r\n', '\n') + '\n'
    with open(OUT, 'w', encoding='utf-8', newline='\r\n') as f:
        f.write(body)
    print(f'wrote {OUT} ({len(body)} chars, {len(ENTRIES)} libraries, {len(FONT_ENTRIES)} font licences)')


if __name__ == '__main__':
    main()
