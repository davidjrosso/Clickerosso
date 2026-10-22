# Historia — Ecos del primer encendido

Implementado sobre Main y la arquitectura existente. NARA guía la restauración de
la estación Orfeo; las grabaciones revelan la memoria de la tripulación dentro
del reactor y los ciclos anteriores. Seis capítulos, una sintonía opcional y un
epílogo después de la primera ascensión. No hay segundo arco jugable todavía.

## Cómo jugar

1. Abre `Assets/ReactorDeCaos/Scenes/Main.unity` en Unity 6000.0.85f1 y pulsa Play.
2. Toca el núcleo y compra un microgenerador. El acceso de NARA debajo del reactor
   muestra el capítulo/progreso; también puedes abrir la pestaña **Historia**.
3. Desliza el archivo: ilustración, objetivos reales, transmisión, resolución y
   recompensa. **Anterior/Siguiente** recorre los capítulos disponibles sin mostrar
   futuros capítulos bloqueados. **Archivar y continuar** guarda la lectura.
4. No necesitas leer para avanzar. Los objetivos y recompensas son automáticos,
   y los generadores siguen produciendo mientras lees. No hay producción offline.
5. En el capítulo 5 puedes elegir NARA (cian) o núcleo (violeta), y cambiar después.
   Afecta la iluminación y la respuesta final; ambas opciones tienen idéntica economía.
6. Al completar el arco, **Ver ascensión** lleva al panel existente con requisitos,
   efectos del reinicio y confirmación. La historia no ejecuta el reinicio por sí sola.
7. Tras ascender, vuelve a Historia para leer el epílogo. Registros, planos y sintonía
   sobreviven; nunca se abre un diálogo obligatorio al regresar al reactor.

## Objetivos y recompensas

La energía histórica incluye ciclos anteriores y recompensas de logros, como ya
hace el motor. Se exige la energía indicada Y la tarea, o su alternativa pasiva.
Comprar/activar una vez registra la tarea; no exige mantener un combo para leer.

| Capítulo | Energía histórica | Tarea o alternativa | Recompensa |
| --- | ---: | --- | --- |
| La primera chispa | 100 | Microgenerador instalado, o 200 E históricas | Luz de emergencia y comunicaciones narrativas |
| Ecos en los conductos | 1.000 | Combo máximo registrado ≥1,5 o producción base ≥5 E/s, o 1.500 E históricas | Plano permanente de plasma |
| Protocolo rojo | 5.000 | Una sobrecarga activada, o 7.500 E históricas | Plano permanente cuántico |
| La tripulación ausente | 25.000 | Turbina cuántica instalada, o 30.000 E históricas | Plano permanente de núcleo auxiliar |
| El otro lado | 100.000 | Núcleo auxiliar instalado, o 125.000 E históricas | Sintonía y respuesta narrativa |
| El último encendido | 1.000.000 | Sin tarea adicional | Final del arco; epílogo tras ascender |

Los desbloqueos económicos anteriores siguen funcionando. Los planos añaden una
recompensa concreta: el acceso al generador permanece después del reinicio aunque
se reinicien los hitos del ciclo. No entregan máquinas gratis, descuentos, energía
ni niveles. Se conservan costes, producción, combo, sobrecarga, esencia y requisitos
de ascensión. Por eso conviene comprar módulos baratos al comenzar un nuevo ciclo,
aunque ya sea posible ahorrar para los más avanzados.

## Presentación y accesibilidad

- Cinco pestañas: Núcleo, Mejoras, Historia, Logros, Ascender. Se conservan los
  índices de las cuatro pestañas existentes para no romper sus consumidores.
- Ilustración original de NARA realmente generada por IA e integrada en el archivo:
  `Resources/Art/StorySignal.png`, 1536×1024 RGB. Material uGUI estándar, bilinear,
  Clamp, sin mipmaps ni copia legible en CPU. Banner recortado por su contenedor;
  todos los textos son editables y están fuera de la imagen.
- Doce tiras de luz empotradas se restauran por parejas al completar capítulos.
  Son geometría fija con `CanvasRenderer`, sin raycasts, objetos nuevos por toque
  ni parpadeos. La sintonía cambia sus colores, conservando las skins existentes.
- Lectura sin efectos de máquina de escribir, interrupciones ni sonidos adicionales
  invasivos. El aviso de nuevo registro reutiliza el sonido de hito, sujeto al volumen
  y silencio de efectos existentes, y se emite después de guardar.
- Texto narrativo con altura automática y scroll; controles principales fijos al pie.
  La elección también puede alcanzarse desplazando. No se encogen párrafos largos.
- Las recompensas automáticas no dependen de animaciones. Los botones de archivar
  ignoran callbacks duplicados en el mismo fotograma.

## Código y persistencia

`Core/StoryProgression.cs` centraliza un catálogo inmutable de datos: ID, título,
textos, umbrales, tarea y recompensa. `GameEngine` evalúa los objetivos; la UI lee
esos valores. Para editar texto o umbrales se modifica el catálogo, no el HUD.
Actualmente requiere recompilar C#; no hay herramienta de edición de historia en
el Inspector. El orden de los seis capítulos forma parte del contrato del guardado;
no debe reordenarse ni eliminarse sin migración.

`GameState.story` es opcional dentro del formato v2 existente. Las partidas v1/v2
sin ese campo recuperan progreso a partir de energía histórica y niveles guardados,
sin inventar sobrecargas ni combos anteriores. Se mantienen tecnologías ya disponibles.
Los registros leídos y la sintonía usan un candidato separado: primero se persiste,
y después cambia el motor en memoria. Si falla, no se marca la lectura ni la elección.
Las instantáneas y las ascensiones copian profundamente el estado narrativo.

## Validación ejecutada y límites

- 122 pruebas .NET aprobadas, incluidas 23 nuevas para historia/persistencia.
- Seis ensamblados compilados contra las DLL oficiales del Editor, incluyendo
  las nuevas pruebas PlayMode de lectura, producción, raycasts, elecciones y epílogo.
- Auditoría geométrica de 38 controles en cinco perfiles (320×568 hasta 768×1024,
  incluidos insets): mínimo 48,33 píxeles lógicos. No sustituye comprobar texto,
  renderizado y gestos en Unity/dispositivo.
- Tres simulaciones de 30 minutos y nueve combinaciones de especialización/estilo
  con seis ciclos cada una. Evidencia: `Docs/Story/results.json`, CSV y `rebirth.json`.
- Intento nativo con el Editor fijado: salida 198, `No valid Unity Editor license
  found. Please activate your license.` No se ejecutaron Main ni las suites nativas.

| Perfil simulado | Cap. 1 | Cap. 2 | Cap. 3 | Cap. 4 | Cap. 5 | Cap. 6 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Activo, 2 toques/s | 15 s | 22 s | 46,5 s | 88 s | 120,3 s | 179,2 s |
| Intermitente | 39,8 s | 108,1 s | 175,4 s | 251,9 s | 301,6 s | 372,7 s |
| Pasivo tras 25 toques iniciales | 28,8 s | 148,1 s | 263,9 s | 365,5 s | 438,5 s | 561,7 s |

El primer arco es breve con un comprador automatizado eficiente. Estos tiempos
no representan una prueba con jugadores ni incluyen tiempo humano para decidir.
El primer ciclo mantiene los resultados económicos anteriores; se registran ahora
además los tiempos de historia. No se alargó artificialmente la economía para la trama.

La primera simulación de ascensiones FALLÓ sus presupuestos: el algoritmo elegía
el mejor retorno marginal sin limitar la espera y ahorraba miles de segundos para
máquinas cuyo plano ya conocía. Se conserva ese resultado en
`unbounded-wait-rebirth.json`. Se mejoró **la política del simulador**, sin cambiar
el juego: considera compras alcanzables en ≤60 s al ingreso estimado actual y luego
elige retorno marginal. Los nueve perfiles completan seis ciclos; el retorno más
lento es 417,1 s y es menor que su primer ciclo (561,7 s). Esto demuestra una ruta
viable, no que todas las decisiones del jugador sean óptimas. Recomendada una prueba
humana de las compras posteriores al reinicio antes de retocar costes.

Pendiente con licencia: abrir Main, comprobar scroll y textos de los siete registros,
leer mientras generan las máquinas, recorrer cinco pestañas, cambiar ambas sintonías,
ascender/recargar, revisar renderers bajo máscaras, música/pausa y gestos en Android.
La ilustración está integrada, pero su aprobación visual dentro de Unity y el
rendimiento móvil siguen pendientes. No hay voces habladas ni arte exclusivo por
capítulo; se reutiliza la transmisión de NARA y las variaciones procedurales.
