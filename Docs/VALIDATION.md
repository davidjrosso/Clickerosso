# Evidencia de validación — 2026-10-20

## Estado actual: fase 1 del núcleo jugable

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
