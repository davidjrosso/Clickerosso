# Fase 3 — Arte, VFX y animaciones

Implementada sobre la fase 2 (`7f48e34`), conservando dominio, datos económicos,
compras, hitos y guardado. **La validación visual en Unity sigue pendiente por
falta de licencia en este entorno.** Compilar las API no acredita el renderizado.

## Recursos entregados

Se utilizó realmente la herramienta disponible `image_gen.imagegen`: tres
imágenes originales generadas para este proyecto, inspeccionadas y copiadas a
Resources. No son enlaces externos ni imágenes de muestra fuera del juego.

| Recurso | Integración y estado |
| --- | --- |
| `Art/ReactorCore.png` | Núcleo de producción propuesto, 1254 × 1254 RGBA; sprite único, pivote central, 100 PPU, proporción conservada. Requiere aprobación visual en escena. |
| `Art/InterfaceAtlas.png` | Nueve iconos originales, 1254 × 1254 RGBA. Recortados una vez al construir el HUD mediante Presentation.asset. Integrados en siete filas, recompensa y Ajustes. |
| `Art/ReactorBackdrop.png` | Cámara vacía de fondo, 1024 × 1536 RGB. Sustituye el reactor pintado de fase 1 para evitar duplicarlo. Se conserva el GUID anterior. |
| Anillos, halos, ondas, partículas | Solución procedural prevista para producción; cinco etapas y variante de sobrecarga sin imágenes duplicadas. Pendiente revisar intensidad/rendimiento en motor. |
| Seis WAV | Síntesis original reproducible. Toque, compra, inicio, final, hito y ambiente de sobrecarga. Sonido provisional pendiente de escucha y mezcla en dispositivo. |
| Fuente | DejaVu Sans y licencia existentes; TMP dinámico/persistente conservado. |
| Arte anterior | `Assets/ReactorDeCaos/Art/Legacy/ReactorBackdrop-phase1.png`, archivado fuera de Resources; no se carga en el juego. |

No quedan referencias a imágenes pendientes de generar para el alcance de esta
fase. La dirección visual está definida en [ART.md](ART.md); falta la revisión
del resultado real en Unity, la mezcla final de sonido y la prueba en móvil.
La [procedencia](../Assets/ReactorDeCaos/Art/PROVENANCE.md) identifica IA, efectos
procedurales y sonidos sintetizados por separado.

Las texturas usan sRGB, filtrado bilineal, Clamp, sin mipmaps ni lectura CPU,
resolución máxima 2048 y alfa transparente donde corresponde. El atlas es una
textura compartida con sprites FullRect creados a partir de UV; no necesita
Sprite Editor ni otro paquete. El núcleo usa importación Sprite/Single.
`Resources/Presentation.asset` contiene referencias GUID reales. La escena
existente carga ese catálogo en `ReactorHud.Build`, siguiendo su arquitectura.

## Cómo probarlo en la escena real

1. Abre el proyecto con Unity **6000.0.85f1** y licencia válida. Espera la
   importación. Si falta TMP, usa **Tools → Reactor de Caos → Prepare Project**.
2. Abre **Tools → Reactor de Caos → Open Main Scene** y pulsa Play. En Game,
   prueba 1080 × 1920 y 720 × 1280: núcleo separado del fondo, sin rectángulos
   opacos en los iconos; cantidades, precios y botones legibles.
3. Toca lento y rápido. Cada toque da una cantidad flotante y reacción del
   núcleo. En la jerarquía, `ReactorViewport/Effects` conserva **50 hijos**;
   `ReactorApp` conserva **4 AudioSources**. Los efectos no cruzan la tienda.
4. Mantén actividad hasta el combo máximo y deja descansar. Observa la
   intensidad gradual y el texto «BAJANDO», sin necesidad de seguir tocando.
5. Carga y activa Sobrecarga. Comprueba onda especial, tono magenta, sonido,
   bonificación y tiempo restante. Al acabar: onda cian hacia dentro, descenso
   de brillo, sonido de cierre y aviso. Vuelve a cargar y activar otra vez.
6. Compra mejoras y generadores, desplaza la tienda y alcanza hitos. Cada
   tecnología usa su icono. Los hitos muestran una tarjeta temporal; reabrir
   el juego no vuelve a concederlos. Las etapas añaden anillos.
7. Abre **AJUSTES** arriba a la derecha. Desactiva sonido durante la sobrecarga:
   todas las voces deben detenerse inmediatamente. Activa destellos reducidos,
   movimiento reducido y efectos suaves. Verifica que el estado, los importes
   y el tiempo restante siguen claros. Cierra/reabre Play para comprobar que
   los ajustes se conservan. Los clics dentro del modal no generan energía.
8. Durante varios minutos alterna toques, compras, ajustes y sobrecarga.
   En Android comprueba pausa/reanudación, notch/área segura, legibilidad,
   vibración opcional, latencia de audio y tiempo de frame con el Profiler.
   Los límites de objetos están definidos; aún no se han medido FPS en móvil.

No hay menú de depuración que otorgue recursos o altere fórmulas para enseñar
el arte. Para revisar todas las etapas sin modificar un guardado personal,
los tests observan estados de GameEngine; los efectos nunca conceden recompensas.

## Evidencia ejecutada y límites

- `bash Tools/test-core.sh`: **69 aprobadas, 0 fallidas, 0 omitidas**.
- `python3 Tools/check-unity-api.py`: **6 ensamblados compilados** contra DLL
  oficiales de Unity 6000.0.85f1, uGUI/TMP y TestRunner; sin stubs.
- Inspección de dimensiones, alfa real, referencias GUID, metadatos únicos y
  WAV mono a 44.1 kHz sin muestras saturadas. Resultados y hashes en
  [Phase3/checks.json](Phase3/checks.json).
- `Scripts/Core`, `Scripts/Persistence` y `Resources/Economy.asset` sin cambios.
  El SHA-256 económico coincide con la simulación final de fase 2. No se ha
  recalibrado ni repetido el balance porque esta fase no modifica sus entradas.
- `UNITY_EDITOR=… bash Tools/test-unity.sh`: **salida 198** durante preparación:
  `No valid Unity Editor license found. Please activate your license.`
  No llegó a importar el proyecto ni ejecutar EditMode/PlayMode. Los ajustes
  incidentales creados antes de salir se archivaron fuera del repositorio.

Se añadieron pruebas nativas sobre catálogo/importadores, integración de sprites
real en Main, máscaras y raycasts, modal, ajustes persistentes, silencio inmediato,
igualdad de ganancias con accesibilidad, pools y finalización única de sobrecarga.
**Compilan, pero no se han ejecutado.** Las pruebas anteriores de Main siguen
incluidas. Con licencia, ejecutar `Tools/test-unity.sh` y después el recorrido
visual: el runner batch usa `-nographics` y tampoco sustituye una inspección visual.

Pendientes: importación UPM/Editor real, inspección visual y escucha en Main,
pruebas nativas, Android, vibración, orientación/áreas seguras y rendimiento.
No se entregan capturas de una ejecución inexistente ni APK validado. No hay
errores de C# detectados en la compilación de API; esto no descarta errores que
solo aparezcan al importar o ejecutar el motor.

## Arreglo tras la primera ejecución: CanvasRenderer

Si aparecen MissingComponentException en EvolutionRing, Wave o CoreGlow, la
versión inicial de fase 3 carecía de la dependencia CanvasRenderer en los dos
gráficos personalizados. Las versiones corregidas de `RingGraphic.cs` y
`GlowGraphic.cs` incluyen RequireComponent para que Unity lo añada antes de
habilitarlos. Detén Play, actualiza ambos archivos en `Scripts/Runtime`, espera
la compilación, limpia la consola y vuelve a Play. No hace falta modificar la
escena ni los objetos creados en ejecución. Véase la evidencia en VALIDATION.md.
