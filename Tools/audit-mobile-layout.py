#!/usr/bin/env python3
"""Static geometry audit of actual UI anchor declarations. NOT a Unity render/test.
Sizes are logical screen pixels: do not substitute physical display DPI here.
"""
import ast
import json
import re
from pathlib import Path

runtime = Path(__file__).resolve().parents[1] / 'Assets/ReactorDeCaos/Scripts/Runtime'

def expression(value, i=0):
    value = re.sub(r'(?<=[0-9])f\b', '', value.strip())
    node = ast.parse(value, mode='eval')
    assert all(isinstance(n, (ast.Expression, ast.Constant, ast.Name, ast.Load, ast.BinOp,
                              ast.Add, ast.Sub, ast.Mult, ast.Div, ast.UnaryOp, ast.USub)) for n in ast.walk(node))
    return eval(compile(node, '<anchor>', 'eval'), {'__builtins__': {}}, {'i': i, 'column': i if i < 2 else 2 if i == 4 else i + 1})

def anchors(file, name, i=0):
    text = (runtime / file).read_text()
    start = text.index('("' + name + '"')
    # Parent expressions may contain nested calls; the final four args are anchors.
    end = text.index(';', start)
    call = text[start:end].rstrip().rstrip(')')
    args = call.split(',')
    if args[-1].strip().startswith('TextAlignmentOptions.'): args = args[:-1]
    values = args[-4:]
    return [expression(x, i) for x in values]

def rect(parent, a):
    x,y,w,h = parent
    return (x+a[0]*w, y+a[1]*h, (a[2]-a[0])*w, (a[3]-a[1])*h)

def overlap(a,b):
    return min(a[0]+a[2],b[0]+b[2])-max(a[0],b[0]) > .01 and min(a[1]+a[3],b[1]+b[3])-max(a[1],b[1]) > .01

profiles = [('compact',320,568,0,0),('standard',360,640,0,0),('tall_notch',390,844,47,34),
            ('android_tall',412,915,32,24),('tablet_portrait',768,1024,24,20)]
results=[]
for name,w,h,top,bottom in profiles:
    safe=(0,bottom,w,h-top-bottom)
    scale=w/1080
    controls={}
    nav=[rect(safe,anchors('ReactorHud.cs','Navigation',i)) for i in range(5)]
    for i,b in enumerate(nav): controls['navigation'+str(i)]=b
    controls['settings']=rect(safe,anchors('ReactorHud.cs','Settings'))
    reactor=rect(safe,anchors('ReactorHud.cs','ReactorPage'))
    controls['storyShortcut']=rect(reactor,anchors('ReactorHud.cs','OpenStory'))
    controls['overload']=rect(reactor,anchors('ReactorHud.cs','ActivateOverload'))
    view=rect(reactor,anchors('ReactorHud.cs','ReactorViewport'))
    controls['tap']=rect(view,anchors('ReactorHud.cs','TapReactor'))
    # Assembly module bounds are dynamic Vector2 positions; verify source constants.
    assembly=(runtime/'ReactorAssembly.cs').read_text()
    assert '.115f, .125f' in assembly and '.09f, .10f' in assembly
    for i,(x,y) in enumerate([(.14,.77),(.86,.77),(.14,.39),(.86,.39)]):
        controls['generator'+str(i)]=rect(view,[x-.115,y-.125,x+.115,y+.125])
    for i,x in enumerate([.27,.50,.73]): controls['buff'+str(i)]=rect(view,[x-.09,.02,x+.09,.22])
    prog=rect(safe,anchors('RebirthPanel.cs','ProgressionOverlay'))
    for i in range(3): controls['ascensionTab'+str(i)]=rect(prog,anchors('RebirthPanel.cs','ProgressionTab',i))
    viewport=rect(prog,anchors('RebirthPanel.cs','ProgressionViewport'))
    content=(viewport[0],viewport[1],viewport[2],1800*scale)
    assert 'new Vector2(0, 1800)' in (runtime/'RebirthPanel.cs').read_text()
    for i in range(3): controls['branch'+str(i)]=rect(content,anchors('RebirthPanel.cs','ChooseBranch',i))
    controls['ascend']=rect(content,anchors('RebirthPanel.cs','PrepareRebirth'))
    controls['confirm']=rect(prog,anchors('RebirthPanel.cs','ConfirmRebirth'))
    controls['cancel']=rect(prog,anchors('RebirthPanel.cs','CancelRebirth'))
    controls['ascensionClose']=rect(prog,anchors('RebirthPanel.cs','CloseProgression'))
    module=rect(safe,anchors('ReactorModulePanel.cs','ModuleDetails'))
    controls['moduleBuy']=rect(module,anchors('ReactorModulePanel.cs','BuyModule'))
    controls['moduleClose']=rect(module,anchors('ReactorModulePanel.cs','CloseModuleDetails'))
    menu=rect(safe,anchors('PresentationMenu.cs','SettingsPanel'))
    controls['settingsClose']=rect(menu,anchors('PresentationMenu.cs','CloseSettings'))
    controls['settingsOption']=(0,0,menu[2]*.9,164*scale)
    controls['volumeSlider']=(0,0,menu[2]*.9,180*scale)
    shop=rect(safe,anchors('ReactorHud.cs','EngineeringPage'))
    controls['shopRow']=(0,0,shop[2]*.95,218*scale)
    achievements=rect(safe,anchors('AchievementPanel.cs','AchievementsPage'))
    card=(0,0,achievements[2]*.95,222*scale)
    controls['claim']=rect(card,anchors('AchievementPanel.cs','Claim_'))
    story=rect(safe,anchors('StoryPanel.cs','StoryPage'))
    for key in ['StoryPrevious','StoryNext','StoryAction']: controls[key]=rect(story,anchors('StoryPanel.cs',key))
    choices=(0,0,story[2]*.95-48*scale,720*scale)
    for key in ['StoryChooseNara','StoryChooseCore']: controls[key]=rect(choices,anchors('StoryPanel.cs',key))
    assert not overlap(controls['StoryPrevious'],controls['StoryNext'])
    for key in ['StoryPrevious','StoryNext','StoryAction']:
        assert not overlap(controls[key],rect(story,anchors('StoryPanel.cs','StoryViewport')))
        for button in nav: assert not overlap(button,controls[key])
    assert not overlap(controls['StoryAction'],controls['StoryPrevious'])
    assert not overlap(controls['StoryAction'],controls['StoryNext'])
    assert not overlap(controls['storyShortcut'],controls['overload'])
    small={key:[round(b[2],2),round(b[3],2)] for key,b in controls.items() if min(b[2:]) < 48-0.01}
    assert not small,(name,small)
    header=[rect(safe,anchors('ReactorHud.cs',key)) for key in ['Brand','Unit','Energy','Rate','Stage','Settings']]
    for i,a in enumerate(header):
        for b in header[i+1:]: assert not overlap(a,b),(name,'header boxes overlap')
    assert not overlap(controls['ascensionClose'],rect(prog,anchors('RebirthPanel.cs','ProgressionMessage')))
    assert not overlap(controls['settingsClose'],rect(menu,anchors('PresentationMenu.cs','SettingsViewport')))
    for a in nav:
        assert not overlap(a,reactor) and not overlap(a,prog)
        assert a[1]>=safe[1] and a[1]+a[3]<=safe[1]+safe[3]
    core=[controls['tap']]+[controls['generator'+str(i)] for i in range(4)]+[controls['buff'+str(i)] for i in range(3)]
    for i,a in enumerate(core):
        for b in core[i+1:]:assert not overlap(a,b),(name,'reactor hit targets overlap')
    assert not overlap(controls['confirm'],controls['cancel'])
    assert not overlap(controls['moduleBuy'],controls['moduleClose'])
    results.append({'profile':name,'logicalWidth':w,'logicalHeight':h,'safeInsets':[top,bottom],
                    'minimumControlDimension':round(min(min(b[2:]) for b in controls.values()),2),
                    'controlsChecked':len(controls),'nonoverlapChecks':'passed'})
print(json.dumps({'method':'static anchor arithmetic; not native rendering or typography validation','profiles':results},indent=2))
