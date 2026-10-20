# Evidencia de validación — 2026-10-20

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
