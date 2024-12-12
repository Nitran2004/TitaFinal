
```markdown
# Sistema de Gestión de Proyectos

## Descripción
Este proyecto es un **sistema de gestión de proyectos** desarrollado con **ASP.NET Core**. Su objetivo es facilitar la administración de tareas, empleados y proyectos mediante funcionalidades de autenticación, autorización y CRUD completo para cada tabla. 

El sistema soporta tres roles principales:

- **Administrador**: Acceso completo a todas las funcionalidades.
- **Gerente**: Acceso restringido a ciertas funciones administrativas y reportes.
- **Desarrollador**: Acceso limitado a la gestión de tareas asignadas y estadísticas relacionadas.

## Funcionalidades Principales
- **Autenticación y Autorización**: 
  - Gestión de roles: Administrador, Gerente y Desarrollador.
  - Acceso controlado a las funcionalidades según el rol.

- **Módulo de Tareas**:
  - **Calcular Productividad**: Permite obtener métricas de productividad por tarea.
  - **Estadísticas por Proyecto**: Muestra un desglose detallado de tareas asociadas a cada proyecto.
  - **Estadísticas por Tarea**: Visualiza estadísticas individuales por tarea.
  - **Filtrado por Fecha**: Filtra tareas según rangos de fechas.
  - **Comparación de Tareas Retrasadas**: Permite analizar tareas retrasadas y su impacto.

- **CRUD Completo**:
  - **Tareas**: Creación, lectura, actualización y eliminación.
  - **Proyectos**: Gestión completa de los proyectos asignados.
  - **Empleados**: Administración de información de empleados.

- **Conexión a Base de Datos**:
  - Base de datos alojada en **SQL Server**.
  - Implementación del servicio de Somee para el hosting de la base de datos.

## Tecnologías Utilizadas
- **Back-End**: ASP.NET Core
- **Base de Datos**: SQL Server
- **Deploy**: Hosting en **Somee**
- **Correo Electrónico**: Configuración de **MailJet** para envío de notificaciones.

## Configuración del Proyecto
Para ejecutar el proyecto localmente, asegúrate de configurar correctamente el archivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ConexionSql": "Server=coremartin.mssql.somee.com;Database=coremartin;User ID=Nitran19_SQLLogin_1;Password=d5cxzbcdcq;Trusted_Connection=false;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MailJet": {
    "ApiKey": "b02912f1e3f547bbc84ddcb344fab282",
    "SecretKey": "1fd6c6ba91f52a4aa6153581b6ce08f3"
  }
}
```

## Requisitos Previos
- **SQL Server Management Studio (SSMS)** o cualquier cliente de base de datos compatible con SQL Server.
- **Configuración del hosting en Somee.**

## Instrucciones para Ejecutar
1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/Nitran2004/ProyectoIdentity.git
   ```
2. **Configurar la cadena de conexión en `appsettings.json`.**
3. **Ejecutar migraciones de base de datos**:
   ```bash
   dotnet ef database update
   ```
4. **Iniciar la aplicación**:
   ```bash
   dotnet run
   ```
   Accede al sistema en: [http://localhost:5000](https://localhost:44301/)

## Proyecto Deployado
Puedes acceder al proyecto en línea desde el siguiente enlace:  
[https://www.minicoremz.somee.com/](https://www.minicoremz.somee.com/)
```

### Cambios realizados:
1. Se corrigieron errores en las instrucciones.
2. Se añadió consistencia en los bloques de código.
3. Se mejoró la estructura para que sea más fácil de leer.
