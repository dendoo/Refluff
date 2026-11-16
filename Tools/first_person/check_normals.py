import json,struct,numpy as np
from pathlib import Path
p=Path('Assets/CarpetCleaning/Models/FirstPerson/Tool_DryVacuum.glb'); data=p.read_bytes(); n=struct.unpack_from('<I',data,12)[0]; j=json.loads(data[20:20+n]); off=20+n; bn=struct.unpack_from('<I',data,off)[0]; b=data[off+8:off+8+bn]
dt={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}; w={'VEC3':3,'SCALAR':1}
def a(i):
 x=j['accessors'][i];v=j['bufferViews'][x['bufferView']];d=np.dtype(dt[x['componentType']]);return np.ndarray((x['count'],w[x['type']]),d,b,v.get('byteOffset',0)+x.get('byteOffset',0),strides=(v.get('byteStride',d.itemsize*w[x['type']]),d.itemsize))
for m in j['meshes']:
 for pr in m['primitives']:
  p=a(pr['attributes']['POSITION']);ns=a(pr['attributes']['NORMAL']);ind=a(pr['indices']).reshape(-1,3); f=p[ind];fn=np.cross(f[:,1]-f[:,0],f[:,2]-f[:,0]);vn=ns[ind].mean(1);dot=(fn*vn).sum(1); print('face normal dot averaged vertex normals:',np.sum(dot>1e-12),'positive',np.sum(dot<-1e-12),'negative',np.sum(abs(dot)<1e-12),'zero'); print('normal lengths',np.quantile(np.linalg.norm(ns,axis=1),[0,.01,.5,.99,1]))
