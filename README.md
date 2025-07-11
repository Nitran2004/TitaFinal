# Sistema Verace Pizza - Gestión de Restaurante

Este proyecto es una aplicación web desarrollada con ASP.NET Core y Entity Framework para la gestión integral de restaurantes, incluyendo sistema de recomendaciones por IA, personalización de productos, programa de fidelización, realidad aumentada y gestión de pedidos. Utiliza SQL Server como base de datos y está optimizado para experiencias móviles y de escritorio.

## Índice
- [Requisitos](#requisitos)
- [Configuración del entorno](#configuración-del-entorno)
- [Base de datos](#base-de-datos)
- [Ejecución de la aplicación](#ejecución-de-la-aplicación)
- [Funcionalidades principales](#funcionalidades-principales)
- [Controladores y Endpoints](#controladores-y-endpoints)
- [Servicios especializados](#servicios-especializados)
- [Arquitectura del sistema](#arquitectura-del-sistema)

## Requisitos

- .NET 8.0 SDK
- SQL Server 2019 o superior
- Visual Studio 2022 o Visual Studio Code
- IIS Express (incluido con Visual Studio)
- Node.js 18+ (para funcionalidades de IA)

## Configuración del entorno

1. Clona el repositorio:
```bash
git clone https://github.com/tu-usuario/ProyectoIdentity.git
cd ProyectoIdentity
```

2. Configura la cadena de conexión en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VeracePizzaDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

3. Configura las variables de entorno necesarias:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.mailjet.com",
    "SmtpPort": 587,
    "SmtpUsername": "tu_mailjet_api_key",
    "SmtpPassword": "tu_mailjet_secret_key"
  },
  "AISettings": {
    "ModelPath": "./AIModels/recommendation_model.pkl",
    "EnableRecommendations": true
  }
}
```

## Base de datos

### Crear y migrar la base de datos

1. Abre la Consola del Administrador de paquetes en Visual Studio
2. Ejecuta las migraciones:
```bash
Add-Migration InitialCreate
Update-Database
```

### Estructura principal de la base de datos

- **Usuarios**: Gestión de cuentas y autenticación
- **Productos**: Catálogo de pizzas, bebidas y otros productos
- **Ingredientes**: Componentes de productos con costos y propiedades
- **Pedidos**: Órdenes de clientes con estados y seguimiento
- **PuntosRecoleccion**: Sucursales donde se recogen pedidos
- **TransaccionesPuntos**: Historial del programa de fidelización
- **Recompensas**: Premios canjeables por puntos

## Ejecución de la aplicación

### Modo desarrollo
```bash
dotnet run
```
O desde Visual Studio: `F5` o `Ctrl+F5`

### Acceso a la aplicación
- URL principal: `https://localhost:7000`
- URL HTTP: `http://localhost:5000`

## Funcionalidades principales

### Sistema de Recomendaciones IA
- Algoritmos TF-IDF y Cosine Similarity
- Análisis de preferencias del usuario
- Recomendaciones personalizadas basadas en historial

### Personalización de Productos
- Remoción de ingredientes
- Notas especiales del cliente
- Cálculo dinámico de precios

### Programa de Fidelización
- Acumulación de puntos (30 puntos por dólar)
- Canje de recompensas
- Historial de transacciones

### Realidad Aumentada
- Visualización 3D de pizzas y sándwiches
- Experiencia inmersiva pre-compra

### Gestión de Recolección
- Selección de puntos de entrega
- Cálculo de distancias con geolocalización
- Confirmación de ubicaciones

## Controladores y Endpoints

### Autenticación y Usuarios
```
POST /Account/Register - Registro de nuevos usuarios
POST /Account/Login - Inicio de sesión
GET /Account/Logout - Cerrar sesión
GET /Account/Profile - Perfil del usuario
```

### Productos y Catálogo
```
GET /Productos/ - Lista de productos disponibles
GET /Productos/Detalle/{id} - Detalle de producto específico
GET /Productos/PorCategoria/{categoria} - Productos por categoría
```

### Sistema de Recomendaciones
```
GET /MenuRecomendacion/Recomendacion - Productos recomendados por IA
GET /MenuRecomendacion/Detalle/{id} - Detalle de producto recomendado
POST /MenuRecomendacion/AgregarAlCarrito - Agregar producto recomendado al carrito
```

### Personalización
```
GET /Personalizacion/ - Lista de productos personalizables
GET /Personalizacion/Detalle/{id} - Personalizar producto específico
POST /Personalizacion/AgregarAlCarrito - Agregar producto personalizado
```

### Programa de Fidelización
```
GET /Fidelizacion/ - Dashboard de puntos
GET /Fidelizacion/Historial - Historial de puntos
GET /Fidelizacion/MisCanjes - Canjes realizados
POST /Fidelizacion/Canjear/{id} - Canjear recompensa
```

### Gestión de Pedidos
```
GET /Carrito/VerCarrito - Visualizar carrito actual
POST /Carrito/AgregarProducto - Agregar producto al carrito
DELETE /Carrito/EliminarProducto/{id} - Eliminar producto del carrito
POST /Carrito/FinalizarPedido - Completar compra
```

### Puntos de Recolección
```
GET /Recoleccion/Seleccionar - Seleccionar punto de entrega
POST /Recoleccion/Confirmar - Confirmar punto seleccionado
GET /CollectionPoints/GetNearby - Puntos cercanos por geolocalización
```

### Realidad Aumentada
```
GET /RealidadAumentada/ - Lista de productos con AR
GET /RealidadAumentada/VistaAR/{id} - Vista AR del producto
```

## Servicios especializados

### MailJetEmailSender
Servicio de notificaciones por correo electrónico para:
- Recuperación de contraseñas
- Confirmaciones de pedidos
- Notificaciones de puntos

### DistanceCalculator
Implementa la fórmula de Haversine para:
- Cálculo de distancias a puntos de recolección
- Optimización de rutas de entrega
- Validación de ubicaciones

### AIRecommendationService
Motor de recomendaciones que incluye:
- Procesamiento de historial de compras
- Análisis de similitudes entre productos
- Generación de sugerencias personalizadas

### ImageOptimizationService
Sistema inteligente de imágenes que:
- Optimiza visualización según tipo de producto
- Aplica fondos adaptativos automáticamente
- Maneja fallbacks para productos sin imagen

## Arquitectura del sistema

### Patrón MVC
- **Models**: Entidades de negocio y DTOs
- **Views**: Razor Pages con componentes responsivos
- **Controllers**: Lógica de aplicación y APIs

### Capas de la aplicación
1. **Presentación**: Views y Controllers
2. **Negocio**: Services y Business Logic
3. **Datos**: Entity Framework y Repository Pattern
4. **Infraestructura**: Servicios externos y configuración

### Tecnologías utilizadas
- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Frontend**: HTML5, CSS3, JavaScript ES6+, Bootstrap 5
- **Base de datos**: SQL Server con Entity Framework
- **IA**: Bibliotecas de Machine Learning para recomendaciones
- **Geolocalización**: APIs nativas del navegador
- **Email**: MailJet API
- **Autenticación**: ASP.NET Core Identity

### Características de seguridad
- Autenticación basada en Identity
- Autorización por roles (Usuario, Administrador)
- Validación de entrada en cliente y servidor
- Protección CSRF
- Conexiones HTTPS obligatorias

### Optimizaciones de rendimiento
- Carga diferida de imágenes
- Compresión de respuestas
- Caché de recomendaciones IA
- Minimización de consultas a base de datos
- Responsividad móvil optimizada

## Estructura de archivos

```
ProyectoIdentity/
├── Controllers/
│   ├── AccountController.cs
│   ├── ProductosController.cs
│   ├── MenuRecomendacionController.cs
│   ├── PersonalizacionController.cs
│   ├── FidelizacionController.cs
│   └── RealidadAumentadaController.cs
├── Models/
│   ├── Producto.cs
│   ├── Usuario.cs
│   ├── Pedido.cs
│   └── ViewModels/
├── Views/
│   ├── MenuRecomendacion/
│   ├── Personalizacion/
│   ├── Fidelizacion/
│   └── Shared/
├── Services/
│   ├── AIRecommendationService.cs
│   ├── MailJetEmailSender.cs
│   └── DistanceCalculator.cs
├── Data/
│   └── ApplicationDbContext.cs
└── wwwroot/
    ├── css/
    ├── js/
    └── images/
```

Este sistema proporciona una experiencia completa de restaurante digital con tecnologías modernas y funcionalidades avanzadas como IA y realidad aumentada.
