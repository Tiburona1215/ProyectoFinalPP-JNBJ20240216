using System;
using System.IO;
using System.Diagnostics;

class SirSequential
{
    const int N      = 1000;
    const int DAYS   = 365;
    const double BETA  = 0.3;
    const double GAMMA = 0.1;
    const double MU    = 0.01;

    const byte S = 0, I = 1, R = 2, D = 3;

    static Random rng = new Random(42);

    static void Main()
    {
        byte[] gridA = new byte[N * N];
        byte[] gridB = new byte[N * N];
        byte[][] grid = { gridA, gridB };

        // Foco inicial en el centro
        grid[0][N / 2 * N + N / 2] = I;

        Directory.CreateDirectory("snapshots_seq");
        using var csv = new StreamWriter("stats_seq.csv");
        csv.WriteLine("dia,S,I,R,D");

        var sw = Stopwatch.StartNew();

        for (int day = 0; day < DAYS; day++)
        {
            int cur = day % 2;
            int nxt = 1 - cur;
            long cntS = 0, cntI = 0, cntR = 0, cntD = 0;

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    byte state = grid[cur][i * N + j];

                    switch (state)
                    {
                        case D:
                            grid[nxt][i * N + j] = D; cntD++; break;

                        case R:
                            grid[nxt][i * N + j] = R; cntR++; break;

                        case S:
                        {
                            int inf = 0;
                            if (i > 0   && grid[cur][(i-1)*N+j] == I) inf++;
                            if (i < N-1 && grid[cur][(i+1)*N+j] == I) inf++;
                            if (j > 0   && grid[cur][i*N+(j-1)] == I) inf++;
                            if (j < N-1 && grid[cur][i*N+(j+1)] == I) inf++;

                            double pInfect = 1.0 - Math.Pow(1.0 - BETA, inf);

                            if (rng.NextDouble() < pInfect)
                            { grid[nxt][i*N+j] = I; cntI++; }
                            else
                            { grid[nxt][i*N+j] = S; cntS++; }
                            break;
                        }

                        case I:
                        {
                            double r = rng.NextDouble();
                            if      (r < MU)           { grid[nxt][i*N+j] = D; cntD++; }
                            else if (r < MU + GAMMA)   { grid[nxt][i*N+j] = R; cntR++; }
                            else                       { grid[nxt][i*N+j] = I; cntI++; }
                            break;
                        }
                    }
                }
            }

            if (day % 7 == 0)
                File.WriteAllBytes($"snapshots_seq/day_{day:D3}.bin", grid[cur]);

            csv.WriteLine($"{day},{cntS},{cntI},{cntR},{cntD}");

            if (day % 30 == 0)
                Console.WriteLine($"Día {day,3} | S={cntS,7} I={cntI,7} R={cntR,7} D={cntD,7}");
        }

        sw.Stop();
        Console.WriteLine($"\nTiempo secuencial: {sw.Elapsed.TotalSeconds:F3} s");
        Console.WriteLine("Presiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}