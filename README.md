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
- **Espacio:** dejar una bomba en la casilla libre delante del personaje. Se permiten hasta dos cargas; cada bomba detona a los 2,5 segundos.
- **T:** con el guante, recoger la bomba que esté delante del robot; pulsa **T** de nuevo para lanzarla hasta dos casillas en la dirección hacia la que mira.
- **P:** pausar o continuar.
- Tras perder las tres vidas, **Enter** o **R** reinicia; **X** sale.

## Reglas

Los bloques de acero no se pueden cruzar ni destruir. Los ladrillos se dibujan como bloques completos y se destruyen con la explosión, cuyo alcance es de una casilla en cada dirección. El robot también recibe daño de sus propias bombas. Hay tres monstruos por nivel: avanzan casilla por casilla, respetan paredes y bombas, y explotan en un área de una casilla cuando se acercan al personaje. El mapa se amplía en cada nivel; al eliminar los tres monstruos se avanza al siguiente.

El guante y el potenciador azul aparecen ocultos bajo ladrillos del primer nivel. El potenciador recupera una carga de bomba.

## Archivos

- `Form1.cs`: ventana principal; reúne el tablero matricial, los gráficos, el movimiento, las bombas, los enemigos y los niveles.
- `Assets/robot.png`: personaje recortado de la imagen adjunta.
- El mapa adjunto está reconstruido en la matriz del nivel 1. `floor.png`, `steel.png` y `brick.png` contienen sus texturas de casilla.
- `Assets/bomb.jpg`: imagen de bomba adjunta, mostrada al detonar.
- `Assets/monster.png` y `Assets/monster-explosion.png`: monstruo y efecto de explosión recortados de la imagen adjunta.



