#!/usr/bin/env python3
"""Original dialogue rendered with eSpeak NG (robotic placeholder speech, NOT neural dubbing).
Dependency: espeakng-loader==0.2.4. No synthesizer binary/model is shipped in the game.
Run with /workspace/toolchains/story-tts/bin/python Tools/generate-narrative-audio.py.
"""
import array, ctypes, hashlib, json, math, random, sys, wave
from pathlib import Path
if '--allow-placeholder' not in sys.argv:
 raise SystemExit('Legacy robotic voices disabled. Use generate-cinematic-voices.py; to explicitly restore placeholders pass --allow-placeholder.')
sys.argv.remove('--allow-placeholder')
import espeakng_loader
root=Path(__file__).resolve().parents[1]
output=root/'Assets/ReactorDeCaos/Resources/Audio/Voice';output.mkdir(parents=True,exist_ok=True)
lib=ctypes.CDLL(espeakng_loader.get_library_path())
lib.espeak_Initialize.argtypes=[ctypes.c_int,ctypes.c_int,ctypes.c_char_p,ctypes.c_int]
lib.espeak_Initialize.restype=ctypes.c_int
rate=lib.espeak_Initialize(2,0,str(Path(espeakng_loader.get_data_path()).parent).encode(),0)
assert rate>0
chunks=[]
callback_type=ctypes.CFUNCTYPE(ctypes.c_int,ctypes.POINTER(ctypes.c_short),ctypes.c_int,ctypes.c_void_p)
@callback_type
def callback(samples,count,events):
 if count:chunks.append(ctypes.string_at(samples,count*2))
 return 0
lib.espeak_SetSynthCallback.argtypes=[callback_type];lib.espeak_SetSynthCallback(callback)
lib.espeak_SetVoiceByName.argtypes=[ctypes.c_char_p]
lib.espeak_SetParameter.argtypes=[ctypes.c_int,ctypes.c_int,ctypes.c_int]
lib.espeak_Synth.argtypes=[ctypes.c_char_p,ctypes.c_size_t,ctypes.c_uint,ctypes.c_int,ctypes.c_uint,ctypes.c_uint,ctypes.c_void_p,ctypes.c_void_p]
voices={'nara':('es+f3',164,56),'mara':('es+f2',174,62),'core':('es',165,42),'memory':('es',143,29)}
prefix=sys.argv[1] if len(sys.argv)>1 else None
manifest=root/'Docs/Cinematics/voice.json'
retained={x['id']:x for x in json.loads(manifest.read_text())['clips']} if prefix and manifest.exists() else {}
report=[]
def write(path,samples):
 if sys.byteorder!='little':samples.byteswap()
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(samples.tobytes())
for scene in json.loads((root/'Assets/ReactorDeCaos/Resources/Narrative.json').read_text())['scenes']:
 for beat in scene['beats']:
  if prefix and not beat['voice'].startswith(prefix):
   report.append(retained[beat['voice']]);continue
  assert len(beat['text'])<=180,(beat['voice'],len(beat['text']))
  name,speed,pitch=voices[beat['portrait']]
  assert lib.espeak_SetVoiceByName(name.encode())==0
  for param,value in [(1,speed),(2,65),(3,pitch)]:lib.espeak_SetParameter(param,value,0)
  chunks.clear();text=beat['text'].encode()+b'\0'
  assert lib.espeak_Synth(text,len(text),0,1,0,1,None,None)==0
  lib.espeak_Synchronize()
  samples=array.array('h');samples.frombytes(b''.join(chunks))
  assert samples
  peak=max(abs(x) for x in samples); scale=min(1,16000/max(1,peak))
  # Short fades prevent cut-line clicks; speech remains dry and intelligible.
  fade=min(int(rate*.012),len(samples)//2)
  samples=array.array('h',(int(x*scale*min(1,i/max(1,fade),(len(samples)-1-i)/max(1,fade))) for i,x in enumerate(samples)))
  path=output/(beat['voice']+'.wav');write(path,samples)
  report.append({'id':beat['voice'],'speaker':beat['speaker'],'text':beat['text'],'seconds':round(len(samples)/rate,3),'peak':round(max(abs(x) for x in samples)/32768,4),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
# Original quiet emergency tone and door motor: no external samples.
for name,duration in ([] if prefix else [('StoryAlarm',2.4),('StoryDoor',2.2)]):
 rng=random.Random(97);last=0;samples=array.array('h')
 for i in range(int(rate*duration)):
  t=i/rate;env=min(1,t/.15,(duration-t)/.25)
  if name=='StoryAlarm':
   gate=math.sin(math.pi*(t%.8)/.4)**2 if t%.8<.4 else 0
   value=.10*gate*(math.sin(2*math.pi*440*t)+.25*math.sin(2*math.pi*660*t))
  else:
   last=.96*last+.04*rng.uniform(-1,1)
   value=.12*math.sin(math.pi*t/duration)*(math.sin(2*math.pi*(60*t+9*t*t))+.5*last)
  samples.append(int(32767*env*value))
 write(output.parent/(name+'.wav'),samples)
lib.espeak_Terminate()
dest=root/'Docs/Cinematics';dest.mkdir(exist_ok=True,parents=True)
(dest/'voice.json').write_text(json.dumps({'engine':'eSpeak NG via espeakng-loader 0.2.4','neuralVoice':False,'sampleRate':rate,'clips':report},ensure_ascii=False,indent=2)+'\n')
print(f'{len(report)} Spanish dialogue WAVs generated, {sum(x["seconds"] for x in report):.1f} seconds in manifest; scope: {prefix or 'all voices and cues'}.')
