using Xunit;
using ProyectoFinal.Metrics;

namespace ProyectoFinal.Tests
{
    public class MetricasTests
    {
        [Fact]
        public void Metricas_DebeInicializarseCorrectamente()
        {
            // Arrange & Act
            var metricas = new Metricas
            {
                Nombre = "Test",
                Tiempo = 100.0,
                NumThreads = 4,
                Speedup = 2.5,
                Eficiencia = 62.5
            };

            // Assert
            Assert.Equal("Test", metricas.Nombre);
            Assert.Equal(100.0, metricas.Tiempo);
            Assert.Equal(4, metricas.NumThreads);
            Assert.Equal(2.5, metricas.Speedup);
            Assert.Equal(62.5, metricas.Eficiencia);
        }

        [Fact]
        public void Speedup_DebeSerMayorQueCeroParaParalelismo()
        {
            // Arrange
            var metricasSecuencial = new Metricas
            {
                Nombre = "Secuencial",
                Tiempo = 1000.0,
                NumThreads = 1,
                Speedup = 1.0,
                Eficiencia = 100.0
            };

            var metricasParalela = new Metricas
            {
                Nombre = "Paralela",
                Tiempo = 250.0,
                NumThreads = 4
            };

            // Act
            metricasParalela.Speedup = metricasSecuencial.Tiempo / metricasParalela.Tiempo;
            metricasParalela.Eficiencia = (metricasParalela.Speedup / metricasParalela.NumThreads) * 100;

            // Assert
            Assert.Equal(4.0, metricasParalela.Speedup);
            Assert.Equal(100.0, metricasParalela.Eficiencia);
        }

        [Theory]
        [InlineData(1000.0, 500.0, 2.0)]
        [InlineData(1000.0, 250.0, 4.0)]
        [InlineData(1000.0, 125.0, 8.0)]
        public void CalcularSpeedup_DebeSerPreciso(double tiempoSeq, double tiempoPar, double speedupEsperado)
        {
            // Arrange
            var metricas = new Metricas
            {
                Tiempo = tiempoPar
            };

            // Act
            double speedup = tiempoSeq / tiempoPar;

            // Assert
            Assert.Equal(speedupEsperado, speedup, precision: 2);
        }

        [Fact]
        public void Eficiencia_DebeEstarEntre0y100()
        {
            // Arrange
            var metricas = new Metricas
            {
                Speedup = 3.5,
                NumThreads = 4
            };

            // Act
            metricas.Eficiencia = (metricas.Speedup / metricas.NumThreads) * 100;

            // Assert
            Assert.InRange(metricas.Eficiencia, 0, 100);
        }

        [Fact]
        public void Eficiencia_LinearSpeedup_Debe100Porciento()
        {
            // Arrange
            var metricas = new Metricas
            {
                Speedup = 4.0,
                NumThreads = 4
            };

            // Act
            metricas.Eficiencia = (metricas.Speedup / metricas.NumThreads) * 100;

            // Assert
            Assert.Equal(100.0, metricas.Eficiencia);
        }

        [Fact]
        public void Eficiencia_SuperLinearSpeedup_DebeSuperarEl100()
        {
            // Arrange (raro, pero posible por efectos de caché)
            var metricas = new Metricas
            {
                Speedup = 4.5,
                NumThreads = 4
            };

            // Act
            metricas.Eficiencia = (metricas.Speedup / metricas.NumThreads) * 100;

            // Assert
            Assert.True(metricas.Eficiencia > 100,
                $"Eficiencia superlinear: {metricas.Eficiencia}%");
        }

        [Theory]
        [InlineData(1, 1.0, 100.0)]
        [InlineData(2, 1.8, 90.0)]
        [InlineData(4, 3.2, 80.0)]
        [InlineData(8, 5.6, 70.0)]
        public void CombinacionSpeedupEficiencia_DebeSerConsistente(
            int threads, double speedup, double eficEsperada)
        {
            // Arrange
            var metricas = new Metricas
            {
                NumThreads = threads,
                Speedup = speedup
            };

            // Act
            metricas.Eficiencia = (speedup / threads) * 100;

            // Assert
            Assert.Equal(eficEsperada, metricas.Eficiencia, precision: 1);
        }
    }
}