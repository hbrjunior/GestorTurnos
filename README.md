# Gestor de Turnos de Trabajo

Aplicación web para la gestión de turnos de trabajo de colaboradores de distintos países, desarrollada como parte de una prueba técnica.

La solución incluye una API REST en **ASP.NET Core 9**, una interfaz web en **Angular 22**, persistencia en **SQL Server 2022** y despliegue mediante **Docker Compose**.

## 1. Funcionalidades implementadas

### Gestión de colaboradores
- Alta de colaboradores con nombre, correo electrónico y país.
- Consulta de todos los colaboradores y búsqueda por identificador.
- Actualización y eliminación de colaboradores.
- Validación de nombres y control de correos electrónicos duplicados.
- Protección de la integridad referencial: no se permite eliminar colaboradores que tengan turnos asociados.

### Gestión de turnos
- Alta, consulta, edición y eliminación de turnos.
- Asociación de cada turno a un colaborador.
- Definición de fecha, hora de inicio y hora de fin.
- Filtrado por rango de fechas.
- Validación de intervalos horarios.
- Detección de solapamientos para un mismo colaborador.

### Importación desde Excel
- Carga de archivos `.xlsx`.
- Procesamiento y validación individual de las filas.
- Identificación del colaborador mediante su correo electrónico.
- Reutilización de las reglas de validación de turnos.
- Informe de resultados con filas importadas, filas rechazadas y motivo de cada error.

### Exportación a Excel
- Generación y descarga de archivos `.xlsx`.
- Filtrado de turnos por rango de fechas.
- Inclusión de información del colaborador, país, fecha y horario.

### Festivos nacionales
- Consulta de festivos por país mediante la API pública Nager.Date.
- Identificación de los turnos que coinciden con un festivo.
- Visualización del nombre del festivo en el listado.
- Uso de caché para reducir consultas externas.
- Gestión de errores y mecanismo de fallback cuando el servicio externo no está disponible.

## 2. Tecnologías utilizadas

| Componente | Tecnología |
|---|---|
| Backend | C# / ASP.NET Core 9 |
| Frontend | Angular 22 / TypeScript / SCSS |
| Base de datos | SQL Server 2022 |
| Persistencia | Entity Framework Core 9 |
| Pruebas automatizadas | xUnit |
| Documentación API | OpenAPI / Swagger |
| Archivos Excel | ClosedXML |
| API externa | Nager.Date |
| Contenedores | Docker y Docker Compose |
| Servidor web | Nginx |
| Control de versiones | Git / GitHub |

## 3. Organización del proyecto

```text
GestorTurnos/
├── backend/
│   ├── GestorTurnos.Api/
│   │   ├── Contracts/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Dockerfile
│   │   └── Program.cs
│   ├── GestorTurnos.Domain/
│   │   ├── Entities/
│   │   └── Rules/
│   ├── GestorTurnos.Infrastructure/
│   │   ├── Persistence/
│   │   └── Migrations/
│   └── GestorTurnos.Tests/
├── frontend/
│   └── gestor-turnos-web/
│       ├── src/
│       ├── Dockerfile
│       └── nginx.conf
├── .dockerignore
├── .env.example
├── compose.yaml
├── GestorTurnos.sln
└── README.md
```

La solución separa las responsabilidades principales:

- **Domain:** entidades y reglas de negocio independientes de la infraestructura.
- **Infrastructure:** acceso a datos, configuración de Entity Framework Core y migraciones.
- **Api:** endpoints REST, contratos, validaciones y servicios de aplicación.
- **Tests:** pruebas automatizadas de reglas de negocio.
- **Frontend:** interfaz web que consume la API mediante solicitudes HTTP.

## 4. Requisitos para la ejecución

Para ejecutar la aplicación completa mediante Docker:

- Docker Desktop con soporte para contenedores Linux.
- Docker Compose.
- Recursos suficientes para ejecutar SQL Server, API y frontend.
- Conexión a Internet para descargar las imágenes y dependencias necesarias.

No es necesario instalar SQL Server, Node.js ni .NET SDK en el equipo anfitrión cuando se utiliza Docker Compose.

## 5. Ejecución con Docker Compose

### 5.1. Clonar el repositorio

```bash
git clone https://github.com/hbrjunior/GestorTurnos.git
cd GestorTurnos
```

### 5.2. Configurar las variables de entorno

Copiar el archivo de ejemplo:

**Windows PowerShell:**

```powershell
Copy-Item .env.example .env
```

**Linux / macOS:**

```bash
cp .env.example .env
```

Editar el archivo `.env` y reemplazar el valor de `MSSQL_SA_PASSWORD` por una contraseña segura y exclusiva para el entorno de pruebas.

El archivo `.env` contiene información sensible y está excluido del control de versiones.

### 5.3. Iniciar la aplicación

Desde la raíz del repositorio:

```bash
docker compose up -d --build
```

Este comando construye e inicia los servicios definidos en `compose.yaml`.

La configuración utiliza tres servicios:

| Servicio | Puerto local |
|---|---|
| Frontend Angular / Nginx | 4200 |
| API ASP.NET Core | 8080 |
| SQL Server | 14333 |

Los puertos se publican únicamente en la interfaz local del equipo.

### 5.4. Acceder a la aplicación

**Interfaz web:**

http://localhost:4200

**API REST:**

http://localhost:8080/api/employees

**Comprobación HTTP de la API:**

http://localhost:8080/health

### 5.5. Comprobar los contenedores

```bash
docker compose ps
```

El resultado debe mostrar los servicios `frontend`, `api` y `sqlserver` en ejecución.

SQL Server y la API disponen de healthchecks. Las dependencias de inicio permiten esperar a que los servicios necesarios alcancen un estado saludable.

### 5.6. Base de datos y migraciones

La aplicación utiliza Entity Framework Core.

En el entorno Docker, las migraciones se aplican automáticamente durante el inicio de la API mediante la configuración `Database__ApplyMigrations`.

En una instalación inicial se crea la base de datos `GestorTurnos` con las tablas necesarias.

Los datos se almacenan en un volumen persistente de Docker.

### 5.7. Detener la aplicación

```bash
docker compose down
```

Este comando detiene y elimina los contenedores y la red del proyecto sin eliminar los volúmenes persistentes.

**Importante:** `docker compose down -v` también elimina los volúmenes del proyecto y puede provocar la pérdida de los datos almacenados.

## 6. API REST

### Colaboradores

| Método | Endpoint | Operación |
|---|---|---|
| GET | `/api/employees` | Listar colaboradores |
| GET | `/api/employees/{id}` | Consultar colaborador |
| POST | `/api/employees` | Crear colaborador |
| PUT | `/api/employees/{id}` | Actualizar colaborador |
| DELETE | `/api/employees/{id}` | Eliminar colaborador |

Ejemplo de creación:

```json
{
  "name": "Carlos Prueba",
  "email": "carlos@example.com",
  "countryCode": "ES"
}
```

### Turnos

| Método | Endpoint | Operación |
|---|---|---|
| GET | `/api/shifts` | Consultar turnos |
| GET | `/api/shifts/{id}` | Consultar turno por ID |
| POST | `/api/shifts` | Crear turno |
| PUT | `/api/shifts/{id}` | Actualizar turno |
| DELETE | `/api/shifts/{id}` | Eliminar turno |

La consulta de turnos admite filtros de fechas mediante parámetros `from` y `to`.

### Importación y exportación

| Método | Endpoint | Operación |
|---|---|---|
| POST | `/api/shifts/import` | Importar turnos desde Excel |
| GET | `/api/shifts/export` | Exportar turnos a Excel |

El endpoint de importación recibe un archivo `.xlsx` utilizando `multipart/form-data`.

La exportación permite indicar el rango de fechas.

### Documentación OpenAPI

La API proporciona documentación interactiva mediante Swagger UI y genera su especificación OpenAPI.

Con Docker Compose, la documentación está disponible en:

- **Swagger UI:** http://localhost:8080/swagger
- **OpenAPI JSON:** http://localhost:8080/openapi/v1.json

La documentación se habilita mediante la variable de entorno `Swagger__Enabled=true`, configurada específicamente para esta prueba técnica.

La aplicación utiliza el entorno `Production` dentro de Docker, manteniendo habilitada la documentación para facilitar la evaluación de los endpoints REST.

En un entorno de producción real, se recomienda restringir o deshabilitar el acceso público a Swagger y OpenAPI.

## 7. Importación Excel

La importación permite cargar turnos desde un archivo `.xlsx`.

El formato utilizado durante las pruebas incluye las siguientes columnas:

| Columna | Ejemplo |
|---|---|
| Colaborador | Carlos Prueba |
| Email | carlos@example.com |
| País | ES |
| Fecha | 2026-10-20 |
| HoraInicio | 08:00 |
| HoraFin | 12:00 |

La identificación del colaborador se realiza mediante el correo electrónico.

Cada fila se procesa individualmente y el sistema devuelve un informe con:

- Cantidad total de filas procesadas.
- Número de filas importadas.
- Número de filas rechazadas.
- Resultado de cada fila, incluyendo el motivo de rechazo.

Entre los errores contemplados se encuentran colaboradores inexistentes, horarios inválidos y solapamientos.

## 8. Reglas de negocio

### Validación de horarios

Se exige que:

`HoraInicio < HoraFin`

Los turnos deben comenzar y terminar dentro del mismo día.

### Solapamiento de turnos

No se permiten dos turnos superpuestos para un mismo colaborador.

Ejemplo rechazado:

- Turno existente: 08:00–12:00.
- Nuevo turno: 11:00–15:00.

Ejemplo permitido:

- Turno existente: 08:00–12:00.
- Nuevo turno: 12:00–16:00.

Los intervalos se consideran semiabiertos: el instante de finalización de un turno puede coincidir con el inicio del siguiente.

Esta decisión evita rechazar turnos consecutivos que no comparten tiempo efectivo de trabajo.

### Colaboradores

- Los correos electrónicos se normalizan en minúsculas.
- Los códigos de país se normalizan en mayúsculas.
- No se permiten correos electrónicos duplicados.
- No se permite eliminar un colaborador que tenga turnos asociados.

## 9. Festivos nacionales

La solución consulta el servicio público:

https://date.nager.at/

Los festivos se obtienen considerando el país del colaborador y el año correspondiente.

Se utiliza una caché en memoria para reducir las solicitudes repetidas y mejorar el rendimiento.

El servicio HTTP tiene un tiempo de espera configurado y contempla fallos de comunicación con el proveedor externo.

La integración con festivos tiene carácter informativo: permite identificar y mostrar los días festivos, sin impedir automáticamente la asignación de turnos en esas fechas.

## 10. Pruebas automatizadas

Las pruebas utilizan xUnit y se centran especialmente en las reglas puras de negocio relacionadas con los horarios.

Desde la raíz del proyecto:

```bash
dotnet test GestorTurnos.sln
```

Para compilar la solución en configuración Release:

```bash
dotnet build GestorTurnos.sln --configuration Release
```

### Escenarios automatizados

Se incluyen 19 casos de prueba, entre ellos:

- Solapamiento parcial.
- Turnos completamente contenidos.
- Turnos con horarios idénticos.
- Turnos consecutivos.
- Intervalos sin intersección.
- Solapamientos de un minuto.
- Hora inicial igual a la final.
- Hora final anterior a la inicial.
- Límites horarios.

**Última ejecución validada:** 19 pruebas superadas, sin fallos ni pruebas omitidas.

Además, durante el desarrollo se realizaron pruebas funcionales de los endpoints, gestión de turnos, importación/exportación Excel, integración de festivos, despliegue Docker y persistencia de datos.

Las pruebas automatizadas actuales no pretenden representar una cobertura completa de integración de todos los componentes.

## 11. Decisiones de diseño

### Separación de responsabilidades

Se eligió una organización en proyectos diferenciados para mantener las reglas de negocio desacopladas de la infraestructura y facilitar las pruebas.

### Entity Framework Core

Se utiliza EF Core para gestionar la persistencia y las migraciones de base de datos.

Las consultas de solo lectura utilizan `AsNoTracking()` cuando corresponde.

### Validación de solapamientos

La lógica de comparación de intervalos se centralizó en una clase de reglas de dominio, evitando duplicar este comportamiento en diferentes operaciones.

### SQL Server

Se seleccionó SQL Server por su integración con .NET y Entity Framework Core y por permitir un entorno reproducible mediante contenedores.

### Docker Compose

Se optó por separar frontend, backend y base de datos en tres servicios.

Los healthchecks y las dependencias de inicio mejoran la fiabilidad del despliegue.

### Nginx

Nginx sirve los archivos estáticos generados por Angular y actúa como proxy inverso hacia la API REST.

### Gestión de fallos externos

La consulta de festivos utiliza caché y un mecanismo de fallback para reducir la dependencia directa de la disponibilidad del proveedor externo.

## 12. Alcance y funcionalidades no incluidas

El objetivo fue cumplir los requisitos obligatorios manteniendo un alcance razonable para una prueba técnica.

No se incluyeron:

- **Autenticación y autorización:** no forman parte de los requisitos solicitados. Serían necesarias antes de una puesta en producción real.
- **Administración de colaboradores desde el frontend:** el CRUD está disponible mediante la API REST. La interfaz se concentró en la gestión de turnos, de acuerdo con el alcance mínimo solicitado.
- **Turnos nocturnos que atraviesan la medianoche:** se adoptó la regla de inicio y fin dentro del mismo día para simplificar el modelo y mantener un comportamiento explícito.
- **Actualización automática de festivos en tiempo real:** se utiliza caché para minimizar la dependencia del servicio externo.
- **Pruebas end-to-end automatizadas:** se priorizaron las reglas críticas de negocio y las pruebas funcionales de integración realizadas durante el desarrollo.
- **Observabilidad avanzada, métricas y trazas distribuidas:** se consideran mejoras propias de una evolución hacia producción.

Estas exclusiones son decisiones de alcance, no requisitos implícitamente implementados.

## 13. Uso de inteligencia artificial

Durante el desarrollo se utilizó **ChatGPT de OpenAI** como herramienta de apoyo para:

- Analizar y desglosar los requisitos del ejercicio.
- Evaluar alternativas de diseño y organización de la solución.
- Asistir en la elaboración y revisión de fragmentos de código C#, TypeScript y configuraciones Docker.
- Apoyar el diagnóstico de errores de compilación e integración.
- Proponer escenarios adicionales de pruebas automatizadas.
- Revisar y preparar la documentación técnica.

Las sugerencias generadas mediante IA se incorporaron al proceso de desarrollo mediante revisión, ajustes, compilaciones y validaciones funcionales.

La herramienta se utilizó como apoyo y no como sustituto de las verificaciones técnicas.

El autor puede explicar las decisiones adoptadas, los componentes implementados y el funcionamiento de la solución.

## 14. Mejoras futuras

Entre las posibles evoluciones se encuentran:

- Ampliar las pruebas automatizadas de integración.
- Incorporar autenticación y autorización.
- Añadir una pantalla de administración de colaboradores.
- Soportar turnos que atraviesen la medianoche mediante un modelo adecuado.
- Incorporar paginación y filtros adicionales.
- Añadir observabilidad y un sistema centralizado de gestión de errores.
- Mejorar la distribución de secretos para escenarios productivos.

## 15. Autor

**Herbert Bruchmann Junior**

Prueba técnica — Gestor de Turnos de Trabajo

Desarrollado con C#, .NET 9, Angular 22, SQL Server, Entity Framework Core y Docker.