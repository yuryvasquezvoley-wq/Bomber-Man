# Bomber Man — Robot del Laberinto

Juego de escritorio en C# con Windows Forms. La partida se juega en pantalla completa y usa el laberinto de referencia como mapa del primer nivel.

## Descarga

[Descargar BomberMan.zip](https://github.com/yuryvasquezvoley-wq/Bomber-Man/raw/refs/heads/main/BomberMan.zip)

## Ejecutar

Necesitas Windows y el SDK de .NET 8. Abre `BomberMan.csproj` en Visual Studio y pulsa **Iniciar**, o ejecuta en esta carpeta:

```powershell
dotnet run --project BomberMan.csproj
```

Pulsa **Esc** para cerrar el juego.

## Controles

- **W, A, S, D:** avanzar una casilla por vez. El personaje solo entra a una casilla libre de la matriz y gira hacia la última dirección válida.
- **Espacio:** dejar una bomba debajo del personaje. Puede salir de esa casilla, pero la bomba le impide regresar hasta explotar. Se permiten hasta dos cargas; cada bomba detona a los 2,5 segundos.
- **T:** con el guante, recoger la bomba que esté delante del robot; pulsa **T** de nuevo para lanzarla hasta dos casillas en la dirección hacia la que mira.
- **P:** pausar o continuar.
- Tras perder las tres vidas, **Enter** o **R** reinicia; **X** sale.

## Reglas

El suelo verde indica las casillas transitables. Los bloques grises de acero no se pueden cruzar ni destruir. Los ladrillos cafés se dibujan como bloques completos; al destruirlos, la casilla pasa a ser césped verde y queda transitable (salvo que revele un potenciador, que también se puede pisar). Las bombas solo se colocan en una casilla verde delante del personaje y su explosión alcanza una casilla en cada dirección. Hay tres monstruos por nivel: avanzan casilla por casilla, respetan los muros y explotan en un área de una casilla al acercarse al personaje.

El guante y el potenciador azul aparecen ocultos bajo ladrillos del primer nivel. El potenciador recupera una carga de bomba.

## Archivos

- `Form1.cs`: ventana principal; reúne el tablero matricial, los gráficos, el movimiento, las bombas, los enemigos y los niveles.
- `Assets/robot.png`: personaje recortado de la imagen adjunta.
- El mapa adjunto está reconstruido en la matriz del nivel 1. `floor.png`, `steel.png` y `brick.png` contienen sus texturas de casilla.
- `Assets/bomb.jpg`: imagen de bomba adjunta, mostrada al detonar.
- `Assets/monster.png` y `Assets/monster-explosion.png`: monstruo y efecto de explosión recortados de la imagen adjunta.



