# Reactor de Caos

MVP de un arcade incremental offline para Android, con Unity 2D y C#.
Toca el núcleo, encadena combos, activa la sobrecarga e invierte en mejoras
y generadores automáticos.

**Estado:** economía y persistencia compiladas y probadas con .NET. La escena,
UI, efectos, importación en Unity y compilación Android están implementados
pero pendientes de ejecución en un Editor disponible y con licencia.
No hay APK validado. Consulta [la evidencia por fase](Docs/VALIDATION.md).

## Abrir y jugar

1. Instala Unity **6000.0.85f1** con **Android Build Support**, incluido SDK,
   NDK y OpenJDK, y activa una licencia válida mediante el flujo oficial.
2. Añade esta carpeta a Unity Hub. Permite resolver los paquetes fijados.
3. La preparación inicial importa los recursos esenciales de TMP, crea el
   atlas de fuente y los datos económicos. Si hace falta repetirla:
   **Reactor de Caos → Prepare Project**.
4. Abre `Assets/ReactorDeCaos/Scenes/Main.unity` y pulsa **Play**.
5. Toca el núcleo central. Tras cargar el medidor, pulsa **Activar sobrecarga**.
   Desplaza la tienda para acceder a las tres mejoras y los dos generadores.

Se usa entrada táctil/ratón mediante uGUI. El guardado está en
`Application.persistentDataPath/reactor-save.json`, con copia `.bak`.
No se otorgan recompensas por tiempo desconectado.

### Si faltan recursos de TextMesh Pro

Espera a que termine de importar los paquetes y ejecuta
**Reactor de Caos → Prepare Project**. La preparación espera al evento de
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
```

El instalador Linux x86-64 verifica el SDK .NET 8.0.426 con SHA-512 del
catálogo oficial y usa `/workspace/toolchains`, fuera del repositorio.
Las dependencias NUnit se restauran con `Tests/packages.lock.json`.
En este sandbox el runner necesita permiso para crear mutex del sistema;
la ejecución autorizada fuera del sandbox ha permitido completar las pruebas.

Con Unity disponible:

```bash
export UNITY_EDITOR=/ruta/al/Editor/Unity
bash Tools/test-unity.sh
bash Tools/build-android.sh
```

El primer script ejecuta las pruebas EditMode y PlayMode y rechaza resultados
vacíos, fallidos o saltados. Los resultados van a `TestResults/`, ignorado por Git.
El segundo genera `Builds/Android/ReactorDeCaos-development.apk`, ARM64/IL2CPP,
Android 8/API 26 o superior. Es una compilación de desarrollo; no una entrega
firmada ni certificada para Google Play.

## Organización

- `Assets/ReactorDeCaos/Scripts/Core`: dominio C# sin Unity.
- `Scripts/Persistence`: guardado atómico, validación y recuperación.
- `Scripts/Runtime`: ciclo de vida, entrada, interfaz y feedback.
- `Editor/ProjectSetup.cs`: preparación del proyecto y compilación Android.
- `Scenes/Main.unity`: escena de entrada, con referencias GUID estables.
- `Resources`: ilustración, fuente y sonidos realmente incluidos.
- `Tests/EditMode`: pruebas compartidas entre .NET y Unity.
- `Tests/PlayMode`: integración de escena, UI y pausa.
- `Docs/DESIGN.md`: reglas, alcance y criterios de aceptación.
- `AGENTS.md`: instrucciones permanentes de desarrollo.

Unity generará los ajustes faltantes y `Packages/packages-lock.json` en el
primer importado. Revisarlos y conservarlos en Git después de validar el
Editor; no se ha inventado una resolución de paquetes sin ejecutarla.

## Limitaciones del entorno actual

La consulta del catálogo de Unity devuelve HTTP 403 por la política de red.
Se guardaron los dominios necesarios en el borrador del entorno; guardarlo
no cambia la red activa ni publica una nueva versión.
Unity 6000.0.85f1 fue identificado en las etiquetas oficiales de
`Unity-Technologies/UnityCsReference`, pero su instalación sigue pendiente.

Para finalizar la validación se necesita aplicar la configuración de red,
disponer del Editor con licencia y probar el APK en un dispositivo Android.
La ilustración fue inspeccionada como archivo; no hay capturas de ejecución
del juego ni mediciones de FPS en dispositivo.
