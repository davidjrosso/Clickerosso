# Fase 1 — cómo probar el núcleo

## Abrir la escena

Usa Unity 6000.0.85f1 con licencia activa. Abre la raíz del proyecto desde Hub.
Espera la resolución de paquetes y la preparación automática. Si falta TMP,
usa **Tools → Reactor de Caos → Prepare Project** en la barra superior; espera
`Reactor project prepared` en Console. Si no aparece ningún menú del proyecto,
resuelve primero los errores rojos de compilación: Unity no carga sus menús
mientras los scripts del Editor no compilan.

Abre **Tools → Reactor de Caos → Open Main Scene** y pulsa Play. La escena también
está en `Assets/ReactorDeCaos/Scenes/Main.unity` y en la lista de escenas de build.
Selecciona una vista Game vertical, por ejemplo 1080 × 1920. El HUD y los efectos
se construyen al comenzar Play, siguiendo la arquitectura existente.

Para probar una partida nueva sin perder progreso, detén Play y mueve a una
carpeta de respaldo `reactor-save.json`, `.bak` y `.tmp` de
`Application.persistentDataPath` si existen. Restaura esos archivos al terminar.
Las pruebas automatizadas de escena preservan/restauran esos archivos solas.

## Recorrido manual con configuración predeterminada

| Acción | Resultado esperado |
|---|---|
| Toca una vez | +1 energía, pulso inmediato, onda, número flotante y chispas; sonido provisional |
| Toca cinco veces sin pausas superiores a 1,5 s | El quinto toque gana 1,25; combo ×1,25 |
| Alcanza 20 toques seguidos | Combo ×2; al descansar 2,25 s baja a ×1,75 y después baja otro escalón cada 0,75 s |
| Retoma los toques | Conserva el combo restante y reinicia la gracia |
| Intenta comprar sin saldo | Botón atenuado; no cambia el saldo ni el nivel |
| Compra Potencia por 15 | Nivel 1, potencia base 2; el siguiente nivel cuesta 24 |
| Compra Combo por 75 | Máximo de combo pasa de ×3 a ×3,25; el siguiente cuesta 128 |
| Completa 25 toques fuera de sobrecarga | Medidor al 100%; botón de activación habilitado |
| Activa Sobrecarga | Toques y producción ×3 durante 10 s; aro cálido, efectos más intensos y cuenta atrás |
| Pulsa activar otra vez | No reinicia ni duplica la sobrecarga; botón deshabilitado mientras dura |
| Compra Sobrecarga por 100 | La siguiente activación dura 11 s; comprarla durante una activación no altera su temporizador |
| Compra Bobina por 25 | Nivel 1 y 1 E/s sin tocar; siguiente precio 30, siguiente producción 2 E/s |
| Espera sin tocar durante una sobrecarga con una bobina | 3 E/s mientras dura; vuelve a 1 E/s al finalizar |
| Toca rápidamente y después espera | Los efectos se reutilizan y desaparecen; no crecen indefinidamente |

La cabecera muestra el saldo, la producción actual por segundo y la ganancia
por toque con el combo actual. El toque que alcanza un nuevo escalón ya recibe
su multiplicador nuevo. La tienda muestra nombre, nivel, precio y efecto
actual/siguiente; desplázala para encontrar la bobina. Se conservan la matriz
de plasma y los hitos ya existentes, sin añadir contenido de fases posteriores.

Mantén una sesión de 5–10 minutos: alterna actividad y descanso, compra mejoras,
activa varias sobrecargas y confirma que la producción continúa al soltar.
Prueba pausa/reanudación, detener Play y volver a entrar. El saldo y las compras
persisten; combo y sobrecarga se reinician, sin recompensas offline.

## Ajustar la economía

Selecciona `Assets/ReactorDeCaos/Resources/Economy.asset` en Project y despliega
`Values` en Inspector. Incluye potencia, combo, carga necesaria, multiplicador,
duración, incrementos por nivel, producción y curvas de precio `initial/growth`.
Detén Play antes de cambiarlo; el motor toma una copia al iniciar.

Recompensa = potencia base mejorada × combo × sobrecarga activa.
Precio = techo(initial × growth elevado al nivel actual).
Hay un máximo de 100 niveles por compra y un límite de recurso de 1e150.
La configuración inválida se rechaza al iniciar con una excepción descriptiva.

## Verificación ejecutada y pendiente

- `bash Tools/test-core.sh`: **53 aprobadas, 0 fallidas, 0 omitidas**.
  Incluye diez minutos simulados con 36.000 avances, compras, pausas de actividad,
  sobrecargas y validación del estado en cada avance; también precisión y guardado.
- `python3 Tools/check-unity-api.py`: los seis ensamblados compilan con bibliotecas
  oficiales del Editor instalado, sin stubs.
- El arranque real de Unity con `PrepareBatch` terminó con código **198**:
  `No valid Unity Editor license found`. No llegó a importar ni ejecutar la escena.
- EditMode/PlayMode nativos, renderizado, sonido y una sesión real de varios
  minutos siguen pendientes. La simulación económica no valida esos aspectos.
- Se añadieron pruebas PlayMode de ráfagas de 1.000 toques con pool fijo,
  recuperación de sobrecarga y concordancia de etiquetas/botones con el dominio.
  Están compiladas, pero no ejecutadas.

Con la licencia disponible, ejecuta `UNITY_EDITOR=/ruta/Unity bash Tools/test-unity.sh`
o abre **Window → General → Test Runner** y ejecuta EditMode y PlayMode.
No hay fallos conocidos en la suite .NET ni errores en la compilación contra API.
Queda pendiente comprobar la resolución UPM/importadores/TMP dentro del Editor,
contraste y distribución en distintas pantallas, audio y rendimiento en dispositivo.
Android no es necesario para probar esta fase en el Editor y aún no hay APK validado.
