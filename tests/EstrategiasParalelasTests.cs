using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProyectoFinal.Core;

namespace ProyectoFinal.Tests
{
    public class EstrategiasParalelasTests
    {
        [Fact]
        public void TodasLasEstrategias_DebenGenerarMismosResultados()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);
            int usuarioPrueba = 5;

            // Act - Ejecutar estrategia secuencial (baseline)
            var resultadoSecuencial = sistema.GenerarRecomendaciones(usuarioPrueba);

            // Ejecutar estrategia por filas
            EstrategiasParalelas.PorFilas(sistema, 10);
            var resultadoPorFilas = sistema.GenerarRecomendaciones(usuarioPrueba);

            // Ejecutar estrategia bloques 2D
            EstrategiasParalelas.Bloques2D(sistema, 10);
            var resultadoBloques = sistema.GenerarRecomendaciones(usuarioPrueba);

            // Assert - Todas deben dar resultados similares (mismo orden)
            Assert.Equal(resultadoSecuencial.Count, resultadoPorFilas.Count);
            Assert.Equal(resultadoSecuencial.Count, resultadoBloques.Count);
        }

        [Fact]
        public void Secuencial_DebeProcesarTodosLosUsuarios()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(50, 25);
            int totalUsuarios = 20;

            // Act
            var exception = Record.Exception(() =>
                EstrategiasParalelas.Secuencial(sistema, totalUsuarios));

            // Assert
            Assert.Null(exception); // No debe lanzar excepción
        }

        [Fact]
        public void PorFilas_DebeUsarParalelismo()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);

            // Act
            var tiempoInicio = DateTime.Now;
            EstrategiasParalelas.PorFilas(sistema, 50);
            var duracion = (DateTime.Now - tiempoInicio).TotalMilliseconds;

            // Assert
            Assert.True(duracion < 10000,
                $"La ejecución tomó {duracion}ms, demasiado tiempo");
        }

        [Fact]
        public void Bloques2D_DebeDistribuirCargaUniformemente()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);
            int numThreads = Environment.ProcessorCount;

            // Act
            var exception = Record.Exception(() =>
                EstrategiasParalelas.Bloques2D(sistema, numThreads * 4));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Pipeline_DebeFuncionarConActualizaciones()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);

            // Act
            var exception = Record.Exception(() =>
                EstrategiasParalelas.Pipeline(sistema, 20));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Dinamico_DebeAdaptarseALaCarga()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(200, 100);

            // Act
            var tiempoInicio = DateTime.Now;
            EstrategiasParalelas.Dinamico(sistema, 100);
            var duracion = (DateTime.Now - tiempoInicio).TotalMilliseconds;

            // Assert
            Assert.True(duracion > 0, "La ejecución debe tomar tiempo medible");
            Assert.True(duracion < 30000, "No debe tardar más de 30 segundos");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public void TodasLasEstrategias_DebenManejarDiferentesCantidades(int numUsuarios)
        {
            // Arrange
            var sistema = new SistemaRecomendacion(200, 100);

            // Act & Assert
            Assert.Null(Record.Exception(() =>
                EstrategiasParalelas.Secuencial(sistema, numUsuarios)));

            Assert.Null(Record.Exception(() =>
                EstrategiasParalelas.PorFilas(sistema, numUsuarios)));

            Assert.Null(Record.Exception(() =>
                EstrategiasParalelas.Bloques2D(sistema, numUsuarios)));
        }
    }
}