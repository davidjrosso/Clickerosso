# Reactor de Caos

MVP de un arcade incremental offline para Android, con Unity 2D y C#.
Toca el núcleo, encadena combos, activa la sobrecarga e invierte en mejoras
y generadores automáticos.

**Estado actual:** reactor y módulos sobre soportes integrados en una nueva
cámara ilustrada; navegación Reactor, Mejoras, Logros y Ascensión. Nueve logros
con energía reclamable una sola vez y conservación entre ciclos. La ascensión
muestra requisitos, recompensa, especialización y confirmación. Se conservan
maestrías, skins, guardados y la economía anterior.

**99 pruebas** de dominio/guardado aprobadas, seis ensamblados compilados y
**54 ciclos simulados** en nueve combinaciones de rama y estilo de juego.
Sin reclamar los nuevos logros, los resultados coinciden con la fase 4.
El Editor sigue bloqueado por falta de licencia: pruebas nativas y revisión
visual en Main pendientes. Consulta [navegación, recompensas y cómo probar](Docs/UI-REFRESH.md),
[fórmulas de ascensión](Docs/PHASE4.md) y [evidencia](Docs/VALIDATION.md).

**Sonido y móvil:** música ambiental y efectos con volúmenes independientes,
sonidos de combo/desbloqueo/ascensión, controles táctiles ampliados y scroll en
Ajustes y Ascensión. Cinco perfiles pasan la auditoría geométrica; la escucha y
el recorrido nativo siguen pendientes. [Detalles y pruebas](Docs/AUDIO-MOBILE.md).

## Abrir y jugar

1. Instala Unity **6000.0.85f1** y activa una licencia válida mediante el flujo
   oficial. Android Build Support, SDK, NDK y OpenJDK solo son necesarios para APK.
2. Añade esta carpeta a Unity Hub. Permite resolver los paquetes fijados.
3. La preparación inicial importa los recursos esenciales de TMP, crea el
   atlas de fuente y los datos económicos. Si hace falta repetirla:
   **Tools → Reactor de Caos → Prepare Project**, en la barra de menús
   superior de Unity (junto a File/Edit/Window). Detén Play antes de usarlo.
4. Usa **Tools → Reactor de Caos → Open Main Scene** o abre
   `Assets/ReactorDeCaos/Scenes/Main.unity` y pulsa **Play**.
5. Toca el núcleo central. Tras cargar el medidor, pulsa **Activar sobrecarga**.
   Abre **Mejoras** y desliza para acceder a las tres mejoras y cuatro generadores.
6. Toca los módulos alrededor del núcleo para abrir sus fichas e instalar o
   mejorar tecnología. **Logros** muestra objetivos y recompensas reclamables.
   **Ascensión** abre requisitos, especialización, maestrías y skins. La primera
   ascensión se habilita con **1 M de energía** acumulada en el ciclo; comprar no
   reduce el progreso. Revisa y confirma los efectos antes de reiniciar.
7. En **AJUSTES**, arriba a la derecha, configura música y efectos por separado, destellos,
   movimiento y vibración. Desliza para ver los controles de volumen. Las preferencias persisten aparte del progreso.
   La vibración está desactivada de inicio; la cámara y la interfaz no tiemblan.

Se usa entrada táctil/ratón mediante uGUI. El guardado está en
`Application.persistentDataPath/reactor-save.json`, con copia `.bak`.
No se otorgan recompensas por tiempo desconectado.

Selecciona `Assets/ReactorDeCaos/Resources/Economy.asset` para editar en el
Inspector la potencia, curva de combo, sobrecarga y los catálogos `upgrades`,
`generators`, `milestones` y `achievements`, incluidos producción, precios,
condiciones de desbloqueo, objetivos y recompensas reclamables.
La sección `values.rebirth` configura el umbral, esencia, maestrías, ramas y
los módulos iniciales de cada renacimiento. Los cambios se aplican al comenzar una nueva sesión de Play. No sustituyen
los niveles comprados que ya estén guardados.

Si todavía no existe `ReactorFont.asset`, el juego genera una fuente dinámica
desde el archivo DejaVuSans.ttf incluido en Resources. Esta alternativa no
necesita ejecutar el menú de preparación, pero sí necesita TMP Essentials.
El archivo de fuente persistente sigue generándose al preparar el proyecto.

### Si faltan recursos de TextMesh Pro

Espera a que termine de importar los paquetes y ejecuta
**Tools → Reactor de Caos → Prepare Project**. La preparación espera al evento de
finalización de TMP Essentials antes de generar la fuente; termina cuando
aparece `Reactor project prepared` en la consola.

Si la importación automática falla, usa
**Window → Text Mesh Pro → Import TMP Essential Resources**, pulsa
**Import TMP Essentials**, espera a que termine y vuelve a ejecutar
**Prepare Project**. Los **Examples & Extras** son opcionales. El manifiesto
incluye el módulo Physics para que sus scripts también puedan compilar si
los has importado; no es necesario borrar esos archivos.

En la consola puedes limpiar los mensajes anteriores después de corregirlos.
Abre `Main.unity` y pulsa Play cuando no haya errores nuevos.

## Pruebas

Desde la raíz:

```bash
bash Tools/install-dotnet.sh
bash Tools/test-core.sh
python3 -m pip install -r Tools/requirements-balance.txt
bash Tools/simulate-balance.sh
```

El instalador Linux x86-64 verifica el SDK .NET 8.0.426 con SHA-512 del
catálogo oficial y usa `/workspace/toolchains`, fuera del repositorio.
Las dependencias NUnit se restauran con `Tests/packages.lock.json`.
Las simulaciones leen el asset económico real, ejecutan GameEngine y escriben
JSON/CSV en `TestResults/Balance`, incluido `rebirth.json` con seis ciclos
por especialización y estilo. Se conservan los resultados de esta entrega
en `Docs/Balance`, junto al hash del asset utilizado.
En este sandbox el runner necesita permiso para crear mutex del sistema;
la ejecución autorizada fuera del sandbox ha permitido completar las pruebas.

Con Unity disponible:

```bash
python3 Tools/install-unity-editor.py  # Linux: descarga verificada, sin activar licencia
python3 Tools/check-unity-api.py       # compilación C#, sin ejecutar el motor
export UNITY_EDITOR=/ruta/al/Editor/Unity
bash Tools/test-unity.sh
bash Tools/build-android.sh
```

`test-unity.sh` ejecuta las pruebas EditMode y PlayMode y rechaza resultados
vacíos, fallidos o saltados. Los resultados van a `TestResults/`, ignorado por Git.
`build-android.sh` genera `Builds/Android/ReactorDeCaos-development.apk`, ARM64/IL2CPP,
Android 8/API 26 o superior. Es una compilación de desarrollo; no una entrega
firmada ni certificada para Google Play.

## Organización

- `Assets/ReactorDeCaos/Scripts/Core`: dominio C# sin Unity.
- `Scripts/Persistence`: guardado atómico, validación y recuperación.
- `Scripts/Runtime`: ciclo de vida, módulos conectados, interfaz, skins y feedback.
- `Scripts/Core/GameEngine.Rebirth.cs`: renacimientos y economía permanente.
- `Scripts/Persistence/RebirthTransaction.cs`: guardar el nuevo ciclo antes de adoptarlo.
- `Editor/ProjectSetup.cs`: preparación del proyecto y compilación Android.
- `Scenes/Main.unity`: escena de entrada, con referencias GUID estables.
- `Resources`: catálogo Presentation, núcleo, atlas de iconos, fondo, fuente y sonidos incluidos.
- `Tests/EditMode`: pruebas compartidas entre .NET y Unity.
- `Tests/PlayMode`: integración de escena, UI y pausa.
- `Docs/DESIGN.md`: reglas, alcance y criterios de aceptación.
- `AGENTS.md`: instrucciones permanentes de desarrollo.

Unity generará los ajustes faltantes y `Packages/packages-lock.json` en el
primer importado. Revisarlos y conservarlos en Git después de validar el
Editor; no se ha inventado una resolución de paquetes sin ejecutarla.

## Limitaciones del entorno actual

El catálogo oficial ya responde correctamente y el Editor está instalado en
`/workspace/toolchains/Unity-6000.0.85f1/Editor/Unity`, con el archivo verificado
contra el hash publicado por Unity. El arranque batch terminó con código 198:
`No valid Unity Editor license found`. Hace falta activar una licencia por el
flujo oficial y volver a ejecutar `Tools/test-unity.sh`.

La comprobación de API usa el compilador y las bibliotecas oficiales instaladas,
incluidos uGUI/TMP y TestRunner de la plantilla del Editor. No sustituye la
resolución UPM, la importación ni PlayMode. No hay capturas del juego ejecutándose,
APK validado ni mediciones de FPS en dispositivo.

## Historia: Ecos del primer encendido

La pestaña **Historia** reúne seis capítulos de NARA, objetivos con alternativas
pasivas, planos que se conservan al ascender y un epílogo. La lectura es opcional
y no pausa la producción. Guía, pruebas y limitaciones: [Docs/STORY.md](Docs/STORY.md).

## Presentación cinematográfica

La partida empieza con una escena interactiva de NARA y Mara. Las misiones presentan
sus escenas automáticamente; Historia permite repetirlas. Incluye nuevo núcleo y
retratos, voces sintéticas provisionales en español, subtítulos y volumen de voz
independiente. Guía y límites de validación: [Docs/CINEMATICS.md](Docs/CINEMATICS.md).
