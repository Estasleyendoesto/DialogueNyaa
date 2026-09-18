# DialogueNyaa — Motor de Diálogos para Unity

Motor de narrativa y diálogos para Unity diseñado bajo la filosofía **KISS** (Keep It Simple, Stupid): modular, legible, ultra ligero (~100-150 líneas por componente), sin dependencias pesadas ni boilerplate innecesario.

---

## 1. Requisitos e Instalación

* **Unity 2022.3 LTS o Unity 6+** (compatible con Universal Render Pipeline y Built-in RP).
* **New Input System** (`com.unity.inputsystem`).
* **TextMeshPro** (`com.unity.ugui`).
* **Newtonsoft Json** (`com.unity.nuget.newtonsoft-json`):
  * Abre en Unity: `Window > Package Manager` → clic en `+` → `Add package by name...` → escribe `com.unity.nuget.newtonsoft-json`.

---

## 2. Inicio Rápido (Quick Start en 3 minutos)

### Paso 1: Añadir a la escena
Arrastra el prefab **`SimpleDialogueNyaa.prefab`** (ubicado en `DialogueNyaa/Prefabs/`) a tu escena. Este prefab ya contiene el `DialogueSystem` y la interfaz visual básica configurada.

### Paso 2: Crear tu primer guion (`.nyaa`)
Crea un archivo de texto en tu carpeta `Assets/` con extensión `.nyaa` (ejemplo: `Intro.nyaa`):

```nyaa
node start:
    Voz: ¡Hola viajero! Bienvenido al reino.
    
    - "Gracias, ¿dónde estoy?"
        Voz: Estás en los límites del bosque prohibido.
        :: jump advertencia

    - "No tengo tiempo para hablar"
        Voz: Qué descortés... pero ten cuidado de todos modos.
        :: endstory

node advertencia:
    Voz: Se dice que criaturas extrañas rondan por la noche.
    :: wait 1.0
    Voz: Si ves una sombra... corre.
    :: endstory
```

### Paso 3: Reproducir desde cualquier script C#
```csharp
using UnityEngine;
using EEsto.DialogueNyaa;

public class TestDialogue : MonoBehaviour
{
    [SerializeField] private TextAsset storyFile;

    private void Start()
    {
        NyaaDialogue.Play(storyFile);
    }
}
```
¡Listo! Al pulsar Play, el diálogo comenzará automáticamente y podrás avanzar haciendo clic o pulsando `Espacio` / `Enter`.

---

## 3. Arquitectura del Motor

El sistema separa estrictamente el motor de ejecución, la integración con Unity y las vistas visuales para garantizar que el código sea limpio y testeable:

```text
DialogueNyaa/
├── Core/             # Motor C# PURO (Agnóstico de Unity, cero MonoBehaviours)
│   ├── DialogueRunner      # Máquina de estados que recorre instrucciones y timers
│   ├── DialogueParser      # Analizador sintáctico que compila .nyaa a bytecode
│   ├── DialogueContext     # Tablas de Condiciones, Predicados y Comandos
│   └── DialogueValidator   # Validador de consistencia de saltos y nodos
│
├── System/           # Integración con Unity (MonoBehaviours y Fachada)
│   ├── NyaaDialogue        # Fachada pública estática (punto de entrada único)
│   ├── DialogueSystem      # Gestor Singleton para la historia principal
│   ├── BubbleSystem        # Gestor de bocadillos concurrentes sobre cabezas de NPCs
│   └── LocalizationManager # Adaptador simple para traducir textos y oradores
│
├── UI/               # Capa visual (Vistas y Presenters intercambiables)
│   ├── IDialogueView       # Contrato común para cualquier interfaz de diálogo
│   ├── Views/              # Vistas de serie listas para usar
│   └── Presenters/         # Componentes reusables (Typewriter, Choices, Lines)
│
└── Audio/            # Sistema de efectos sonoros y beeps de voz por carácter
```

---

## 4. Catálogo de Vistas Incluidas (`UI/Views/`)

El sistema incluye varias vistas listas para usar con solo asignarlas en el campo `View` de `DialogueSystem`:

1. **`SimpleDialogueView`**: Ventana clásica inferior con caja de texto, nombre de orador y contenedor de opciones.
2. **`SpeechBubbleView`**: Bocadillo flotante estilo cómic sobre la cabeza de personajes con auto-orientación hacia la cámara (*billboarding*).
3. **`CinematicDialogueView`**: Subtítulos cinematográficos limpios con barras negras letterbox, ideal para cinemáticas o intros.
4. **`DocumentDialogueView`**: Lectura in-game de cartas, diarios o libros con paginación, sonido de pasar página y modo lectura.
5. **`TransmissionDialogueView`**: Estilo códec / radio militar / transmisión holográfica con efecto de estática, parpadeo y retrato del interlocutor.

### Cómo crear tu propia vista personalizada
Si quieres un diseño totalmente a medida, solo crea un `MonoBehaviour` que implemente la interfaz **`IDialogueView`**:

```csharp
using System.Collections.Generic;
using UnityEngine;
using EEsto.DialogueNyaa;

public class MiVistaPersonalizada : MonoBehaviour, IDialogueView
{
    public void Initialize(DialogueSystem system) { }
    public void ShowLine(DialogueLine line) { /* Mostrar texto y orador */ }
    public void ShowChoices(List<ChoiceOption> choices) { /* Mostrar botones de opción */ }
    public void ChangeSpeed(float multiplier) { }
    public void Close() { gameObject.SetActive(false); }
}
```

---

## 5. Bocadillos Concurrentes en Cabezas de NPCs (`BubbleSystem`)

Para evitar que los diálogos flotantes sobre aldeanos o enemigos bloqueen o interfieran con el diálogo principal, existe **`BubbleSystem`**:

* **Múltiples NPCs hablando a la vez:** Cada bocadillo tiene su propia máquina de estados (`DialogueRunner`) independiente.
* **Seguimiento 3D/2D:** El bocadillo se ancla al `Transform` de la cabeza del NPC y se orienta hacia la cámara.
* **Evita solapamientos:** Si un NPC ya tiene un bocadillo activo y se le pide otro, el anterior se detiene automáticamente.

### Uso desde C#
```csharp
public Transform npcHead;
public TextAsset npcDialogue;

// Mostrar bocadillo
NyaaDialogue.Bubble(npcHead, npcDialogue, "saludo");

// Detener el bocadillo de este NPC
NyaaDialogue.StopBubble(npcHead);

// Detener todos los bocadillos activos del mundo
NyaaDialogue.StopAllBubbles();
```

---

## 6. API Completa en C# (`NyaaDialogue`)

La clase estática **`NyaaDialogue`** es el único punto de contacto que necesitarás en tu código:

```csharp
// --- Control del Diálogo Principal ---
NyaaDialogue.Play(storyAsset);                             // Inicia en nodo "start"
NyaaDialogue.Play(storyAsset, "capitulo_2");                // Inicia en nodo específico
NyaaDialogue.Play(storyAsset, "mision", resultados =>       // Recibe valores de :: return
{
    Debug.Log($"Resultado: {resultados[0]}");
});

NyaaDialogue.Continue();    // Avanza el diálogo manualmente
NyaaDialogue.Stop();        // Detiene el diálogo principal inmediatamente
bool activo = NyaaDialogue.IsActive;

// --- Condiciones y Predicados ---
// Condiciones booleanas evaluadas al vuelo (ej: :: if TieneLlave o :: if !EsDeDia)
NyaaDialogue.Conditions["TieneLlave"] = () => PlayerInventory.HasKey;
NyaaDialogue.Conditions["EsDeDia"] = () => DayNightCycle.IsDay;

// Predicados con argumentos (ej: :: if has_item pocion 2)
NyaaDialogue.Predicates["has_item"] = args => PlayerInventory.Count(args[0]) >= int.Parse(args[1]);

// --- Comandos de Juego y Eventos ---
// Registrar comando personalizado invocado desde guion con :: dar_recompensa espada
NyaaDialogue.Commands["dar_recompensa"] = args =>
{
    string item = args[0];
    PlayerInventory.Add(item);
};

// Escuchar eventos generales lanzados con :: event puerta_abierta
NyaaDialogue.OnEvent += (eventName, args) =>
{
    if (eventName == "puerta_abierta")
        Door.Open();
};

// Eventos de ciclo de vida
NyaaDialogue.OnStart += () => Debug.Log("Diálogo iniciado");
NyaaDialogue.OnEnd   += () => Debug.Log("Diálogo finalizado");
```

---

## 7. Sintaxis de Guiones (`.nyaa`)

### Estructura Básica
* Los comentarios empiezan por `//`.
* Cada sección de conversación comienza con `node <nombre>:`.
* Los diálogos se escriben en formato `Orador: Texto a mostrar`. Si no se incluye orador, se muestra solo el texto narrativo.
* Las opciones de elección empiezan por `- "Texto de la opción"`.
* Los comandos y lógica empiezan por `::`.

```nyaa
node tienda:
    Tendero: ¿Qué deseas comprar hoy?
    
    - "Comprar poción (50 monedas)"
        :: if tiene_oro_pocion
            Tendero: ¡Aquí tienes!
            :: return pocion
        :: else
            Tendero: No tienes suficiente oro.
            :: jump tienda
        :: end

    - "Solo estoy mirando"
        Tendero: Vuelve cuando tengas dinero.
        :: endstory
```

### Tabla de Comandos Nativos del Motor

| Comando | Sintaxis | Descripción |
| :--- | :--- | :--- |
| **`jump`** | `:: jump <nodo>` | Salta a otro nodo del guion. |
| **`wait`** | `:: wait <segundos>` | Pausa la historia X segundos antes de continuar. |
| **`speed`** | `:: speed <multiplicador>` | Modifica la velocidad del typewriter (`1.0` es normal, `2.0` rápido). |
| **`auto`** | `:: auto <segundos>` | Activa auto-play continuo con retardo de X segundos por línea (`:: auto 0` para desactivar). |
| **`autoonce`** | `:: autoonce <segundos>` | Solo avanza automáticamente la siguiente línea. |
| **`if` / `elseif` / `else` / `end`** | `:: if <condición>` | Bifurcaciones lógicas. Evalúa condiciones simples (soporta `!TieneLlave`) o predicados con argumentos (`has_item pocion 2`). |
| **`endstory`** | `:: endstory` | Finaliza la historia inmediatamente. |
| **`return`** | `:: return <valores...>` | Devuelve valores al callback `onReturn` de C# y finaliza. |
| **`event`** | `:: event <nombre> [args...]` | Dispara el evento `NyaaDialogue.OnEvent` en Unity. |
| **`log` / `warn`** | `:: log <mensaje>` | Imprime mensajes en la consola de Unity para depuración. |

---

## 8. Localización (`LocalizationManager`)

`DialogueNyaa` incluye soporte para proyectos multi-idioma:
* Cada línea generada por el parser recibe un identificador único basado en el archivo y nodo (`storyId_node_index`).
* En el guion puedes especificar claves explícitas si lo prefieres:
  ```nyaa
  node saludo:
      [loc:saludo_rey] Rey: Bienvenido a mi castillo.
  ```
* Puedes registrar un proveedor de traducción conectándolo a tu sistema de localización favorito (I2 Localization, Unity Localization Package, o un simple CSV/JSON):
  ```csharp
  LocalizationManager.SetProvider((key, defaultText) =>
  {
      return MisTraducciones.Obtener(key) ?? defaultText;
  });
  ```