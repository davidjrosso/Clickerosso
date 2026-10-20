# Asset provenance

- `Resources/Art/ReactorBackdrop.png`: original illustration generated with the
  available OpenAI image generation tool on 2026-10-20 for this project.
  Prompt: portrait dark navy futuristic reactor, cyan plasma, violet rings,
  restrained magenta conduits, original engineered silhouette, no text or UI.
  The source image is 1024 × 1536. It is used whole as a background RawImage.
  No artwork from an existing game was used as a reference.
- Ring meshes, expanding waves, sparks and floating numbers are drawn and
  animated by `RingGraphic` and `ReactorFeedback`; these are engine effects,
  not generated illustrations.
- `Resources/Audio/*.wav`: original additive/chirp synthesis from
  `Tools/generate-audio.py`. No external recordings, models or samples.
- `Resources/Fonts/DejaVuSans.ttf`: DejaVu Sans bundled from the system font
  installation. See `Resources/Fonts/LICENSE-DejaVu.txt` for its license.

The PNG has been inspected directly. Appearance inside Unity, Android rendering
and sound playback require Editor/device validation and have not yet been verified.
