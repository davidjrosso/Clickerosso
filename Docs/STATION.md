# Orfeo vive — salas, expedición y la flor de Mara

La estación pasa a ser un lugar que recuperamos. La expansión conserva la campaña
existente: no cambia su orden, sus tecnologías ni sus requisitos narrativos.
Después del soporte vital, la pestaña del Archivo muestra **Orfeo**, con acceso al Archivo.
La misión principal sigue bajo el reactor. Esta historia adicional puede completarse
antes o después de ascender; no exige terminarla para continuar la campaña principal.

## Contenido integrado

| Paso | Requisitos | Coste / tiempo | Resultado |
|---|---|---|---|
| Invernadero | Soporte vital restaurado | 150 E | Sala encendida, +5% producción, primera conversación de Mara |
| Jardín exterior | Invernadero y primera conversación | Dron gratuito, 3 minutos | Cápsula de semillas y energía; segunda conversación |
| Enfermería | Puerta roja abierta, cápsula recogida y segunda conversación | 2.5 K E | Cápsula reparada, +5% producción, tercera conversación |
| Observatorio | Caja negra recuperada, enfermería y tercera conversación | 12 K E | Espejos alineados, +5% producción, cuarta conversación y floración |

Los tres bonos se suman: multiplicador de producción **1 + 0.05 × salas**, hasta 1.15.
Afectan a los generadores y a su producción base en ausencia. El enlace de la rama
Toques recibe indirectamente ese aumento mediante su fórmula existente. Los precios,
combo y multiplicador de sobrecarga anteriores no cambian.

La historia de Mara, **Una flor para volver**, tiene principio y cierre: pide recuperar
una semilla, el dron la encuentra, reparamos su cápsula y orientamos la luz de su estrella
para verla florecer. Usa cuatro escenas automáticas, dieciséis líneas originales con
subtítulos y voz sintética local, y repeticiones desde Orfeo. Las condiciones respetan
lo ya descubierto en la campaña; no revelan anticipadamente su identidad o el final.
Una escena pendiente se recupera después de cerrar. Repetirla no da recompensas;
la producción normal sigue funcionando mientras se presenta.

El panel incluye una vista 2D de las tres salas: luces y plantas del invernadero,
equipo de conservación, telescopio, flor final y dron animado. Es geometría original
integrada en uGUI, con CanvasRenderer obligatorio, sin instanciar partículas u objetos
por fotograma. El modo reducido detiene el movimiento del dron. Se reutilizan los
retratos y fondos existentes para los diálogos; **no se generaron imágenes nuevas**.
La presentación es provisional y requiere revisión visual en Unity.

## Expedición y producción en ausencia

Solo hay un dron en ruta. La primera expedición trae la cápsula; las posteriores traen
suministros de energía. No hay sorteos, peligros aleatorios ni pérdida por llegar tarde.
La recompensa se muestra antes de enviar y queda fijada al salir:

`máximo(200, mínimo(50 000, producción base al partir × 60 segundos))`

El tiempo es real: avanza durante el juego y durante suspensión/cierre. Un regreso
completo espera indefinidamente hasta que se recoge. La recompensa y la cápsula se
guardan antes de celebrar; el mismo resultado no se puede reclamar dos veces.

Al iniciar o reanudar se calcula:

`producción base guardada × mínimo(tiempo ausente, 28 800 segundos)`

Se guarda como reserva pendiente, separada del saldo. El jugador puede reclamarla
cuando quiera en Orfeo. Varias ausencias pueden acumular reserva, con límite por
cada ausencia. No se multiplica por combo ni por sobrecarga. Sin generadores no se
inventa producción. El dron puede regresar aunque la producción sea cero.
Las cantidades se limitan al máximo económico del juego; si no cabe una recompensa
completa, el reclamo permanece pendiente. El informe muestra energía y tiempo reconocido.
Cambios de foco breves no abren el panel automáticamente, pero su producción se conserva.

No hay notificaciones push, rachas, calendario obligatorio ni premios que expiren.
Los valores viven centralizados en `Core/StationProgress.cs` / `StationCatalog`.
Cambiar duración exige mantener coherentes el diálogo introductorio y su WAV.

## Guardado y reloj

- Se conserva `reactor-campaign-1.json`. El campo opcional `station` se inicializa
  sin reiniciar energía, habitaciones de campaña, mejoras o ascensiones.
- Una partida que todavía no tenía reloj no recibe ingresos retroactivos. La primera
  apertura fija el punto de partida; las siguientes cuentan normalmente.
- Salas, recuerdos, cápsula obtenida, dron y reserva pendiente sobreviven a la ascensión.
  La tasa guardada se actualiza a los generadores iniciales del nuevo ciclo.
- Se usa UTC local con una marca máxima persistida. Retroceder el reloj no da tiempo
  adicional; un salto hacia delante está limitado por la regla de ocho horas.
  No hay servidor: no es un sistema de reloj resistente a manipulación del dispositivo.
- El tiempo de pausa se separa de los fotogramas activos para no contarlo dos veces.
  Incluso si falla el guardado de pausa, se conserva el límite temporal de esa sesión.
- Cada compra de sala, envío, recepción, recuerdo y reclamo trabaja sobre un candidato
  separado. Un fallo deja el estado anterior; la escritura atómica y `.bak` se mantienen.
- Si no se puede guardar el regreso, la simulación espera y reintenta cada dos segundos.
  Se informa el problema. Esto evita que un guardado posterior borre la producción aún
  no procesada. Los ajustes siguen accesibles; no se inventa una recompensa sin guardar.

## Validación ejecutada

- **167 pruebas .NET aprobadas**: 17 nuevas sobre costes, secuencia, progreso conservado,
  relojes adelantados/atrasados, pausa fallida, llegada fallida, límite de ocho horas,
  cero producción, reservas sin reclamar, reentrada, copias profundas, límites,
  recuperación desde backup y campañas que no se saltan al reclamar.
- Seis ensamblados compilados contra las API oficiales de Unity, incluidas pruebas
  nativas nuevas para navegación, raycast del Archivo, restauración, escenas, expedición,
  callbacks duplicados, informe al volver y persistencia. **No ejecutadas en PlayMode**.
- Dos simulaciones desde partida nueva hasta el cierre de Mara y primera ascensión:
  una activa y otra principalmente automática. Ambas incluyen una salida de cuatro
  minutos con el dron en ruta. El motor real registra 12:26 y 15:25 de tiempo total,
  respectivamente, con diálogos y ausencia. Las tres salas y cuatro recuerdos sobreviven.
  Son compras guiadas, no mediciones con jugadores ni validación de entretenimiento.
- Tres campañas anteriores completas, tres perfiles económicos de 30 minutos y
  54 ciclos de ascensión también pasan. Sin comprar salas, la economía anterior se mantiene.
- Auditoría estática de 51 medidas de controles en cinco perfiles, mínimo 48.33 píxeles
  lógicos. Incluye acciones de la estación y navegación al archivo; no valida tipografía
  nativa, gestos ni animaciones.
- 62 WAV (16 nuevos), subtítulos, hashes, canales, PCM y metadatos auditados. Voces
  **provisionales eSpeak NG**, no doblaje neural. La síntesis se hizo realmente y los
  archivos están integrados en Resources; la escucha en escena sigue pendiente.
- Intento con Unity 6000.0.85f1: salida **198**, `No valid Unity Editor license found.
  Please activate your license.` No se ejecutó Main ni Android. Los ProjectSettings
  incidentales se guardaron fuera del repositorio.

Evidencia en `Docs/Station/{checks,media,layout}.json`, `Docs/Station/balance/` y
`Docs/Cinematics/voice.json`. Quedan pendientes revisión audiovisual real, ajuste de
mezcla/voz, legibilidad y prueba táctil/FPS en dispositivos. No se considera arte final.

## Recorrido manual

1. Abrir Main.unity con licencia válida. Completar el soporte vital y entrar en Orfeo.
2. Restaurar invernadero; comprobar gasto de 150 E, aumento de producción y escena.
3. Enviar el dron; comprobar premio fijado y bloqueo de un segundo envío.
4. Suspender o cerrar cuatro minutos. Volver: comprobar informe, dron listo y saldo
   sin acreditar hasta reclamar. Recoger dos veces rápidamente: solo debe entregarse una vez.
5. Completar puerta roja si falta, restaurar enfermería y escuchar a Mara. Recuperar
   caja negra si falta, restaurar observatorio y comprobar floración y cierre de la historia.
6. Repetir una escena: no debe dar premios extra. Probar lectura manual, silencio,
   movimiento reducido y pausa durante el diálogo.
7. Ascender con los requisitos de la campaña: salas, recuerdos, reserva y dron deben
   conservarse; los generadores se reinician según las reglas existentes.
8. Reabrir una partida de campaña anterior: debe conservar progreso, sin inventar
   producción por el tiempo anterior a la actualización.

Comandos: `bash Tools/test-core.sh`, `python3 Tools/check-unity-api.py`,
`bash Tools/simulate-balance.sh TestResults/Station`, `python3 Tools/audit-cinematics.py`,
`python3 Tools/audit-mobile-layout.py`. Con Editor activado: `bash Tools/test-unity.sh`.
Para regenerar solo las voces nuevas: `Tools/generate-narrative-audio.py station_`
con el entorno Python que tiene `espeakng-loader==0.2.4`.
