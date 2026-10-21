# Asset provenance

## Original AI images — phase 3, 2026-10-20

The available OpenAI `image_gen.imagegen` tool actually generated all three
images below in this task. No existing game art or external reference was used.
The delivered source PNGs were inspected; these are not Unity screenshots.

- `Resources/Art/ReactorCore.png`: 1254 × 1254 RGBA, real transparency.
  Prompt direction: isolated front-facing circular gunmetal containment core,
  luminous cyan energy sphere, violet/magenta retaining arcs, restrained amber
  status accents, clean silhouette, transparent padding; no text, digits or UI.
  Integrated as the main core sprite via Resources/Presentation.asset.
- `Resources/Art/InterfaceAtlas.png`: 1254 × 1254 RGBA, real transparency.
  Prompt direction: nine consistent sci-fi icons in a 3 × 3 sheet, generous
  transparent separation, no typography. Rows: lightning tap, linked combo
  diamonds, overload burst; cyan battery, plasma coils, quantum turbine;
  auxiliary orb, amber milestone crystal, cyan settings sliders.
  Presentation.asset records normalized rectangles; PresentationSprites creates
  nine shared-texture sprites once. The generated pixels were not repainted.
- `Resources/Art/ReactorBackdrop.png`: 1024 × 1536 RGB.
  Prompt direction: dark navy portrait science-fiction chamber, cyan/violet edge
  conduits, restrained amber details, empty center and dark UI zones; no central
  reactor, text, price, UI, logos or typography. Integrated as background RawImage.

The prior phase-1 illustration (also originally generated with OpenAI on the
same date; portrait cyan reactor with violet rings) is retained at
`Art/Legacy/ReactorBackdrop-phase1.png`, outside Resources, for provenance only.
It is no longer the runtime backdrop. The runtime background keeps its old GUID.

Hashes, source dimensions and alpha ranges: `Docs/Phase3/checks.json`.
Style and resource status: `Docs/ART.md` and `Docs/PHASE3.md`.

## Procedural and licensed resources

- Ring meshes, radial glow, expanding/contracting waves, sparks and floating
  values are Unity uGUI geometry animated by RingGraphic, GlowGraphic and
  ReactorFeedback. These are engine effects, not AI illustrations.
- `Resources/Audio/*.wav`: original additive/chirp synthesis reproducible with
  `Tools/generate-audio.py`. No AI audio, external recordings or samples.
  New phase-3 sounds: Milestone, OverloadEnd and OverloadHum. All are provisional
  until listening and mixing in the real game/device.
- `Resources/Fonts/DejaVuSans.ttf`: bundled DejaVu Sans; its license is in
  `Resources/Fonts/LICENSE-DejaVu.txt`. The existing TMP preparation is preserved.

The art is proposed production art, integrated into the existing scene's HUD,
but final in-engine visual approval is pending. Editor startup was blocked by
its missing license. Android rendering, audio and haptics have not been tested.


## Phase 4 procedural expansion

No new AI image generation was used in phase 4. ReactorAssembly reuses the
original core and atlas icons in the actual HUD. ConduitGraphic builds energy
connections, ComboSegmentsGraphic builds a segmented combo ring, and CrownGraphic
builds four skin silhouettes with stage-dependent geometry. Skin palettes and
shapes are authored in C# and use standard uGUI materials. Existing sounds are
reused for installations and collapse. See Docs/PHASE4.md for integration and
pending native visual/device validation.

## Interface refresh — 2026-10-21

A new original backdrop was actually generated with OpenAI `image_gen.imagegen`
for this update and copied directly into `Resources/Art/ReactorBackdrop.png`.
Source: `/workspace/generated_images/exec-f037879a-cd7d-4c06-94d1-ed12f3bc0e9b.png`.
1024 × 1536 RGB; existing GUID and texture importer retained. The previous
phase-3 backdrop remains available in Git history (commit 112cff5).

Prompt direction: straight-on portrait industrial reactor wall, dark brushed
navy titanium, restrained cyan/violet side lights and copper accents, quiet
central mounting surface, mechanical edge brackets; no reactor, icons, UI,
letters or numbers. Generated pixels were not repainted or processed.

`SocketGraphic` supplies inset metallic mounts and bolts in the actual scene;
transparent module hit areas eliminate the old opaque rectangular button fills.
Core and icon alpha textures from phase 3 are reused, not newly generated.
The new background is integrated with those interactive objects through
ReactorHud/ReactorAssembly and shared light/conduit geometry. In-engine art
approval remains pending because the Editor cannot activate a license here.

## Audio and mobile revision — 2026-10-21

No callable AI audio-generation tool was available. The added ComboRise,
GeneratorUnlock, Ascension and AmbientMusic WAV files were synthesized locally
with the project's own `Tools/generate-audio.py`. They contain original sine
oscillators, envelopes and short melodic patterns, with no third-party samples
or recordings. Existing six synthesized clips are retained unchanged.

AmbientMusic is a quiet 16-second beatless pad. Music and OverloadHum import as
PCM so their sample boundaries are preserved. All clips are integrated under
Resources/Audio and used by ReactorFeedback, with separate effect/music controls.
Seven fixed voices bound playback. They remain provisional until listening and
mixing in the actual game and on mobile speakers. PCM analysis is documented in
Docs/AudioMobile/audio.json; it is not auditory validation. No claim of AI audio
production or third-party licensing is made for these original generated files.
