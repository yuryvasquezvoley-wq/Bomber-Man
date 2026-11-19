# Bomber Man — Robot del Laberinto

Juego de escritorio en C# con Windows Forms. La partida se juega en pantalla completa y usa el laberinto de referencia como mapa del primer nivel.

## Ejecutar

Necesitas Windows y el SDK de .NET 8. Abre `BomberMan.csproj` en Visual Studio y pulsa **Iniciar**, o ejecuta en esta carpeta:

```powershell
dotnet run --project BomberMan.csproj
```

Pulsa **Esc** para cerrar el juego.

## Controles

- **W, A, S, D:** mover al robot.
- **Espacio:** dejar una bomba. Se permiten hasta dos cargas; cada bomba detona a los 2,5 segundos.
- **P:** pausar o continuar.
- Tras perder las tres vidas, **Enter** o **R** reinicia; **X** sale.
- Al recoger el guante, mantén una dirección y pulsa **Espacio** para lanzar la bomba hasta dos casillas.

## Reglas

Los bloques de acero no se pueden cruzar ni destruir. Las paredes de ladrillo se destruyen con la explosión, cuyo alcance es de una casilla en cada dirección. El robot también recibe daño de sus propias bombas. Los monstruos recorren el laberinto, respetan paredes y bombas, y mueren con la explosión. El mapa se amplía en cada nivel; al eliminar todos los monstruos se avanza al siguiente.

El guante y el potenciador azul aparecen ocultos bajo ladrillos del primer nivel. El potenciador recupera una carga de bomba.

## Archivos

- `GameForm.cs`: tablero matricial, movimiento, bombas, enemigos, niveles y dibujo.
- `Assets/robot.png`: personaje recortado de la imagen adjunta.
- `Assets/maze-reference.png`: mapa adjunto reconstruido en la matriz del nivel 1; `floor.png`, `steel.png` y `brick.png` son sus texturas de casilla.
- `Assets/bomb.jpg`: imagen de bomba adjunta, mostrada al detonar.

