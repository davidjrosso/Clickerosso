# Identidad sonora y experiencia móvil

## Completado en código y recursos

Se conservó el sistema de gameplay, la economía y el pool visual. El audio
observa eventos reales; no otorga energía ni ejecuta compras.

| Evento | Recurso integrado | Comportamiento |
| --- | --- | --- |
| Toque | Tap.wav | Dos voces alternadas; máximo un inicio cada 60 ms |
| Combo creciente | ComboRise.wav, nuevo | Dos notas; tono ascendente según el multiplicador real; mínimo 220 ms entre avisos |
| Sobrecarga | Overload.wav + OverloadHum.wav | Entrada breve y zumbido durante el estado activo |
| Fin de sobrecarga | OverloadEnd.wav | Descenso tonal una sola vez por transición |
| Compra | Purchase.wav | Acorde corto después de una compra válida |
| Generador desbloqueado | GeneratorUnlock.wav, nuevo | Tres notas al pasar de bloqueado a disponible; no se repite al cargar el guardado |
| Hito / logro reclamado | Milestone.wav | Recompensa sonora; la reclamación ya está guardada |
| Ascensión | Ascension.wav, nuevo | Secuencia ascendente después del guardado confirmado |
| Ambiente musical | AmbientMusic.wav, nuevo | Pad sin percusión, ciclo de 16 s y entrada gradual |

Son **siete AudioSource fijos**: dos de toque, combo, evento, desbloqueo, zumbido
y música. No se crean voces por pulsación ni se acumulan PlayOneShot. Las
transiciones importantes y el combo no compiten por una misma voz. Los eventos
que comparten la voz de evento pueden sustituir el sonido anterior.

No hay una herramienta de generación de audio por IA disponible en esta sesión.
Todos los WAV son síntesis original del proyecto, reproducible mediante
`python3 Tools/generate-audio.py`, sin muestras, música comercial ni dependencias
externas. **Son recursos provisionales legítimos; no se presentan como audio de
IA ni como mezcla final aprobada.** Su procedencia figura en Art/PROVENANCE.md.

## Controles independientes

En Ajustes: **Efectos de sonido**, **Música**, **Volumen de efectos** y **Volumen de
música**. Hay que deslizar para ver todas las opciones. Los cambios de volumen
se oyen durante el arrastre; se guardan al cerrar Ajustes, pausar, perder foco o
salir. Esto evita escribir PlayerPrefs en cada muestra del arrastre.

Por defecto, efectos al 75% y música al 35% de un bus ya atenuado a 0,14.
La música no cambia el volumen de los efectos, y viceversa. El silencio de las
preferencias antiguas se migra a ambas categorías; las preferencias conservan
su clave para no perder ajustes de accesibilidad. Los volúmenes se validan y
limitan a [0,1]. La música pausa conservando la posición; al reanudar sigue desde
ella, mientras los efectos transitorios no se reproducen de nuevo.

## Adaptación móvil

- Escala por ancho para estabilizar texto y tamaños táctiles en vertical.
- Tipografía de interfaz ampliada y menor reducción automática; nombres de etapa
  abreviados en la cabecera para separar producción y evolución.
- Navegación inferior, sobrecarga y controles de fichas más altos.
- Ajustes y las tres secciones de Ascensión tienen scroll. La confirmación tiene
  texto desplazable, con Confirmar y Cancelar visibles por separado, y vuelve al
  principio al revisar una nueva propuesta.
- Toda interacción sigue dentro del contenedor Screen.safeArea. Arte y fondo
  pueden extenderse por fuera; los botones permanecen dentro.
- Se separaron las cajas de texto de la cabecera y el aviso de guardado de la
  navegación. Los módulos conservan zonas táctiles independientes del núcleo.
- Pausa y pérdida de foco se combinan: recuperar foco no reanuda mientras siga
  activa la pausa del sistema. Se guarda antes de quedar inactivo; no se genera
  energía por el tiempo fuera de la aplicación.
- Las compras de un mismo módulo o maestría ignoran callbacks repetidos en el
  mismo fotograma. El siguiente toque, en otro fotograma, puede comprar de nuevo.
  Logros y ascensiones mantienen sus controles propios contra duplicación.

## Pruebas realmente ejecutadas

- `bash Tools/test-core.sh`: **99 aprobadas**, 0 fallidas, 0 omitidas. Son pruebas
  de dominio/guardado, no pruebas de sonido ni de renderizado.
- `python3 Tools/check-unity-api.py`: compilación de **seis ensamblados** con las
  DLL oficiales, incluidas las pruebas nativas nuevas.
- `python3 Tools/audit-mobile-layout.py`: geometría calculada desde anclas del
  código, en 320×568, 360×640, 390×844 (insets 47/34), 412×915 (32/24) y 768×1024
  (24/20). **31 tipos/instancias de control por perfil**; mínimo observado
  **48,33 píxeles lógicos**. Comprueba también separación entre objetivos del
  reactor, cabecera, navegación, acciones de confirmación y cierres/scroll.
  Es una auditoría estática de cajas, no una captura ni ejecución en esos móviles.
  No demuestra lectura efectiva, gestos físicos, DPI o comodidad de una mano.
- `python3 Tools/check-audio.py`: los **10 WAV** son PCM mono 44,1 kHz, sin
  muestras saturadas. Los bucles de música y zumbido no añaden una discontinuidad
  mayor que sus pasos normales. Cota conservadora de pico con todas las voces a
  volumen máximo: **0,4182**, por debajo de 1. No equivale a una escucha de mezcla.
- Regeneración local: los diez WAV resultaron idénticos byte por byte.
- `Tools/test-unity.sh`: intento real del Editor, **salida 198**:
  `No valid Unity Editor license found. Please activate your license.`

Se añadieron seis pruebas PlayMode: categorías de audio/migración/volumen,
señales de combo y desbloqueo, compra duplicada, foco más pausa, acceso por scroll
a Ascensión y desbordamiento de textos económicos en paneles/estados. También
se actualizaron las comprobaciones existentes para las siete voces y la compra
repetida de maestrías. **Se compilaron; no pudieron ejecutarse.**

La economía no cambió, por lo que no se repitieron las simulaciones ya validadas
en la entrega anterior. Evidencia de esta revisión: [AudioMobile](AudioMobile/).

## Pendiente y recorrido manual obligatorio

No se pudo recorrer el juego ejecutándose, escuchar la mezcla, medir fotogramas
ni usar un dispositivo Android desde este entorno. Siguen pendientes:

1. En Unity 6000.0.85f1 con licencia, abrir Main y ejecutar EditMode/PlayMode.
2. En Game View o Device Simulator, repetir los cinco tamaños anteriores y
   comprobar texto largo, safe area, scroll y botones con el dedo/puntero.
3. Recorrer Reactor, fichas, Mejoras, Logros, Ajustes y las tres secciones de
   Ascensión. Probar sobrecarga cargando/activa/finalizada, combo descendiendo,
   desbloqueo, reclamación, cancelación y confirmación de ascensión.
4. Comparar saldo, precio y nivel antes/después de cada compra; provocar dos
   callbacks en el mismo fotograma y comprobar que hay una sola compra. Esperar
   animaciones y verificar que no compran nada por sí mismas.
5. Escuchar con auriculares y altavoz: tocar rápido, comprar y desbloquear; dejar
   dos bucles musicales. Ajustar efectos sin afectar música y al revés. Comprobar
   silencio, persistencia y ausencia de saltos audibles en el bucle.
6. Pausar con sobrecarga activa, bloquear el teléfono y abrir una notificación;
   volver con foco/pausa en distinto orden. Confirmar guardado, música continuada,
   ausencia de producción fuera de la aplicación y controles disponibles.

El arte y audio requieren aprobación dentro de la escena real. No se incorporan
anuncios, compras integradas ni servicios externos.
