# Orfeo: campaña secuencial, revisión 1

La historia es el recorrido principal. NARA despierta al jugador para restaurar el
soporte vital; al conseguirlo, una voz pide ayuda. Buscar a Mara conduce al sector
rojo, a la caja negra, a la tripulación y a una reconstrucción que conserva sus memorias.
Los personajes no revelan lo que todavía no se ha descubierto. El archivo deja de ser
el lugar donde hay que buscar la historia: permite consultar o repetir lo vivido.

## Recorrido implementado

| Etapa | Acción del jugador | Consecuencia y siguiente sistema |
|---|---|---|
| Despertar | Avanzar el diálogo y encender físicamente el núcleo | Primer 1 E; Potencia y Microgenerador; misión de soporte vital |
| Volver a respirar | Potencia 1, Microgenerador 1 y 100 E producidas; restaurar soporte vital | Ventilación y luces; llamada de Mara; combo, comunicaciones y logros |
| Una voz al otro lado | Combo histórico ×1.5 **o** 5 E/s, y 1 K E; localizar a Mara | Antenas en línea; plasma, sobrecarga y su mejora |
| La puerta roja | Plasma 1, una sobrecarga en esta misión y 5 K E; abrir la puerta | Sello abierto; habitación vacía; turbina cuántica |
| Una habitación vacía | Turbina cuántica 1 y 25 K E; leer caja negra | Registro de transferencia; núcleo auxiliar |
| Los que se quedaron | Núcleo auxiliar 1 y 100 K E; estabilizar enlace | Memorias recuperadas; identidad del jugador; elegir sintonía |
| No volver a olvidarlos | Sintonía elegida y 1 M E; preservar recuerdos | Copia persistente; escena final; primera ascensión habilitada |
| Ascensión | Revisar pérdidas, permanencias, rama y recompensa; confirmar | Reinicia instalaciones; conserva campaña, planos, sintonía y recuerdos; epílogo |

Las cantidades de misión usan energía **producida en total**, no saldo disponible.
Comprar no hace retroceder la misión. Los costes y multiplicadores originales no
cambiaron. No hay alternativa de acumular más energía para saltarse máquinas o la
puerta: la misión requiere la acción relacionada. La señal admite producción automática
para no obligar a mantener un combo. La sobrecarga de la puerta es una intervención
puntual; los demás recursos pueden producirse automáticamente.

La tarjeta de misión siempre indica el siguiente paso y se destaca cuando puede
realizarse. Su botón realiza una acción concreta del mundo, como abrir la puerta,
y presenta inmediatamente su consecuencia. No hace falta abrir el archivo para verla.
Las compras previas útiles cuentan; nunca se exige volver a comprar una máquina solo
para registrar una misión. Cada consecuencia debe terminar antes de habilitar la
tecnología siguiente. Elegir NARA o tripulación es obligatorio para preservar la memoria;
ambas rutas conservan a todos y tienen la misma economía. La sintonía se puede cambiar.

## Presentación y controles

- El despertar aparece automáticamente en un perfil nuevo. Avanzar o pulsar Atrás
  no saltea el encendido. Los diálogos muestran el texto completo inmediatamente.
- En primeras escenas, Continuar acelera las líneas y el otro control cambia lectura
  automática/manual. Las repeticiones sí tienen **Omitir escena**. Las voces se pueden
  silenciar independientemente; los subtítulos siguen disponibles.
- Mejoras, módulos y sus conductos aparecen al habilitarse. La navegación redistribuye
  el espacio entre las pestañas disponibles. Logros oculta tecnologías aún desconocidas.
- El archivo muestra capítulos experimentados y la misión actual, sin acceso a futuros
  registros. La elección de sintonía se abre directamente tras su escena.
- La primera ascensión aparece al preparar la memoria, con un requisito narrativo
  explícito además del económico. No se puede ejecutar prematuramente desde el motor.
- La estación añade ventilación, antenas, sello abierto, registros y enlace de memoria
  mediante geometría 2D fija. Se reutilizan los sprites y fondos originales existentes;
  no se generaron imágenes nuevas para esta revisión. Efectos y audio siguen acotados.
- Se revisaron 21 líneas y se regeneraron sus WAV locales con eSpeak NG. Hay 46 líneas
  totales, aproximadamente 285 segundos. **Son voces robóticas provisionales**. No se
  afirma doblaje profesional ni generación neural. Los WAV no modificados se conservaron.
- La producción continúa durante los diálogos, mientras las compras y los toques
  cubiertos quedan bloqueados. Ajustes y suspensión pausan la presentación y la voz.

## Guardado y arquitectura

`ReactorApp` usa exclusivamente `reactor-campaign-1.json` en persistentDataPath,
con la misma escritura atómica y copia `.bak`. El usuario autorizó descartar el progreso
anterior: `reactor-save.json` queda intacto, separado y no se importa. Preferencias de
sonido y accesibilidad conservan su propio archivo/clave.

`Core/Campaign.cs` contiene gates, requisitos, acciones y el estado inicial.
`story.campaignRevision = 1` identifica el recorrido. `completed`, `read` y
`narrative.scenesSeen` avanzan juntos **después** de guardar la consecuencia.
`missionReady` se guarda antes de abrir la escena y permite retomarla si se cierra
el juego. Una escena interrumpida vuelve a su inicio; no se conserva la línea exacta
entre sesiones. Una pausa dentro de la sesión sí conserva la línea y el audio.

Un fallo de escritura de misión no abre la escena. Un fallo al confirmar su consecuencia
mantiene la última línea y permite reintentar; no libera el siguiente capítulo. No hay
catch-up de campaña por energía, hitos o recompensas de logros. El encendido inicial
acredita una sola unidad, y repetirlo tras un cierre guardado no concede otra.
La ascensión copia la campaña y se guarda antes de sustituir el motor vivo.

Las rutas antiguas de datos/core siguen disponibles para pruebas económicas y de
compatibilidad aisladas; **no son la ruta de entrada de Unity**. Las simulaciones de
campaña construyen `NewCampaignState()` y atraviesan las misiones reales.

## Pruebas ejecutadas y límites

- 150 pruebas .NET de economía, persistencia y narrativa aprobadas. Las 16 pruebas
  adicionales cubren encendido obligatorio, gates, saldo sin saltos, misiones, guardado
  fallido/reentrante, recuperación de `.bak`, sintonía y ascensión conservando campaña.
- Compilación de los seis ensamblados con las API oficiales de Unity. Se adaptaron
  las pruebas PlayMode a escenas obligatorias y se separaron los checkpoints económicos
  de las partidas nuevas. **Compilar esas pruebas no equivale a ejecutarlas.**
- Tres simulaciones completas desde cero hasta la primera ascensión: activo,
  intermitente y principalmente automático. Usan el Economy.asset real, las duraciones
  de voz más margen de lectura y compras guiadas por misión/rendimiento. Resultados en
  `Campaign/balance/campaign.json` y CSV: aproximadamente 7:11, 9:58 y 11:28 hasta
  completar las seis misiones. Incluyen despertar y diálogos de misiones; no incluyen
  el tiempo de revisar la confirmación de ascensión ni su escena posterior.
- El perfil principalmente automático usa 53 toques (reparación inicial y sobrecarga
  de la puerta). No se presenta como una ruta completamente sin interacción.
- Además: tres simulaciones económicas de 30 minutos y 54 ciclos de ascensión,
  sin cambiar las fórmulas económicas.
- Auditoría estática de 44 controles en cinco perfiles, mínimo 48,33 píxeles lógicos.
  La navegación de campaña usa anchos mayores cuando hay menos pestañas.
- Auditoría de 46 WAV, subtítulos, hashes, PCM y metadatos; assets existentes con alfa
  real. No es una escucha ni una inspección de capturas de Unity.
- Editor 6000.0.85f1: intento nativo finalizó con **198**, `No valid Unity Editor
  license found. Please activate your license.` No se pudo ejecutar Main, PlayMode
  ni probar Android. El log también muestra avisos de base de datos de solo lectura
  y cierre EPIPE del Package Manager; el bloqueo determinante sigue siendo la licencia.

Los tiempos simulados no predicen sesiones humanas: no modelan exploración, errores
ni lectura lenta. Recomendación: probar el primer arco con jugadores antes de ajustar
precios. No alargar artificialmente las primeras reparaciones. Quedan pendientes
revisión visual en la escena, legibilidad en pantallas pequeñas, escucha y mezcla,
gestos y rendimiento Android, y doblaje definitivo.

## Recorrido manual en Unity

1. Abrir Main.unity con el Editor fijado. Usar un perfil de prueba; esta versión ya
   inicia campaña nueva si solo existe el guardado antiguo.
2. Avanzar hasta el núcleo interactivo. Intentar Continuar/Atrás antes de tocarlo:
   debe permanecer ahí. Tocar una vez; comprobar 1 E y completar el despertar.
3. Comprobar que solo se muestran Potencia y Microgenerador, sin combo, sobrecarga ni
   ascensión. Comprar ambas reparaciones, producir 100 E y restaurar soporte vital.
4. Seguir la tabla. Comprobar que cada tecnología aparece después de su escena,
   que los recursos no saltean el objetivo y que el archivo no revela registros futuros.
5. Cerrar durante una consecuencia y reabrir: debe retomarse, sin duplicar desbloqueos.
   Repetir una escena desde Archivo: no debe cambiar energía, campaña ni ascensiones.
6. En la misión final elegir sintonía, reunir la energía y preservar recuerdos.
   Antes de terminarla Ascender debe estar bloqueado. Después revisar y confirmar.
7. Comprobar epílogo, instalaciones reiniciadas, planos y archivo conservados.
8. Verificar ajustes, silencios independientes, pausa, modo reducido, cinco perfiles
   móviles y raycasts. Ejecutar `Tools/test-unity.sh` con una licencia válida.

Comandos: `bash Tools/test-core.sh`, `python3 Tools/check-unity-api.py`,
`bash Tools/simulate-balance.sh TestResults/Campaign`, `python3 Tools/audit-cinematics.py`
y `python3 Tools/audit-mobile-layout.py`.
