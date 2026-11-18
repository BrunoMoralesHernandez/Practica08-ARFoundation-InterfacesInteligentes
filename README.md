# Práctica 8: Realidad aumentada con AR Foundation y ARCore — Interfaces Inteligentes

* **Autores:**
  * Bruno Morales Hernández     — alu0101664309@ull.edu.es
  * Francisco Gabriel Ruiz Ruiz — alu0101618586@ull.edu.es
  * Adriel Reyes Suárez         — alu0101640136@ull.edu.es
* **Asignatura:** Interfaces Inteligentes
* **Curso:** 4.º Curso — Ingeniería Informática, ULL — 2026/2027

---

## Descripción

Esta práctica consiste en un pequeño juego de **realidad aumentada** para Android desarrollado en Unity con **AR Foundation** y **ARCore**. La aplicación utiliza la cámara del móvil para escanear el entorno y detectar tanto **superficies horizontales** (como el suelo o una mesa) como **superficies verticales** (paredes). Al tocar la pantalla sobre una de las superficies detectadas, se coloca un coche que sigue en todo momento a un retículo (puntero) controlado al mover el teléfono. Sobre ese mismo plano van apareciendo en posiciones aleatorias dos tipos de coleccionables: **gusanos buenos**, que suman un punto, y **bichos malos**, que restan uno.

El proyecto toma como base el *starter package* del codelab de AR Foundation de Google y construye encima la lógica y los elementos propios del juego:

* **Reconocimiento de superficies horizontales y verticales**, permitiendo fijar con un toque el plano sobre el que se va a jugar y ocultando el resto.
* **Conducción mediante apuntado**, proyectando un retículo desde el centro de la pantalla hacia el plano para guiar al coche sin necesidad de botones ni joysticks.
* **Generación continua de dos coleccionables con efectos opuestos**: un `Bicho Bueno` (+1 punto) y un `Bicho Malo` (−1 punto) que se reponen automáticamente en otro punto del plano al recogerlos.
* **Marcador de puntuación desacoplado por eventos** (patrón observador) mostrado en pantalla con TextMeshPro.

El juego está diseñado para ejecutarse directamente en un teléfono Android compatible con ARCore, que es donde se han realizado las pruebas.

---

## Estructura del repositorio

El repositorio contiene el proyecto de Unity, ignorando las carpetas autogeneradas (`Library/`, `Temp/`, `Logs/`, `.utmp/`, etc.):

```text
.
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity        # Escena única de la práctica
│   ├── Scripts/                     # Scripts propios: Recolectable y ContadorPuntos
│   ├── Prefabs/                     # Coche, Bicho Bueno y Bicho Malo
│   ├── Starter Package/             # Scripts y prefabs del codelab de Google (coche, retículo, planos...)
│   ├── ARCADE - FREE Racing Car/     # Modelo del coche
│   ├── Dirty_Bucket/                 # Modelo del gusano (Bicho Bueno)
│   ├── Hatogame_new/Watcher/         # Modelo del bicho malo
│   ├── Garshasp/One_Eye_Monster/     # Asset importado durante la práctica
│   ├── XR/                          # Loaders de ARCore y de la simulación XR
│   ├── TextMesh Pro/                # Recursos esenciales de TMP
│   └── Settings/                    # Assets de URP
├── docs/
│   ├── gifs/                        # GIF del funcionamiento final de este README
│   └── Referencias/                 # READMEs de prácticas anteriores
├── Packages/                        # Manifiesto de paquetes de Unity
├── ProjectSettings/                 # Configuración del motor
└── README.md
```

---

## Conceptos clave de AR Foundation

Para entender cómo está montada la escena conviene tener claras las cuatro piezas principales de AR Foundation sobre las que se apoya el proyecto:

* **AR Session y XR Origin (la cámara en el mundo real):** `AR Session` controla el ciclo de vida de la realidad aumentada en el móvil, mientras que `XR Origin` conecta el dispositivo físico con el mundo virtual de Unity. Dentro de él se encuentra la cámara principal (`Main Camera`), que muestra de fondo el vídeo real del teléfono (`ARCameraBackground`) y copia el movimiento de nuestras manos gracias al `TrackedPoseDriver`.
* **ARPlaneManager (detector de superficies horizontales y verticales):** a medida que movemos la cámara, analiza la imagen buscando superficies planas —tanto horizontales como verticales— y crea en la escena una malla visual por cada una (`Driving Surface Plane`) para que el jugador vea dónde puede colocar el coche.
* **ARRaycastManager (el puntero invisible):** permite lanzar rayos desde la pantalla hacia el mundo real y detectar en qué punto exacto cortan a los planos generados. Es lo que utiliza el retículo para posarse siempre sobre la superficie a la que apuntamos.
* **ARCore:** es el motor de realidad aumentada de Google que trabaja por debajo en Android para hacer el seguimiento del movimiento y del entorno físico.

---

## 1. Preparación del proyecto

### 1.1 Entorno y paquetes

El proyecto se ha creado con **Unity 6000.5.11f1** a partir de la plantilla **URP** (*Universal Render Pipeline*). Desde **Window > Package Manager** se instalaron los paquetes necesarios para dar soporte a realidad aumentada en Android, control de entrada e interfaz de usuario:

| Paquete | Versión | Función |
|---|---|---|
| XR Plugin Management | 4.7.0 | Gestiona y activa los proveedores de realidad extendida por plataforma. |
| AR Foundation | 6.5.1 | Proporciona los componentes de RA en Unity (sesión, planos, raycast y cámara). |
| Google ARCore XR Plugin | 6.5.1 | Conecta AR Foundation con ARCore en dispositivos Android. |
| Input System | 1.20.0 | Sistema de entrada que alimenta el seguimiento de posición de la cámara (`TrackedPoseDriver`). |
| uGUI / TextMesh Pro | 2.5.0 | Interfaz de usuario (Canvas) y texto del marcador de puntos. |

Además de estos paquetes (y de los recursos esenciales de TextMeshPro), se importó como base el paquete `codelab-starter-package.unitypackage` de Google en `Assets/Starter Package/`. Este paquete incluye los scripts que resuelven el movimiento del coche, el retículo, la generación sobre planos y el bloqueo de la superficie (`CarManager`, `CarBehaviour`, `ReticleBehaviour`, `PackageSpawner`, `PackageBehaviour` y `DrivingSurfaceManager`). A partir de esa base se crearon los prefabs personalizados en `Assets/Prefabs/` y los scripts de puntuación en `Assets/Scripts/`.

### 1.2 Configuración para Android

En **Edit > Project Settings > XR Plug-in Management** se habilitó **ARCore** dentro de la pestaña de Android (además del soporte de *XR Simulation* para el editor). Por último, en **Build Settings** se dejó `Assets/Scenes/SampleScene.unity` como única escena activa para generar el APK final.

---

## 2. Montaje de la escena AR

### 2.1 Cámara y sesión AR

La escena arranca con dos objetos base de AR Foundation: **AR Session** (que activa la sesión y la lectura de entrada con `ARInputManager`) y **XR Origin**, que contiene la **Main Camera**. Gracias a los componentes `ARCameraManager`, `ARCameraBackground` y `TrackedPoseDriver`, la cámara de Unity reproduce en tiempo real lo que enfoca el teléfono y se desplaza por la escena al mismo ritmo que el usuario mueve el móvil.

### 2.2 Detección de planos (horizontales y verticales) y bloqueo de superficie

El objeto **Driving Surface Manager** centraliza el reconocimiento del entorno: agrupa el `ARPlaneManager`, el `ARRaycastManager` y el script `DrivingSurfaceManager.cs`.

En el componente **`ARPlaneManager`** se ha asignado `Driving Surface Plane.prefab` para dibujar las mallas detectadas y se ha configurado su *Detection Mode* en **Everything (Horizontal y Vertical)**. De esta manera, el sistema reconoce tanto superficies horizontales (suelos, mesas) como superficies verticales (paredes) cuando las enfocamos con el móvil.

Para que el juego se desarrolle sobre una única superficie limpia, cuando el jugador toca la pantalla para colocar el coche se llama al método `LockPlane()` de `DrivingSurfaceManager`, que hace tres cosas:

1. **Guarda el plano elegido** como superficie de juego (`LockedPlane`).
2. **Oculta todos los demás planos** que se hubieran detectado hasta ese momento y desactiva automáticamente cualquier plano nuevo que aparezca después.
3. **Mantiene la superficie actualizada:** si mientras jugamos ARCore refina la detección y fusiona o amplía nuestro plano por uno más preciso (propiedad `subsumedBy`), la referencia se actualiza sola para que ni el coche ni los bichos pierdan la pista.

### 2.3 El retículo

El objeto **Reticle Prefab** actúa como mira y guía del coche mediante el script `ReticleBehaviour.cs`. En cada fotograma lanza un rayo desde el centro de la pantalla buscando planos detectados:

* **Antes de colocar el coche**, el retículo se posa sobre cualquier plano horizontal o vertical al que estemos apuntando, indicando dónde aparecerá el vehículo si tocamos la pantalla.
* **Una vez colocado el coche**, el retículo ignora cualquier otra superficie y solo se mueve dentro del plano que quedó bloqueado.
* Si en algún momento apuntamos fuera del plano, el indicador visual del retículo se oculta hasta que volvemos a enfocar la superficie.

---

## 3. Mecánica del juego

### 3.1 Colocar el coche y conducirlo

Al abrir la aplicación, el retículo —un pequeño indicador en el centro de la pantalla— solo aparece cuando la cámara está apuntando a una superficie horizontal o vertical detectada. En cuanto se toca la pantalla en ese momento, el coche aparece sobre el plano y esa superficie queda fijada como zona de juego: los demás planos que detecte ARCore dejan de tenerse en cuenta.

A partir de ahí, el coche avanza siempre hacia el retículo, girando suavemente para orientarse hacia él. Como el retículo sigue el centro de la imagen de la cámara, para conducir basta con mover y apuntar con el móvil: el coche va hacia donde se mira, sin joysticks ni botones.

Toda esta mecánica (colocación del coche, bloqueo del plano y seguimiento del retículo) viene ya resuelta en el starter package del codelab; el trabajo del grupo consistió en añadir los coleccionables y la puntuación encima.

### 3.2 Aparición de los coleccionables

Sobre el plano bloqueado aparecen dos bichos en posiciones elegidas al azar dentro de la propia superficie, siempre a la altura correcta para que queden apoyados y no floten ni se hundan. Cuando el coche alcanza uno de ellos, el bicho desaparece y vuelve a generarse otro en un punto distinto, de forma que siempre hay un gusano bueno y un bicho malo disponibles sobre el plano.

Este comportamiento también forma parte del starter package. La única incorporación del grupo fue un segundo generador independiente, para que los dos tipos de bicho puedan estar en escena a la vez y reponerse por separado.

### 3.3 Prefabs y assets importados

Los tres prefabs finales están en `Assets/Prefabs/`:

| Prefab | Modelo | Componentes añadidos | Valor |
|---|---|---|---|
| `Coche.prefab` | *Free Racing Car* (**ARCADE - FREE Racing Car**) | `CarBehaviour`, `BoxCollider`, `Rigidbody`, `ContadorPuntos`, tag `Player` | — |
| `Bicho Bueno.prefab` | *Worm_Bucket_2* (**Dirty_Bucket**, con animación `worm_anim`) | `PackageBehaviour`, `Recolectable`, `BoxCollider` en modo *trigger* | `pointsToGive = 1` |
| `Bicho Malo.prefab` | *Watcher* (**Hatogame_new**, con `Watcher_Controller` y el material `WatcherA_Mat`) | `PackageBehaviour`, `Recolectable`, `Animator`, `BoxCollider` en modo *trigger* | `pointsToGive = -1` |

Los tres sustituyen a los prefabs del starter package, pero conservan sus scripts de base. Los dos bichos comparten la configuración de colisión (collider *trigger*) y el script `Recolectable`; lo único que cambia entre ellos, aparte del modelo, es el signo de `pointsToGive`.

---

## 4. Sistema de puntuación con eventos

La comunicación entre los coleccionables y el marcador no se hace con referencias directas, sino con un **evento estático**, siguiendo el mismo patrón observador de prácticas anteriores.

### 4.1 `Recolectable.cs`: el notificador

```csharp
public int pointsToGive = 0;

public delegate void Message(int points);
public static event Message OnHit;

void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.CompareTag("Player")) {
        if (OnHit != null) {
            OnHit(pointsToGive);
        }
    }
}
```

Cualquier bicho con este script avisa al entrar en contacto con el coche (`Player`) y pasa su `pointsToGive` como argumento. Al ser `static`, el evento pertenece a la clase y no a una instancia: el notificador no necesita saber quién escucha ni arrastrar referencias al marcador.

### 4.2 `ContadorPuntos.cs`: el suscriptor

```csharp
void Awake() {
    Recolectable.OnHit += AddPointsAndUpdateText;
}
```

En `Start()` se inicializa `playerPoints = 0`, se busca el GameObject con la etiqueta **`Puntuacion`** (añadida en el `TagManager` del proyecto) y se obtiene su `TMP_Text`. Ese texto es un `TextMeshProUGUI` hijo del **Canvas** de la escena, con el valor inicial `Gusanos = 0` en el editor.

Cada vez que llega el evento, `AddPointsAndUpdateText(int points)` suma (o resta) los puntos y llama a `UpdateText()`, que escribe:

```csharp
textToKeepUpdated.text = "Gusanos: " + playerPoints;
```

Si no existe el objeto con la etiqueta o no tiene `TMP_Text`, el script avisa con `Debug.LogError`. El componente está colocado en el Canvas de la escena y se conserva también en `Coche.prefab`; como ambos escuchan el mismo evento estático y escriben el mismo texto, el valor visible es el mismo.

### 4.3 Flujo completo de una colisión

1. El coche, etiquetado como `Player`, entra en el `BoxCollider` *trigger* de un bicho.
2. `Recolectable.OnTriggerEnter()` dispara `OnHit(pointsToGive)` (+1 o −1).
3. `ContadorPuntos` recibe el aviso, actualiza `playerPoints` y reescribe el texto TMP.
4. `CarBehaviour.OnTriggerEnter()` destruye el bicho por tener `PackageBehaviour`.
5. El spawner correspondiente detecta `Package == null` y coloca un nuevo bicho en otra posición aleatoria del plano.

---

## 5. Demostración del funcionamiento final

![Funcionamiento final de la práctica](docs/gifs/funcionamiento-final.gif)

*Detección del plano, colocación del coche con un toque, conducción con el retículo, recolección de gusanos buenos (+1) y bichos malos (−1) y actualización de la puntuación en pantalla.*

---

## 6. Ejecución y resultados

Durante el desarrollo se entró en modo Play únicamente para comprobar que la consola no mostraba ningún error rojo. Ese modo no sirvió para probar el juego: la detección de planos y las interacciones solo se pueden validar con la cámara del dispositivo, así que **todas las pruebas reales se hicieron en un teléfono Android** con la compilación final de **ARCore**. La tabla resume el comportamiento comprobado en el móvil:

| Elemento | Acción | Respuesta |
|---|---|---|
| `ARPlaneManager` | Apuntar la cámara a una superficie horizontal o vertical | Se detecta el plano y se dibuja con `Driving Surface Plane.prefab`. |
| Retículo | Mover el móvil | El rayo al centro de la pantalla reposiciona el retículo sobre el plano. |
| Pantalla | Tocar con el retículo sobre un plano | Aparece el coche y el plano queda bloqueado (los demás se desactivan). |
| Coche | Apuntar a otro punto del plano | El coche gira y avanza hacia el retículo. |
| `Bicho Bueno` | El coche lo alcanza | Se destruye, se emite `OnHit(1)` y el marcador sube a `Gusanos: N+1`. |
| `Bicho Malo` | El coche lo alcanza | Se destruye, se emite `OnHit(-1)` y el marcador baja a `Gusanos: N-1`. |
| Spawners | Tras destruir un bicho | Se genera uno nuevo en una posición aleatoria de la malla del plano. |
| Plano bloqueado | ARCore refina la detección (`subsumedBy`) | `LockedPlane` se actualiza y todo sigue sobre la superficie correcta. |

---

## Conclusión

El proyecto muestra cómo se construye una experiencia de realidad aumentada completa a partir de tres piezas de AR Foundation: la **sesión y el XR Origin** que aportan la cámara real, la **detección de planos** con `ARPlaneManager` y `ARRaycastManager` que convierte una superficie del mundo en un escenario de juego, y los **scripts de juego** que se apoyan en esa superficie. El bloqueo de un único plano y el uso de su malla para generar posiciones aleatorias son los que permiten que el coche y los coleccionables se comporten de forma estable aunque ARCore siga refinando la detección.

La parte más útil de la práctica ha sido la separación de responsabilidades en la puntuación: `Recolectable` solo avisa de que ha habido un impacto y `ContadorPuntos` solo decide qué hacer con ese aviso. Gracias al evento estático, añadir un nuevo tipo de coleccionable, cambiar su valor o sustituir el marcador por otro no obliga a tocar el resto del juego.
