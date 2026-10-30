# Voces cinematográficas latinoamericanas, sin créditos

El flujo local usa **Qwen3-TTS VoiceDesign 1.7B** para diseñar cuatro voces
sintéticas originales y **Qwen3-TTS Base 0.6B** para mantener cada identidad en
los 62 diálogos. La dirección pide español latinoamericano con pronunciación
mexicana y actuación cinematográfica contenida. El cumplimiento del acento y
la interpretación requieren escuchar las muestras; no se deducen de un prompt.

`Tools/voice-direction.json` define NARA (serena, urgente), Mara (cálida,
vulnerable), protagonista (desorientado, decidido) y memoria (grave, compasiva).
No se imita a personas reales. Las referencias son generadas, no grabaciones ajenas.
El modelo Base hereda el estilo de la referencia; no admite dirección actoral
independiente por línea, de modo que la interpretación puede necesitar nuevas tomas.

Unity sigue leyendo `Resources/Audio/Voice/*.wav`: no hay red, cuentas, modelos
ni motores de síntesis dentro del juego. Se conservan identificadores, GUIDs,
subtítulos y controles de volumen. No se modifica economía ni persistencia.

## Reproducir

Python 3.12 y GPU NVIDIA compatible, desde la raíz del repositorio (Windows):

```powershell
python -m venv .voice-env
.voice-env/Scripts/python -m pip install torch==2.8.0 torchaudio==2.8.0 --index-url https://download.pytorch.org/whl/cu129
.voice-env/Scripts/python -m pip install -r Tools/requirements-cinematic-voices.txt
.voice-env/Scripts/python Tools/generate-cinematic-voices.py download --work-dir .voice-models
Copy-Item -Recurse Tools/VoiceReferences .voice-models/references
.voice-env/Scripts/python Tools/generate-cinematic-voices.py render --work-dir .voice-models
# Escuchar references/ y rendered/ antes de instalar.
.voice-env/Scripts/python Tools/generate-cinematic-voices.py install --work-dir .voice-models
.voice-env/Scripts/python Tools/test-neural-voices.py
```

En Linux usar `.voice-env/bin/python` y `cp -r Tools/VoiceReferences .voice-models/references`.
Las referencias incluidas conservan los cuatro timbres sintéticos de esta entrega.
Para diseñar un elenco nuevo, editar `voice-direction.json` y ejecutar la fase `design`
en lugar de copiar las referencias. Nunca usar grabaciones de personas sin autorización.
Instalación y descarga requieren red;
las otras fases fuerzan modo offline. Los modelos se fijan por revisión y quedan
fuera de Assets y Git. `render --only intro_0` permite preparar una muestra.
El render se puede reanudar: comprueba texto, dirección, referencia y hash antes
de reutilizar clips. Para repetir una toma, borrar su WAV de `rendered/`.
La opción `--batch-size 1` reduce la memoria necesaria; el valor predeterminado es 4.
Las semillas se guardan por lote; cambiar los lotes puede cambiar la toma resultante.
La instalación exige que estén todas las líneas del catálogo y que sus textos,
dirección y referencias coincidan. Cerrar Unity durante la instalación de WAVs.

Se conservan las semillas y la dirección; los resultados pueden variar entre
versiones y hardware. `Docs/Cinematics/voice.json` registra los archivos entregados.
Las voces se convierten a mono PCM16, 22050 Hz; el pico queda por debajo de 0,5
para respetar la mezcla existente. Se suavizan 12 ms en los bordes.
No se añade reverberación ni se altera el tono artificialmente.

Auditoría completa: instalar Pillow y ejecutar `python Tools/audit-cinematics.py`.
Comprueba catálogo, hashes, formato, señal no silenciosa y extremos suavizados;
no escucha ni valida la actuación. El generador eSpeak histórico exige ahora
`--allow-placeholder` para evitar que reemplace voces neuronales por accidente.

Comprobación opcional del guion: instalar `faster-whisper==1.2.1`, descargar el
modelo `Systran/faster-whisper-base` y ejecutar `Tools/audit-spoken-text.py` con
`--model-dir`, `--audio-dir` y `--report`. La transcripción se hace offline en CPU;
las diferencias son pistas para escuchar, no errores confirmados de pronunciación.

## Procedencia

Modelos oficiales Qwen bajo Apache-2.0:
- https://huggingface.co/Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign
- https://huggingface.co/Qwen/Qwen3-TTS-12Hz-0.6B-Base
- API y flujo VoiceDesign → Clone: https://github.com/QwenLM/Qwen3-TTS

Los modelos y el motor se usan como herramientas externas; no se incluyen en
la aplicación. El audio representa los textos originales del proyecto.

## Alternativas verificadas el 29 de octubre de 2026

- **Piper local**: sin saldo ni cobro por generación; requiere descargar modelo y
  respetar la licencia de cada voz. No se eligió para la entrega cinematográfica: tiene poco control actoral.
- **eSpeak NG**: local y ya usado por el proyecto, pero síntesis robótica; queda
  como generador histórico explícito, no como opción predeterminada.
- **ElevenLabs Free**: 10.000 créditos mensuales, no generación ilimitada sin créditos.
  La página pública indica uso personal no comercial con atribución. No se consultó
  el saldo privado ni se generó audio en la cuenta del usuario.
  https://elevenlabs.io/pricing y https://elevenlabs.io/text-to-speech
- **Regeneraciones ElevenLabs**: dos gratuitas en el playground bajo condiciones
  (misma voz, modelo y texto, menos de dos horas, sin salir ni refrescar).
  No evitan el coste inicial ni aplican a la API. En v3 cada clic Generate consume
  créditos y entrega dos alternativas. No sirven para generar este catálogo con cero consumo.
  https://help.elevenlabs.io/hc/en-us/articles/13313274666769-Do-I-use-quota-on-every-generation

## Escucha y validación nativa pendientes

En Unity 6000.0.85f1, abrir Main y reproducir despertar, escenas de Mara y memoria.
Comprobar pronunciación de NARA/Orfeo, balance con música, avance automático al final,
avance manual, pausa/reanudación y volumen de voces. Repetir en Android.
No afirmar validación auditiva o Android basándose en inspección PCM o pruebas .NET.
