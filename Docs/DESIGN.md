# Diseño del MVP

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
| Bobina | +1 energía/s; coste 25, crecimiento ×1,18 |
| Matriz de plasma | +12 energía/s; coste 500, crecimiento ×1,22 |
| Hitos | 1.000 producidos: plasma; 10.000: singularidad |

Los precios se redondean hacia arriba. Compras y generadores tienen un máximo
de 100 niveles/unidades. El combo solo multiplica ingresos manuales; la
sobrecarga multiplica ingresos manuales y automáticos. Los hitos usan el
total histórico, no el saldo. La economía limita los recursos a 1e150.

La configuración de potencia, tiempos, multiplicadores, producción, umbrales
y curvas de precio se encuentra en `Resources/Economy.asset`, incluido en Git.
Prepare Project lo crea solo si falta. EconomyConfig valida y copia los datos
al empezar; la interfaz consulta efectos actuales/siguientes al motor.

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

`ReactorHud` crea el Canvas y controles desde la escena; no requiere enlaces
manuales entre decenas de objetos. La tienda tiene scroll y botones de
150 unidades de referencia. La interfaz se adapta al área segura.
`ReactorFeedback` limita los efectos a 12 números, 6 ondas y 32 chispas.

## Criterios de aceptación

- Toque con recompensa y respuesta visual inmediata.
- Combo se construye, tiene límite y disminuye por escalones durante el descanso.
- Sobrecarga requiere carga y activación, multiplica y termina.
- Compras muestran precio/efecto y nunca producen saldo negativo.
- Dos generadores producen según tiempo; plasma requiere su hito.
- Gastar recursos no revierte hitos; las etapas cambian color/velocidad del aro.
- Guardar/cargar conserva progreso, maneja corrupción y no concede ingresos offline.
- Controles utilizables con ratón y toque, sin pulsaciones a través de la tienda.
- Pausa detiene producción/entrada y no recupera tiempo suspendido.
- Juego principal no necesita red.
- APK instala y funciona en Android ARM64; objetivo 60 FPS sujeto a medición.

## Fuera del alcance

Prestigio, anuncios, compras integradas, cuentas, servidores, clasificaciones,
producción offline, localización multilingüe y publicación en Google Play.

## Riesgos pendientes

- Validar importadores, referencias de ensamblado, shaders TMP y ajustes
  generados en el Editor real.
- Confirmar File.Replace/Flush en el almacenamiento del dispositivo Android.
- Ajustar contraste, tamaños, zona táctil sobre la ilustración y scroll con
  pantallas 16:9, 19.5:9, 20:9 y áreas seguras con recortes.
- Medir sobrecarga gráfica y audio en dispositivo; limitar más efectos si hace falta.
- Ajustar economía con sesiones reales de 5–10 minutos, además de pruebas numéricas.
