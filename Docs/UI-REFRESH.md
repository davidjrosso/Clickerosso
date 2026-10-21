# Reactor integrado, navegación y recompensas

## Qué cambia

La pantalla principal dedica su espacio al reactor. La barra inferior abre
**Reactor**, **Mejoras**, **Logros** y **Ascensión**; el saldo y la producción
permanecen arriba. La tienda y los logros tienen desplazamiento independiente.
La economía continúa funcionando al consultar otra pestaña.

Los rectángulos oscuros de los módulos procedían de sus botones: el atlas y el
núcleo ya tenían alfa real. Ahora los botones tienen fondo transparente, y cada
pieza se asienta en un soporte circular de metal generado con geometría uGUI.
Los conductos enlazan esos soportes con el núcleo. Se amplió el espacio central
y se alinearon las partículas y las ondas con la nueva posición del núcleo.
El fondo es una nueva ilustración original generada con OpenAI imagegen e
integrada en `Resources/Art/ReactorBackdrop.png`, conservando su GUID, importador
y referencia. No es una captura de la escena ni contiene UI o cifras pintadas.

La tienda separa nombre, nivel, descripción y efecto del precio/acción. Los
módulos siguen abriendo su ficha para consultar e instalar mejoras. Se mantienen
skins, etapas, efectos limitados, ajustes de accesibilidad y audio existentes.

## Logros

Nueve objetivos configurables en `Resources/Economy.asset > achievements`.
Los hitos originales siguen desbloqueando tecnologías automáticamente; los
logros son un registro permanente separado y requieren **Reclamar**.

| Logro | Requisito | Recompensa |
| --- | --- | --- |
| Primera chispa | 100 E acumulados | 25 E |
| Manos libres | Microgenerador nivel 1 | 25 E |
| Red de energía | Microgenerador nivel 5 | 100 E |
| Plasma estable | Bobina de plasma nivel 1 | 250 E |
| Salto cuántico | Turbina cuántica nivel 1 | 1.000 E |
| Energía sin límites | 10.000 E acumulados | 1.000 E |
| Potencia estelar | 1.000.000 E acumulados | 10.000 E |
| Un nuevo comienzo | Primera ascensión | 100 E |
| Más allá del núcleo | Tres ascensiones | 500 E |

La energía acumulada incluye ciclos anteriores. Una vez alcanzado un objetivo,
queda disponible aunque se reinicie el nivel de sus generadores al ascender.
Tanto los logros reclamados como los pendientes se conservan entre ciclos.
Los guardados anteriores recuperan objetivos comprobables con sus datos actuales;
no se inventan niveles de generadores de ciclos antiguos que no fueron guardados.

La recompensa es fija, sin multiplicador de combo ni sobrecarga. Cuenta como
energía del ciclo y del histórico, por la misma regla explícita de crédito que
los demás ingresos. No entrega esencia ni cambia las fórmulas existentes.
No hay reclamación automática, caducidad ni recompensas repetibles por ascender.
Los límites numéricos impiden reclamar si no cabe la recompensa completa.

`GameEngine.TryClaimAchievement` prepara un candidato separado, guarda saldo y
registro juntos, y solo entonces actualiza el modelo vivo. El fallo de escritura
no consume el logro ni entrega energía. No se sustituyen el motor, combo ni
temporizadores de sobrecarga. `GameState.Copy/IsValid` conserva y valida ambos
registros; los payloads v1/v2 sin esos campos siguen siendo compatibles.

## Ascensión

La pestaña muestra número de ascensiones, esencia disponible, potencia permanente,
energía del ciclo y una barra del requisito de **1M E**. Comprar no reduce ese
progreso. El botón se habilita al cumplirlo y muestra la recompensa real.
Se elige una especialización y se confirma antes de reiniciar. La confirmación
explica qué se reinicia y qué se conserva, incluidos logros y recompensas
pendientes. Maestrías y Skins siguen disponibles como secciones internas.
Se mantienen los controles de recompensa desactualizada y guardado previo al
reinicio. Los límites de esencia/ascensiones se indican si impiden ascender.

## Cómo probar en Unity

1. Abre `Assets/ReactorDeCaos/Scenes/Main.unity` y pulsa Play. Si falta la fuente,
   usa `Tools > Reactor de Caos > Prepare Project` y los recursos esenciales TMP.
2. En Reactor, comprueba los soportes circulares, transparencia alrededor de los
   módulos, conexiones y respuesta del núcleo. Toca un módulo: debe abrir su
   ficha sin generar energía accidentalmente.
3. En Mejoras, compra potencia y un microgenerador. Comprueba nivel, precio,
   siguiente efecto y producción automática. Desliza hasta los generadores
   bloqueados: deben mostrar su requisito.
4. En Logros, reclama Manos libres o Primera chispa. Comprueba el incremento
   exacto, el estado Reclamado y que otro clic no lo repita. Detén Play y vuelve
   a entrar: la reclamación debe seguir guardada.
5. En Ascensión, comprueba el requisito antes de llegar a 1M E; el botón debe
   estar deshabilitado. Al cumplirlo, revisa la recompensa, elige rama, cancela
   una vez y confirma después. Comprueba que pendientes y reclamados sobreviven.
6. Cambia repetidamente de pestaña, prueba Ajustes desde cada una y el botón
   Atrás. Los ajustes deben quedar por encima de la página y la confirmación
   debe cancelarse al abandonar Ascensión.
7. Repite en relaciones 9:16 y 9:19.5, con safe area y texto largo; comprueba
   tamaños táctiles, recorte del scroll, sonido y modo de efectos reducidos.

## Validación ejecutada y límites

- 99 pruebas .NET aprobadas. Incluyen guardado real en disco, fallo de escritura,
  doble reclamación, migración, copia aislada, límites y conservación al ascender.
- Una prueba controlada de diez minutos usa toques a 2/s, compras por orden de
  catálogo, sobrecarga y reclamaciones. Completó una ascensión, conservó las
  reclamaciones y mantuvo estados válidos. Es una comprobación de estabilidad,
  no una estrategia óptima de compra.
- Seis ensamblados compilados contra las API oficiales del Editor instalado.
- Tres simulaciones económicas de 30 minutos y nueve escenarios de seis ciclos
  pasan. Sin reclamar los nuevos logros, resultados y compras coinciden con
  fase 4. Los parámetros económicos anteriores no cambiaron.
- Se añadieron/actualizaron pruebas PlayMode para pestañas, raycasts, reclamación,
  persistencia, requisitos, número de objetos y CanvasRenderer de los soportes.
  **Compiladas, todavía no ejecutadas.**
- El intento nativo termina con código 198: `No valid Unity Editor license found.
  Please activate your license.` No hay validación visual dentro de Main ni
  medición en Android. La captura del usuario corresponde a la versión anterior.

Evidencia: [UIRefresh/checks.json](UIRefresh/checks.json). Pendientes: ejecutar
EditMode/PlayMode con licencia, inspeccionar la escena real, revisar tipografía,
mezcla de audio y rendimiento en dispositivo. No se conocen fallos en las pruebas
.NET; eso no garantiza la ausencia de problemas del ciclo de vida de Unity.
