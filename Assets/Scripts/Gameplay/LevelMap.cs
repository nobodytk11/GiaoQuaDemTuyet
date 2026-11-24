using System;
using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>
    /// A level drawn as text. One character = one tile (1 x 1 world unit). The top row of the text is the top of the world.
    /// Map rows are written between two bars like |..S..|, so empty rows are kept. Lines starting with ';' are comments.
    /// Lines starting with '@' are settings (see Directive).
    /// </summary>
    public class LevelMap
    {
        public struct Directive
        {
            public string Name;
            public string[] Args;
            public float Float(int i, float fallback = 0f) =>
                i < Args.Length && float.TryParse(Args[i], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        public const char Solid = '#', Ice = 'I', Roof = 'H', Wall = 'W';

        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Directive> Directives { get; } = new List<Directive>();
        string[] rows;

        public static LevelMap Parse(string text)
        {
            var map = new LevelMap();
            var rows = new List<string>();
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0 || line[0] == ';') continue;
                if (line[0] == '@')
                {
                    var parts = line.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;
                    var args = new string[parts.Length - 1];
                    Array.Copy(parts, 1, args, 0, args.Length);
                    map.Directives.Add(new Directive { Name = parts[0].ToLowerInvariant(), Args = args });
                    continue;
                }
                if (line[0] != '|') continue;                    // map rows are written between two bars: |....|
                string body = line.Substring(1);
                if (body.EndsWith("|")) body = body.Substring(0, body.Length - 1);
                rows.Add(body);
            }
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            for (int i = 0; i < rows.Count; i++) rows[i] = rows[i].PadRight(w, ' ');
            map.rows = rows.ToArray();
            map.Width = w;
            map.Height = rows.Count;
            return map;
        }

        public char At(int col, int row)
        {
            if (row < 0 || row >= Height || col < 0 || col >= Width) return ' ';
            return rows[row][col];
        }

        public bool IsSolid(int col, int row)
        {
            char c = At(col, row);
            return c == Solid || c == Ice || c == Roof || c == Wall;
        }

        public IEnumerable<(int col, int row, char ch)> Cells()
        {
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    if (rows[r][c] != ' ') yield return (c, r, rows[r][c]);
        }

        public bool Find(char ch, out int col, out int row)
        {
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    if (rows[r][c] == ch) { col = c; row = r; return true; }
            col = row = -1;
            return false;
        }

        public int Count(char ch)
        {
            int n = 0;
            foreach (var c in Cells()) if (c.ch == ch) n++;
            return n;
        }

        // ---- world coordinates ----
        public Vector2 Center(int col, int row) => new Vector2(col + 0.5f, Height - 1 - row + 0.5f);
        public Vector2 Bottom(int col, int row) => new Vector2(col + 0.5f, Height - 1 - row);
        public Vector2 CornerToWorld(float col, float row) => new Vector2(col, Height - row);   // upper-left corner of a cell

        public Directive? Get(string name)
        {
            foreach (var d in Directives) if (d.Name == name) return d;
            return null;
        }

        /// <summary>Axis-aligned rectangles that together cover every tile of the given kinds (fewer, bigger colliders).</summary>
        public List<RectInt> MergedRects(params char[] kinds)
        {
            var result = new List<RectInt>();
            var open = new Dictionary<(int x, int w), int>();      // (start col, width) -> index in result of the rect growing downward
            for (int r = 0; r < Height; r++)
            {
                var runs = new List<(int x, int w)>();
                int c = 0;
                while (c < Width)
                {
                    if (Array.IndexOf(kinds, At(c, r)) < 0) { c++; continue; }
                    int start = c;
                    while (c < Width && Array.IndexOf(kinds, At(c, r)) >= 0) c++;
                    runs.Add((start, c - start));
                }
                var next = new Dictionary<(int x, int w), int>();
                foreach (var run in runs)
                {
                    if (open.TryGetValue(run, out int idx))
                    {
                        var rect = result[idx];
                        result[idx] = new RectInt(rect.x, rect.y, rect.width, rect.height + 1);
                        next[run] = idx;
                    }
                    else
                    {
                        result.Add(new RectInt(run.x, r, run.w, 1));
                        next[run] = result.Count - 1;
                    }
                }
                open = next;
            }
            return result;
        }
    }

    public struct LevelIssue
    {
        public bool IsError;
        public string Message;
        public override string ToString() => (IsError ? "LỖI: " : "Cảnh báo: ") + Message;
    }

    /// <summary>Checks a level map against the rules the hero's physics allow (see LevelBudget).</summary>
    public static class LevelValidator
    {
        // Numbers every level must respect so that every hero skin can finish it (tiles).
        public const int MaxGapTiles = 3;
        public const int MaxStepUpTiles = 1;

        public static List<LevelIssue> Validate(LevelMap map)
        {
            var issues = new List<LevelIssue>();
            void Err(string m) => issues.Add(new LevelIssue { IsError = true, Message = m });
            void Warn(string m) => issues.Add(new LevelIssue { IsError = false, Message = m });

            if (map.Width < 20 || map.Height < 6) Err($"Màn quá nhỏ ({map.Width}x{map.Height}).");
            if (map.Count('S') != 1) Err($"Cần đúng 1 điểm xuất phát 'S' (đang có {map.Count('S')}).");
            if (map.Count('C') != 1) Err($"Cần đúng 1 ống khói 'C' (đang có {map.Count('C')}).");
            if (map.Count('L') != 1) Err($"Cần đúng 1 ván bập bênh 'L' (đang có {map.Count('L')}).");
            if (map.Count('c') < 1) Warn("Chưa có điểm lưu 'c'.");
            if (map.Count('o') < 8) Warn("Ít xu (dưới 8).");

            if (map.Find('S', out int sc, out int sr) && !map.IsSolid(sc, sr + 1)) Err("Điểm xuất phát 'S' không đứng trên nền.");
            if (map.Find('L', out int lc, out int lr) && !map.IsSolid(lc, lr + 1)) Err("Ván bập bênh 'L' không đứng trên nền.");
            if (map.Find('C', out int cc, out int cr) && !map.IsSolid(cc, cr + 1)) Err("Ống khói 'C' không đứng trên mái 'H'.");

            // The walkable ground of each column: the top of the stack of '#'/'I' tiles that starts at the bottom row.
            // -1 means a pit. Floating platforms and the house are not part of it.
            var surface = new int[map.Width];
            for (int c = 0; c < map.Width; c++)
            {
                int r = map.Height - 1;
                surface[c] = -1;
                while (r >= 0 && (map.At(c, r) == LevelMap.Solid || map.At(c, r) == LevelMap.Ice)) { surface[c] = r; r--; }
            }
            // columns that count as "something to stand on" even though the ground is missing:
            // a moving platform covers the columns it sweeps, other platforms cover their own column.
            bool Platform(int c)
            {
                for (int cc = Math.Max(0, c - 4); cc <= Math.Min(map.Width - 1, c + 4); cc++)
                    for (int r = 0; r < map.Height; r++)
                    {
                        char ch = map.At(cc, r);
                        int d = Math.Abs(cc - c);
                        if (ch == 'm' && d <= 4) return true;
                        if ((ch == 'v' || ch == 't') && d <= 1) return true;
                        if ((ch == '?' || ch == '!' || ch == 'H') && d == 0) return true;
                    }
                return false;
            }

            int gap = 0;
            bool reported = false;
            for (int c = 0; c < map.Width; c++)
            {
                bool hole = surface[c] < 0 && !Platform(c);
                if (hole)
                {
                    gap++;
                    if (gap > MaxGapTiles && !reported) { Err($"Hố rộng hơn {MaxGapTiles} ô quanh cột {c}."); reported = true; }
                }
                else { gap = 0; reported = false; }
                if (c > 0 && surface[c] >= 0 && surface[c - 1] >= 0)
                {
                    int up = surface[c - 1] - surface[c];          // positive = this column is higher than the one before
                    if (up > MaxStepUpTiles) Err($"Bậc cao {up} ô tại cột {c} (tối đa {MaxStepUpTiles}).");
                }
            }
            return issues;
        }
    }
}
