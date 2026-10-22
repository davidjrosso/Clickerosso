# Orfeo — escenas, comunicaciones y encendido interactivo

## Qué cambia al jugar

Una partida nueva abre con la vista de Orfeo, alarma suave, NARA, subtítulos y voz.
El primer toque se hace sobre el núcleo dentro de la escena y produce la energía
real del motor una sola vez. La sala se ilumina; Mara pide ayuda desde el sector rojo.
La misión inicial es restablecer el soporte vital, y la siguiente, localizarla.

Los seis capítulos tienen escenas automáticas al cumplir sus objetivos. Se presentan
al volver al reactor, con ocho segundos de separación, sin interrumpir una sobrecarga
activa ni un panel de compra/ajustes. Hay comunicaciones breves al instalar el primer
microgenerador, activar sobrecarga y alcanzar sincronía. Se muestran en el comunicador
bajo el reactor; sus voces se pausan si se oculta ese panel para mantener visibles los
subtítulos de cualquier diálogo audible.

Las escenas usan cámara con movimiento suave, retratos con entradas fundidas,
partículas acotadas, hojas del sello que se separan y cambios de iluminación. En la
ascensión, un oscurecimiento y la reducción de música preceden al nuevo encendido.
Mara, NARA y la voz del núcleo aportan contexto a las misiones y a la revelación.
Al terminar la escena del enlace, se abre la sintonía existente para elegir una voz.
Las ascensiones posteriores usan una intervención corta de Mara.

La pestaña Historia permite repetir el despertar, cada escena desbloqueada y la
ascensión. Las repeticiones no conceden recursos ni ejecutan reinicios. **Omitir
escena** siempre permite seguir jugando. La producción automática continúa durante
las escenas; el acceso a compras y toques cubiertos está bloqueado. Los subtítulos
se muestran completos inmediatamente, sin tener que esperar una máquina de escribir.
Se puede desactivar el avance automático y continuar manualmente.

Una partida existente recibe una presentación breve del objetivo actual una sola vez.
No reproduce automáticamente todos los capítulos anteriores ni pierde progreso. En
sesiones siguientes, un resumen hablado corto recuerda el objetivo. Al cerrar durante
una escena se conserva lo conseguido y la escena pendiente puede empezar desde el
principio; pausar/reanudar la aplicación mantiene la línea y la posición de voz en
la sesión actual. Si se interrumpe el despertar después del primer toque guardado,
el encendido repetido es visual: no añade otra recompensa de introducción.

## Arte, animación y sonido

- `NarrativeAtlas.png`: atlas original generado realmente con imagegen, 1254×1254
  RGBA con transparencia. Cuatro sprites integrados: nuevo núcleo, NARA, Mara y
  cristal de memoria. Recortes UV separados, pivotes centrales, filtro bilinear,
  Clamp, sin mipmaps ni lectura en CPU. No se eliminaron fondos mediante scripts.
- `OrfeoExterior.png`: ilustración original generada realmente con imagegen,
  1024×1536 RGB. Se utiliza como fondo de la apertura y del final.
- Los fondos de cámara del reactor y transmisión previos se reutilizan. El soporte
  vital reduce el oscurecimiento del escenario; aparece un cristal al recuperar
  las memorias. Las skins y los anillos existentes siguen funcionando.
- El toque añade recuperación elástica amortiguada y una respiración tenue del
  núcleo. Movimiento reducido elimina cámara, oscilación y polvo; efectos suaves
  limitan partículas. Las transiciones oscurecen, no producen flashes blancos.
- 46 líneas de voz en español: **254,2 segundos de síntesis local con eSpeak NG**.
  Son voces robóticas provisionales, no doblaje neural ni actores. El texto es
  original; no hay clonación de voces, grabaciones de terceros ni modelos
  descargados dentro del proyecto. No había servicio de generación de voz
  disponible; las peticiones de modelos Piper a Hugging Face devolvieron HTTP 403.
- Dos sonidos originales adicionales sintetizados: alarma de reserva y motor del
  sello. Se reutilizan los sonidos de encendido/ascensión existentes.
- Nueve AudioSource fijas en total: siete existentes más diálogo y ambiente de escena.
  Música, efectos y voces tienen controles independientes; la música baja al hablar.
  El silencio de efectos heredado también silencia voces al migrar ajustes antiguos.
  Los subtítulos permanecen disponibles aunque todas las voces estén desactivadas.

Las voces y su mezcla requieren escucha real en Unity y móvil antes de considerarse
finales. Se entregan WAV utilizables y referencias correctas, no simples textos ni
URLs externas. Para doblaje final basta reemplazar cada WAV conservando su nombre.

## Datos y persistencia

`Resources/Narrative.json` es el catálogo de escenas, interlocutores, textos,
retratos, fondos y archivos de voz; contiene 19 secuencias (incluidos los resúmenes)
y 46 intervenciones. `NarrativeCatalog.Validate` comprueba IDs, rutas, longitudes y
presencia de escenas obligatorias. Los textos de voz se generan desde ese mismo JSON.

`NarrativeDirector` presenta las escenas sin calcular ingresos, compras o premios.
El primer toque llama al mismo motor del juego. La ascensión ya debe haberse
confirmado y guardado mediante la transacción existente antes de presentar su escena.
Saltar, repetir o cerrar una escena de ascensión nunca vuelve a conceder esencia.

El campo opcional `story.narrative` conserva introducción, presentación de llegada,
capítulos vistos, primera ascensión y tres comunicaciones. Se copia profundamente
al guardar/ascender; la confirmación se persiste antes de actualizar el estado vivo.
Un fallo de guardado libera la interfaz, informa el problema y no marca la escena
como vista. No se vuelve a forzar esa misma escena durante la sesión fallida.
No se modificaron precios, multiplicadores, objetivos o recompensas del motor.

## Cómo probar en Unity

1. Abre `Assets/ReactorDeCaos/Scenes/Main.unity` con Unity 6000.0.85f1 y pulsa Play.
   Una partida nueva abre el despertar; una existente muestra su objetivo y conserva
   datos. Para ver el inicio en una partida existente: Historia, **Repetir el despertar**.
2. En el despertar, usa Continuar hasta que se destaque el núcleo. Tócalo; comprueba
   que genera 1 E en una partida nueva y que pulsaciones duplicadas no repiten el evento.
3. Omite o termina la escena. Toca y compra un microgenerador. Cumple el primer
   objetivo y espera en Núcleo; la siguiente escena se presenta sin abrir Historia.
4. Cambia entre avance automático/manual, silencio y volumen de voces. Abre Ajustes
   mientras habla un personaje: la escena y su voz deben detenerse y continuar al cerrar.
5. Visita las demás pestañas durante una comunicación menor: su voz debe pausarse.
   Comprueba que la economía automática siga avanzando mientras lees una escena.
6. Recorre los capítulos y repítelos desde Historia. Revisa los textos sin recortes,
   las transparencias, el sello del sector rojo y la elección de sintonía.
7. Confirma una ascensión usando sus requisitos y revisión habituales. Omite o termina
   la escena y comprueba que solo se ejecutó un reinicio. Repite la escena desde Historia:
   los recursos y el número de ascensiones deben permanecer intactos.
8. Pausa/reanuda, cambia foco, activa movimiento/efectos reducidos y revisa los cinco
   tamaños auditados, particularmente 320×568 y pantallas con zona segura.

Las pruebas PlayMode crean copias de seguridad y restauran los archivos del jugador.
Para probar una partida nueva manualmente, usa un perfil de prueba o copia antes el
archivo `reactor-save.json`; no es necesario borrar una partida real para repetir escenas.

## Validación ejecutada

- **134 pruebas .NET aprobadas**, incluidas 12 nuevas de introducción, migración,
  persistencia fallida, orden de escenas, repetición, comunicaciones, ascensión y catálogo.
- **Seis ensamblados compilados** contra API oficiales de Unity. Las nuevas pruebas
  PlayMode cubren primer toque, interrupción/repetición, raycasts, diálogos automáticos,
  producción, bloqueo de compras cubiertas, ajustes, silencio independiente y pausa.
- Tres simulaciones de 30 minutos y 54 ciclos de ascensión: todos pasan. Los JSON
  económicos son idénticos a la revisión anterior de historia.
- Auditoría estática de **44 controles en cinco perfiles**, mínimo 48,33 píxeles
  lógicos. Incluye omitir, continuar, encendido, ajustes y repetición. No es una
  validación nativa de tipografía, animación, dibujo o gestos.
- 46 WAV comprobados contra sus subtítulos y hashes: mono PCM, 22050 Hz, picos <0,5,
  señal no vacía y extremos fundidos. Atlas con canal alfa real; GUID completos y únicos.
- Intento con el Editor fijado: **salida 198**, `No valid Unity Editor license found.
  Please activate your license.` No se ejecutó Main ni las pruebas nativas. No hay
  revisión audiovisual en escena, medición Android ni aprobación de calidad del doblaje.

Evidencia: `Docs/Cinematics/checks.json`, `media.json`, `voice.json`, `layout.json` y
`Balance/`. Los ajustes incidentales del Editor quedaron fuera del repositorio.

Reproducción de medios: `Tools/generate-narrative-audio.py` requiere
`espeakng-loader==0.2.4`; auditoría: `python3 Tools/audit-cinematics.py` (Pillow).
El sintetizador se instaló únicamente en el entorno de herramientas y no se distribuye
con Unity. Fuentes del sintetizador: https://github.com/espeak-ng/espeak-ng y
https://github.com/thewh1teagle/espeakng-loader. Los WAV son la representación de los
textos originales del proyecto; no se usaron muestras de audio de terceros.
