import sys,re,json
raw=sys.stdin.read()
line0=[l for l in raw.splitlines() if l.startswith('eval')][0]
js=json.loads(line0.split('\t')[2])
for line in js['result'].strip().split('\n'):
    u=line[:4]
    objs=re.findall(r'\{[^}]*\}',line)
    f=json.loads(objs[0]); g=json.loads(objs[1])
    print(u,'b %.3f f %.3f w %.3f bSet %.3f fSet %.3f | h %.3f F %.3f FMax %.3f s %.3f' % (f['b'],f['f'],f['w'],f['bSet'],f['fSet'],g['h'],g['F'],g['FMax'],g['s']))
