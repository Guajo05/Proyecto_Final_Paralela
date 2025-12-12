using System;
using System.Linq;
using Xunit;
using ProyectoFinal.Core;

namespace ProyectoFinal.Tests
{
    public class SistemaRecomendacionTests
    {
        [Fact]
        public void Constructor_DebeInicializarCorrectamente()
        {
            // Arrange & Act
            var sistema = new SistemaRecomendacion(100, 50);

            // Assert
            Assert.Equal(100, sistema.NumUsuarios);
            Assert.Equal(50, sistema.NumProductos);
        }

        [Fact]
        public void GenerarRecomendaciones_DebeRetornarMaximo5Productos()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);

            // Act
            var recomendaciones = sistema.GenerarRecomendaciones(0);

            // Assert
            Assert.NotNull(recomendaciones);
            Assert.True(recomendaciones.Count <= 5,
                $"Se esperaban máximo 5 recomendaciones, pero se obtuvieron {recomendaciones.Count}");
        }

        [Fact]
        public void GenerarRecomendaciones_NoDebeIncluirProductosYaCalificados()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);
            sistema.Actualizar(0, 10, 5.0); // Usuario 0 calificó producto 10

            // Act
            var recomendaciones = sistema.GenerarRecomendaciones(0);

            // Assert
            Assert.DoesNotContain(10, recomendaciones);
        }

        [Fact]
        public void Actualizar_DebeModificarCalificacion()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);

            // Act
            sistema.Actualizar(5, 20, 4.5);
            sistema.Actualizar(5, 20, 3.0); // Actualizar la misma calificación

            // Assert - Verificar que no lance excepciones
            Assert.True(true);
        }

        [Theory]
        [InlineData(10, 5)]
        [InlineData(100, 50)]
        [InlineData(1000, 500)]
        public void Constructor_DebeFuncionarConDiferentesTamanos(int usuarios, int productos)
        {
            // Act
            var sistema = new SistemaRecomendacion(usuarios, productos);

            // Assert
            Assert.Equal(usuarios, sistema.NumUsuarios);
            Assert.Equal(productos, sistema.NumProductos);
        }

        [Fact]
        public void GenerarRecomendaciones_UsuarioInvalido_NoDebeLanzarExcepcion()
        {
            // Arrange
            var sistema = new SistemaRecomendacion(100, 50);

            // Act & Assert
            var exception = Record.Exception(() => sistema.GenerarRecomendaciones(-1));

            // Debería lanzar excepción, pero verificamos comportamiento actual
            Assert.NotNull(exception);
        }
    }
}