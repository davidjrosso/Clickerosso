# Reactor de Caos

MVP de un arcade incremental offline para Android, con Unity 2D y C#.
Toca el núcleo, encadena combos, activa la sobrecarga e invierte en mejoras
y generadores automáticos.

**Estado de fase 3:** núcleo, fondo y nueve iconos originales generados con IA
integrados en la escena existente; VFX limitados, transiciones de sobrecarga,
recompensas y ajustes de accesibilidad. La economía de fase 2 se conserva.
**69 pruebas** de economía/guardado aprobadas y seis ensamblados compilados contra
las API oficiales de Unity 6000.0.85f1. El Editor vuelve a detenerse por falta
de licencia: importación, PlayMode, renderizado y audio en escena siguen pendientes.
Consulta [arte, integración y recorrido de prueba](Docs/PHASE3.md),
[guía visual](Docs/ART.md), [economía](Docs/PHASE2.md) y [evidencia](Docs/VALIDATION.md).

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
   Desplaza la tienda para acceder a las tres mejoras y los cuatro generadores.
6. En **AJUSTES**, arriba a la derecha, configura sonido, destellos, movimiento,
   efectos y vibración. Las preferencias persisten aparte del progreso.
   La vibración está desactivada de inicio; la cámara y la interfaz no tiemblan.

Se usa entrada táctil/ratón mediante uGUI. El guardado está en
`Application.persistentDataPath/reactor-save.json`, con copia `.bak`.
No se otorgan recompensas por tiempo desconectado.

Selecciona `Assets/ReactorDeCaos/Resources/Economy.asset` para editar en el
Inspector la potencia, curva de combo, sobrecarga y los catálogos `upgrades`,
`generators` y `milestones`, incluidos producción, precios y condiciones de desbloqueo.
Los cambios se aplican al comenzar una nueva sesión de Play. No sustituyen
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
JSON/CSV en `TestResults/Balance`. Se conservan los resultados de esta entrega
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
- `Scripts/Runtime`: ciclo de vida, entrada, interfaz y feedback.
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
