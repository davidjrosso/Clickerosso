# Fase 4 — Reactor modular y renacimientos

La ampliación conserva la arquitectura C#/Unity existente e incorpora el
renacimiento autorizado después de la fase 3. No cambia los precios, producción,
combo, sobrecarga o hitos del primer ciclo. El conjunto se sigue abriendo desde
`Assets/ReactorDeCaos/Scenes/Main.unity`.

## Construir una máquina visible

Hay siete módulos interactivos, uno por cada entrada del catálogo inicial:
cuatro familias de generadores a los lados y tres mejoras bajo el núcleo.
Los módulos sin comprar aparecen atenuados; cada uno abre su ficha con nivel,
precio, efecto siguiente y condición de desbloqueo. Los generadores muestran
además su producción actual, incluida la sobrecarga. Comprar desde la ficha o
la tienda actualiza el mismo GameEngine y anima la instalación.

Los conductos muestran pulsos hacia el núcleo. Sus segmentos aumentan con el
nivel, con un límite de seis por conexión. Durante sobrecarga los flujos se
sincronizan. El combo enciende doce segmentos que se apagan al descender; los
módulos de potencia y sobrecarga reflejan su actividad. Se reutilizan objetos:
ninguna compra crea un generador visual por unidad adquirida.

Las formas del núcleo son **Semilla, Plasma, Cámara cuántica, Estrella artificial
y Singularidad**. Usan las cinco etapas de hitos existentes, con crecimiento del
sprite, anillos y placas exteriores. La transformación es visual: el dominio
sigue siendo la única fuente de ganancias.

Los módulos están dentro del área recortada del reactor. Sus botones tienen
prioridad sobre el núcleo; los conductos y VFX no reciben raycasts. Las fichas
y menús bloquean los clics de fondo, mientras la producción continúa.

## Colapso controlado

El botón **EVOLUCIÓN**, bajo la tienda, abre tres pestañas: RENACER, MAESTRÍAS y
SKINS. La pantalla principal mantiene un objetivo cercano de hitos y el objetivo
lejano de energía para renacer.

El primer renacimiento se habilita al generar **1 millón de energía durante el
ciclo**. Cuenta la energía generada, no el saldo disponible: gastar en compras
no reduce la recompensa. Guardados anteriores conservan todo su progreso y su
energía histórica cuenta como el ciclo inicial.

La revisión previa muestra esencia, especialización, bonificación permanente,
la skin que se desbloquea y los elementos que se reinician o conservan. Hay una
segunda acción explícita **CONFIRMAR COLAPSO**, y una opción de cancelar. Escape
primero cancela esa confirmación. Si la recompensa cambia mientras se revisa,
se solicita revisar el importe actualizado; no se ejecuta un reset silencioso.

| Se reinicia | Se conserva |
| --- | --- |
| Saldo y energía generada del ciclo | Energía histórica de todos los ciclos |
| Niveles de compras y generadores | Esencia disponible y total ganada |
| Hitos y etapa del ciclo | Maestrías permanentes y colección de skins |
| Combo, carga y sobrecarga activa | Ajustes de accesibilidad y sonido |

El nuevo ciclo empieza con **un microgenerador y potencia de toque nivel 1**.
No se regala saldo ni energía que compute para el siguiente renacimiento. La
especialización puede cambiarse gratis al renacer; las maestrías compradas en
las tres ramas siguen aplicándose. No hay recompensas de tiempo desconectado.

`TryPrepareRebirth` crea un motor candidato separado. `RebirthTransaction`
guarda primero su snapshot y solo después ReactorApp lo adopta. Si falla la
escritura, permanecen intactos energía, módulos, combo y sobrecarga del ciclo
actual. Los efectos de colapso se reproducen después de guardar con éxito.

## Economía permanente y fórmulas

Todos los parámetros económicos nuevos están en `Resources/Economy.asset`,
sección **values.rebirth**; las fórmulas están en GameEngine.Rebirth.cs.

- Recompensa: `floor(3 × sqrt(energía del ciclo / 1.000.000))`, una vez superado
  el umbral; máximo 100.000 esencias por colapso y espacio disponible hasta
  1.000 millones de esencia ganada. Máximo 1 millón de renacimientos.
- Bonificación global: `1 + 0,25 × sqrt(esencia total ganada)` para potencia
  base de toque y producción automática. Gastar esencia no la reduce.
- Maestría de cada rama: `1 + 0,05 × nivel`, máximo 20 niveles. El siguiente
  nivel cuesta `nivel actual + 1` esencias: 1, 2, 3… El modelo valida también
  que la esencia gastada y disponible sumen la esencia ganada.
- Especialización TOQUES: ×1,5 en esa rama; añade el 8% de producción base
  automática a la potencia por toque antes del multiplicador de rama, combo y
  sobrecarga. Mantiene útiles los toques cuando crecen los generadores.
- Especialización AUTOMATIZACIÓN: ×1,5 a la producción automática.
- Especialización SOBRECARGA: ×1,8 al multiplicador de sobrecarga, además de
  su maestría; afecta toques y producción mientras está activa. El ×3 inicial
  pasa a ×5,4 con esta especialización y sin maestrías de sobrecarga.

Más formalmente: `A` es la producción base del catálogo por niveles, `P` la
bonificación global y `B(rama)` la maestría multiplicada por la especialización
si está elegida. La producción base es `A × P × B(auto)`. La potencia de toque
es `[(base + niveles) × P + enlace] × B(toques)`, donde `enlace` es el 8% de esa
producción base solo al especializarse en toques. Después se aplica combo y,
si corresponde, sobrecarga. No hay una bonificación económica oculta en skins,
conductos o placas decorativas.

La energía sigue usando double con suma compensada y límite explícito de 1e150.
La esencia usa enteros acotados; se rechazan datos inválidos, cantidades negativas
y estados incoherentes. El histórico conserva el resto de baja magnitud al
archivar un ciclo. Los multiplicadores expuestos a la UI provienen del motor.

## Colección de skins

| Skin | Desbloqueo | Silueta procedural |
| --- | --- | --- |
| Aurora | Inicio | Placas cian y crecimiento de segmentos |
| Corona solar | Renacimiento 1 | Doce rayos ámbar |
| Vacío violeta | Renacimiento 3 | Seis placas triangulares magenta |
| Prisma | Renacimiento 5 | Ocho puntas cristalinas verdes |

Una skin recién desbloqueada se equipa al renacer; las anteriores pueden
reequiparse desde SKINS en cualquier momento. Elegir otra no cambia ninguna
estadística. La familia visual usa el núcleo original y geometría uGUI propia,
sin imágenes nuevas generadas con IA ni paquetes gráficos adicionales.

## Guardados y actualización

Se mantienen la ruta `reactor-save.json`, su envoltorio con checksum y la copia
`.bak`. El payload evoluciona a versión 2. La versión 1 se migra en memoria sin
perder saldo, niveles, generadores, hitos o sus restos numéricos; el siguiente
guardado escribe versión 2. La prueba incluye recarga real desde disco y fallo
de escritura durante un colapso. Las versiones antiguas del juego no conocen el
payload 2: para seguir la partida, usar esta versión o una posterior.

Esta actualización incluye scripts nuevos y cambios en Economy.asset. Instala
el proyecto completo actualizado, o integra todos esos archivos y sus `.meta`
en tu copia, con Play detenido. El parche anterior de dos scripts solo corregía
CanvasRenderer y no contiene esta ampliación. No borres el progreso del usuario.

## Pruebas y recorrido en Unity

Ejecutar `bash Tools/test-core.sh`, `bash Tools/simulate-balance.sh` y
`python3 Tools/check-unity-api.py`. Los resultados finales se conservan en
[Phase4](Phase4/). El simulador usa el motor real y el asset real: tres perfiles
originales de 30 minutos, más seis ciclos para cada combinación de tres ramas
y tres estilos. Una heurística compra la mejor rentabilidad marginal estimada;
no representa a todos los jugadores ni demuestra una estrategia óptima.

Con un Editor licenciado, ejecutar `Tools/test-unity.sh` y revisar Main:

1. Confirmar que todos los anillos, halos, conductos y coronas tienen
   CanvasRenderer. Los tres tipos de gráficos nuevos declaran RequireComponent,
   igual que la corrección anterior de RingGraphic y GlowGraphic.
2. Tocar un módulo lateral, abrir su ficha y comprarlo. El precio se descuenta
   una vez; se instala el módulo y aparece flujo hacia el núcleo. El clic de
   apertura no genera energía accidentalmente. Probar también una ficha bloqueada.
3. Comprar desde la tienda, aumentar niveles y comprobar conexiones, etiquetas
   y potencia actual/siguiente. En especialización de toques, comprar un generador
   debe actualizar también su ganancia por toque.
4. Construir y dejar decaer combo. Activar sobrecarga y observar sincronización,
   multiplicador, tiempo restante y finalización sin tapar botones.
5. Abrir EVOLUCIÓN, revisar un colapso y cancelarlo: no debe cambiar el ciclo.
   Confirmarlo después: saldo cero, generador inicial, esencia, nueva skin y
   sobrecarga/combo reiniciados. Reabrir Play y comprobar persistencia.
6. Comprar maestrías con esencia y cambiar de skin. Las maestrías cambian sus
   efectos indicados; cambiar skin no cambia energía, producción ni ganancias.
   Renacer otras veces permite cambiar especialización y desbloquear la colección.
7. Con efectos suaves y movimiento reducido, comprobar fichas, niveles, combo
   y carga legibles. Continúan los ingresos; disminuyen flujos y movimiento.
   No crecen los pools de 50 efectos ni las cuatro voces de audio.
8. Probar pantallas verticales y áreas seguras, sesiones largas, pausa y retorno
   en Android; revisar latencia de input, GPU, audio y tiempo de frame.

Las pruebas PlayMode nuevas verifican raycasts de módulos, compra desde ficha,
objetos acotados, cancelar/confirmar, protección frente a doble confirmación,
persistencia de esencia/maestrías y renderers presentes en toda la escena.
**Compilan pero no se han ejecutado en este entorno**: el Editor termina con
código 198, `No valid Unity Editor license found`. No se ha validado visualmente
Main ni un APK; el detalle de compilación y simulación no sustituye esa revisión.


## Resultados del balance ejecutado

Se ejecutaron **88 pruebas** de dominio/guardado (todas aprobadas), seis ensamblados
compilaron y los nueve escenarios completaron seis ciclos cada uno: **54 ciclos**.
Los JSON y las trazas de compras de los tres perfiles originales coinciden
exactamente con la fase 2. Solo se añadieron parámetros bajo `rebirth`.

| Rama | Estilo | Primer renacimiento | Siguiente ciclo | Sexto ciclo |
| --- | --- | ---: | ---: | ---: |
| Toques | Activo | 179.2 s | 83.8 s | 63.5 s |
| Toques | Intermitente | 372.7 s | 216.6 s | 151.8 s |
| Toques | Pasivo | 561.7 s | 409.2 s | 298.4 s |
| Automatización | Activo | 179.2 s | 93.9 s | 67.6 s |
| Automatización | Intermitente | 372.7 s | 163.5 s | 111.2 s |
| Automatización | Pasivo | 561.7 s | 248.5 s | 160.1 s |
| Sobrecarga | Activo | 179.2 s | 90.2 s | 66.9 s |
| Sobrecarga | Intermitente | 372.7 s | 179.0 s | 130.8 s |
| Sobrecarga | Pasivo | 561.7 s | 409.2 s | 298.4 s |

Activo: dos toques por segundo. Intermitente: dos por segundo durante cinco
segundos de cada quince. Pasivo: 25 toques únicamente al iniciar el primer ciclo;
después solo generadores. Se activa sobrecarga cuando está disponible y se gasta
la esencia en maestría de la rama elegida. No se acelera artificialmente el motor.

En esta política, Toques recupera antes el ciclo activo; Automatización gana en
intermitente y pasivo. Sobrecarga es una alternativa cercana en juego activo,
pero no fue la más rápida de estos tres perfiles. Su multiplicador se ajustó
a ×1,8 para compensar su duración limitada. Hace falta medir decisiones humanas
y sesiones reales antes de considerar definitivo el balance. Se recomienda
observar si los ciclos tardíos resultan demasiado breves y ajustar umbral o
bonificaciones con esa evidencia, conservando el primer ciclo.
