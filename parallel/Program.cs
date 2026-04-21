using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class SirParallel
{
    const int N      = 1000;
    const int DAYS   = 365;
    const double BETA  = 0.3;
    const double GAMMA = 0.1;
    const double MU    = 0.01;

    const byte S = 0, I = 1, R = 2, D = 3;

    static ThreadLocal<Random> tlRng = new ThreadLocal<Random>(
        () => new Random(42 + Environment.CurrentManagedThreadId * 1000)
    );

    static void Main(string[] args)
    {
        int numThreads = args.Length > 0 ? int.Parse(args[0]) : Environment.ProcessorCount;
        Console.WriteLine($"Corriendo con {numThreads} threads...");

        var options = new ParallelOptions { MaxDegreeOfParallelism = numThreads };

        byte[] gridA = new byte[N * N];
        byte[] gridB = new byte[N * N];
        byte[][] grid = { gridA, gridB };

        grid[0][N / 2 * N + N / 2] = I;

        Directory.CreateDirectory("snapshots_par");
        using var csv = new StreamWriter("stats_par.csv");
        csv.WriteLine("dia,S,I,R,D");

        var sw = Stopwatch.StartNew();

        for (int day = 0; day < DAYS; day++)
        {
            int cur = day % 2;
            int nxt = 1 - cur;
            long totalS = 0, totalI = 0, totalR = 0, totalD = 0;
            object lockObj = new object();

            Parallel.For(0, N, options,
                () => (0L, 0L, 0L, 0L),
                (i, state, local) =>
                {
                    var (lS, lI, lR, lD) = local;
                    Random rng = tlRng.Value!;

                    for (int j = 0; j < N; j++)
                    {
                        byte s = grid[cur][i * N + j];

                        switch (s)
                        {
                            case D:
                                grid[nxt][i*N+j] = D; lD++; break;

                            case R:
                                grid[nxt][i*N+j] = R; lR++; break;

                            case S:
                            {
                                int inf = 0;
                                if (i > 0   && grid[cur][(i-1)*N+j] == I) inf++;
                                if (i < N-1 && grid[cur][(i+1)*N+j] == I) inf++;
                                if (j > 0   && grid[cur][i*N+(j-1)] == I) inf++;
                                if (j < N-1 && grid[cur][i*N+(j+1)] == I) inf++;

                                double pInfect = 1.0 - Math.Pow(1.0 - BETA, inf);

                                if (rng.NextDouble() < pInfect)
                                { grid[nxt][i*N+j] = I; lI++; }
                                else
                                { grid[nxt][i*N+j] = S; lS++; }
                                break;
                            }

                            case I:
                            {
                                double r = rng.NextDouble();
                                if      (r < MU)           { grid[nxt][i*N+j] = D; lD++; }
                                else if (r < MU + GAMMA)   { grid[nxt][i*N+j] = R; lR++; }
                                else                       { grid[nxt][i*N+j] = I; lI++; }
                                break;
                            }
                        }
                    }
                    return (lS, lI, lR, lD);
                },
                local =>
                {
                    lock (lockObj)
                    {
                        totalS += local.Item1;
                        totalI += local.Item2;
                        totalR += local.Item3;
                        totalD += local.Item4;
                    }
                }
            );

            if (day % 7 == 0)
                File.WriteAllBytes($"snapshots_par/day_{day:D3}.bin", grid[cur]);

            csv.WriteLine($"{day},{totalS},{totalI},{totalR},{totalD}");

            if (day % 30 == 0)
                Console.WriteLine($"Día {day,3} | S={totalS,7} I={totalI,7} R={totalR,7} D={totalD,7}");
        }

        sw.Stop();
        Console.WriteLine($"\nTiempo con {numThreads} threads: {sw.Elapsed.TotalSeconds:F3} s");
        Console.WriteLine("Presiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}