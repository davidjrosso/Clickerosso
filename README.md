# Reactor de Caos

MVP de un arcade incremental offline para Android, con Unity 2D y C#.
Toca el núcleo, encadena combos, activa la sobrecarga e invierte en mejoras
y generadores automáticos.

**Estado actual:** campaña de rescate en Orfeo. La partida empieza con el despertar
interactivo de NARA; después se encadenan seis misiones, sus escenas y sus tecnologías.
El objetivo inicial es recuperar la estación y encontrar a Mara. Acumular energía no
saltea misiones ni habilita una ascensión antes del cierre del arco.

La expansión actual **conserva tu campaña** en `reactor-campaign-1.json` y agrega
los datos de la estación. Los guardados anteriores a la campaña (`reactor-save.json`)
siguen separados. La campaña y las salas restauradas se conservan después de ascender.

**Orfeo vive:** invernadero, enfermería y observatorio recuperables; expedición de tres
minutos; producción durante la ausencia hasta ocho horas y recompensas reclamables
sin caducidad. Mara tiene una historia de cuatro escenas con cierre propio.
[Contenido, fórmulas y cómo probar](Docs/STATION.md).

Guía completa y recorrido de prueba: [Docs/CAMPAIGN.md](Docs/CAMPAIGN.md).
**167 pruebas .NET aprobadas**, compilación de seis ensamblados contra las API de Unity,
tres campañas, dos recorridos de la estación con regreso y 54 ciclos económicos simulados.
La ejecución nativa del Editor sigue bloqueada por falta de licencia; no se afirma
validación visual, sonora o Android. Los 62 diálogos incluyen voces neuronales con
dirección cinematográfica latinoamericana, generadas localmente con Qwen3-TTS sin
consumir créditos. El estilo de la muestra de NARA fue aprobado; la escucha completa
y la mezcla en Unity/Android siguen pendientes.
[Generación, atribución y límites de ElevenLabs](Docs/VOICES.md).

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
5. Avanza los diálogos del despertar y toca el núcleo cuando NARA lo indique.
   Mejora Potencia e instala un microgenerador. La tarjeta de misión bajo el reactor
   indica qué hacer y permite **Restaurar soporte vital** cuando todo esté preparado.
6. Completa las misiones para habilitar combo, plasma/sobrecarga, turbina cuántica
   y núcleo auxiliar, en ese orden. **Archivo** permite consultar la misión y repetir
   escenas ya vividas. Al recuperar el soporte vital, la pestaña del Archivo pasa a **Orfeo**:
   desde allí restaura salas, envía el dron y abre el **Archivo**. **Logros** aparece después del soporte vital. **Ascender** se
   presenta al preparar la memoria; exige la última misión y 1 M E producidas en el ciclo.
   Revisa y confirma los efectos antes de reiniciar las instalaciones.
7. En **AJUSTES**, arriba a la derecha, configura música y efectos por separado, destellos,
   movimiento y vibración. Desliza para ver los controles de volumen. Las preferencias persisten aparte del progreso.
   La vibración está desactivada de inicio; la cámara y la interfaz no tiemblan.

Se usa entrada táctil/ratón mediante uGUI. El guardado está en
`Application.persistentDataPath/reactor-campaign-1.json`, con copia `.bak`.
Al volver se reserva hasta ocho horas de producción base de los generadores para
reclamar en **Orfeo**. La sobrecarga y el combo no se aplican durante la ausencia.
Las ausencias cortas también cuentan; los cambios de foco menores a un minuto no
abren automáticamente el informe. La primera carga tras actualizar fija el reloj,
sin inventar ingresos por tiempo anterior a esta función.

Selecciona `Assets/ReactorDeCaos/Resources/Economy.asset` para editar en el
Inspector la potencia, curva de combo, sobrecarga y los catálogos `upgrades`,
`generators`, `milestones` y `achievements`, incluidos producción, precios,
objetivos y recompensas reclamables. En campaña, los desbloqueos tecnológicos los
controla `Core/Campaign.cs`; los hitos económicos no permiten eludir las misiones.
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

## Campaña y presentación

El recorrido **escena → misión → acción → consecuencia → tecnología** se describe en
[Docs/CAMPAIGN.md](Docs/CAMPAIGN.md). La primera presentación forma parte del juego;
las repeticiones del Archivo son opcionales y no entregan recompensas. Se mantienen
los sprites originales integrados, movimientos reducidos, controles de voz/música/efectos
y subtítulos. La estación conserva ventilación, antenas, puerta y memoria recuperadas.

[Docs/CINEMATICS.md](Docs/CINEMATICS.md) describe los medios de la entrega cinematográfica
anterior; sus reglas de saltado/migración fueron reemplazadas por esta campaña.
