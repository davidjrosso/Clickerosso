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
