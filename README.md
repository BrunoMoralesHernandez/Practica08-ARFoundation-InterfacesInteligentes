# Práctica 7: Realidad aumentada con AR Foundation y ARCore — Interfaces Inteligentes

* **Autores:**
  * Bruno Morales Hernández     — alu0101664309@ull.edu.es
  * Francisco Gabriel Ruiz Ruiz — alu0101618586@ull.edu.es
  * Adriel Reyes Suárez         — alu0101640136@ull.edu.es
* **Asignatura:** Interfaces Inteligentes
* **Curso:** 4.º Curso — Ingeniería Informática, ULL — 2026/2027

---

## Descripción

Esta práctica es un pequeño juego de **realidad aumentada** para Android desarrollado con **AR Foundation** y **ARCore** en Unity. La aplicación detecta superficies horizontales con la cámara del móvil, coloca un coche sobre el plano detectado con un toque en la pantalla y hace que el coche siga un retículo que se mueve con el dispositivo. Sobre el mismo plano aparecen, en posiciones aleatorias, dos tipos de coleccionables: **gusanos buenos**, que suman un punto, y **bichos malos**, que restan uno. La puntuación se muestra en pantalla con TextMeshPro.

El proyecto parte del *starter package* del codelab de AR Foundation de Google (coche, retículo, detección de planos y spawners) y añade encima la capa de juego propia:

* **Detección de planos** con `ARPlaneManager` y bloqueo de una única superficie de conducción.
* **Raycast al centro de la pantalla** con `ARRaycastManager` para mover el retículo.
* **Colocación del coche por toque** y bloqueo del plano a partir de ese momento.
* **Aparición aleatoria** de coleccionables dentro de la malla del plano (`ARPlaneMeshVisualizer.mesh`).
* **Puntuación con eventos** (patrón observador): `Recolectable` emite `OnHit(int)` y `ContadorPuntos` se suscribe y actualiza el texto TMP.
* **Dos coleccionables con valores opuestos**: `Bicho Bueno` (+1) y `Bicho Malo` (−1).
* **Interfaz** con un Canvas y un texto `TMP_Text` etiquetado como `Puntuacion`.

El juego se ha pensado para ejecutarse en un móvil Android con ARCore, que es donde se han realizado todas las pruebas.

---

## Estructura del repositorio

El repositorio contiene el proyecto de Unity, filtrando las carpetas autogeneradas (`Library/`, `Temp/`, `Logs/`, etc.):

```text
.
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity        # Escena única de la práctica
│   ├── Scripts/                     # Scripts propios: BichoSpawner, Recolectable, ContadorPuntos
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

Antes de describir el montaje conviene fijar los elementos sobre los que se construye la práctica:

* **AR Session:** gestiona el ciclo de vida de la sesión de RA en el dispositivo. En la escena va acompañado de `ARInputManager`.
* **XR Origin:** rig que representa al usuario. Contiene la **AR Camera**, que además lleva `ARCameraManager` (configuración de la cámara), `ARCameraBackground` (vuelca la imagen de la cámara real en el fondo) y un `TrackedPoseDriver` que aplica la pose del dispositivo.
* **ARPlaneManager:** detecta superficies planas y crea un GameObject por cada una a partir de `Driving Surface Plane.prefab`, que incluye `ARPlaneMeshVisualizer` para generar la malla del plano.
* **ARRaycastManager:** lanza rayos contra los planos detectados. Aquí se usa `TrackableType.PlaneWithinPolygon` para obtener el punto del plano que hay bajo el centro de la pantalla.
* **ARPlane:** cada superficie detectada, con su `trackableId`, su `center` y su malla. Los planos pueden ser *subsumidos* (`subsumedBy`) por otros cuando el sistema mejora la estimación.
* **ARCore / XR Simulation:** ARCore es el proveedor de RA en Android que usa la aplicación. El proyecto incorpora también el loader de XR Simulation de AR Foundation.

---

## 1. Preparación del proyecto

### 1.1 Entorno y paquetes

El proyecto se ha desarrollado con **Unity 6000.5.11f1** y la plantilla **URP** (`com.unity.render-pipelines.universal` 17.5.0). Desde **Window > Package Manager** se instalaron, o quedaron como dependencias, estos paquetes:

| Paquete | Versión | Función |
|---|---|---|
| XR Plugin Management | 4.7.0 | Activa los proveedores de XR por plataforma. |
| AR Foundation | 6.5.1 | API común de RA (sesión, planos, raycast, cámara). |
| Google ARCore XR Plugin | 6.5.1 | Implementación de AR Foundation para Android. |
| Input System | 1.20.0 | Entrada moderna; alimenta el `TrackedPoseDriver`. |
| uGUI / TextMesh Pro | 2.5.0 | Canvas y texto de puntuación. |

También se importaron los **TMP Essential Resources** (`Assets/TextMesh Pro/`) para poder usar `TMP_Text` en la interfaz.

Como punto de partida se importó el `codelab-starter-package.unitypackage` del codelab de AR Foundation de Google. De él viene la carpeta `Assets/Starter Package/`, con los scripts `CarManager`, `CarBehaviour`, `ReticleBehaviour`, `PackageSpawner`, `PackageBehaviour` y `DrivingSurfaceManager`, y los prefabs `Car Prefab`, `Package Prefab`, `Reticle Prefab` y `Driving Surface Plane`. Esa base se conservó y se extendió con los scripts y prefabs propios de `Assets/Scripts/` y `Assets/Prefabs/`.

### 1.2 Configuración de XR Plug-in Management

En **Edit > Project Settings > XR Plug-in Management** se activó **ARCore** para la plataforma Android (`Assets/XR/Loaders/ARCoreLoader.asset`). El proyecto incorpora además el loader de **XR Simulation** de AR Foundation (`Assets/XR/Loaders/SimulationLoader.asset`), pero las pruebas del juego se realizaron en el dispositivo.

La escena `Assets/Scenes/SampleScene.unity` es la única habilitada en **Build Settings** y la compilación final se realizó para Android con ARCore.

---

## 2. Montaje de la escena AR

### 2.1 AR Session, XR Origin y cámara AR

La escena contiene un objeto **AR Session** con `ARSession` y `ARInputManager`, y un **XR Origin** con el **Camera Offset** y la **Main Camera**. La cámara lleva `ARCameraManager`, `ARCameraBackground` y un `TrackedPoseDriver` del Input System, que actualiza su posición y rotación con la pose del dispositivo. Así, el fondo de la escena es la imagen en directo de la cámara del móvil.

### 2.2 Detección y bloqueo de planos

El objeto **Driving Surface Manager** reúne `ARPlaneManager`, `ARRaycastManager`, `XROrigin` y el script `DrivingSurfaceManager.cs`. El `ARPlaneManager` tiene asignado `Driving Surface Plane.prefab` como prefab de plano y detecta superficies horizontales.

`DrivingSurfaceManager.LockPlane()` se llama cuando se coloca el coche. Su trabajo es:

1. Recorrer `PlaneManager.trackables` y **desactivar todos los planos menos el elegido**.
2. Guardar ese plano en `LockedPlane`.
3. Suscribirse a `PlaneManager.planesChanged` con `DisableNewPlanes()`, para que los planos que se detecten después también se desactiven.

Además, en `Update()` se comprueba `LockedPlane.subsumedBy`: si ARCore reemplaza el plano por una versión mejorada, la referencia se actualiza al nuevo plano. Así el coche y los coleccionables siguen sobre la superficie correcta aunque el sistema refine la detección.

### 2.3 El retículo

El prefab **Reticle Prefab** lleva `ReticleBehaviour.cs`. En cada `Update()`:

* Lanza un rayo desde el centro de la pantalla (`Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f))`) con `RaycastManager.Raycast(...)` y `TrackableType.PlaneWithinPolygon`.
* Si aún no hay plano bloqueado, usa el primer impacto; si ya hay uno, busca el impacto cuyo `trackableId` coincide con `LockedPlane.trackableId`.
* Coloca el retículo en `hit.Value.pose.position`, guarda el plano en `CurrentPlane` y activa o desactiva su hijo según haya plano válido.

El retículo es, por tanto, el punto al que se dirige el coche y la posición donde se colocará al principio.

---

## 3. Mecánica del juego

### 3.1 Colocar el coche y conducirlo

Al abrir la aplicación, el retículo —un pequeño indicador en el centro de la pantalla— solo aparece cuando la cámara está apuntando a una superficie horizontal detectada. En cuanto se toca la pantalla en ese momento, el coche aparece sobre el plano y esa superficie queda fijada como zona de juego: los demás planos que detecte ARCore dejan de tenerse en cuenta.

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
| `ARPlaneManager` | Apuntar la cámara a una superficie horizontal | Se detecta el plano y se dibuja con `Driving Surface Plane.prefab`. |
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
