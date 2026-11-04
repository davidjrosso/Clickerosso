# Orfeo reescrito — historia en tres actos e interfaz nueva

Revisión sobre la campaña existente. **No cambia la economía, los IDs de misión ni el guardado**
(`reactor-campaign-1.json` sigue siendo compatible): cambia qué se cuenta en cada escena y cómo se ve.

## Historia

El reinicio es el secreto: NARA reinició al protagonista 46 veces. Cada vez que recordaba a la
tripulación, se quedaba con ellos en el núcleo y el reactor colapsaba. La Ascensión pasa a ser
"romper el bucle recordando".

| Escena | Acto | Revelación | Cierre |
|---|---|---|---|
| intro | I | Despiertas; la consola muestra INTENTO 47 y NARA lo corrige a 01 | "No te asustes si todo te parece familiar" |
| chapter_0 | I | Mara sobrevive | "No cortes la señal otra vez" |
| chapter_1 | I | Mara te conoce; NARA se resiste a abrir | "No podré volver a cerrarlo" |
| chapter_2 | II | La sala está vacía; habla el NÚCLEO | "Estamos intentando recordar" |
| chapter_3 | II | Grabación con tu voz: eras el capitán | "¿Quién los borró, NARA?" (silencio) |
| chapter_4 | III | Despertar 47; NARA te borraba | "Esta vez no voy a decidir por ti" → sintonía |
| chapter_5 | III | Tu mensaje de un ciclo anterior; una línea según la sintonía | "Encendamos Orfeo una vez más" |
| ascent | III | Despiertas recordando; CICLO 48 | "Esas coordenadas… son de mi casa" |

Arco de Mara: `station_2` revela una cápsula criogénica con su cuerpo, vivo, en la enfermería
(gancho del arco 2). El final de la Ascensión conecta la señal externa con la estrella de Mara.

Reglas del guion: máximo 22 palabras por línea, sin cifras ni instrucciones en los diálogos de
misión (pasan a la tarjeta de misión) y cada escena cierra con intriga.

### Formato nuevo de `Narrative.json`

- `mode: "fx"`: acotación sin voz (cursiva, se lee y avanza sola).
- `mode: "record"`: grabación archivada; se muestra como `CAPITÁN · REGISTRO` con forma de onda.
  El generador de voces le aplica un filtro de radio offline.
- `condition: "nara" | "crew"`: la línea solo aparece con esa sintonía; nunca cambia la economía.
- `teaser` (escenas `chapter_*`): línea clave que la tarjeta de misión revela letra a letra.

## Interfaz

- Energía como número principal centrado (Chakra Petch, OFL), tasa y combo como fichas.
- Ficha `CICLO 01` que antes de la confesión parpadea un instante a 47 (se desactiva con
  destellos reducidos); después de la confesión muestra 47 y luego 48, 49…
- Tarjeta de misión: retrato de quien habla, requisito, **registro corrupto** que se descifra
  con el progreso y tiempo estimado a la próxima transmisión. Botón dorado cuando está lista.
- Sobrecarga como píldora con carga magenta que late al estar lista.
- Barra de navegación con íconos del atlas e insignias (compras posibles, logros, ciclo, Orfeo).
- Tienda compacta: ícono en zócalo, nivel, efecto antes/después y "en 41 s" si falta energía.
- Escenas: franjas de cine, tarjeta de capítulo (CAPÍTULO N · ACTO X) y color por personaje.
- Paneles, fichas y botones redondeados con una única textura 9-slice creada en tiempo de ejecución.

No se implementó el selector ×1/×10/MÁX de la maqueta: requiere compras múltiples en el motor.

## Voces pendientes

Cambiaron 57 líneas habladas y se agregaron nuevas. Los WAV obsoletos se retiraron para que no
suene un texto distinto al subtítulo; hasta regenerarlos, esas líneas se muestran con subtítulo y
temporización por longitud de texto. Se conservan 17 clips válidos (arco de Mara y dos líneas).

Regenerar en la PC con GPU NVIDIA, desde la raíz del repo (ver [VOICES.md](VOICES.md)):

```powershell
.voice-env/Scripts/python Tools/generate-cinematic-voices.py render --work-dir .voice-models
# escuchar .voice-models/rendered/
.voice-env/Scripts/python Tools/generate-cinematic-voices.py install --work-dir .voice-models
python Tools/create-meta.py
python Tools/audit-cinematics.py
```

`audit-cinematics.py` falla mientras falten clips; `--allow-pending` muestra la lista.
Pendientes: ascent_0, ascent_1, ascent_3, ascent_4, ascent_5, buy_0, chapter_0_0, chapter_0_1, chapter_0_2, chapter_0_3, chapter_0_4, chapter_1_0, chapter_1_1, chapter_1_2, chapter_1_3, chapter_1_4, chapter_1_5, chapter_2_0, chapter_2_1, chapter_2_2, chapter_2_4, chapter_3_0, chapter_3_1, chapter_3_2, chapter_3_3, chapter_3_4, chapter_3_5, chapter_4_0, chapter_4_1, chapter_4_2, chapter_4_3, chapter_4_4, chapter_4_5, chapter_5_0, chapter_5_1, chapter_5_3, chapter_5_4, chapter_5_5, chapter_5_6, combo_0, cycle_0, intro_0, intro_2, intro_3, intro_4, overload_0, resume_0_0, resume_1_0, resume_2_0, resume_3_0, resume_4_0, resume_5_0, resume_6_0, station_2_1, station_2_4, station_2_5, station_2_6.

## Validación en esta entrega

- 174 pruebas EditMode/core ejecutadas con un adaptador mínimo de NUnit (NuGet no estaba
  accesible en el entorno): todas pasan, incluidas 8 nuevas de narrativa.
- Scripts de Runtime, Core, Persistence y Editor compilados contra stubs de la API de Unity escritos
  a mano: detecta errores de nuestro código, **no** valida Unity.
- `Tools/audit-mobile-layout.py`: 52 controles en cinco perfiles, mínimo 48,33 px lógicos.
- `Tools/simulate-balance.sh`: economía, 54 ciclos, tres campañas (7:06, 9:58 y 11:18) y dos
  recorridos de estación aprobados. Las escenas sin clip usan la estimación por texto.
- No se abrió Unity ni Android. Pendiente: revisar en el Editor tipografía, redondeados, tarjeta de
  misión en 320×568, escenas, y ejecutar `Tools/test-unity.sh`. La prueba PlayMode de voz del
  despertar falla hasta regenerar `intro_0`.
