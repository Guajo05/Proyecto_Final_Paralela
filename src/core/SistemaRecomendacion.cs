using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProyectoFinal.Metrics;

namespace ProyectoFinal.Core
{
    public class SistemaRecomendacion
    {
        private readonly int numUsuarios, numProductos;
        private readonly double[,] matriz;
        private readonly ConcurrentDictionary<(int, int), double> cacheSimilitud;
        private readonly ReaderWriterLockSlim rwLock;
        private readonly Dictionary<string, Metricas> resultados;

        public int NumUsuarios => numUsuarios;
        public int NumProductos => numProductos;

        public SistemaRecomendacion(int usuarios, int productos)
        {
            numUsuarios = usuarios;
            numProductos = productos;
            matriz = new double[usuarios, productos];
            cacheSimilitud = new ConcurrentDictionary<(int, int), double>();
            rwLock = new ReaderWriterLockSlim();
            resultados = new Dictionary<string, Metricas>();

            InicializarDatos();
        }

        private void InicializarDatos()
        {
            var rand = new Random(42);
            for (int i = 0; i < numUsuarios; i++)
                for (int j = 0; j < numProductos; j++)
                    if (rand.NextDouble() < 0.1)
                        matriz[i, j] = rand.Next(1, 6);
        }

        public void EjecutarTodasLasEstrategias()
        {
            Console.WriteLine("══════════════════════════════════════════════");
            Console.WriteLine("  COMPARACIÓN DE ESTRATEGIAS DE PARALELIZACIÓN");
            Console.WriteLine("══════════════════════════════════════════════\n");

            int usuarios = Math.Min(100, numUsuarios);

            // 1. SECUENCIAL (Baseline)
            Ejecutar("Secuencial", 1, () =>
            {
                for (int i = 0; i < usuarios; i++)
                    GenerarRecomendaciones(i);
            });

            // 2. DESCOMPOSICIÓN POR FILAS
            Ejecutar("Por Filas", Environment.ProcessorCount, () =>
            {
                Parallel.For(0, usuarios, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, i => GenerarRecomendaciones(i));
            });

            // 3. DESCOMPOSICIÓN POR BLOQUES 2D
            Ejecutar("Bloques 2D", Environment.ProcessorCount, () =>
            {
                int threads = Environment.ProcessorCount;
                int porBloque = (int)Math.Ceiling(usuarios / (double)threads);

                Parallel.For(0, threads, new ParallelOptions
                {
                    MaxDegreeOfParallelism = threads
                }, bloque =>
                {
                    int inicio = bloque * porBloque;
                    int fin = Math.Min(inicio + porBloque, usuarios);
                    for (int i = inicio; i < fin; i++)
                        GenerarRecomendaciones(i);
                });
            });

            // 4. PIPELINE PARALELO
            Ejecutar("Pipeline", Environment.ProcessorCount, () =>
            {
                var eventos = new ConcurrentQueue<(int, int, double)>();

                Parallel.For(0, usuarios, i =>
                    eventos.Enqueue((i, i % numProductos, 4.5)));

                Parallel.ForEach(eventos, e =>
                {
                    Actualizar(e.Item1, e.Item2, e.Item3);
                    GenerarRecomendaciones(e.Item1);
                });
            });

            // 5. PARTICIONAMIENTO DINÁMICO
            Ejecutar("Dinámico", Environment.ProcessorCount, () =>
            {
                var particiones = Partitioner.Create(0, usuarios, usuarios / Environment.ProcessorCount);
                Parallel.ForEach(particiones, rango =>
                {
                    for (int i = rango.Item1; i < rango.Item2; i++)
                        GenerarRecomendaciones(i);
                });
            });

            // ANÁLISIS DE ESCALABILIDAD
            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("  ESCALABILIDAD (Strong Scaling)");
            Console.WriteLine("══════════════════════════════════════════════\n");

            int[] configs = { 1, 2, 4, 8, Environment.ProcessorCount };
            var tiempos = new Dictionary<int, double>();

            foreach (int t in configs)
            {
                var sw = Stopwatch.StartNew();
                Parallel.For(0, usuarios, new ParallelOptions { MaxDegreeOfParallelism = t },
                    i => GenerarRecomendaciones(i));
                sw.Stop();

                tiempos[t] = sw.Elapsed.TotalMilliseconds;
                double speedup = tiempos[1] / tiempos[t];
                double efic = speedup / t * 100;

                Console.WriteLine($"  {t,2} threads → {tiempos[t],7:F1} ms | " +
                                $"Speedup: {speedup,5:F2}x | Eficiencia: {efic,5:F1}%");
            }

            // RESUMEN
            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("  RESUMEN");
            Console.WriteLine("══════════════════════════════════════════════\n");

            Console.WriteLine("┌────────────────┬──────────┬─────────┬──────────┐");
            Console.WriteLine("│ Estrategia     │ Tiempo   │ Speedup │ Efic.    │");
            Console.WriteLine("├────────────────┼──────────┼─────────┼──────────┤");

            foreach (var r in resultados.OrderBy(x => x.Value.Tiempo))
            {
                var m = r.Value;
                Console.WriteLine($"│ {m.Nombre,-14} │ {m.Tiempo,7:F1}ms │ {m.Speedup,6:F2}x │ {m.Eficiencia,7:F1}% │");
            }

            Console.WriteLine("└────────────────┴──────────┴─────────┴──────────┘");

            var mejor = resultados.Values.OrderByDescending(m => m.Speedup).First();
            Console.WriteLine($"\n✓ MEJOR: {mejor.Nombre} ({mejor.Speedup:F2}x speedup)");
        }

        private void Ejecutar(string nombre, int threads, Action accion)
        {
            Console.WriteLine($"► {nombre}");

            var sw = Stopwatch.StartNew();
            accion();
            sw.Stop();

            var m = new Metricas
            {
                Nombre = nombre,
                Tiempo = sw.Elapsed.TotalMilliseconds,
                NumThreads = threads
            };

            if (resultados.ContainsKey("Secuencial"))
            {
                m.Speedup = resultados["Secuencial"].Tiempo / m.Tiempo;
                m.Eficiencia = m.Speedup / threads * 100;
            }
            else
            {
                m.Speedup = 1.0;
                m.Eficiencia = 100.0;
            }

            resultados[nombre] = m;

            Console.WriteLine($"  Tiempo: {m.Tiempo:F1} ms | Speedup: {m.Speedup:F2}x | " +
                            $"Eficiencia: {m.Eficiencia:F1}%\n");
        }

        public List<int> GenerarRecomendaciones(int usuario)
        {
            var scores = new List<(int prod, double score)>();

            for (int p = 0; p < numProductos; p++)
            {
                if (matriz[usuario, p] > 0) continue;

                double pred = Predecir(usuario, p);
                if (pred > 0) scores.Add((p, pred));
            }

            return scores.OrderByDescending(x => x.score).Take(5).Select(x => x.prod).ToList();
        }

        private double Predecir(int usuario, int producto)
        {
            double sumaRatings = 0, sumaSim = 0;

            for (int u = 0; u < Math.Min(50, numUsuarios); u++)
            {
                if (u == usuario || matriz[u, producto] == 0) continue;

                double sim = Similitud(usuario, u);
                if (sim > 0)
                {
                    sumaRatings += sim * matriz[u, producto];
                    sumaSim += sim;
                }
            }

            return sumaSim > 0 ? sumaRatings / sumaSim : 0;
        }

        private double Similitud(int u1, int u2)
        {
            var key = u1 < u2 ? (u1, u2) : (u2, u1);
            if (cacheSimilitud.TryGetValue(key, out double sim)) return sim;

            double producto = 0, norma1 = 0, norma2 = 0;

            rwLock.EnterReadLock();
            try
            {
                for (int p = 0; p < numProductos; p++)
                {
                    double r1 = matriz[u1, p], r2 = matriz[u2, p];
                    if (r1 > 0 && r2 > 0)
                    {
                        producto += r1 * r2;
                        norma1 += r1 * r1;
                        norma2 += r2 * r2;
                    }
                }
            }
            finally { rwLock.ExitReadLock(); }

            sim = (norma1 > 0 && norma2 > 0) ? producto / (Math.Sqrt(norma1) * Math.Sqrt(norma2)) : 0;
            cacheSimilitud.TryAdd(key, sim);
            return sim;
        }

        public void Actualizar(int usuario, int producto, double rating)
        {
            rwLock.EnterWriteLock();
            try { matriz[usuario, producto] = rating; }
            finally { rwLock.ExitWriteLock(); }
        }
    }
}