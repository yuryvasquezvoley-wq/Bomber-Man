using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BomberMan;

public sealed class GameForm : Form
{
    private enum Cell { Floor, Steel, Brick, HiddenGlove, HiddenPlus, Glove, Plus }
    private sealed class Bomb(int x, int y, DateTime placed)
    {
        public int X = x, Y = y;
        public DateTime Placed = placed;
        public bool Exploding;
        public DateTime ExplodedAt;
    }
    private sealed class Monster(int x, int y)
    {
        public float X = x, Y = y;
        public float FromX = x, FromY = y, ToX = x, ToY = y;
        public DateTime MoveAt = DateTime.UtcNow;
    }

    private readonly System.Windows.Forms.Timer timer = new() { Interval = 30 };
    private readonly HashSet<Keys> held = [];
    private readonly List<Bomb> bombs = [];
    private readonly List<Monster> monsters = [];
    private readonly HashSet<(int x, int y)> flames = [];
    private Cell[,] map = new Cell[0, 0];
    private Image? hero, bombArt;
    private Image? floorTile, steelTile, brickTile;
    private int level = 1, lives = 3, bombCharges = 2, cols, rows;
    private float px = 1, py = 1;
    private bool glove, paused, gameOver, victory;
    private DateTime chargeAt = DateTime.UtcNow, flameUntil;
    private DateTime invulnerableUntil;
    private string banner = "";
    private DateTime bannerUntil;
    private const int HudHeight = 82;

    public GameForm()
    {
        Text = "Bomber Man — Robot del Laberinto";
        BackColor = Color.FromArgb(9, 14, 25);
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        DoubleBuffered = true;
        Cursor = Cursors.Default;
        try
        {
            var baseDir = AppContext.BaseDirectory;
            hero = Image.FromFile(Path.Combine(baseDir, "Assets", "robot.png"));
            bombArt = Image.FromFile(Path.Combine(baseDir, "Assets", "bomb.jpg"));
            floorTile = Image.FromFile(Path.Combine(baseDir, "Assets", "floor.png"));
            steelTile = Image.FromFile(Path.Combine(baseDir, "Assets", "steel.png"));
            brickTile = Image.FromFile(Path.Combine(baseDir, "Assets", "brick.png"));
        }
        catch { }
        KeyDown += OnKeyDown;
        KeyUp += (_, e) => held.Remove(e.KeyCode);
        timer.Tick += (_, _) => { UpdateGame(); Invalidate(); };
        timer.Start();
        StartLevel(1);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); return; }
        if (e.KeyCode == Keys.P && !gameOver && !victory) { paused = !paused; return; }
        if (gameOver || victory)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.R) Restart();
            if (e.KeyCode == Keys.X) Close();
            return;
        }
        if (e.KeyCode == Keys.Space && !paused) { PlaceBomb(); e.Handled = true; return; }
        held.Add(e.KeyCode);
    }

    private void Restart() { lives = 3; glove = false; gameOver = victory = paused = false; StartLevel(1); }

    private void StartLevel(int n)
    {
        level = n;
        int size = 18 + (n - 1) * 2;
        cols = rows = size;
        map = new Cell[rows, cols];
        bombs.Clear(); monsters.Clear(); flames.Clear();
        px = py = 1; bombCharges = 2; chargeAt = DateTime.UtcNow;
        var rng = new Random(215 + n * 89);
        string[] firstMap = [
            "SSSSSSSSSSSSSSSSSS", "SBB...B..BB....BBS", "SBS.S.SS.SBS.SBSBS", "S..BB..B..BB.....S",
            "S.S.S.S.SB.S.SBS.S", "SBB.BBB..B..BB..BS", "S.S.S.SBS.SB.S.S.S", "S..BB..B..B..BB..S",
            "SBS.S.S.SS.S.S.SBS", "SB...BB.BB.BB...BS", "S.SBS.SBS.SS.S.S.S", "S..BB..B..B..BB..S",
            "SBS.S.S.SB.S.S.SBS", "S...BBB....BB....S", "S.SBS.S.SS.S.SBS.S", "SBB...BB.BB...BBBS",
            "SB..S.S..S.S.S..BS", "SSSSSSSSSSSSSSSSSS"];
        if (n == 1)
        {
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++) map[y, x] = firstMap[y][x] switch { 'S' => Cell.Steel, 'B' => Cell.Brick, _ => Cell.Floor };
        }
        else for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
        {
            if (x == 0 || y == 0 || x == cols - 1 || y == rows - 1 || (x % 2 == 0 && y % 2 == 0)) map[y, x] = Cell.Steel;
            else if (x < 4 && y < 4) map[y, x] = Cell.Floor;
            else map[y, x] = rng.NextDouble() < .47 ? Cell.Brick : Cell.Floor;
        }
        map[rows - 2, cols - 2] = Cell.Floor;
        map[1, 1] = map[1, 2] = map[2, 1] = map[1, 3] = map[3, 1] = Cell.Floor;
        // Keep a route through each board so every level can be completed.
        for (int x = 1; x < cols - 1; x++) map[rows - 2, x] = Cell.Floor;
        for (int y = 1; y < rows - 1; y++) map[y, cols - 2] = Cell.Floor;
        if (n == 1) PlaceHiddenPowerups(rng);
        int count = 2 + level * 2;
        for (int i = 0; i < count; i++)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                int x = rng.Next(3, cols - 2), y = rng.Next(3, rows - 2);
                if (map[y, x] == Cell.Floor && (Math.Abs(x - 1) + Math.Abs(y - 1)) > 6 && monsters.All(m => (int)m.X != x || (int)m.Y != y)) { monsters.Add(new Monster(x, y)); break; }
            }
        }
        banner = $"NIVEL {level}"; bannerUntil = DateTime.UtcNow.AddSeconds(1.5);
    }

    private void PlaceHiddenPowerups(Random rng)
    {
        var choices = new List<(int x, int y)>();
        for (int y = 2; y < rows - 2; y++) for (int x = 2; x < cols - 2; x++) if (map[y, x] == Cell.Brick) choices.Add((x, y));
        if (choices.Count == 0) return;
        var gloveSpot = choices[rng.Next(choices.Count)]; map[gloveSpot.y, gloveSpot.x] = Cell.HiddenGlove;
        choices.Remove(gloveSpot);
        if (choices.Count > 0) { var plusSpot = choices[rng.Next(choices.Count)]; map[plusSpot.y, plusSpot.x] = Cell.HiddenPlus; }
    }

    private void UpdateGame()
    {
        if (paused || gameOver || victory) return;
        var now = DateTime.UtcNow;
        if (now >= flameUntil) flames.Clear();
        if (bombCharges < 2 && (now - chargeAt).TotalSeconds >= 2.5) { bombCharges++; chargeAt = now; }
        float dx = 0, dy = 0;
        if (held.Contains(Keys.A)) dx--; if (held.Contains(Keys.D)) dx++;
        if (held.Contains(Keys.W)) dy--; if (held.Contains(Keys.S)) dy++;
        if (dx != 0 || dy != 0)
        {
            float len = MathF.Sqrt(dx * dx + dy * dy); dx /= len; dy /= len;
            float speed = 3.7f * timer.Interval / 1000f;
            TryMovePlayer(px + dx * speed, py);
            TryMovePlayer(px, py + dy * speed);
        }
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            var b = bombs[i];
            if (!b.Exploding && (now - b.Placed).TotalSeconds >= 2.5) Explode(b, now);
            if (b.Exploding && (now - b.ExplodedAt).TotalMilliseconds > 430) bombs.RemoveAt(i);
        }
        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (now < m.MoveAt) continue;
            m.X = m.ToX; m.Y = m.ToY;
            if (Math.Abs(px - m.X) < .48f && Math.Abs(py - m.Y) < .48f) { LoseLife(); if (gameOver) return; }
            var dirs = new[] { (1,0),(-1,0),(0,1),(0,-1) }.OrderBy(_ => Random.Shared.Next()).ToArray();
            foreach (var (ax, ay) in dirs)
            {
                int nx = (int)m.X + ax, ny = (int)m.Y + ay;
                if (CanMonsterEnter(nx, ny)) { m.FromX = m.X; m.FromY = m.Y; m.ToX = nx; m.ToY = ny; m.MoveAt = now.AddMilliseconds(340 + Random.Shared.Next(250)); break; }
            }
            float t = 1 - Math.Clamp((float)(m.MoveAt - now).TotalMilliseconds / 340f, 0, 1);
            m.X = m.FromX + (m.ToX - m.FromX) * t; m.Y = m.FromY + (m.ToY - m.FromY) * t;
        }
        foreach (var m in monsters) if (Math.Abs(px - m.X) < .45f && Math.Abs(py - m.Y) < .45f) { LoseLife(); if (gameOver) return; }
        if (monsters.Count == 0) { if (level < 3) StartLevel(level + 1); else victory = true; }
    }

    private bool CanMonsterEnter(int x, int y) => InBounds(x, y) && map[y, x] is Cell.Floor or Cell.Glove or Cell.Plus && !bombs.Any(b => b.X == x && b.Y == y) && !flames.Contains((x, y));
    private void TryMovePlayer(float x, float y)
    {
        if (!CanPlayerEnter((int)MathF.Round(x), (int)MathF.Round(y))) return;
        px = x; py = y;
        int cx = (int)MathF.Round(px), cy = (int)MathF.Round(py);
        if (map[cy, cx] == Cell.Glove) { glove = true; map[cy, cx] = Cell.Floor; ShowBanner("¡GUANTE! Ahora puedes lanzar bombas"); }
        if (map[cy, cx] == Cell.Plus) { bombCharges = Math.Min(2, bombCharges + 1); map[cy, cx] = Cell.Floor; ShowBanner("¡POTENCIADOR! Cargas de bomba recuperadas"); }
    }
    private bool CanPlayerEnter(int x, int y) => InBounds(x, y) && map[y, x] is Cell.Floor or Cell.Glove or Cell.Plus && !bombs.Any(b => b.X == x && b.Y == y);
    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < cols && y < rows;

    private void PlaceBomb()
    {
        if (bombCharges <= 0) { ShowBanner("Espera para recuperar una bomba"); return; }
        int x = (int)MathF.Round(px), y = (int)MathF.Round(py);
        if (bombs.Any(b => b.X == x && b.Y == y)) return;
        var bomb = new Bomb(x, y, DateTime.UtcNow); bombs.Add(bomb); bombCharges--;
        if (bombCharges == 1) chargeAt = DateTime.UtcNow;
        if (glove)
        {
            var key = held.FirstOrDefault(k => k is Keys.A or Keys.D or Keys.W or Keys.S);
            var (dx, dy) = key switch { Keys.A => (-1,0), Keys.D => (1,0), Keys.W => (0,-1), Keys.S => (0,1), _ => (0,0) };
            for (int step = 1; step <= 2 && (dx != 0 || dy != 0); step++)
            {
                int nx = x + dx * step, ny = y + dy * step;
                if (!InBounds(nx, ny) || map[ny, nx] is Cell.Steel or Cell.Brick or Cell.HiddenGlove or Cell.HiddenPlus || bombs.Any(b => b.X == nx && b.Y == ny)) break;
                bomb.X = nx; bomb.Y = ny;
            }
        }
    }

    private void Explode(Bomb b, DateTime now)
    {
        b.Exploding = true; b.ExplodedAt = now; flames.Clear();
        var affected = new List<(int x, int y)> { (b.X, b.Y) };
        foreach (var (dx, dy) in new[] { (1,0),(-1,0),(0,1),(0,-1) })
            for (int d = 1; d <= 1; d++)
            {
                int x = b.X + dx * d, y = b.Y + dy * d;
                if (!InBounds(x, y) || map[y, x] == Cell.Steel) break;
                affected.Add((x, y));
                if (map[y, x] is Cell.Brick or Cell.HiddenGlove or Cell.HiddenPlus)
                {
                    map[y, x] = map[y, x] switch { Cell.HiddenGlove => Cell.Glove, Cell.HiddenPlus => Cell.Plus, _ => Cell.Floor };
                    break;
                }
            }
        foreach (var cell in affected) flames.Add(cell);
        flameUntil = now.AddMilliseconds(430);
        monsters.RemoveAll(m => affected.Any(c => (int)MathF.Round(m.X) == c.x && (int)MathF.Round(m.Y) == c.y));
        if (affected.Any(c => c.x == (int)MathF.Round(px) && c.y == (int)MathF.Round(py))) LoseLife();
    }
    private void LoseLife()
    {
        if (DateTime.UtcNow < invulnerableUntil) return;
        lives--; invulnerableUntil = DateTime.UtcNow.AddSeconds(1.4); if (lives <= 0) { gameOver = true; return; }
        px = py = 1; bombs.RemoveAll(b => b.X == 1 && b.Y == 1); ShowBanner($"¡Cuidado! Vidas: {lives}");
    }
    private void ShowBanner(string text) { banner = text; bannerUntil = DateTime.UtcNow.AddSeconds(1.7); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.None; g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.FromArgb(9, 14, 25));
        DrawHud(g);
        int tile = Math.Max(16, Math.Min((ClientSize.Width - 80) / cols, (ClientSize.Height - HudHeight - 50) / rows));
        int boardW = tile * cols, boardH = tile * rows;
        int ox = (ClientSize.Width - boardW) / 2, oy = HudHeight + (ClientSize.Height - HudHeight - boardH) / 2;
        using var floor = new SolidBrush(Color.FromArgb(53, 125, 54));
        using var gridPen = new Pen(Color.FromArgb(35, 16, 41, 22));
        for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
        {
            int xx = ox + x * tile, yy = oy + y * tile;
            if (floorTile != null) g.DrawImage(floorTile, xx, yy, tile, tile); else g.FillRectangle(floor, xx, yy, tile, tile);
            switch (map[y, x])
            {
                case Cell.Steel: if (steelTile != null) g.DrawImage(steelTile, xx, yy, tile, tile); else DrawSteel(g, xx, yy, tile); break;
                case Cell.Brick: case Cell.HiddenGlove: case Cell.HiddenPlus: if (brickTile != null) g.DrawImage(brickTile, xx, yy, tile, tile); else DrawBrick(g, xx, yy, tile); break;
                case Cell.Glove: DrawPower(g, xx, yy, tile, "✊", Color.Gold); break;
                case Cell.Plus: DrawPower(g, xx, yy, tile, "+", Color.Cyan); break;
            }
            g.DrawRectangle(gridPen, xx, yy, tile, tile);
        }
        if (DateTime.UtcNow < flameUntil) foreach (var c in flames)
        {
            using var flame = new SolidBrush(Color.FromArgb(190, 255, 154, 26)); g.FillRectangle(flame, ox + c.x * tile + 3, oy + c.y * tile + 3, tile - 6, tile - 6);
        }
        foreach (var b in bombs)
        {
            int xx = ox + b.X * tile, yy = oy + b.Y * tile;
            if (b.Exploding && bombArt != null) { var rect = new Rectangle(xx + 2, yy + 2, tile - 4, tile - 4); g.DrawImage(bombArt, rect); }
            else DrawBomb(g, xx, yy, tile);
        }
        foreach (var m in monsters) DrawMonster(g, ox + m.X * tile, oy + m.Y * tile, tile);
        DrawHero(g, ox + px * tile, oy + py * tile, tile);
        if (DateTime.UtcNow < bannerUntil) DrawBanner(g, banner);
        if (paused) DrawOverlay(g, "PAUSA", "Pulsa P para continuar");
        if (gameOver) DrawOverlay(g, "FIN DEL JUEGO", "ENTER / R  Reiniciar       X  Salir");
        if (victory) DrawOverlay(g, "¡VICTORIA!", "Completaste los 3 niveles   ·   ENTER para jugar otra vez   ·   X salir");
    }

    private void DrawHud(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(17, 25, 42)); g.FillRectangle(b, 0, 0, Width, HudHeight);
        using var titleFont = new Font("Segoe UI", 20, FontStyle.Bold); using var infoFont = new Font("Segoe UI", 11, FontStyle.Bold);
        using var cyan = new SolidBrush(Color.FromArgb(79, 222, 237)); using var white = new SolidBrush(Color.White); using var muted = new SolidBrush(Color.FromArgb(176, 194, 211));
        g.DrawString("BOMBER  /  LABERINTO", titleFont, cyan, 28, 8);
        g.DrawString($"NIVEL  {level} / 3", infoFont, white, 31, 49);
        g.DrawString($"VIDAS  {new string('♥', lives)}", infoFont, new SolidBrush(Color.FromArgb(255, 92, 118)), 195, 49);
        g.DrawString($"BOMBAS  {bombCharges}/2", infoFont, white, 350, 49);
        g.DrawString(glove ? "GUANTE  EQUIPADO · W A S D + ESPACIO LANZA" : "W A S D  MOVER    ·    ESPACIO  BOMBA    ·    P  PAUSA", infoFont, muted, 535, 49);
    }
    private void DrawSteel(Graphics g, int x, int y, int s)
    {
        using var shadow = new SolidBrush(Color.FromArgb(18, 22, 34)); using var block = new SolidBrush(Color.FromArgb(95, 101, 119)); using var edge = new Pen(Color.FromArgb(174, 183, 198), Math.Max(1, s / 18f));
        g.FillRectangle(shadow, x + s * .08f, y + s * .08f, s * .84f, s * .84f); g.FillRectangle(block, x + s * .1f, y + s * .1f, s * .8f, s * .8f); g.DrawRectangle(edge, x + s * .12f, y + s * .12f, s * .76f, s * .76f);
        using var bolt = new SolidBrush(Color.FromArgb(216, 222, 229)); int r = Math.Max(2, s / 12); foreach (var (bx, by) in new[] { (.22f,.22f),(.78f,.22f),(.22f,.78f),(.78f,.78f) }) g.FillEllipse(bolt, x + s * bx - r / 2, y + s * by - r / 2, r, r);
    }
    private void DrawBrick(Graphics g, int x, int y, int s)
    {
        using var body = new SolidBrush(Color.FromArgb(205, 76, 30)); using var mortar = new Pen(Color.FromArgb(255, 166, 65), Math.Max(1, s / 18f)); using var hi = new Pen(Color.FromArgb(255, 119, 43), Math.Max(1, s / 9f));
        g.FillRectangle(body, x + 1, y + 1, s - 2, s - 2); g.DrawRectangle(hi, x + 2, y + 2, s - 5, s - 5);
        g.DrawLine(mortar, x + s / 2, y + 2, x + s / 2, y + s / 2); g.DrawLine(mortar, x + 2, y + s / 2, x + s - 2, y + s / 2); g.DrawLine(mortar, x + s / 4, y + s / 2, x + s / 4, y + s - 2); g.DrawLine(mortar, x + s * .75f, y + s / 2, x + s * .75f, y + s - 2);
    }
    private void DrawPower(Graphics g, int x, int y, int s, string icon, Color color)
    {
        using var b = new SolidBrush(color); using var f = new Font("Segoe UI", s * .52f, FontStyle.Bold); var textSize = g.MeasureString(icon, f); g.DrawString(icon, f, b, x + (s - textSize.Width) / 2, y + (s - textSize.Height) / 2);
    }
    private void DrawBomb(Graphics g, int x, int y, int s)
    {
        using var sh = new SolidBrush(Color.FromArgb(55, 7, 10, 15)); using var body = new SolidBrush(Color.FromArgb(20, 27, 38)); using var rim = new Pen(Color.FromArgb(35, 51, 72), Math.Max(2, s / 12)); using var fuse = new Pen(Color.Orange, Math.Max(2, s / 12));
        int d = (int)(s * .62f), bx = x + (s - d) / 2, by = y + (s - d) / 2; g.FillEllipse(sh, bx + 2, by + d * .1f, d, d); g.FillEllipse(body, bx, by, d, d); g.DrawEllipse(rim, bx, by, d, d); g.DrawLine(fuse, x + s / 2, by + 2, x + s * .64f, y + s * .13f);
        using var glow = new SolidBrush((DateTime.UtcNow.Millisecond / 180) % 2 == 0 ? Color.Gold : Color.OrangeRed); g.FillEllipse(glow, x + s * .59f, y + s * .1f, Math.Max(4, s * .13f), Math.Max(4, s * .13f));
    }
    private void DrawHero(Graphics g, float cx, float cy, int s)
    {
        int d = (int)(s * .86f); var r = new Rectangle((int)(cx - d / 2f), (int)(cy - d / 2f), d, d);
        if (hero != null)
        {
            if (DateTime.UtcNow < invulnerableUntil && DateTime.UtcNow.Millisecond / 100 % 2 == 0) return;
            g.DrawImage(hero, r);
        }
        else { using var b = new SolidBrush(Color.Cyan); g.FillEllipse(b, r); }
    }
    private void DrawMonster(Graphics g, float cx, float cy, int s)
    {
        int d = (int)(s * .66f); using var body = new SolidBrush(Color.FromArgb(213, 58, 85)); using var eye = new SolidBrush(Color.White); using var pupil = new SolidBrush(Color.FromArgb(31, 31, 44));
        g.FillEllipse(body, cx - d / 2f, cy - d / 2f, d, d); g.FillEllipse(eye, cx - d * .22f, cy - d * .17f, d * .17f, d * .22f); g.FillEllipse(eye, cx + d * .05f, cy - d * .17f, d * .17f, d * .22f); g.FillEllipse(pupil, cx - d * .18f, cy - d * .11f, d * .08f, d * .12f); g.FillEllipse(pupil, cx + d * .09f, cy - d * .11f, d * .08f, d * .12f);
    }
    private void DrawBanner(Graphics g, string text)
    {
        using var f = new Font("Segoe UI", 15, FontStyle.Bold); var size = g.MeasureString(text, f); float x = (Width - size.Width - 36) / 2, y = HudHeight + 14;
        using var b = new SolidBrush(Color.FromArgb(225, 17, 25, 42)); using var p = new Pen(Color.FromArgb(79, 222, 237)); g.FillRectangle(b, x, y, size.Width + 36, size.Height + 18); g.DrawRectangle(p, x, y, size.Width + 36, size.Height + 18); g.DrawString(text, f, Brushes.White, x + 18, y + 8);
    }
    private void DrawOverlay(Graphics g, string title, string subtitle)
    {
        using var veil = new SolidBrush(Color.FromArgb(205, 5, 9, 17)); g.FillRectangle(veil, ClientRectangle);
        using var f = new Font("Segoe UI", 38, FontStyle.Bold); using var sf = new Font("Segoe UI", 16, FontStyle.Regular); using var cyan = new SolidBrush(Color.FromArgb(79, 222, 237));
        var a = g.MeasureString(title, f); var b = g.MeasureString(subtitle, sf); g.DrawString(title, f, cyan, (Width - a.Width) / 2, Height * .41f); g.DrawString(subtitle, sf, Brushes.White, (Width - b.Width) / 2, Height * .51f);
    }
}
