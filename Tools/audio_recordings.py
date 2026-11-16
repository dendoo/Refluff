"""Every real recording the tool sounds are cut from, and exactly how (Docs/AUDIO.md 11, Docs/AUDIO_CREDITS.md).

    python Tools/audio_recordings.py list                  what plays where, source and licence
    python Tools/audio_recordings.py credits               the attribution lines the game and the store page must carry
    python Tools/audio_recordings.py build [name ...]      re-cut every clip (or the named ones) into Audio/Recorded
        [--sources <dir>]                                  e.g. a folder of the ORIGINAL Freesound downloads
    python Tools/audio_recordings.py fetch                 download the previews into Audio/Sources (fresh checkout)

The clips in the game were cut from Freesound's HQ previews (OGG, ~192 kbps, 44.1 kHz), which anyone can fetch, and
from Mixkit's WAV downloads. The Freesound originals (WAV / FLAC, often 96 kHz) need a Freesound login; their timeline
is the preview's, so after downloading them into a folder (file names starting with "<id>__", as Freesound names
them) `build --sources <folder>` redoes every cut from the originals with the same numbers. Then, in the Editor,
ToolSoundBaker.ReimportRecordings() re-reads the curves and credits.

Licences: CC0 and the Mixkit licence need no credit; CC BY needs the credit line (credits); a NonCommercial source
must never be listed here (the Samsung take the dry vacuum played until 2026-09-26 is CC BY-NC and is deliberately not
in this list; InspectorJ's Dyson, CC BY 4.0, replaced it for the release and the trailer).
"""
import argparse, glob, os, subprocess, sys, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AUDIO = os.path.join(ROOT, 'Assets', 'CarpetCleaning', 'Audio')
SOURCES = os.path.join(AUDIO, 'Sources')
OUT = os.path.join(AUDIO, 'Recorded')
GEN = os.path.join(AUDIO, 'Generated')
CUT_MACHINE = os.path.join(ROOT, 'Tools', 'cut_machine_recording.py')
PREPARE = os.path.join(ROOT, 'Tools', 'prepare_recording.py')


# Sources: key -> dict(author, title, licence, page, preview, stem). Freesound keys are the sound id (the preview URL
# needs the uploader's user id); Mixkit keys are "mixkit_<n>" (their WAV download is public).
def freesound(sid, uid, author, title, lic, stem):
    return {'author': author, 'title': title, 'licence': lic, 'page': f'https://freesound.org/s/{sid}/',
            'preview': f'https://cdn.freesound.org/previews/{sid // 1000}/{sid}_{uid}-hq.ogg', 'stem': stem}


def mixkit(n, title, category, stem):
    return {'author': 'Mixkit', 'title': title, 'licence': 'Mixkit Sound Effects Free License',
            'page': f'https://mixkit.co/free-sound-effects/{category}/',
            'preview': f'https://assets.mixkit.co/active_storage/sfx/{n}/{n}.wav', 'stem': stem, 'mixkit': n}


SRC = {
    79236: freesound(79236, 1218676, 'joedeshon', 'bissell_carpet_cleaner_vacuum_01.wav', 'CC BY 4.0',
                     '79236__joedeshon__bissell_carpet_cleaner_vacuum_01'),
    726634: freesound(726634, 12909930, 'busabx', 'loud metal fan turned on and off a few times (machine starting up)', 'CC BY 4.0',
                      '726634__busabx__loud_metal_fan_turned_on_and_off_a_few_times'),
    864976: freesound(864976, 63692, 'prometheus888', 'Floor Buffer Sound', 'CC BY 4.0', '864976__prometheus888__floor_buffer_sound'),
    451933: freesound(451933, 612689, 'kyles', 'switch big breaker metal click on, off.flac', 'CC0',
                      '451933__kyles__switch_big_breaker_metal_click_on_off'),
    519974: freesound(519974, 533534, 'adda_records', 'shovelling scraping water', 'CC BY 4.0',
                      '519974__adda_records__shovelling_scraping_water'),
    444946: freesound(444946, 6794407, 'byronabadia', '26_Fregona_Agua.mp3', 'CC BY 4.0', '444946__byronabadia__26_fregona_agua'),
    430632: freesound(430632, 5121236, 'InspectorJ', 'Vacuum Cleaner, On Idle Off, Close, A.wav', 'CC BY 4.0',
                      '430632__inspectorj__vacuum_cleaner_on_idle_off_close_a'),
    580534: freesound(580534, 1481531, 'richwise', 'Pressure Washer', 'CC0', '580534__richwise__pressure_washer'),
    321055: freesound(321055, 5415774, 'kev_durr', 'Whipped Cream Spray Can', 'CC BY 4.0', '321055__kev_durr__whipped_cream_spray_can'),
    178499: freesound(178499, 3316196, 'akent', 'BerberCarpetScratching.wav', 'CC BY 3.0', '178499__akent__berbercarpetscratching'),
    543471: freesound(543471, 11181745, 'RossJuterbock', '200088_RossJuterbock_Hair brush on carpet MSH-6.wav', 'CC0',
                      '543471__rossjuterbock__hair_brush_on_carpet_msh-6'),
    154630: freesound(154630, 2592500, 'crcavol', 'Dragging_On_Carpet.aif', 'CC0', '154630__crcavol__dragging_on_carpet'),
    720073: freesound(720073, 15619185, 'yehdawgo', 'Slurping Water', 'CC0', '720073__yehdawgo__slurping_water'),
    'mixkit_3215': mixkit(3215, 'Spray water or liquid', 'spray', 'mixkit_3215__spray_water_or_liquid'),
}


def credit(key, note):
    s = SRC[key]
    if 'mixkit' in s:
        head = f"{s['title']}, Mixkit sound effect {s['mixkit']}, {s['page']}, {s['licence']}"
    else:
        head = f"{s['title']} by {s['author']}, {s['page']}, {s['licence']}"
    return head + (f' ({note})' if note else '')


# name -> (tool, slot, source key, tool script, arguments, credit note). "machine" = cut_machine_recording.py (start /
# loop / stop + speed curves, imported with ImportMachineRecording); anything else = prepare_recording.py (one clip,
# ImportClip). Every clip is levelled against the placeholder it replaced ({gen}), so the bank volumes keep their mix.
CLIPS = {
    'dryvacuum_carpetdrag_contact': ('DryVacuum', 'contact', 154630, 'loop',
        '--slot contact --span 1.3 6.2 --span 9.3 13.4 --xfade 0.08 --hp 80 --match {gen}/tool_dryvacuum_contact.wav', ''),
    'dryvacuum_dyson': ('DryVacuum', 'machine', 430632, 'machine',
        '--start 0.25 --loop 3.0 10.6 --stop 10.90 19.5 --from-rest --hp 30 --match {gen}/tool_dryvacuum_motor.wav', 'cut, levelled'),
    'scraper_berberscratch_contact': ('Scraper', 'contact', 178499, 'loop',
        '--slot contact --span 1.05 5.75 --span 8.05 14.6 --xfade 0.06 --hp 80 --limit 6 --release 0.02 '
        '--match {gen}/tool_scraper_contact.wav', 'two takes joined into a loop, levelled'),
    'hose_pressurewasher': ('Hose', 'machine', 580534, 'machine',
        '--start 113.0 --loop 121.8 128.75 --stop 133.70 137.6 --speed envelope --env-smooth 0.05 --hp 60 '
        '--match {gen}/tool_hose_motor.wav', ''),
    'foamsprayer_aerosol': ('FoamSprayer', 'machine', 321055, 'machine',
        '--start 6.30 --loop 6.9 10.95 --stop 11.00 11.58 --speed envelope --env-smooth 0.05 --hp 60 '
        '--match {gen}/tool_foamsprayer_motor.wav', 'cut, levelled'),
    'brush_floorbuffer_motor': ('Brush', 'motor', 864976, 'loop',
        '--slot motor --span 0.6 10.2725 --xfade 0.08 --hp 30 --match {gen}/tool_brush_motor.wav', 'cut to 30 pad turns, levelled'),
    'brush_breaker_on': ('Brush', 'on', 451933, 'oneshot',
        '--slot on --span 0.185 0.56 --fadeout 0.15 --match {gen}/tool_brush_on.wav', ''),
    'brush_breaker_off': ('Brush', 'off', 451933, 'oneshot',
        '--slot off --span 1.615 2.15 --fadeout 0.2 --match {gen}/tool_brush_off.wav', ''),
    'brush_hairbrush_contact': ('Brush', 'contact', 543471, 'loop',
        '--slot contact --span 1.9 5.8 --span 10.6 14.6 --span 18.6 21.2 --xfade 0.08 --hp 120 --limit 6 --release 0.02 '
        '--match {gen}/tool_brush_contact.wav', ''),
    'squeegee_waterpush_contact': ('Squeegee', 'contact', 519974, 'loop',
        '--slot contact --span 0.5 75 --grains --len 10 --keep 7 --hp 100 --limit 6 --match {gen}/tool_squeegee_contact.wav',
        'loud middles of the strokes re-joined into a loop, levelled'),
    'wetvacuum_bissell': ('WetVacuum', 'machine', 79236, 'machine',
        '--start 0 --loop 3.8 14.5 --stop 15.18 23.0 --duck 0.03 0.05 8 --duck 15.21 15.23 8 --from-rest '
        '--match {gen}/tool_wetvacuum_motor.wav', 'cut, levelled, switch clicks softened'),
    'wetvacuum_slurp_contact': ('WetVacuum', 'contact', 720073, 'loop',
        '--slot contact --span 1.4 12.9 --xfade 0.06 --hp 80 --limit 6 --release 0.02 --match {gen}/tool_wetvacuum_contact.wav', ''),
    'dryer_metalfan': ('Dryer', 'machine', 726634, 'machine',
        '--start 1.80 --loop 5.2 16.3 --stop 16.58 24.5 --speed envelope --env-smooth 0.25 --hp 30 --duck 1.86 1.93 10 '
        '--duck 16.60 16.65 12 --duck 16.77 16.83 12 --duck 16.88 16.91 12 --duck 16.99 17.05 12 --duck 17.11 17.15 9 '
        '--match {gen}/tool_dryer_motor.wav', 'cut, levelled, switch clicks softened'),
    'floorcleaner_pumpspray': ('FloorCleaner', 'machine', 'mixkit_3215', 'machine',
        '--start 0.0 --loop 0.2 1.85 --stop 1.86 2.5 --speed envelope --env-smooth 0.05 --hp 60 '
        '--match {gen}/tool_floorcleaner_motor.wav', 'cut, levelled'),
    'mop_fregona_contact': ('Mop', 'contact', 444946, 'loop',
        '--slot contact --span 1.55 4.15 --span 5.25 7.05 --span 8.6 10.75 --xfade 0.06 --hp 80 --limit 3 '
        '--match {gen}/tool_mop_contact.wav', 'three takes joined into a loop, levelled'),
}


def source_file(key, folder):
    hits = [h for h in sorted(glob.glob(os.path.join(folder, f'{key}__*'))) if not h.endswith('.meta')]
    if not hits: raise SystemExit(f"no source for {key} in {folder} (expected {SRC[key]['stem']}.*)")
    return hits[0]


def build(names, folder):
    for name in names or CLIPS:
        tool, slot, key, kind, args, note = CLIPS[name]
        src = source_file(key, folder)
        a = args.format(gen=GEN).split()
        if kind == 'machine':
            cmd = [sys.executable, CUT_MACHINE, src, '--out', OUT, '--name', name] + a
        else:
            cmd = [sys.executable, PREPARE, kind, src, '--out', OUT, '--name', name] + a
        cmd += ['--credit', credit(key, note)]
        print(f'--- {name} ({tool} {slot}) <- {os.path.basename(src)}', flush=True)
        subprocess.run(cmd, check=True)


def fetch():
    os.makedirs(SOURCES, exist_ok=True)
    for key, s in SRC.items():
        url = s['preview']
        dst = os.path.join(SOURCES, s['stem'] + os.path.splitext(url)[1])
        if os.path.exists(dst): continue
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=60) as r, open(dst, 'wb') as fh: fh.write(r.read())
        print(f'{dst}  <- {url}')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('cmd', choices=('list', 'credits', 'build', 'fetch'))
    ap.add_argument('names', nargs='*')
    ap.add_argument('--sources', default=SOURCES)
    a = ap.parse_args()
    if a.cmd == 'list':
        for name, (tool, slot, key, kind, args, note) in CLIPS.items():
            s = SRC[key]
            print(f"{tool:38s} {slot:8s} {name:30s} {s['licence'][:10]:10s} {key} {s['author']}: {s['title']}")
    elif a.cmd == 'credits':
        used = sorted({c[2] for c in CLIPS.values()}, key=str)
        for key in used:
            s = SRC[key]
            if s['licence'].startswith('CC BY'):
                print(f"\"{s['title']}\" by {s['author']} ({s['page']}), licensed under {s['licence']}")
        free = [k for k in used if not SRC[k]['licence'].startswith('CC BY')]
        if free: print('No credit required: ' + ', '.join(f"{SRC[k]['author']} {k} ({SRC[k]['licence']})" for k in free))
    elif a.cmd == 'build':
        build(a.names, a.sources)
    else:
        fetch()


if __name__ == '__main__':
    main()
