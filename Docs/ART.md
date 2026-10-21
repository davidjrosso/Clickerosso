# Guía visual — Reactor de Caos

## Dirección

Arcade de ciencia ficción en vertical: una esfera de energía contenida por una
carcasa metálica, sobre una cámara oscura. El centro concentra la luz; las zonas
superior e inferior mantienen poco detalle para leer la economía. Siluetas
simples y diferenciadas, sin texto ni cifras pintados en las imágenes.

| Función | Color | Uso |
| --- | --- | --- |
| Fondo | `#03050A` / azul marino | Cámara y espacios de descanso |
| Panel | `#060B17` | Contadores, filas, ajustes |
| Energía | `#2EEBFF` | Núcleo, producción, interacción |
| Progresión | `#AD63FF` | Anillos y tecnologías avanzadas |
| Sobrecarga | `#FF3DAB` | Cambio de estado y halo |
| Recompensa | `#FFB045` | Precios disponibles, hitos, energía intensa |
| Texto principal | `#FFFFFF` | Cantidades, nombres, acciones |
| Texto secundario | `#A5B5CF` | Descripciones y efectos de compra |

DejaVu Sans con TextMesh Pro; cifras, precios y multiplicadores siempre editables.
El panel oscuro separa texto y arte. La disponibilidad también se indica con
botones habilitados, «BLOQUEADO», niveles y cifras; nunca solo mediante color.
La sobrecarga muestra multiplicador y segundos, y el combo indica su descenso.
La lectura efectiva en pantalla pequeña sigue pendiente de revisión en Unity.

## Vocabulario de iconos

- Potencia: rayo cian sujeto por una abrazadera.
- Combo: tres diamantes violetas enlazados.
- Sobrecarga: estallido magenta con centro cálido.
- Microgenerador: batería cian; bobina: espiras violetas.
- Turbina: seis palas cian; núcleo auxiliar: esfera magenta contenida.
- Hitos: cristal ámbar con aro violeta; ajustes: tres deslizadores cian.

Un atlas contiene los nueve iconos. `Resources/Presentation.asset` relaciona
IDs estables con rectángulos UV normalizados. Los recortes respetan el margen
transparente; no se generan nueve texturas adicionales. Si se añade una entrada
de tienda sin icono, se usa temporalmente el del microgenerador.

## Composición y movimiento

`Main.unity` conserva su bootstrap `ReactorApp`. `ReactorHud` construye la interfaz
con el catálogo de arte; no hay una escena de exhibición separada. El fondo se
recorta conservando su proporción; los sprites conservan proporción y pivote
central. Los efectos se recortan con `RectMask2D` en el área del núcleo, fuera
del contador, combo, sobrecarga y tienda. Ningún efecto intercepta raycasts.

El toque deforma suavemente la carcasa, emite una onda, partículas y el valor
real devuelto por GameEngine. La luz es local y de baja opacidad, sin parpadeo
de pantalla completa. El combo aumenta gradualmente el tono, velocidad orbital,
cantidad de partículas y altura sonora. La sobrecarga añade magenta, un halo
más intenso, onda especial y zumbido suave; al finalizar, una onda cian se
contrae y el halo vuelve gradualmente al estado normal. Una tarjeta reutilizable
muestra cada hito ya concedido y guardado.

Las cinco etapas conservan un único sprite base y activan cero a cuatro anillos
con distinto giro y color. No cambian la economía. No hay modelos 3D, cámaras
con postprocesado, paquetes de partículas ni shaders de terceros: uGUI/UI Default,
TMP y mallas 2D de `RingGraphic` y `GlowGraphic`.

## Presupuestos y accesibilidad

- Pool fijo: 12 cantidades, 6 ondas y 32 partículas. Se reutilizan las más antiguas.
- Ráfagas separadas al menos 40 ms; en efectos suaves, 200 ms y sin partículas.
- Audio: dos voces de toque, una de evento y una de ambiente. Sin acumulación
  de PlayOneShot; los toques sonoros se separan al menos 60 ms.
- Nueve sprites de iconos creados una vez por HUD y liberados al destruirlo.
- Cámara, contadores y botones inmóviles. El pequeño desplazamiento del núcleo
  desaparece con movimiento reducido, junto a los giros y desplazamientos de VFX.
- Destellos reducidos elimina el brillo transitorio y reduce opacidad de ondas
  y partículas; efectos suaves elimina partículas. Las cantidades siguen visibles.
- Sonido desactivado detiene inmediatamente las cuatro voces; pausa también.
- Vibración desactivada de inicio, opcional solo en hitos e inicio de sobrecarga,
  con intervalo mínimo de dos segundos. Duración y disponibilidad dependen del
  dispositivo mediante `Handheld.Vibrate`; falta probarla en Android.

Los ajustes se guardan aparte en PlayerPrefs (`reactor.presentation.v1`), sin
modificar el archivo de progreso. El modal impide tocar el reactor por detrás;
la producción automática continúa. Escape/atrás abre o cierra el panel.


## Ampliación modular de fase 4

ReactorAssembly organiza cuatro generadores laterales y tres nodos de mejora
bajo el núcleo. ConduitGraphic une esos módulos al centro; ComboSegmentsGraphic
representa el estado del combo en doce segmentos. Las compras pulsan el módulo
existente y los niveles aumentan los segmentos de conexión hasta un máximo de
seis, sin crear objetos por cada unidad comprada.

CrownGraphic aporta siluetas distintas: placas cian Aurora, doce rayos ámbar
Corona solar, seis triángulos magenta Vacío violeta y ocho puntas verde claro
Prisma. Las cinco etapas aumentan la silueta y el tamaño del sprite original.
Estos recursos son geometría 2D original, no nuevas imágenes de IA. Todos los
gráficos personalizados requieren RectTransform y CanvasRenderer explícitamente.

Movimiento reducido detiene el desplazamiento por conductos y el giro de
módulos; efectos suaves oculta los paquetes de energía. Las conexiones, niveles,
fichas y estado textual siguen accesibles. Los pools anteriores permanecen en
50 efectos y cuatro voces. Falta la inspección de legibilidad y composición en
el Editor real y la medición de rendimiento en Android.

## Revisión de interfaz y montaje

El nuevo fondo es una pared de titanio vista de frente, con menos brillo y ruido
que la cámara anterior. El núcleo y siete módulos se montan sobre soportes
circulares `SocketGraphic`, con sombra, aro metálico y tornillos; los botones
interactivos tienen relleno transparente. Los conductos pasan detrás de los
soportes. La tienda usa tarjetas para información; la máquina no usa tarjetas
rectangulares detrás de sus piezas. Reactor, Mejoras, Logros y Ascensión tienen
áreas separadas, con cabecera y navegación persistentes. Véase
[UI-REFRESH.md](UI-REFRESH.md) para los recorridos y la validación pendiente.
