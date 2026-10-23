# Evidencia de validación — 2026-10-20

## Estado actual: fase 2, progresión y economía

- Catálogo de cuatro familias y tres mejoras; tienda creada desde sus datos.
  Se conservan energía, combo, sobrecarga y llamadas Purchase existentes.
- Doce hitos por energía histórica o nivel, marcas persistentes y cola acotada
  de celebraciones. Migración compatible de partidas de versión 1.
- Cuatro anillos procedurales reutilizados para cinco etapas visuales;
  los hitos y anillos no conceden multiplicadores ocultos.
- `bash Tools/test-core.sh`: **69 aprobadas, 0 fallidas, 0 omitidas**, 315 ms.
  Incluye compras bloqueadas, crecimiento real, límites, catálogo extensible,
  reordenación por IDs, hitos únicos, guardado/carga y compatibilidad histórica.
- `bash Tools/simulate-balance.sh TestResults/Balance-final`: tres perfiles de
  30 minutos, con el GameEngine real y el Economy.asset real. Se probaron tres
  curvas y se repitió la final con comprobaciones de tiempos. Todos los estados
  inspeccionados cada segundo fueron válidos.
- Primera compra: 7/17/10 s para activo/intermitente/pasivo. Espera máxima final
  entre compras: 84/74/69 s, frente a 189/168/162 s de la primera curva.
- Resultados, CSV de compras, configuración y hashes archivados en `Docs/Balance`.
  [PHASE2.md](PHASE2.md) detalla metodología, fórmulas, crecimiento y recomendaciones.
- Compilación contra API oficiales: seis ensamblados, sin errores. Incluye una
  nueva prueba EditMode del asset y una PlayMode de tienda/capas procedurales.
- Se volvió a ejecutar `UNITY_EDITOR=... bash Tools/test-unity.sh`. Preparación
  abortada con **198**: `No valid Unity Editor license found`. Log actual en
  `TestResults/Unity/prepare.log`. EditMode/PlayMode nativos **no ejecutados**.
  Se apartaron los ajustes por defecto que ese intento generó incidentalmente.
- Pendientes: importación UPM/TMP, distribución visual de siete filas,
  celebraciones, audio y sesiones humanas con licencia. No se ha validado APK.

## Fase 1: registro anterior del núcleo jugable

Esta sección sustituye las limitaciones históricas de herramientas que aparecen
más abajo; se conserva el registro anterior para explicar los fallos reportados.

- Se conservó la arquitectura Core/Persistence/Runtime, escena y recursos existentes.
- Combo con gracia configurable y descenso gradual, toques limitados al máximo
  útil, feedback de máximo/descanso y multiplicador legible.
- Economía configurable mediante el asset incluido, también curvas de precios,
  incrementos por nivel, carga, duración y multiplicador de sobrecarga.
- Acumulación compensada para conservar ganancias y costes pequeños; sus restos
  se guardan sin romper las partidas de versión 1. Tope de recurso visible.
- Sobrecarga captura su duración, no se solapa y divide correctamente la
  producción cuando termina dentro de un intervalo.
- UI muestra efectos actuales/siguientes, disponibilidad de compra, producción
  efectiva y tiempo restante. Los pools siguen limitados a 50 objetos.
- Main incluida en EditorBuildSettings y accesible desde Tools/Open Main Scene.
- **53 pruebas .NET aprobadas; 0 fallidas, 0 omitidas**. Último resultado:
  `TestResults/Core/core.trx`, 118 ms. Incluye diez minutos simulados a 60 avances/s,
  ingresos pequeños sobre saldos grandes, límites, compras y guardados antiguos.
- El catálogo oficial respondió HTTP 200. Se descargó e instaló el Editor Linux
  6000.0.85f1, revisión `ee7790f1426a`, verificando el MD5 que publica Unity
  (`Jf6OqfgF0oJ3I9Tr6ILnJA==` en base64). Instalador reproducible:
  `Tools/install-unity-editor.py`. No se instalaron módulos Android.
- Se intentó arrancar el Editor real con `-batchmode -nographics -projectPath
  /workspace/Clickerosso -executeMethod ReactorDeCaos.Editor.ProjectSetup.PrepareBatch`.
  **Salida 198**, `No valid Unity Editor license found`. Log:
  `TestResults/unity-phase1-prepare.log`. No se llegó a importar/ejecutar Main.
- **Los seis ensamblados compilan** con `Tools/check-unity-api.py`, que usa
  Roslyn, netstandard/shims, UnityEngine/UnityEditor y los DLL oficiales uGUI/TMP/
  TestRunner incluidos en el Editor y su plantilla 2D. No utiliza stubs.
  Artefactos en `TestResults/UnityApi`. No equivale a una compilación/importación
  realizada por el pipeline de Unity ni a la resolución UPM.
- Test Framework fijado a 1.6.0, versión integrada en este Editor según su
  manifiesto oficial y el diagnóstico de Package Manager.
- Nuevas pruebas PlayMode de ráfagas, reciclaje de efectos, recuperación de
  sobrecarga y concordancia de UI. **Compiladas, no ejecutadas**.
- `install_script` y `start_skill` actualizados y guardados en el borrador del
  entorno con instalación verificada de Unity y comandos actuales. El guardado
  no publica ni aplica una configuración; no se verificó restauración en otra tarea.

Pendiente: activar una licencia oficial y ejecutar `Tools/test-unity.sh`, seguir
[Docs/PHASE1.md](PHASE1.md) en Play durante varios minutos y comprobar renderizado,
sonido y distintas resoluciones. La prueba económica simula diez minutos, pero
no certifica una sesión real del juego. No hay errores en la suite .NET ni en la
compilación contra API; UPM, importadores/TMP y comportamiento visual siguen
pendientes de validación nativa. No hay APK ni rendimiento Android validados.

## Fase 0: herramientas — parcial

- Git disponible, checkout inicialmente vacío, acceso remoto de lectura correcto.
- .NET SDK 8.0.426 instalado fuera del checkout. Archivo descargado desde el
  servidor oficial y verificado con su SHA-512 en release-metadata/8.0.
- Script `Tools/install-dotnet.sh` ejecutado: verificación e instalación
  correctas; restauración con lockfile correcta.
- Unity 6000.0.85f1 fijado tras consultar las etiquetas oficiales de
  Unity-Technologies/UnityCsReference. No se descargó ni ejecutó el Editor.
- Catálogo oficial services.api.unity.com: HTTP 403, incluso tras guardar el
  borrador de dominios. El guardado no aplicó cambios a la máquina activa.
- No hay Editor/Hub, licencia verificada, SDK/NDK Android o adb utilizables.
- `AGENTS.md`, manifest de Unity, versión del Editor y scripts de trabajo creados.

## Fase 1: dominio — verificado con .NET

- Implementados GameState, EconomyConfig, ComboSystem, OverloadSystem,
  GameEngine y NumberFormat.
- 36 pruebas NUnit ejecutadas: **36 aprobadas, 0 fallidas, 0 omitidas**.
  Incluyen las pruebas de persistencia descritas abajo.
- Comando: `bash Tools/test-core.sh`, desde la raíz, con autorización para
  los mutex del sistema requeridos por .NET.
- Resultado local: `TestResults/Core/core.trx` (generado, no versionado).
- Se compiló el código real del dominio y almacenamiento, no copias ni stubs.
- Se comprobó un recorrido inicial: 25 toques, compra de bobina, activación
  de sobrecarga y producción automática.

## Fase 2: integración de juego — implementada, sin ejecutar en Unity

- Main.unity enlaza ReactorApp por GUID. Runtime crea Canvas, reactor táctil,
  cabecera, combo, sobrecarga, tienda desplazable y avisos.
- Se incluyen tres pruebas PlayMode para escena/compras/sobrecarga,
  raycasts de interfaz y pausa/reanudación.
- **Pruebas PlayMode no ejecutadas.** Tampoco se ha confirmado compilación del
  código que depende de Unity, importación de la escena ni interacción táctil.

## Fase 3: persistencia — verificada parcialmente

- SaveStore y sus operaciones reales de archivos probados en Linux/.NET.
- Probados: ida/vuelta, guardado ausente, corrupción, copia de seguridad,
  checksum incorrecto, versión desconocida, estado inválido, temporal
  interrumpido, tamaño excesivo y fallo de escritura.
- La primera ejecución reveló cuatro fallos al capturar InvalidDataException.
  Se corrigió la clasificación y la ejecución posterior aprobó los 36 casos.
- JsonUtility, ciclo de vida real de Unity y filesystem Android **pendientes**.

## Fase 4: presentación — archivos integrados, validación visual pendiente

- Ilustración original generada e inspeccionada directamente: PNG 1024 × 1536.
- Copiada a Resources/Art/ReactorBackdrop.png y enlazada por Resources.Load.
- Tres WAV sintetizados: Tap, Purchase, Overload; generador reproducible incluido.
- Fuente DejaVu Sans incluida con licencia; atlas TMP se crea en el Editor.
- Importadores con mipmaps desactivados para el fondo y audio mono.
- Efectos limitados mediante pools: números flotantes, ondas y chispas.
- Comprobados los JSON, GUIDs únicos y referencia de script de la escena.
  Los tres WAV decodifican como PCM mono 44.100 Hz, con señal y sin clipping;
  esto valida los archivos, no la reproducción dentro del motor.
- **No se verificó el renderizado Unity, reproducción de audio ni FPS.**

## Fase 5: Android — bloqueada por herramientas

- Código de build y script preparados para APK de desarrollo ARM64/IL2CPP,
  mínimo API 26, orientación vertical.
- **No se ejecutó una compilación Android. No existe un APK validado.**
- No se hizo una publicación, firma de distribución ni prueba de dispositivo.

## Cómo completar la validación

1. Revisar y guardar en los ajustes del entorno los dominios del borrador y
   publicar el entorno cuando corresponda. Reintentar el catálogo oficial.
2. Instalar el Editor fijado con sus módulos Android y activar una licencia
   mediante el flujo oficial, sin guardar credenciales en el repositorio.
3. Ejecutar Prepare Project, conservar los ajustes/lockfile que genere Unity,
   y ejecutar `Tools/test-unity.sh`.
4. Abrir Main.unity y verificar lectura, interacción, efectos y audio.
5. Ejecutar `Tools/build-android.sh`, instalar el APK y validar el recorrido,
   persistencia, pausa, áreas seguras y rendimiento en un dispositivo real.

El proyecto es una implementación inicial pendiente de validación integral;
las pruebas .NET no acreditan que la aplicación Unity o el APK funcionen.

## Configuración reutilizable y conservación

Se guardaron `install_script`, `start_skill` y los cuatro dominios Unity en
el borrador del entorno. La publicación corresponde al usuario en los ajustes
del producto. Guardar el borrador no lo publica ni valida una tarea nueva.
El trabajo se conserva en Git local; no se ha subido al remoto. La restauración
de commits exclusivamente locales en una tarea nueva no está verificada.

## Corrección tras la primera apertura del usuario

El usuario ejecutó la importación en Unity y comunicó dos bloqueos:

- CS0104: `PackageInfo` ambiguo entre UnityEditor y UnityEditor.PackageManager.
  ProjectSetup ahora usa un alias explícito al tipo de Package Manager.
- Dependencia inexistente `com.unity.modules.inputlegacy@1.0.0`.
  Eliminada del manifiesto; la API de entrada clásica pertenece al motor y
  no requiere ese paquete UPM. Se mantienen uGUI, StandaloneInputModule y
  los ejes de InputManager.

Se comprobó la sintaxis JSON del manifiesto y se prepararon un proyecto ZIP
actualizado y un parche con los dos archivos afectados. No se volvió a
ejecutar la suite .NET porque estos cambios no afectan al dominio ni al
almacenamiento. La compilación del Editor y Play Mode todavía deben repetirse
en Unity; esta corrección no acredita que toda la aplicación esté validada.

## Corrección de TMP Essentials y ejemplos importados

El usuario comunicó recursos esenciales TMP ausentes y errores de
`Physics`/`RaycastHit` en CameraController de Examples & Extras.

- Se añadió `com.unity.modules.physics: 1.0.0` para compilar esos ejemplos
  sin borrar archivos del proyecto del usuario.
- La preparación comprueba TMP Settings y el shader real, no la mera
  existencia de una carpeta.
- La creación de fuentes y datos espera `AssetDatabase.importPackageCompleted`;
  también maneja cancelación, fallo y tiempo máximo de espera.
- Los comandos batch usan PrepareBatch/BuildAndroidBatch sin `-quit`,
  y salen explícitamente con éxito o error después de la preparación.
- Se contrastó el flujo con el importador oficial de uGUI en su rama 6000.0.
  El paquete Essentials oficial contiene TMP Settings y TMP_SDF.shader,
  sin scripts C# que obliguen a recargar el dominio durante esa importación.
- Se añadieron dos pruebas EditMode sobre recursos TMP y material, atlas y
  caracteres de la fuente. **Pendientes de ejecución en Unity.**
- Se ejecutó de nuevo `Tools/test-core.sh`: 36 aprobadas, 0 fallidas y 0
  omitidas. Confirma que los casos exclusivos del Editor quedan excluidos
  correctamente del proyecto .NET; no ejecuta esas dos pruebas nuevas.

No se ha ejecutado el Editor localmente para validar esta corrección.
Las pruebas anteriores de .NET no prueban el importador ni estos nuevos casos.

## Corrección de ReactorFont ausente y acceso al menú

El usuario consiguió entrar en Play, pero Awake falló porque ReactorFont.asset
todavía no existía y no encontró el menú de preparación.

- Se añadió `Tools > Reactor de Caos > Prepare Project` en la barra superior,
  conservando también el menú original. La preparación indica que hay que
  detener Play si se invoca durante la ejecución.
- ReactorHud usa una fuente dinámica creada desde DejaVuSans.ttf cuando falta
  el asset generado. El Editor usa la misma función para crear la versión
  persistente; TMP Essentials siguen siendo un requisito.
- La fuente creada en ejecución se libera al destruir el HUD; los assets
  cargados desde Resources no se destruyen.
- Si hay otro fallo de inicialización, se detiene Update y no se guarda un
  estado parcialmente inicializado.
- Se añadió un caso PlayMode que crea la fuente alternativa y genera geometría
  de texto con números y caracteres españoles, sin borrar assets del usuario.

Se revisaron las API de creación y liberación de TMP_FontAsset en el código
oficial de uGUI/6000.0. Se comprobaron los archivos, metadatos y ZIP.
**El nuevo caso PlayMode y el arranque corregido no se han ejecutado en Unity
en este entorno.** No hubo cambios en economía ni persistencia.


## Fase 3 — 2026-10-20

Se generaron realmente tres imágenes originales con image_gen.imagegen y se
integraron como núcleo, atlas de nueve iconos y fondo del HUD de Main. Se
comprobaron sus dimensiones y alfa, referencias del catálogo y metadatos únicos.
Se conservaron economía, persistencia y hash del asset de balance de fase 2.

- `Tools/test-core.sh`: 69 aprobadas, 0 fallidas, 0 omitidas.
- `Tools/check-unity-api.py`: seis ensamblados compilados con API oficiales,
  incluidas las nuevas pruebas nativas de recursos, UI y accesibilidad.
- WAV: seis archivos mono a 44.1 kHz; muestras sin saturación. No equivale a
  comprobar su mezcla ni su reproducción dentro del motor.
- `Tools/test-unity.sh`: salida 198 en preparación; mensaje literal:
  `No valid Unity Editor license found. Please activate your license.`
  No se ejecutaron EditMode ni PlayMode; tampoco se inspeccionó Main ejecutándose.

Resultados estructurados: [Phase3/checks.json](Phase3/checks.json).
Recursos definitivos propuestos, provisionales, límites y pasos para completar
la validación: [PHASE3.md](PHASE3.md). Sigue pendiente la aprobación visual en
Unity, escucha real, Android, haptics y medición de rendimiento en dispositivo.

## Corrección de CanvasRenderer ausente — fase 3

El usuario ejecutó la escena y comunicó MissingComponentException al desactivar
EvolutionRing0–3 y Wave0–5, y al recortar CoreGlow. RingGraphic y GlowGraphic
heredaban de MaskableGraphic pero no declaraban su dependencia CanvasRenderer.
La compilación C# no detecta este requisito del ciclo de vida de uGUI.

Ambas clases ahora declaran RequireComponent(RectTransform, CanvasRenderer).
Unity añade el renderer al crear el componente, antes de registrarlo con la
máscara en OnEnable; cubre órbita, anillos, ondas y ambos halos. Se conserva el
recorte RectMask2D, los pools, la escena, el balance y los guardados.

Se añadió una prueba PlayMode que crea ambos gráficos bajo una máscara activa,
los desactiva inmediatamente y comprueba tres ciclos de activación, recorte y
reutilización sin excepciones. La prueba de Main comprueba también el renderer
en todos sus Graphic, incluidos objetos inactivos.

La compilación de los seis ensamblados con las API oficiales pasa. Estas pruebas
nativas siguen pendientes de ejecución: el último arranque del Editor en este
entorno terminó con código 198 por falta de licencia. No se ha vuelto a ejecutar
la suite económica, ya que solo cambian dependencias de gráficos y sus pruebas.

Para aplicar el arreglo a una copia existente: detener Play, reemplazar
Scripts/Runtime/RingGraphic.cs y GlowGraphic.cs por las versiones corregidas,
esperar la recompilación, limpiar la consola y volver a Play en Main. No hace
falta añadir componentes manualmente ni borrar Library o los guardados.


## Fase 4 — módulos y renacimientos

La ampliación solicitada incorpora siete módulos interactivos, conductos, buffos
visibles, cinco formas, cuatro skins, esencia, tres especializaciones y maestrías.
Se conserva la arquitectura y se migra el payload v1 a v2 sin reiniciar el ciclo.

Ejecutado en esta entrega:

- `Tools/test-core.sh`: **88 aprobadas, 0 fallidas, 0 omitidas**, incluidas
  migración, recompensa basada en producción, límites, maestrías, skins sin efecto
  económico, preservación del resto numérico, recarga desde disco y colapso con
  fallo de escritura que mantiene intacto el motor anterior.
- `Tools/check-unity-api.py`: seis ensamblados compilados contra API oficiales.
- `Tools/simulate-balance.sh`: tres perfiles de 30 minutos con JSON de resultados
  y trazas de compras idénticos a fase 2, más **54 ciclos** distribuidos en nueve
  combinaciones de especialización/estilo. Todos cumplen los presupuestos del
  simulador. Los costes y parámetros originales del asset no cambian; se añade
  únicamente la sección `rebirth`.
- Metadatos completos y GUID únicos; los cinco gráficos procedurales declaran
  RequireComponent(RectTransform, CanvasRenderer), conservando la corrección del
  error informado por el usuario.
- `Tools/test-unity.sh`: nuevo intento, **salida 198** en preparación:
  `No valid Unity Editor license found. Please activate your license.`

Las pruebas PlayMode nuevas de raycasts, fichas, objetos acotados, cancelación,
confirmación, persistencia y maestrías se compilaron pero no se ejecutaron.
No hay verificación visual en Main ni medición en Android. Los ajustes incidentales
creados por el intento fallido del Editor se archivaron fuera del repositorio.

Evidencia estructurada y salidas del simulador: [Phase4/checks.json](Phase4/checks.json)
y [Phase4/rebirth.json](Phase4/rebirth.json). Fórmulas, tiempos medidos, limitaciones
y recorrido para probar en Unity: [PHASE4.md](PHASE4.md).

## Revisión de arte, navegación, logros y ascensión — 2026-10-21

Se integró una nueva ilustración original de la cámara, soportes procedurales y
botones transparentes para resolver los fondos rectangulares de los módulos.
Cuatro pestañas separan reactor, compras, logros reclamables y ascensión.

- 99 pruebas .NET pasan; 11 nuevas cubren logros y persistencia, incluidas
  escritura fallida, doble reclamación, migración y conservación entre ciclos.
- Seis ensamblados compilan contra las API oficiales de Unity.
- Simulaciones: tres perfiles de 30 minutos y 54 ciclos de ascensión pasan;
  sin reclamar logros, los resultados de fase 4 permanecen idénticos.
- Una prueba controlada de diez minutos con compras por orden, reclamaciones
  y ascensión mantiene estados válidos y no duplica recompensas.
- Intento nativo: salida 198 por ausencia de licencia del Editor. Las pruebas
  PlayMode de navegación/claims/raycasts/renderers se compilaron pero no corrieron.
  Los ajustes por defecto creados por el intento quedaron archivados fuera del repo.

No se afirma validación visual en Unity ni rendimiento Android. Detalles,
limitaciones y pasos de reproducción: [UI-REFRESH.md](UI-REFRESH.md).
Resultados estructurados: [UIRefresh/checks.json](UIRefresh/checks.json).

## Sonido y móvil — 2026-10-21

Integrados cuatro WAV originales sintetizados (combo, generador, ascensión y
música ambiental), siete voces acotadas y controles separados de música/efectos.
El silencio previo se migra. Ajustes y Ascensión tienen scroll; se ampliaron
controles y texto, se combinan pausa/foco y se evita la doble compra del mismo
artículo o maestría dentro de un fotograma.

Ejecutado: 99 pruebas .NET aprobadas; compilación de seis ensamblados; auditoría
geométrica de 31 controles en cinco perfiles (mínimo 48,33 píxeles lógicos);
análisis de diez WAV sin saturación y regeneración idéntica. La cota de mezcla
calculada es 0,4182 a volumen máximo. No se cambiaron fórmulas económicas.

El intento nativo del Editor vuelve a terminar con salida 198 por falta de
licencia. No se pudo ejecutar el recorrido de paneles, escuchar la mezcla ni
validar tipografía/gestos en móviles. Seis pruebas PlayMode nuevas se compilaron,
pero no se ejecutaron. No se confunde la auditoría de anclas con renderizado ni
el análisis PCM con escucha. Evidencia y recorrido pendiente:
[AUDIO-MOBILE.md](AUDIO-MOBILE.md), [AudioMobile](AudioMobile/).

## Historia — 2026-10-22

Integrados seis capítulos de NARA, tareas activas/pasivas, planos permanentes,
sintonía cosmética, archivo con scroll, acceso desde el reactor y epílogo de ascensión.
Nueva ilustración original generada realmente con imagegen e integrada; luces
procedurales acotadas representan la restauración. Guardados previos compatibles.

122 pruebas .NET pasan; seis ensamblados compilan contra API oficiales. Auditoría
estática de 38 controles en cinco perfiles, mínimo 48,33 píxeles lógicos. Tres
sesiones simuladas de 30 minutos completan el arco en 179,2/372,7/561,7 segundos.
La simulación inicial de ascensiones falló por ahorrar para máquinas caras con
planos permanentes. Se conserva el resultado fallido; con una política de compra
que limita la espera a 60 segundos, nueve combinaciones completan seis ciclos.
No se retocaron fórmulas para obtener ese resultado; cambió la política del simulador.

Intento nativo: salida 198 por falta de licencia. Las pruebas nuevas de paneles,
lectura, raycasts, producción, elecciones, doble callback y epílogo están compiladas,
pero no ejecutadas. No hay validación visual de Main ni de Android. Los ajustes
incidentales del Editor se archivaron fuera del repositorio. Recorrido, diseño,
riesgos de balance y evidencia: [STORY.md](STORY.md), [Story](Story/).

## Presentación cinematográfica — 2026-10-22

Apertura automática con primer toque real; seis escenas de misión, Mara y NARA,
comunicaciones contextuales, resumen de regreso y escena de ascensión. La interfaz
bloquea acciones tapadas, mantiene producción automática y permite omitir/repetir.
Dos imágenes originales generadas e integradas aportan nuevo núcleo, retratos,
cristal y vista exterior. 46 líneas de voz local eSpeak NG y dos ambientes están
integrados; el doblaje es provisional y no se presenta como voz neural.

Ejecutado: 134 pruebas .NET; seis ensamblados compilados; tres simulaciones de
30 minutos y 54 ciclos con resultados económicos idénticos a la revisión anterior;
auditoría geométrica de 44 controles en cinco perfiles; inspección de WAV, alfa y
metadatos. El Editor termina con salida 198 por falta de licencia. Las pruebas
nativas están compiladas pero no ejecutadas. Revisión visual, escucha de la mezcla,
tipografía/gestos y rendimiento Android siguen pendientes.

Diseño, procedimiento y limitaciones: [CINEMATICS.md](CINEMATICS.md).
Evidencia: [Cinematics](Cinematics/).

## 2026-10-23 — campaña obligatoria y secuencial

Se implementó el recorrido aprobado: despertar con encendido obligatorio, seis misiones
con acciones concretas, consecuencias guardadas antes de desbloquear tecnología y
ascensión condicionada al cierre del arco. La nueva ranura `reactor-campaign-1.json`
no importa progreso antiguo (reinicio autorizado por el usuario). La campaña sí
permanece entre ascensiones. Se sincronizaron diálogos y 21 WAV revisados, se introducen
los sistemas progresivamente y se añadieron consecuencias visuales fijas en la estación.

Evidencia vigente y pasos: [CAMPAIGN.md](CAMPAIGN.md), `Campaign/checks.json`,
`Campaign/media.json`, `Campaign/layout.json` y `Campaign/balance/`.
150 pruebas .NET pasan; seis ensamblados compilan contra API oficiales.
Tres campañas completas llegan a la primera ascensión; también pasan tres perfiles
económicos de 30 minutos y 54 ciclos. La auditoría estática cubre 44 controles/cinco
perfiles y 46 WAV con subtítulos, hashes y metadatos.

El intento nativo con Unity 6000.0.85f1 termina con 198 y `No valid Unity Editor license
found. Please activate your license.` El log contiene además avisos de base de datos
de solo lectura y EPIPE al finalizar UPM. No se ejecutaron las pruebas nativas ni se
hizo revisión audiovisual en Main o Android. Los ProjectSettings incidentales del
arranque se archivaron fuera del repositorio. Las voces siguen siendo provisionales.
