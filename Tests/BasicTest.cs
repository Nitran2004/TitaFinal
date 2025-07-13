using Xunit;

namespace ProyectoIdentity.Tests
{
    public class BasicTest
    {
        [Fact]
        public void TestBasicFunctionality()
        {
            // Prueba simple
            var result = 2 + 2;
            Assert.Equal(4, result);
        }

        [Fact]
        public void TestStringOperations()
        {
            // Test de strings
            var texto = "Hola Mundo";
            Assert.Contains("Mundo", texto);
        }

        [Fact]
        public void TestBooleanOperations()
        {
            // Test booleano
            var esVerdadero = true;
            var esFalso = false;
            Assert.True(esVerdadero);
            Assert.False(esFalso);
        }

        [Fact]
        public void TestMathOperations()
        {
            // Tests matemáticos
            Assert.Equal(10, 5 + 5);
            Assert.Equal(10, 2 * 5);
            Assert.True(10 > 5);
        }

        [Fact]
        public void TestCollections()
        {
            // Test de colecciones
            var lista = new List<int> { 1, 2, 3, 4, 5 };
            Assert.Equal(5, lista.Count);
            Assert.Contains(3, lista);
        }

        [Fact]
        public void TestDateOperations()
        {
            // Test de fechas
            var fecha = DateTime.Now;
            var mañana = fecha.AddDays(1);
            Assert.True(mañana > fecha);
        }

        [Fact]
        public void TestNullValidation()
        {
            // Test de null
            string texto = null;
            string textoVacio = "";
            string textoLleno = "Contenido";

            Assert.Null(texto);
            Assert.Empty(textoVacio);
            Assert.NotEmpty(textoLleno);
        }
    }
}