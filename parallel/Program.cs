using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class SirParallelGhost
{
    const int    N     = 1000;
    const int    DAYS  = 365;
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
        Console.WriteLine($"Corriendo con {numThreads} threads (ghost-cells)...");

        byte[] gridA = new byte[N * N];
        byte[] gridB = new byte[N * N];
        byte[][] grid = { gridA, gridB };

        grid[0][N / 2 * N + N / 2] = I;

        int[] bandStart = new int[numThreads];
        int[] bandEnd   = new int[numThreads];
        int rowsPerThread = N / numThreads;
        for (int t = 0; t < numThreads; t++)
        {
            bandStart[t] = t * rowsPerThread;
            bandEnd[t]   = (t == numThreads - 1) ? N : bandStart[t] + rowsPerThread;
        }

        byte[][] ghostTop    = new byte[numThreads][];
        byte[][] ghostBottom = new byte[numThreads][];
        for (int t = 0; t < numThreads; t++)
        {
            ghostTop[t]    = new byte[N];
            ghostBottom[t] = new byte[N];
        }

        Directory.CreateDirectory("snapshots_par");
        using var csv = new StreamWriter("stats_par.csv");
        csv.WriteLine("dia,S,I,R,D,R_eff");

        var options = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
        var sw = Stopwatch.StartNew();

        long prevI = 1; 

        for (int day = 0; day < DAYS; day++)
        {
            int cur = day % 2;
            int nxt = 1 - cur;

            for (int t = 0; t < numThreads; t++)
            {
                if (bandEnd[t] < N)
                {
                    int srcRow = bandEnd[t];
                    Array.Copy(grid[cur], srcRow * N, ghostBottom[t], 0, N);
                }

                if (bandStart[t] > 0)
                {
                    int srcRow = bandStart[t] - 1;
                    Array.Copy(grid[cur], srcRow * N, ghostTop[t], 0, N);
                }
            }

            long totalS = 0, totalI = 0, totalR = 0, totalD = 0;

            Parallel.For(0, numThreads, options, t =>
            {
                Random rng = tlRng.Value!;
                long lS = 0, lI = 0, lR = 0, lD = 0;

                int rStart = bandStart[t];
                int rEnd   = bandEnd[t];

                for (int i = rStart; i < rEnd; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        byte state = grid[cur][i * N + j];

                        switch (state)
                        {
                            case D:
                                grid[nxt][i * N + j] = D; lD++; break;

                            case R:
                                grid[nxt][i * N + j] = R; lR++; break;

                            case S:
                            {
                                int inf = 0;

                                if (i > 0)
                                {
                                    byte above = (i == rStart)
                                        ? ghostTop[t][j]
                                        : grid[cur][(i - 1) * N + j];
                                    if (above == I) inf++;
                                }

                                if (i < N - 1)
                                {
                                    byte below = (i == rEnd - 1)
                                        ? ghostBottom[t][j]
                                        : grid[cur][(i + 1) * N + j];
                                    if (below == I) inf++;
                                }

                                if (j > 0   && grid[cur][i * N + (j - 1)] == I) inf++;
                                if (j < N-1 && grid[cur][i * N + (j + 1)] == I) inf++;

                                double pInfect = 1.0 - Math.Pow(1.0 - BETA, inf);
                                if (rng.NextDouble() < pInfect)
                                    { grid[nxt][i * N + j] = I; lI++; }
                                else
                                    { grid[nxt][i * N + j] = S; lS++; }
                                break;
                            }

                            case I:
                            {
                                double r = rng.NextDouble();
                                if      (r < MU)           { grid[nxt][i * N + j] = D; lD++; }
                                else if (r < MU + GAMMA)   { grid[nxt][i * N + j] = R; lR++; }
                                else                       { grid[nxt][i * N + j] = I; lI++; }
                                break;
                            }
                        }
                    }
                }

                Interlocked.Add(ref totalS, lS);
                Interlocked.Add(ref totalI, lI);
                Interlocked.Add(ref totalR, lR);
                Interlocked.Add(ref totalD, lD);
            });

            double rEff = 0;
            if (prevI > 0)
            {
                long deltaI = totalI - prevI;
                rEff = (double)deltaI / prevI + 1.0;
                if (rEff < 0) rEff = 0;
            }
            prevI = totalI;

            if (day % 7 == 0)
                File.WriteAllBytes($"snapshots_par/day_{day:D3}.bin", grid[cur]);

            csv.WriteLine($"{day},{totalS},{totalI},{totalR},{totalD},{rEff:F4}");

            if (day % 30 == 0)
                Console.WriteLine($"Día {day,3} | S={totalS,7} I={totalI,7} R={totalR,7} D={totalD,7} | R_eff={rEff:F2}");
        }

        sw.Stop();
        Console.WriteLine($"\nTiempo con {numThreads} threads: {sw.Elapsed.TotalSeconds:F3} s");
        Console.WriteLine("Presiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}