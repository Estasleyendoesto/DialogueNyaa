# DialogueNyaa
Unity Dialogue System

## Requiere instalar Newtonsoft
> Añadir por Git URL / Nombre de Paquete

1. En Unity, abre la ventana Window > Package Manager.

2. Haz clic en el icono de + (esquina superior izquierda de la ventana).

3. Selecciona Add package from git URL....

4. Pega exactamente esto:

    ````
    com.unity.nuget.newtonsoft-json
    ````

5. Pulsa Add.

## Ejemplo de uso
```nyaa
// El script debe comenzar declarando un nodo válido
node entrada_mansion:
Voz: ¿Hay alguien ahí?
:: speed 0.5
Voz: Te advertí que no debías venir a esta casa...
:: speed
:: wait 1.5
Voz: Pero ya que estás aquí, ¿qué vas a hacer?

- Forzar la puerta principal
    Tú: (Empujas la puerta con todas tus fuerzas).
    Voz: Demasiado ruidoso. Ya saben que eres una visita inesperada.
    :: jump pasillo_principal

- Buscar una ventana abierta
    Tú: (Rodeas la estructura buscando otra entrada).
    :: jump jardin_trasero

- Revisar el viejo buzón
    Tú: (Miras dentro del metal oxidado).
    :: continue

Tú: Solo hay telarañas y una llave oxidada.
:: if HasFlashlight
    Tú: Enciendo la linterna. La luz parpadea, pero ilumina el porche.
    :: jump pasillo_principal
:: elseif HasLighter
    Tú: Uso el encendedor. Apenas veo el suelo, pero servirá.
    :: jump pasillo_principal
:: else
    Tú: Está demasiado oscuro para seguir por aquí. Volveré de día.
    :: endstory
:: end

node pasillo_principal:
:: auto 2.0
Voz: Los suelos de madera crujen bajo tus pies sin que pulses nada.
Voz: Sientes una presencia...
:: auto 0
Voz: No eres bienvenido aquí. (El texto se detiene y espera input).
:: jump final_demo

node jardin_trasero:
:: autoonce 1.0
Tú: La ventana del sótano está rota. Entro con cuidado.
// 'autoonce' asume que se desactiva solo tras esta línea, 
// por lo que aquí no haría falta un ':: auto 0' manual.
:: jump final_demo

node final_demo:
Voz: Ya no hay vuelta atrás.
// Un comando custom que el validador ignorará sin lanzar error
:: PlaySound PuertaCerrandose 
:: endstory
```

## Comandos del Parser
```nyaa
:: jump <nodo> – Salta a otro nodo de la conversación.
:: wait <segundos> – Pausa la historia durante X segundos antes de continuar.
:: speed <multiplicador> – Altera la velocidad del typewriter (ej: :: speed 2 para rápido, 0.5 para lento).
:: auto <segundos> – Activa el avance automático con retardo por línea.
:: autoonce <segundos> – Solo avanza automáticamente la siguiente línea.
:: if <condicion> / :: elseif / :: else / :: end – Bifurcaciones condicionales.
:: endstory – Finaliza la historia inmediatamente.
:: return <valores...> – Envía resultados al juego al terminar (ej: :: return to_work).
:: event <nombre> [args...] – Dispara un evento de juego genérico (NyaaDialogue.OnEvent).
:: log <texto...> / :: warn <texto...> – Mensajes a la consola de Unity.
```