using System;
using ProyectoFinal.Core;

namespace ProyectoFinal
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SISTEMA DE RECOMENDACIÓN PARALELO ===\n");

            Console.WriteLine("Tamaño del dataset:");
            Console.WriteLine("1. Pequeño (1,000 usuarios × 500 productos)");
            Console.WriteLine("2. Mediano (10,000 × 5,000)");
            Console.WriteLine("3. Grande (50,000 × 10,000)\n");
            Console.Write("Opción: ");

            int opcion = int.Parse(Console.ReadLine() ?? "1");
            int usuarios = opcion == 2 ? 10000 : opcion == 3 ? 50000 : 1000;
            int productos = opcion == 2 ? 5000 : opcion == 3 ? 10000 : 500;

            Console.WriteLine($"\n✓ Dataset: {usuarios} usuarios × {productos} productos\n");

            var sistema = new SistemaRecomendacion(usuarios, productos);
            sistema.EjecutarTodasLasEstrategias();

            Console.WriteLine("\n\nPresione Enter para salir...");
            Console.ReadLine();
        }
    }
}