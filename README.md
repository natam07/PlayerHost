Multiplayer Fox vs Rabbits (TCP Unity Game)
Descripción general del proyecto
Este proyecto es un videojuego multijugador desarrollado en Unity utilizando una arquitectura cliente-servidor basada en TCP (con el código dado en el primer corte)
Los jugadores pueden unirse a una sala (lobby), donde el primero en conectarse actúa como host y los demás como clientes. Una vez iniciada la partida, se asignan roles aleatorios (Zorro o Conejo), y los jugadores deben moverse dentro del mapa en tiempo real.
El objetivo del juego es simple: el zorro debe atrapar a los conejos.

Arquitectura del sistema
- Servidor TCP (Host)
- Clientes TCP (Jugadores)
- Comunicación en tiempo real mediante mensajes de texto
- Sin uso de servicios externos (todo local o en red LAN)

Instrucciones para ejecutar el sistema
1. Ejecutar el Host
	1. Abrir el proyecto en Unity
	2. Ejecutar la escena principal
	3. El primer cliente que no encuentre servidor automáticamente actúa como HOST
	4. El host podrá:
   	   - Iniciar partida
   	   - Pausar/reanudar juego
   	   - Expulsar jugadores
- Ver el ping de los otros jugadores
2. Ejecutar clientes
	1. Abrir el build del juego o ejecutar otra instancia de Unity
	2. Conectarse a la IP del host (por defecto `127.0.0.1` para local)
	3. Entrar al lobby
	4. Esperar inicio de partida

Requisitos técnicos

- Unity 2021 o superior
- .NET Framework compatible con Unity
- Conexión TCP (LAN o local)
- Sistema operativo Windows (probado en Windows 10/11) 

Funcionalidades implementadas
- Sistema de lobby multijugador
- Arquitectura cliente-servidor TCP
- Asignación de roles (Host / Client)
- Roles de juego (Zorro / Conejo)
- Movimiento sincronizado en tiempo real
- Sistema de ping entre jugadores
- Expulsión de jugadores desde host
- Pausa y reanudación del juego
- Inicio y finalización de partida
- Detección de colisión (Zorro atrapa Conejo → Game Over)

Lógica del juego
- El servidor asigna roles aleatorios al iniciar la partida
- Los clientes envían su posición constantemente al servidor
- El servidor distribuye posiciones a todos los jugadores
- Si el zorro se acerca a un conejo dentro de un rango determinado, el juego termina

Errores o limitaciones conocidas
- Puede haber desincronización ligera en redes inestables
- No hay sistema de reconexión automática
- No se implementó interpolación avanzada de movimiento
- El sistema depende de que todos los clientes usen la misma versión del build
  
Capturas de pantalla (Están en el word)
- Lobby de jugadores
- Gameplay con zorro y conejos
- Panel de Game Over
- UI de host
