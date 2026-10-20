# Fase 2 — progresión y economía

## Economía implementada

Se conserva GameEngine como autoridad económica, con los sistemas de toque,
combo, sobrecarga y guardado existentes. La tienda ahora obtiene sus filas de
un catálogo: tres mejoras y cuatro familias. Los nombres anteriores de bobina
y matriz se convierten en microgenerador y bobina de plasma; sus niveles
guardados conservan los IDs `coil` y `plasma` y su producción por nivel.

| Familia | Coste inicial | Crecimiento del coste | Primer nivel E/s | Crecimiento del incremento | Desbloqueo histórico |
|---|---:|---:|---:|---:|---:|
| Microgenerador | 25 | ×1,18 | 1 | ×1 | Disponible |
| Bobina de plasma | 500 | ×1,16 | 12 | ×1 | 1.000 E |
| Turbina cuántica | 2.500 | ×1,14 | 60 | ×1,06 | 5.000 E |
| Núcleo auxiliar | 12.000 | ×1,14 | 350 | ×1,08 | 25.000 E |

Con `n` niveles comprados:

- Precio siguiente = `techo(precio inicial × crecimiento del coste^n)`.
- Incremento siguiente = `producción base × crecimiento del incremento^n`.
- Producción total = suma de los incrementos de los niveles comprados.
- Sobrecarga multiplica la producción total y los toques por ×3 inicialmente;
  el combo solo multiplica los toques.

Ejemplo: una turbina produce 60 E/s; dos producen 123,6 E/s. Su segunda compra
cuesta 2.850 E. El número de niveles sigue limitado a 100 por elemento.
Potencia, combo y duración mantienen sus reglas y precios de fase 1.

## Hitos y evolución

Hay doce hitos: 100 E, cinco microgeneradores, 1.000, 5.000, 10.000, 25.000,
100.000, 1 millón, 10 millones, 100 millones, 1.000 millones y 10.000 millones
de energía histórica. Las compras no restan energía histórica.

Los hitos desbloquean familias o celebraciones. Cinco microgeneradores o el hito
de plasma activan el primer anillo; 5.000 E, 25.000 E y 100.000 E habilitan las
capas siguientes. La sobrecarga cambia su color y velocidad. Las cuatro capas
se crean una sola vez; las ondas, chispas y números conservan el pool de fase 1.
Los cambios visuales y las celebraciones no conceden energía ni bonos ocultos.

El dominio registra cada ID antes de encolar el aviso. Se guarda la marca antes
de reproducir la celebración. Cargar o volver a superar un umbral no la repite.
En partidas antiguas se registran silenciosamente los hitos ya cumplidos.
Una celebración interrumpida al cerrar no se repite al volver a abrir.

## Simulaciones ejecutadas

Se probaron tres curvas con tres perfiles de **30 minutos simulados**. La curva
final se repitió al incorporar los límites de aceptación. Se ejecuta el dominio
real con pasos de 0,1 s, usando los datos exportados del asset real; no una copia
de las fórmulas de ganancias.

- **Activo:** dos toques por segundo durante toda la sesión. Es una referencia
  de actividad sostenida, no el ritmo que se exige al jugador.
- **Intermitente:** dos toques por segundo durante cinco segundos, seguidos de
  diez segundos sin tocar.
- **Pasivo:** 25 toques iniciales; después solo deja producir y compra.
  La aplicación permanece abierta. No se simulan recompensas offline.

Los perfiles revisan compras cada segundo y activan la sobrecarga cuando está
lista. Ahorran para el elemento desbloqueado con mejor retorno marginal estimado.
El estimador contempla el ciclo de carga/sobrecarga y el combo propio del perfil.
No busca una estrategia óptima global ni modela errores humanos, lectura de UI
o el tiempo de desplazarse por la tienda. Los tiempos tienen resolución de
0,1 s para hitos y 1 s para compras.

| Resultado final | Activo | Intermitente | Pasivo |
|---|---:|---:|---:|
| Primera compra | 7 s | 17 s | 10 s |
| Primera mejora de toque | 7 s | 17 s | No compra |
| Primer microgenerador | 19 s | 33 s | 10 s |
| Primera bobina de plasma | 28 s | 2:06 | 3:00 |
| Primera turbina | 1:00 | 3:19 | 4:50 |
| Primer núcleo auxiliar | 1:35 | 4:23 | 6:25 |
| Compras en 30 minutos | 251 | 214 | 147 |
| Mayor intervalo entre compras, primeros 5 min | 14 s | 26 s | 42 s |
| Percentil 95 de intervalos, sesión completa | 38 s | 41 s | 45 s |
| Mayor intervalo, sesión completa | 84 s | 74 s | 69 s |
| Sobrecargas | 46 | 29 | 1 |
| Toques totales | 3.600 | 1.200 | 25 |
| Energía histórica final | 3.638,3 millones | 1.457,3 millones | 259,7 millones |
| Producción final sin sobrecarga | 2.254.348 E/s | 1.318.078 E/s | 485.589 E/s |

Los intervalos incluyen la espera inicial y el tiempo desde la última compra
hasta el final. En la curva final se comprobaron: primera compra ≤20 s,
espera máxima en los cinco minutos iniciales ≤60 s, espera máxima total ≤90 s
y compra del núcleo auxiliar ≤7 min. Los tres perfiles cumplen estos límites.
No alcanzaron el límite de 100 niveles ni se detectaron saldos inválidos.

### Ajustes realizados a partir de los resultados

La primera curva usaba crecimientos de coste 1,22/1,24/1,26 para plasma,
turbina y auxiliar. Produjo esperas máximas de **189/168/162 s** para los tres
perfiles. Una primera reducción a 1,16 y aumentos moderados de producción
bajó esos máximos a **111/84/83 s**. La reducción final a 1,14 para las dos
familias avanzadas los dejó en **84/74/69 s**, con la misma política de compra.

Los objetivos iniciales se agotaban rápidamente en el perfil activo; se
añadieron hitos cosméticos crecientes hasta 10.000 millones. El perfil activo
alcanzó 1.000 millones a los 18:46,1; el pasivo alcanzó 100 millones a los
22:41,8. No se añadió prestigio ni contenido monetizado.

### Decisiones y eficiencia

El archivo `efficiency.csv` contiene estimaciones de retorno marginal, separadas
de los ingresos medidos en las simulaciones:

- Con potencia nivel 3, cinco microgeneradores y dos bobinas, dos toques/s y
  combo máximo, la siguiente potencia se amortiza en aproximadamente **4,94 s**;
  una turbina en **22,06 s**. Sin tocar, la potencia no aporta ingresos y la
  turbina se amortiza en **41,67 s**.
- Con potencia 8, combo 3, diez microgeneradores, cinco bobinas, dos turbinas y
  un auxiliar, la primera mejora de duración se amortiza en **3,46 s** bajo
  actividad sostenida; la siguiente potencia en **35,29 s**.
- Sin nuevas cargas, mejorar duración no genera retorno. El perfil pasivo
  dedica su presupuesto a generadores y alcanza todas las familias.

Son estimaciones de régimen estable: no representan el tiempo exacto de
recuperación de una compra concreta durante un combo o carga incompletos.
Muestran por qué la mejor inversión cambia con el estilo y la producción.

## Configuración y ampliación

Selecciona `Assets/ReactorDeCaos/Resources/Economy.asset` en el Inspector.
`generators` contiene ID, nombre, descripción, precio, producción y el ID del
hito que desbloquea cada familia. Duplica una entrada, asigna un ID nuevo y
configura sus parámetros; aparecerá automáticamente en la tienda.

`upgrades` contiene nombres, descripciones y precios de los tres tipos de efecto
existentes. Añadir un tipo de efecto distinto sí requiere implementar su regla.
`milestones` admite energía histórica o nivel de un generador, objetivo,
texto de recompensa y etapa visual 0–4. Su orden decide qué objetivo pendiente
se muestra en la cabecera; los logros se evalúan todos, independientemente de
ese orden. No se admiten referencias inexistentes ni ciclos de desbloqueo.

Mantén los IDs estables: reordenar el catálogo conserva las compras; renombrar
un ID equivale a crear otro contenido. Los IDs retirados se conservan en el
guardado, sin producir mientras estén fuera del catálogo. Hay límites de 32
definiciones de generador y 64 hitos; el guardado reserva 32 registros extra
de generador y 128 marcas de hito. Las uniones con IDs retirados se validan al
iniciar para no fallar a mitad de una compra.

La fase 2 sustituye los antiguos campos planos de configuración de generadores
por estas listas en el asset incluido. Si conservas otro Economy.asset con
ajustes propios de fase 1, traslada esos valores a las entradas correspondientes.
Las partidas `reactor-save.json` siguen siendo compatibles; no hay que borrarlas.
Los alias de código de fase 1 se mantienen para los llamadores existentes.

## Reproducir las comprobaciones

```bash
bash Tools/test-core.sh
python3 -m pip install -r Tools/requirements-balance.txt
bash Tools/simulate-balance.sh
python3 Tools/check-unity-api.py
```

El simulador requiere .NET 8.0.426 y Python con PyYAML 6.0.3. Lee el asset,
guarda una copia JSON y su SHA-256 y produce resultados/CSV en `TestResults/Balance`.
Los presupuestos de espera son los de esta fase: si pruebas otra curva deliberada,
un fallo requiere revisar sus resultados y los límites, no ocultar la prueba.

[Curva inicial](Balance/initial/results.json),
[ajuste intermedio](Balance/tuned/results.json),
[curva final](Balance/final/results.json) y
[eficiencia estimada](Balance/final/efficiency.csv) están versionados.
Los CSV finales permiten reconstruir cada compra y el crecimiento minuto a minuto
está en `results.json`. El balance no depende de animaciones o de Unity.

Para probar visualmente, usa **Tools → Reactor de Caos → Prepare Project**,
después **Open Main Scene → Play**. Comprueba las siete filas, requisitos de
desbloqueo, efectos del siguiente nivel, nuevos anillos y avisos de hitos.
Detén y vuelve a iniciar Play para comprobar que conserva niveles/desbloqueos
sin repetir celebraciones. Usa Test Runner para EditMode y PlayMode.

## Estado y recomendaciones

**69 pruebas .NET aprobadas, ninguna fallida ni omitida.** Los seis ensamblados,
incluidas las nuevas pruebas de asset e integración, compilan contra las API
oficiales. El intento nativo terminó con código **198**, por falta de licencia
de Unity. PlayMode, importación real del catálogo, legibilidad y animaciones
siguen pendientes; estas simulaciones no acreditan esas comprobaciones.

Mantendría esta curva como punto de partida para pruebas humanas. El estilo
intermitente desbloquea las cuatro familias en menos de cinco minutos y el pasivo
en menos de siete. La actividad sostenida acelera mucho el crecimiento, pero
los generadores permiten avanzar con solo 25 toques iniciales.

Antes de ampliar contenido, recomiendo medir si los intervalos de 69–84 s
al final de la sesión se sienten largos y si el desbloqueo activo de todas las
familias en 1:35 consume demasiado pronto la novedad. La sobrecarga ocupa el
69,1% del perfil activo y el 39,7% del intermitente: conviene comprobar si sigue
resultando especial antes de aumentar su duración o añadir multiplicadores.
Los anillos y textos nuevos necesitan una sesión real en móvil. No se ha
equilibrado el juego más allá de 30 minutos ni se ha validado un APK.
