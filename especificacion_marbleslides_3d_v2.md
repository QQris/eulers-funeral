# Especificación funcional v2: juego tipo MarbleSlides en 3D (Unity 6)

Esta versión incorpora las respuestas del usuario. Donde las respuestas se contradicen, se indica en la sección 2 con una propuesta de solución.

---

## 1. Decisiones cerradas

- **Simulación:** la pelota es una partícula que se desliza sin fricción sobre la superficie z = f(x, y). El terreno del nivel 5 usa la función exponencial e^x.
- **Integrador:** RK4 con paso fijo (dt = 1/120 s).
- **Entrada:** el jugador escribe una fórmula z = f(x, y) con notación implícita (2x = 2·x) y sintaxis en español (sen, ln). La superficie se genera solo al confirmar, sin vista previa.
- **Ayudas:** se muestran pistas (pendiente o gradiente en un punto), pero nunca la fórmula del nivel.
- **Visualización:** malla de alambre sobre fondo negro. La cámara orbita con clic derecho y hace zoom con la rueda, sin bajar por debajo del plano.
- **Colores:** pelota blanca, objetivos y estrellas rojos, fondo negro. El color de la malla no fue especificado (propuesta: gris claro).
- **Audio:** sonidos minimalistas de rodar, además de sonidos de rebote, recoger estrella, victoria y derrota.
- **Alcance:** niveles fijos en código, solo guardado de progreso, sin ranking ni estadísticas.
- **Uso del documento:** documento de requisitos para un modelo de IA, con fines académicos. Por eso está redactado con identificadores, criterios de aceptación y valores numéricos.
- **Motor:** Unity 6 LTS con pipeline de render (ver P5).

---

## 2. Conflictos a resolver

**Conflicto A: orden de techo y piso.** En la respuesta 3 se propuso el nivel 2 con ⌊e⌋ = 2 y el nivel 3 con ⌈e⌉ = 3. En la respuesta 9 se pidió que el piso vaya después del techo. Ambas cosas no se cumplen a la vez si el número del nivel es el que se muestra en la animación. La tabla de la sección 3 respeta el orden de la respuesta 9 (techo antes que piso). El título del nivel 2 usa ⌊e⌋ aunque la mecánica sea techo, lo que conviene resolver (ver P1).

**Conflicto B: cantidad de niveles.** Hay seis mecánicas (pendiente, paraboloide, techo, piso, senoidales y Euler) más un nivel 0. Los títulos ocupan las posiciones 0 a 5, es decir seis posiciones. Se propone que el nivel 0 (tutorial) use la pendiente simple, de modo que el tutorial sirva como primer reto y no haya una mecánica extra (ver P2).

---

## 3. Requerimientos funcionales

**RF-01 Menú principal.** Opciones: Jugar, Selección de niveles, Ajustes (volumen y controles) y Salir.

**RF-02 Selección de niveles.** Muestra los niveles 0 a 5 con nombre y estado (bloqueado, disponible, completado). El nivel N+1 se desbloquea al completar el N.

**RF-03 Cuadro de ayuda inicial.** Aparece la primera vez que se abre el juego. Explica los controles (clic derecho para orbitar, rueda para zoom, teclado para escribir la fórmula) y el objetivo general: llevar la pelota blanca hasta el objetivo rojo recogiendo las estrellas rojas.

**RF-04 Animación de título de nivel.** Al iniciar un nivel se reproduce una animación con tipografía formal de estilo LaTeX/Manim (Computer Modern o Latin Modern), de unos 3 s en total.

| Nivel | Expresión de título | Valor mostrado |
|---|---|---|
| 0 | e^(iπ) + 1 = 0, luego + 0 | 0 |
| 1 | e | e (símbolo) |
| 2 | ⌊e⌋ | 2 |
| 3 | ⌈e⌉ | 3 |
| 4 | lim (1 + 4/n)^n = e^4, luego e^(ln 4) | 4 |
| 5 | e^(iπ) + 1 + 5 | 5 |

Cada expresión se transforma en la siguiente mediante un morph de glifos (por ejemplo, la "e" se transforma en "⌊e⌋" y luego en "2"). Al terminar la animación se pasa a la fase de edición.

**RF-05 Mesa de graficación 3D.** Plano de referencia con ejes x, y, z, cuadrícula y etiquetas. Fondo negro. La cámara orbita con clic derecho sostenido y hace zoom con la rueda. La altura de la cámara no puede bajar del plano.

**RF-06 Entrada de fórmula.** Campo de texto para z = f(x, y).
- Valida la sintaxis en tiempo real y muestra errores comprensibles en español.
- Al confirmar, evalúa f en una malla de 100 × 100 puntos y genera la superficie.
- Animación de aparición: z(t) = t · f(x, y), con t de 0 a 1 en 1.5 s.

**RF-07 Pistas.** Panel opcional que muestra la pendiente y el gradiente en el punto bajo el cursor. No muestra la fórmula del nivel.

**RF-08 Simulación.** Al lanzar, la pelota blanca parte del punto A (marcado con un punto blanco) y rueda por gravedad sobre la superficie.

**RF-09 Estrellas.** Hay 3 estrellas representadas como puntos rojos sobre la trayectoria. Tocarlas las recoge.

**RF-10 Objetivo.** El objetivo B es una esfera roja de radio r_t = 0.9 · r_bola. El nivel se completa cuando las 3 estrellas están recogidas y la pelota contiene por completo al objetivo, es decir, cuando |p_bola − p_B| + r_t ≤ r_bola.

**RF-11 Derrota.** La partida se pierde si la pelota sale de los límites del dominio, si cae de la superficie o si se agota el tiempo máximo del nivel.

**RF-12 Reinicio y progreso.** Botones para reintentar, editar la fórmula y pasar al siguiente nivel. Solo se guarda el progreso (niveles completados) en almacenamiento local.

**RF-13 Rebotes y gravedad.** En los niveles donde esté activado, la pelota rebota con un coeficiente de restitución definido por nivel. Cuando la gravedad cambia, el HUD muestra un aviso con el nuevo valor durante 2 s.

**RF-14 Niveles de techo y piso.** En el nivel de techo, la pelota está confinada entre el terreno y un techo definido en el nivel. Las colisiones con el techo se simulan y generan rebote. En el nivel de piso, la superficie principal se basa en la función piso.

---

## 4. Requerimientos de física y matemática

- **Modelo:** partícula sobre z = f(x, y). El Lagrangiano es L = ½m(ẋ² + ẏ² + ż²) − mgz, con ż = f_x·ẋ + f_y·ẏ. Esto da la métrica inducida g_ij = δ_ij + f_i·f_j.
- **Gravedad:** g = 9.8 m/s² por defecto. El valor puede cambiar por nivel.
- **Integrador:** RK4 con paso fijo de 1/120 s. Se usa interpolación para renderizar a 60 FPS.
- **Derivadas:** diferencias finitas centradas (h = 1e-4) o derivación simbólica sobre el AST del parser.
- **Parser:**
  - Operadores: +, −, *, /, ^.
  - Funciones: sen, cos, tan, exp, ln, sqrt, abs, floor, ceil.
  - Constantes: pi y e.
  - Acepta multiplicación implícita (2x, 3sen(x)).
  - No ejecuta código arbitrario.
- **Casos especiales:**
  - División por cero o raíz de número negativo: la malla deja ese punto sin superficie y la pelota lo trata como vacío (caída).
  - Discontinuidades de floor y ceil: el terreno se genera con escalones, y la pelota puede rebotar contra el escalón.
  - Puntos no definidos fuera del dominio: se tratan como vacío.
- **Función exponencial:** en el nivel 5, la superficie base usa e^x. Sus derivadas son e^x, y la simulación las usa directamente.

---

## 5. Requerimientos de visualización y audio

- **Malla:** alambre gris claro sobre fondo negro. Opcional: mapa de color por altura en configuración (fuera del prototipo).
- **Pelota:** esfera blanca de radio r_bola fijo.
- **Estrellas y objetivo:** puntos y esfera rojos.
- **Marcador de A:** punto blanco pequeño en el punto de inicio.
- **Tipografía:** formal, estilo LaTeX/Manim, para títulos y etiquetas matemáticas. Para la interfaz se puede usar una sans-serif neutra.
- **Audio:**
  - Sonido de rodar, con volumen proporcional a la velocidad.
  - Sonido de rebote, si el nivel tiene restitución mayor que 0.
  - Sonido de recoger estrella, y sonido de victoria o derrota.
  - Sin música de fondo, para mantener el carácter minimalista.

---

## 6. Requerimientos técnicos

- **Motor:** Unity 6 LTS. Lenguaje C#.
- **Render:** URP asumido (ver P5).
- **Plataforma:** PC, con control de ratón y teclado.
- **Idioma:** español.
- **Rendimiento:** 60 FPS con malla de 100 × 100 y una sola pelota en simulación.
- **Niveles:** definidos en código o en ScriptableObjects, sin editor en tiempo de ejecución. Cada nivel define dominio, A, B, estrellas, funciones base, restituciones, gravedad, tiempo máximo y expresión de título.
- **Persistencia:** archivo JSON local con los niveles completados.

---

## 7. Criterios de aceptación

1. Cada nivel se puede iniciar desde la selección de niveles y muestra su animación de título con la expresión correcta.
2. Una fórmula válida genera la malla en menos de 200 ms.
3. El clic derecho orbita la cámara sin cruzar el plano.
4. La pelota completa el nivel solo si recoge las 3 estrellas y contiene al objetivo.
5. La caída fuera de la superficie produce derrota inmediata.
6. El progreso se conserva al cerrar y reabrir el juego.
7. En el nivel 0 se muestra el tutorial con la misma mecánica del nivel de pendiente.

---

## 8. Preguntas pendientes

**P1.** En el nivel de techo y piso, ¿el título debe coincidir con la mecánica (por ejemplo, techo en el nivel 2 con ⌈e⌉ = 3, lo que obliga a cambiar el número) o se mantienen los títulos actuales aunque no coincidan con la mecánica?

**P2.** ¿Se confirma que el nivel 0 es el tutorial con la pendiente simple, y que la pendiente deja de ser un nivel independiente?

**P3.** Sobre las estrellas: ¿completar el nivel exige las 3 estrellas más el objetivo, o las estrellas solo cuentan para una calificación? Lo descrito sugiere lo primero.

**P4.** En la restricción de cámara: ¿la cámara nunca debe bajar del plano, o también debe impedirse subir más allá de la vertical?

**P5.** ¿Qué pipeline de render se usa en Unity 6: URP, HDRP o Built-in? Si no se sabe, se asume URP.
