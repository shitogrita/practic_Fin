using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TransportAllVariants
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int I;
        public readonly int J;
        public Cell(int i, int j) { I = i; J = j; }
        public bool Equals(Cell other) => I == other.I && J == other.J;
        public override bool Equals(object? obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(I, J);
        public override string ToString() => $"({I},{J})";
    }

    /// <summary>DSU для предотвращения циклов при добавлении нулевых базисных клеток (устранение вырождения).</summary>
    public sealed class Dsu
    {
        private readonly int[] p;
        private readonly int[] r;
        public Dsu(int n)
        {
            p = new int[n];
            r = new int[n];
            for (int i = 0; i < n; i++) p[i] = i;
        }
        public int Find(int x)
        {
            while (p[x] != x) { p[x] = p[p[x]]; x = p[x]; }
            return x;
        }
        public bool Union(int a, int b)
        {
            a = Find(a); b = Find(b);
            if (a == b) return false;
            if (r[a] < r[b]) (a, b) = (b, a);
            p[b] = a;
            if (r[a] == r[b]) r[a]++;
            return true;
        }
    }

    public sealed class Plan
    {
        public double[,] X { get; }
        public HashSet<Cell> Basis { get; }
        public Plan(int m, int n)
        {
            X = new double[m, n];
            Basis = new HashSet<Cell>();
        }
        public Plan Clone()
        {
            int m = X.GetLength(0), n = X.GetLength(1);
            var p = new Plan(m, n);
            Array.Copy(X, p.X, X.Length);
            foreach (var c in Basis) p.Basis.Add(c);
            return p;
        }
    }

    public sealed class TransportationSolver
    {
        private const double EPS = 1e-9;

        public int M { get; }
        public int N { get; }
        public double[,] C { get; }
        public double[] Supply { get; }
        public double[] Demand { get; }

        public TransportationSolver(double[,] costs, double[] supply, double[] demand)
        {
            C = (double[,])costs.Clone();
            Supply = (double[])supply.Clone();
            Demand = (double[])demand.Clone();
            M = Supply.Length;
            N = Demand.Length;

            double sumS = Supply.Sum();
            double sumD = Demand.Sum();
            if (Math.Abs(sumS - sumD) > EPS)
                throw new ArgumentException("Задача должна быть сбалансирована (в данной таблице варианты сбалансированы).");
        }

        public double Cost(Plan p)
        {
            double s = 0;
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    s += p.X[i, j] * C[i, j];
            return s;
        }

        public Plan NorthwestCorner()
        {
            var p = new Plan(M, N);
            var a = (double[])Supply.Clone();
            var b = (double[])Demand.Clone();

            int i = 0, j = 0;
            while (i < M && j < N)
            {
                double q = Math.Min(a[i], b[j]);
                p.X[i, j] = q;
                p.Basis.Add(new Cell(i, j));
                a[i] -= q;
                b[j] -= q;

                bool rowDone = Math.Abs(a[i]) < EPS;
                bool colDone = Math.Abs(b[j]) < EPS;

                if (rowDone) i++;
                if (colDone) j++;
            }

            EnsureNonDegenerate(p);
            return p;
        }

        public Plan LeastCost()
        {
            var p = new Plan(M, N);
            var a = (double[])Supply.Clone();
            var b = (double[])Demand.Clone();

            var rowAlive = Enumerable.Repeat(true, M).ToArray();
            var colAlive = Enumerable.Repeat(true, N).ToArray();

            int remainingRows = M, remainingCols = N;

            while (remainingRows > 0 && remainingCols > 0)
            {
                double bestCost = double.PositiveInfinity;
                int bi = -1, bj = -1;

                // выбор клетки минимальной стоимости среди ещё активных строк/столбцов
                for (int i = 0; i < M; i++)
                {
                    if (!rowAlive[i] || a[i] < EPS) continue;
                    for (int j = 0; j < N; j++)
                    {
                        if (!colAlive[j] || b[j] < EPS) continue;
                        double c = C[i, j];
                        if (c < bestCost - EPS)
                        {
                            bestCost = c;
                            bi = i; bj = j;
                        }
                        // при равенстве стоимости можно дополнительно выбирать клетку с большим возможным объёмом
                        else if (Math.Abs(c - bestCost) < EPS)
                        {
                            double capOld = Math.Min(a[bi], b[bj]);
                            double capNew = Math.Min(a[i], b[j]);
                            if (capNew > capOld + EPS)
                            {
                                bi = i; bj = j;
                            }
                        }
                    }
                }

                if (bi < 0 || bj < 0) break;

                double q = Math.Min(a[bi], b[bj]);
                p.X[bi, bj] += q;
                p.Basis.Add(new Cell(bi, bj));

                a[bi] -= q;
                b[bj] -= q;

                if (a[bi] < EPS && rowAlive[bi]) { rowAlive[bi] = false; remainingRows--; }
                if (b[bj] < EPS && colAlive[bj]) { colAlive[bj] = false; remainingCols--; }
            }

            EnsureNonDegenerate(p);
            return p;
        }

        /// <summary>Добор базиса до m+n-1 нулевыми перевозками без образования циклов в двудольном графе.</summary>
        private void EnsureNonDegenerate(Plan p)
        {
            int target = M + N - 1;
            if (p.Basis.Count >= target) return;

            // DSU по вершинам: 0..M-1 (строки), M..M+N-1 (столбцы)
            var dsu = new Dsu(M + N);
            foreach (var cell in p.Basis)
                dsu.Union(cell.I, M + cell.J);

            // кандидаты по возрастанию стоимости
            var candidates = new List<Cell>();
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                {
                    var c = new Cell(i, j);
                    if (!p.Basis.Contains(c)) candidates.Add(c);
                }

            candidates.Sort((x, y) => C[x.I, x.J].CompareTo(C[y.I, y.J]));

            foreach (var c in candidates)
            {
                if (p.Basis.Count >= target) break;
                int r = c.I;
                int colNode = M + c.J;

                // если уже связаны, добавление ребра создаст цикл
                if (dsu.Find(r) == dsu.Find(colNode)) continue;

                p.Basis.Add(c);
                // p.X[r, c.J] = 0; // можно не присваивать явно
                dsu.Union(r, colNode);
            }

            // страховка (в учебных задачах почти не нужна)
            while (p.Basis.Count < target)
            {
                for (int i = 0; i < M && p.Basis.Count < target; i++)
                    for (int j = 0; j < N && p.Basis.Count < target; j++)
                    {
                        var c = new Cell(i, j);
                        if (!p.Basis.Contains(c))
                        {
                            p.Basis.Add(c);
                            break;
                        }
                    }
            }
        }

        public Plan SolveToOptimalByModi(Plan initial)
        {
            var p = initial.Clone();

            while (true)
            {
                ComputePotentials(p, out double[] u, out double[] v);

                // поиск входящей клетки (наиболее отрицательный reduced cost)
                double bestNeg = 0.0;
                Cell entering = new Cell(-1, -1);

                for (int i = 0; i < M; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        var cell = new Cell(i, j);
                        if (p.Basis.Contains(cell)) continue;
                        double r = C[i, j] - (u[i] + v[j]);
                        if (r < bestNeg - 1e-9)
                        {
                            bestNeg = r;
                            entering = cell;
                        }
                    }
                }

                // условие оптимальности выполнено
                if (entering.I < 0) return p;

                var loop = FindLoop(p.Basis, entering);
                if (loop == null || loop.Count < 4)
                    throw new InvalidOperationException("Не удалось построить замкнутый контур. Проверьте корректность базиса.");

                // loop: [entering, ..., entering], знаки: 0:+, 1:-, 2:+, 3:- ...
                double theta = double.PositiveInfinity;
                Cell leaving = new Cell(-1, -1);

                for (int k = 1; k < loop.Count - 1; k += 2)
                {
                    var c = loop[k];
                    double val = p.X[c.I, c.J];
                    if (val < theta - 1e-9)
                    {
                        theta = val;
                        leaving = c;
                    }
                }

                if (double.IsPositiveInfinity(theta))
                    throw new InvalidOperationException("Контур некорректен: отсутствуют минусовые клетки.");

                // перераспределение
                for (int k = 0; k < loop.Count - 1; k++)
                {
                    var c = loop[k];
                    if (k % 2 == 0) p.X[c.I, c.J] += theta;
                    else p.X[c.I, c.J] -= theta;
                }

                // обновление базиса
                p.Basis.Add(entering);
                p.Basis.Remove(leaving);

                // устранение возможного вырождения
                EnsureNonDegenerate(p);
            }
        }

        private void ComputePotentials(Plan p, out double[] u, out double[] v)
        {
            u = Enumerable.Repeat(double.NaN, M).ToArray();
            v = Enumerable.Repeat(double.NaN, N).ToArray();

            // вычисление потенциалов по компонентам связности (вырождение => несколько компонент)
            for (int startRow = 0; startRow < M; startRow++)
            {
                if (!double.IsNaN(u[startRow])) continue;
                if (!p.Basis.Any(c => c.I == startRow)) continue;

                u[startRow] = 0.0;

                bool changed;
                do
                {
                    changed = false;
                    foreach (var cell in p.Basis)
                    {
                        int i = cell.I, j = cell.J;
                        if (!double.IsNaN(u[i]) && double.IsNaN(v[j]))
                        {
                            v[j] = C[i, j] - u[i];
                            changed = true;
                        }
                        else if (double.IsNaN(u[i]) && !double.IsNaN(v[j]))
                        {
                            u[i] = C[i, j] - v[j];
                            changed = true;
                        }
                    }
                } while (changed);
            }

            // страховка от NaN
            for (int i = 0; i < M; i++) if (double.IsNaN(u[i])) u[i] = 0.0;
            for (int j = 0; j < N; j++) if (double.IsNaN(v[j])) v[j] = 0.0;
        }

        /// <summary>Поиск цикла (замкнутого контура) для entering при множестве базисных клеток.</summary>
        private List<Cell>? FindLoop(HashSet<Cell> basis, Cell start)
        {
            var allowed = new HashSet<Cell>(basis) { start };

            var rowToCols = new List<int>[M];
            var colToRows = new List<int>[N];
            for (int i = 0; i < M; i++) rowToCols[i] = new List<int>();
            for (int j = 0; j < N; j++) colToRows[j] = new List<int>();

            foreach (var c in allowed)
            {
                rowToCols[c.I].Add(c.J);
                colToRows[c.J].Add(c.I);
            }

            // пробуем стартовать ходом по строке, затем по столбцу
            var path = new List<Cell> { start };
            if (Dfs(start, start, moveAlongRow: true, rowToCols, colToRows, allowed, path)) return path;

            path = new List<Cell> { start };
            if (Dfs(start, start, moveAlongRow: false, rowToCols, colToRows, allowed, path)) return path;

            return null;
        }

        private bool Dfs(
            Cell current,
            Cell start,
            bool moveAlongRow,
            List<int>[] rowToCols,
            List<int>[] colToRows,
            HashSet<Cell> allowed,
            List<Cell> path)
        {
            if (moveAlongRow)
            {
                foreach (int j2 in rowToCols[current.I])
                {
                    if (j2 == current.J) continue;
                    var next = new Cell(current.I, j2);
                    if (!allowed.Contains(next)) continue;

                    if (next.Equals(start) && path.Count >= 4)
                    {
                        path.Add(start);
                        return true;
                    }
                    if (path.Contains(next)) continue;

                    path.Add(next);
                    if (Dfs(next, start, !moveAlongRow, rowToCols, colToRows, allowed, path)) return true;
                    path.RemoveAt(path.Count - 1);
                }
            }
            else
            {
                foreach (int i2 in colToRows[current.J])
                {
                    if (i2 == current.I) continue;
                    var next = new Cell(i2, current.J);
                    if (!allowed.Contains(next)) continue;

                    if (next.Equals(start) && path.Count >= 4)
                    {
                        path.Add(start);
                        return true;
                    }
                    if (path.Contains(next)) continue;

                    path.Add(next);
                    if (Dfs(next, start, !moveAlongRow, rowToCols, colToRows, allowed, path)) return true;
                    path.RemoveAt(path.Count - 1);
                }
            }
            return false;
        }
    }

    public sealed class VariantData
    {
        public int Id { get; }
        public double[] Supply { get; }
        public double[] Demand { get; }
        public double[,] Costs { get; }

        public VariantData(int id, double[] supply, double[] demand, double[,] costs)
        {
            Id = id;
            Supply = supply;
            Demand = demand;
            Costs = costs;
        }
    }

    internal static class Program
    {
        private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;
        private const double EPS = 1e-9;

        private static string Fmt(double x)
        {
            // для отчёта: если близко к целому — печатаем как целое
            double r = Math.Round(x);
            if (Math.Abs(x - r) < 1e-9) return ((long)r).ToString(Ci);
            return x.ToString("0.###", Ci);
        }

        private static string MdTable(string[] colNames, string[] rowNames, double[,] values)
        {
            int m = rowNames.Length;
            int n = colNames.Length;

            var sb = new StringBuilder();
            sb.Append("| | ");
            sb.Append(string.Join(" | ", colNames));
            sb.AppendLine(" |");
            sb.Append("|---|");
            sb.Append(string.Join("|", Enumerable.Repeat("---", n)));
            sb.AppendLine("|");

            for (int i = 0; i < m; i++)
            {
                sb.Append("| ");
                sb.Append(rowNames[i]);
                sb.Append(" | ");
                for (int j = 0; j < n; j++)
                {
                    sb.Append(Fmt(values[i, j]));
                    sb.Append(j == n - 1 ? " |\n" : " | ");
                }
            }

            return sb.ToString();
        }

        private static string MdCostMatrix(string[] colNames, string[] rowNames, double[,] costs)
            => MdTable(colNames, rowNames, costs);

        private static string MdPlan(string[] colNames, string[] rowNames, Plan p)
            => MdTable(colNames, rowNames, p.X);

        private static void Main()
        {
            // Базовые (уникальные) наборы данных из таблицы
            var set1 = (supply: new double[] { 200, 350, 300 },
                        demand: new double[] { 270, 130, 190, 150, 110 },
                        C: new double[,]
                        {
                            { 24, 50, 55, 27, 16 },
                            { 50, 47, 23, 17, 21 },
                            { 35, 59, 55, 27, 41 }
                        });

            var set2 = (supply: new double[] { 200, 300, 250 },
                        demand: new double[] { 210, 150, 120, 135, 135 },
                        C: new double[,]
                        {
                            { 20, 10, 13, 13, 18 },
                            { 27, 19, 20, 16, 22 },
                            { 26, 17, 19, 21, 23 }
                        });

            var set3 = (supply: new double[] { 230, 250, 170 },
                        demand: new double[] { 140, 90, 160, 110, 150 },
                        C: new double[,]
                        {
                            { 40, 19, 25, 25, 35 },
                            { 49, 26, 27, 18, 38 },
                            { 46, 27, 36, 40, 45 }
                        });

            var set4 = (supply: new double[] { 350, 200, 300 },
                        demand: new double[] { 170, 140, 200, 195, 145 },
                        C: new double[,]
                        {
                            { 22, 14, 16, 28, 30 },
                            { 19, 17, 26, 36, 36 },
                            { 37, 30, 31, 39, 41 }
                        });

            var set5 = (supply: new double[] { 300, 250, 200 },
                        demand: new double[] { 210, 150, 120, 135, 135 },
                        C: new double[,]
                        {
                            {  4,  8, 13,  2,  7 },
                            {  9,  4, 11,  9, 17 },
                            {  3, 16, 10,  1,  4 }
                        });

            // Соответствие вариантов (1–22) наборам данных согласно изображению
            var map = new Dictionary<int, (double[] s, double[] d, double[,] c)>
            {
                {  1, set1 }, {  2, set2 }, {  3, set3 }, {  4, set4 }, {  5, set5 },
                {  6, set1 }, {  7, set2 }, {  8, set3 }, {  9, set5 }, { 10, set4 },
                { 11, set1 }, { 12, set3 }, { 13, set1 }, { 14, set2 }, { 15, set3 },
                { 16, set4 }, { 17, set5 }, { 18, set1 }, { 19, set4 }, { 20, set3 },
                { 21, set2 }, { 22, set5 }
            };

            string[] rows = { "A1", "A2", "A3" };
            string[] cols = { "B1", "B2", "B3", "B4", "B5" };

            var md = new StringBuilder();
            md.AppendLine("# Решение транспортной задачи для вариантов 1–22");
            md.AppendLine();
            md.AppendLine("Построение опорных планов: метод северо-западного угла и метод минимальных элементов; оптимизация: метод потенциалов (MODI).");
            md.AppendLine();

            for (int variant = 1; variant <= 22; variant++)
            {
                var data = map[variant];

                var solver = new TransportationSolver(data.c, data.s, data.d);

                var nw = solver.NorthwestCorner();
                var lc = solver.LeastCost();

                double fnw = solver.Cost(nw);
                double flc = solver.Cost(lc);

                var best = (flc <= fnw) ? lc : nw;
                var opt = solver.SolveToOptimalByModi(best);
                double fopt = solver.Cost(opt);

                md.AppendLine($"## Вариант {variant}");
                md.AppendLine();
                md.AppendLine("### Исходные данные");
                md.AppendLine($"- Запасы поставщиков: $a_1={Fmt(data.s[0])}$, $a_2={Fmt(data.s[1])}$, $a_3={Fmt(data.s[2])}$.");
                md.AppendLine($"- Потребности потребителей: $b_1={Fmt(data.d[0])}$, $b_2={Fmt(data.d[1])}$, $b_3={Fmt(data.d[2])}$, $b_4={Fmt(data.d[3])}$, $b_5={Fmt(data.d[4])}$.");
                md.AppendLine();
                md.AppendLine("Матрица удельных затрат (стоимостей) $C=(c_{ij})$:");
                md.AppendLine();
                md.AppendLine(MdCostMatrix(cols, rows, data.c));
                md.AppendLine();

                md.AppendLine("### Опорный план методом северо-западного угла");
                md.AppendLine(MdPlan(cols, rows, nw));
                md.AppendLine();
                md.AppendLine($"**Суммарные транспортные издержки:**  \n\\(F = {Fmt(fnw)}\\).");
                md.AppendLine();

                md.AppendLine("### Опорный план методом минимальных элементов");
                md.AppendLine(MdPlan(cols, rows, lc));
                md.AppendLine();
                md.AppendLine($"**Суммарные транспортные издержки:**  \n\\(F = {Fmt(flc)}\\).");
                md.AppendLine();

                md.AppendLine("### Выбор наилучшего опорного плана");
                md.AppendLine($"В качестве исходного для оптимизации выбран план, полученный методом **{(flc <= fnw ? "минимальных элементов" : "северо-западного угла")}**, как обеспечивающий меньшую величину целевой функции $F$.");
                md.AppendLine();

                md.AppendLine("### Оптимальный план (метод потенциалов / MODI)");
                md.AppendLine(MdPlan(cols, rows, opt));
                md.AppendLine();
                md.AppendLine($"**Суммарные транспортные издержки:**  \n\\(F = {Fmt(fopt)}\\).");
                md.AppendLine();
            }

            string outPath = "transport_variants_1-22_solution_csharp.md";
            File.WriteAllText(outPath, md.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Console.WriteLine("Markdown-отчёт сформирован: " + Path.GetFullPath(outPath));
        }
    }
}
