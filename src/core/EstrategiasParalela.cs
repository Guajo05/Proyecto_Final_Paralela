using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ProyectoFinal.Core
{
    public static class EstrategiasParalelas
    {
        // 1. SECUENCIAL
        public static void Secuencial(SistemaRecomendacion sistema, int usuarios)
        {
            for (int i = 0; i < usuarios; i++)
                sistema.GenerarRecomendaciones(i);
        }

        // 2. POR FILAS
        public static void PorFilas(SistemaRecomendacion sistema, int usuarios)
        {
            Parallel.For(0, usuarios, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, i => sistema.GenerarRecomendaciones(i));
        }

        // 3. BLOQUES 2D
        public static void Bloques2D(SistemaRecomendacion sistema, int usuarios)
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
                    sistema.GenerarRecomendaciones(i);
            });
        }

        // 4. PIPELINE
        public static void Pipeline(SistemaRecomendacion sistema, int usuarios)
        {
            var eventos = new ConcurrentQueue<(int, int, double)>();

            Parallel.For(0, usuarios, i =>
                eventos.Enqueue((i, i % sistema.NumProductos, 4.5)));

            Parallel.ForEach(eventos, e =>
            {
                sistema.Actualizar(e.Item1, e.Item2, e.Item3);
                sistema.GenerarRecomendaciones(e.Item1);
            });
        }

        // 5. DINÁMICO
        public static void Dinamico(SistemaRecomendacion sistema, int usuarios)
        {
            var particiones = Partitioner.Create(0, usuarios, usuarios / Environment.ProcessorCount);
            Parallel.ForEach(particiones, rango =>
            {
                for (int i = rango.Item1; i < rango.Item2; i++)
                    sistema.GenerarRecomendaciones(i);
            });
        }
    }
}