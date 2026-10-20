# Diseño del MVP

## Reglas iniciales

| Sistema | Regla |
|---|---|
| Toque | 1 + nivel de potencia |
| Combo | Ventana de 1 segundo; +0,25 cada 5 toques; máximo inicial ×3 |
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

La configuración de tiempos, producción y umbrales se encuentra en
`Resources/Economy.asset`, generado por Prepare Project. Precios y fórmulas
están centralizados en EconomyConfig para evitar duplicación en la UI.

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
- Combo se construye, tiene límite y caduca.
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
