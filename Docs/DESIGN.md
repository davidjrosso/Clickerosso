# Diseño del juego

## Reglas iniciales

| Sistema | Regla |
|---|---|
| Toque | 1 + nivel de potencia |
| Combo | Gracia de 1,5 s; +0,25 cada 5 toques; máximo inicial ×3; pierde un escalón cada 0,75 s después de la gracia |
| Sobrecarga | +4% por toque; activación manual al 100%; ×3 durante 10 s |
| Recarga | Se detiene mientras está activa la sobrecarga |
| Potencia | +1 por nivel; coste inicial 15, crecimiento ×1,55 |
| Capacidad de combo | +0,25 al máximo por nivel; coste 75, crecimiento ×1,7 |
| Duración de sobrecarga | +1 s por nivel; coste 100, crecimiento ×1,7 |
| Microgenerador | +1 energía/s por nivel; coste 25, crecimiento ×1,18 |
| Bobina de plasma | +12 energía/s por nivel; coste 500, crecimiento ×1,16 |
| Turbina cuántica | Primer nivel +60 E/s; incremento siguiente ×1,06; coste 2.500, crecimiento ×1,14 |
| Núcleo auxiliar | Primer nivel +350 E/s; incremento siguiente ×1,08; coste 12.000, crecimiento ×1,14 |
| Desbloqueos | 1.000 E del ciclo: plasma; 5.000: turbina; 25.000: auxiliar |
| Hitos | Energía del ciclo hasta 10.000 millones y cinco microgeneradores; doce objetivos registrados |

Los precios se redondean hacia arriba. Compras y generadores tienen un máximo
de 100 niveles/unidades. El combo solo multiplica ingresos manuales; la
sobrecarga multiplica ingresos manuales y automáticos. Los hitos usan el
total generado en el ciclo o niveles, no el saldo. La economía limita los recursos a 1e150.

La configuración de potencia, tiempos, multiplicadores, producción, umbrales
y curvas de precio se encuentra en `Resources/Economy.asset`, incluido en Git.
Prepare Project lo crea solo si falta. EconomyConfig valida y copia los datos
al empezar; la interfaz consulta efectos actuales/siguientes al motor.

Las tres mejoras se definen como datos con un tipo de efecto conocido. Los
generadores y los hitos usan identificadores estables y listas configurables;
añadir otro generador no requiere ampliar Purchase ni duplicar scripts de UI.
Para un generador de nivel `n`, su producción base total es
`baseProduction × suma(productionGrowth^i, i=0..n-1)`.
El precio del siguiente nivel es `techo(initial × price.growth^n)`.
La primera compra usa `n=0`. Los parámetros solo se aplican al empezar la sesión.

Los recursos conservan `double` para mantener la arquitectura y el formato de
guardado. Una suma compensada guarda también la parte de baja magnitud: evita
perder ingresos pequeños por fotograma o costes pequeños ante saldos grandes.
Ambas partes se persisten y validan; los guardados anteriores usan resto cero.
No es aritmética exacta de precisión arbitraria. El límite explícito de 1e150,
los límites de configuración y niveles evitan Infinity/NaN y desbordamientos
silenciosos. Al llegar al límite, la UI muestra «LÍMITE» y el toque devuelve
solo lo realmente acreditado. Los números grandes usan K/M/B/T o notación
científica; los multiplicadores conservan sus centésimas.

El combo tiene toques limitados al máximo útil y decae gradualmente, sin acumular
una reserva invisible de miles de toques. Retomarlo reinicia la gracia.
La sobrecarga captura su duración al activarse y reparte un intervalo de
producción entre tiempo bonificado y normal si termina dentro de él.
Comprar duración durante una sobrecarga mejora la siguiente activación.

## Arquitectura

`ReactorApp` adapta el reloj y las acciones de Unity a `GameEngine`.
El motor posee una copia del estado. `ComboSystem` y `OverloadSystem`
calculan los estados temporales. El motor devuelve ganancias; la presentación
las muestra, sin volver a calcularlas.

`SaveStore` recibe un adaptador JSON. .NET prueba el mismo almacenamiento y
dominio con System.Text.Json; Unity usa JsonUtility. El guardado contiene
versión, payload y SHA-256 (detección de corrupción, no protección antitrampas).
Escribe y sincroniza un temporal, reemplaza el principal y conserva una copia
válida. No restaura combo, carga ni sobrecarga activa tras cerrar.

Se mantienen los campos `coils/plasma` y las llamadas Purchase de fase 1.
Los demás generadores se guardan por ID, junto a los IDs de hitos completados.
Al cargar, se reconcilian objetivos ya cumplidos sin repetir celebraciones.
Los hitos se marcan en el dominio antes de encolar su aviso; el runtime guarda
esas marcas antes de mostrar la animación. Modificar un umbral no revoca un
desbloqueo ya conseguido. Ningún hito añade multiplicadores de producción.

`ReactorHud` crea el Canvas y controles desde la escena; no requiere enlaces
manuales entre decenas de objetos. La tienda tiene scroll y botones de
192 unidades de referencia creados desde el catálogo. La interfaz se adapta al
área segura. `ReactorFeedback` conserva 12 números, 6 ondas y 32 chispas;
cuatro anillos procedurales adicionales se crean una sola vez y se habilitan
por etapa. Su color/rotación de sobrecarga no altera la economía.

## Criterios de aceptación

- Toque con recompensa y respuesta visual inmediata.
- Combo se construye, tiene límite y disminuye por escalones durante el descanso.
- Sobrecarga requiere carga y activación, multiplica y termina.
- Compras muestran precio/efecto y nunca producen saldo negativo.
- Cuatro familias producen según tiempo y respetan sus desbloqueos.
- Los hitos se registran una vez y sobreviven a gasto, guardado y carga.
- Gastar recursos no revierte hitos; las etapas cambian color/velocidad del aro.
- Guardar/cargar conserva progreso, maneja corrupción y no concede ingresos offline.
- Controles utilizables con ratón y toque, sin pulsaciones a través de la tienda.
- Pausa detiene producción/entrada y no recupera tiempo suspendido.
- Juego principal no necesita red.
- APK instala y funciona en Android ARM64; objetivo 60 FPS sujeto a medición.

## Fuera del alcance

Anuncios, compras integradas, cuentas, servidores, clasificaciones,
producción offline, localización multilingüe y publicación en Google Play.

## Riesgos pendientes

- Validar importadores, referencias de ensamblado, shaders TMP y ajustes
  generados en el Editor real.
- Confirmar File.Replace/Flush en el almacenamiento del dispositivo Android.
- Ajustar contraste, tamaños, zona táctil sobre la ilustración y scroll con
  pantallas 16:9, 19.5:9, 20:9 y áreas seguras con recortes.
- Medir sobrecarga gráfica y audio en dispositivo; limitar más efectos si hace falta.
- Ajustar economía con sesiones reales de 5–10 minutos, además de pruebas numéricas.

El balance medido, metodología, recomendaciones y límites están en
[PHASE2.md](PHASE2.md). Las cifras corresponden al dominio simulado, no a
mediciones de rendimiento ni pruebas humanas dentro de Unity.


## Ampliación autorizada: reactor modular y renacimientos

La fase 4 incorpora colapso controlado, esencia, tres especializaciones,
maestrías permanentes y colección de skins. Las fórmulas, límites, balance medido
y política de reinicio están en [PHASE4.md](PHASE4.md). La economía inicial no
cambia; las bonificaciones nuevas requieren renacimiento o esencia ganada.

El payload de guardado pasa a versión 2; GameEngine migra la versión 1 sin
modificar saldo, niveles ni hitos del ciclo inicial. Un candidato de renacimiento
no sustituye al motor activo hasta que SaveStore confirma su escritura.
Las preferencias permanecen separadas. Los IDs de generadores siguen estables;
los índices de skins y especializaciones también deben conservarse o migrarse.

ReactorAssembly dibuja un módulo por entrada de tienda, conductos de flujo,
combo segmentado y coronas procedurales. Los modelos visuales se actualizan desde
el dominio; no conceden energía. Las skins cambian apariencia sin estadísticas.
La tienda y las fichas comparten las mismas acciones de compra de ReactorApp.
