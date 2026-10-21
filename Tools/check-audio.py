#!/usr/bin/env python3
"""Inspect PCM artifacts and conservative mixing headroom; does not listen to audio."""
import hashlib
import json
import math
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[1] / 'Assets/ReactorDeCaos/Resources/Audio'
expected = ['Tap','Purchase','Overload','OverloadEnd','Milestone','OverloadHum',
            'ComboRise','GeneratorUnlock','Ascension','AmbientMusic']
results={}
for name in expected:
    path=root/(name+'.wav')
    with wave.open(str(path)) as wav:
        assert wav.getnchannels()==1 and wav.getsampwidth()==2 and wav.getframerate()==44100,name
        data=wav.readframes(wav.getnframes())
    values=struct.unpack('<'+'h'*(len(data)//2),data)
    peak=max(abs(x) for x in values)/32768
    assert 0 < peak < .95,name
    rms=math.sqrt(sum(x*x for x in values)/len(values))/32768
    row={'duration':len(values)/44100,'peak':peak,'rms':rms,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    if name in ['AmbientMusic','OverloadHum']:
        seam=abs(values[-1]-values[0])/32768
        max_step=max(abs(values[i]-values[i-1]) for i in range(1,len(values)))/32768
        assert seam <= max_step+1/32768,(name,'loop discontinuity')
        row['loopStep']=seam;row['maxAdjacentStep']=max_step
    results[name]=row
peak=lambda name:results[name]['peak']
worst=2*.18*peak('Tap')+.28*max(peak(x) for x in ['Purchase','Overload','OverloadEnd','Milestone','Ascension'])
worst+=.16*peak('ComboRise')+.23*peak('GeneratorUnlock')+.09*peak('OverloadHum')+.14*peak('AmbientMusic')
assert worst<1,worst
print(json.dumps({'method':'PCM file analysis; not listening or native playback','clips':results,'conservativeMixPeakBoundAtFullVolume':worst},indent=2))
