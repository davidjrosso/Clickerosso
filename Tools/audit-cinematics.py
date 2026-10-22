#!/usr/bin/env python3
"""Verify shipped narrative media and PCM, not native rendering or listening quality."""
import array, hashlib, json, math, re, wave
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1];assets=root/'Assets/ReactorDeCaos/Resources'
catalog=json.loads((assets/'Narrative.json').read_text());ids=[];voices=[]
for scene in catalog['scenes']:
 for beat in scene['beats']:
  key=beat['voice'];assert key not in ids;ids.append(key)
  path=assets/'Audio/Voice'/(key+'.wav');assert len(beat['text'])<=180
  with wave.open(str(path)) as w:
   assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==22050
   rate=w.getframerate();frames=w.getnframes();samples=array.array('h',w.readframes(frames))
  peak=max(abs(x) for x in samples)/32768
  rms=math.sqrt(sum((x/32768)**2 for x in samples)/len(samples))
  assert 0<peak<.5 and rms>0
  assert abs(samples[0])<5 and abs(samples[-1])<5
  voices.append(dict(id=key,seconds=round(frames/rate,3),peak=round(peak,4),rms=round(rms,4)))
assert len(ids)==46
image_reports=[]
for name,alpha in [('NarrativeAtlas',True),('OrfeoExterior',False)]:
 path=assets/'Art'/(name+'.png');im=Image.open(path)
 if alpha:assert im.mode=='RGBA' and im.getchannel('A').getextrema()==(0,255)
 image_reports.append(dict(name=name,width=im.width,height=im.height,mode=im.mode,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
# Actual sprite UV bounds must stay apart; no image editing is performed here.
cells=[(0,.5,.5,.5),(.54,.5,.46,.5),(0,0,.535,.5),(.55,0,.45,.5)]
for i,(x,y,w,h) in enumerate(cells):
 assert x>=0 and y>=0 and x+w<=1.00001 and y+h<=1.00001
 for a,b,c,d in cells[i+1:]:assert min(x+w,a+c)<=max(x,a) or min(y+h,b+d)<=max(y,b)
assets_root=root/'Assets';guids=set()
for p in assets_root.rglob('*'):
 if p.name.endswith('.meta'):
  match=re.search(r'^guid: (\w+)',p.read_text(),re.M)
  if match:assert match[1] not in guids;guids.add(match[1])
 elif p.is_file():assert Path(str(p)+'.meta').exists(),p
print(json.dumps(dict(method='static media and PCM inspection; not native rendering or auditory validation',sceneDefinitions=len(catalog['scenes']),spokenLines=len(ids),voices=voices,images=image_reports,metadataUnique=True),ensure_ascii=False,indent=2))
