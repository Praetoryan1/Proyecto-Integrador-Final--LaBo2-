# Trazo · Portafolios creativos

Proyecto Integrador Final de **Laboratorio II**. Autor: **Muñoz, Jonathan**. Entrega preliminar: 15/10/2026. Entrega final: **05/11/2026**.

Trazo reúne portafolios de diseño, ilustración y animación con descubrimiento por etiquetas. Los reclutadores pueden navegar sin cuenta; cada creador decide la visibilidad de su perfil, publicaciones y contacto profesional. Busca resolver la dispersión de trabajos y datos de contacto entre redes generalistas y sitios personales.

**Estado al 21/10/2026: primer incremento funcional local.** Ya se puede registrar una cuenta, ingresar y salir, editar el perfil, subir un avatar, explorar publicaciones de demostración y administrar etiquetas con el rol correspondiente. El editor de publicaciones, los medios adicionales, Vue, JWT y las funciones sociales todavía están pendientes. Esta versión no está preparada para abrirse al público.

![Explorar en Trazo](docs/evidencias/explorar-desktop.png)

## Ejecución local

Requisitos: .NET SDK **10.0.400**, Docker Desktop con motor Linux activo, **PowerShell 7.2 o superior**, Git y VS Code. No hace falta instalar MySQL directamente ni modificar bases de otros proyectos. Node no es necesario para este incremento.

En una terminal PowerShell de VS Code, abierta en la raíz del repositorio:

```powershell
./tools/Preparar-Entorno.ps1
dotnet dev-certs https --trust
dotnet run --project src/Trazo.Web --launch-profile https -- --seed-demo
dotnet run --project src/Trazo.Web --launch-profile https
```

Abrir **https://localhost:7280**. El comando `--seed-demo` aplica migraciones, crea roles y carga datos ficticios; termina sin iniciar el servidor. La última línea inicia la web. El certificado requiere la confirmación del sistema operativo la primera vez. Usar HTTPS para que funcionen las cookies de sesión.

El script genera contraseñas aleatorias en `.env` y configura la conexión mediante .NET User Secrets. Estos secretos, la base y los archivos subidos **no se versionan**. MySQL queda disponible solo en la máquina local, en `127.0.0.1:3307`. Para detenerlo conservando los datos: `docker compose stop`.

La [guía de desarrollo](docs/desarrollo.md) explica cada paso, la conexión desde Workbench, las migraciones, las pruebas y el primer push.

## Cuentas de demostración

Disponibles después de ejecutar `--seed-demo`, exclusivamente en Development. Contraseña común: **`Trazo.Demo2026!`**. Son credenciales ficticias públicas para evaluación local; no deben reutilizarse en un despliegue.

| Correo de acceso | Roles | Perfil |
| --- | --- | --- |
| `ana@trazo.test` | Creador | `/p/ana` |
| `bruno@trazo.test` | Creador | `/p/bruno` |
| `admin@trazo.test` | Creador, Administrador | `/p/admin` |

Los datos iniciales incluyen 6 etiquetas, 18 actualizaciones públicas y 3 casos de Ana: una publicación para registrados, una privada y un borrador. Las portadas geométricas están hechas con CSS; no simulan archivos multimedia cargados. El administrador tampoco puede leer publicaciones privadas ajenas.

## Arquitectura y versiones

Un proyecto **ASP.NET Core MVC/Razor** con EF Core y MySQL; ASP.NET Core Identity administra usuarios, roles y cookies. Los controladores reciben modelos de formulario explícitos y las reglas de lectura se aplican en consultas antes de contar y paginar. El avatar se decodifica y convierte a PNG fuera de `wwwroot` y se sirve con autorización.

| Componente | Versión / situación |
| --- | --- |
| .NET SDK / destino | 10.0.400 / net10.0 |
| Identity, EF Core Design y herramienta EF | 10.0.13 |
| Proveedor MySql.EntityFrameworkCore | 10.0.9 |
| MySQL | Imagen Docker 8.4; ejecución local comprobada con 8.4.11 |
| SkiaSharp | 4.153.1 |
| Bootstrap | 5.3.3, archivos locales |
| Vue 3 / Web API con JWT / colección Postman | Próximos incrementos, aún no implementados |

Las dependencias NuGet están fijadas en los proyectos y en `packages.lock.json`. Docker sigue los parches de la rama 8.4. No se utilizan ADO.NET directo, SignalR ni un frontend separado: no son necesarios para esta fase.

```text
src/Trazo.Web/          Aplicación MVC
  Controllers/         Rutas públicas, cuenta, perfil y administración
  Domain/              Entidades y enumeraciones
  Data/                DbContext, migraciones e inicializador de datos
  Services/            Autorización de lectura, consultas y avatar
  Models/              Formularios, DTO y paginación
  Views/ y wwwroot/    Razor y recursos de interfaz
tests/Trazo.Tests/     Pruebas unitarias e integración con MySQL real
database/             SQL del esquema e inicialización de la base de pruebas
docs/                 Propuesta, DER, instrucciones y evidencias
tools/                Preparación del entorno y ejecución de pruebas
```

## Matriz de evidencias de requerimientos mínimos

Se prioriza el mínimo de **6 entidades principales** de la consigna, aunque su tabla de ejemplo diga 4. No se considera que tener una tabla equivalga a completar su módulo funcional.

| Requerimiento | Estado real | Código / controladores | Evidencia |
| --- | --- | --- | --- |
| 1. Al menos 6 entidades principales, 1:N y N:M | Implementado en persistencia: 7 principales, asociaciones y soporte Identity | [Modelo](src/Trazo.Web/Domain/Modelo.cs), [DbContext](src/Trazo.Web/Data/TrazoDbContext.cs), [migración](src/Trazo.Web/Data/Migrations/20261021051003_ModeloInicial.cs) | [DER](docs/der.png), [correspondencia física](docs/modelo-de-datos.md), [SQL](database/001-modelo-inicial.sql), pruebas con MySQL |
| 2. Login/logout, roles y avatar | Implementado y probado localmente | [Cuenta](src/Trazo.Web/Controllers/CuentaController.cs), [Perfil](src/Trazo.Web/Controllers/PerfilController.cs), [etiquetas solo administrador](src/Trazo.Web/Controllers/EtiquetasController.cs), [avatar](src/Trazo.Web/Services/AvatarService.cs) | [Login](docs/evidencias/login.png), [edición de perfil](docs/evidencias/perfil-editar.png), [pruebas de acceso y subida](tests/Trazo.Tests/MySqlIntegrationTests.cs) |
| 3. Archivos adicionales al avatar | Pendiente | Existe la entidad RecursoMultimedia; falta el flujo de archivos de publicación | Captura de subida pendiente; el avatar no cumple este punto adicional |
| 4. ABM completo Vue + AJAX/JSON | Pendiente | El ABM de etiquetas actual es MVC con recarga | Pendiente editor Vue y video de alta, listado, modificación y baja |
| 5. Paginado desde el servidor | Implementado en los listados actuales; debe mantenerse en los próximos | [Pagina](src/Trazo.Web/Models/Pagina.cs), [Explorar](src/Trazo.Web/Controllers/ExplorarController.cs), [Perfil](src/Trazo.Web/Controllers/PerfilController.cs), [Etiquetas](src/Trazo.Web/Controllers/EtiquetasController.cs) | [Explorar](docs/evidencias/explorar-desktop.png), integración comprueba páginas de 12 y 6 registros; captura de red pendiente |
| 6. Búsqueda asíncrona de relacionados | Pendiente | Se incorporará al selector de etiquetas del editor | La búsqueda actual de Explorar no sustituye este requisito |
| 7. Web API REST con JWT | Pendiente | La sesión MVC actual usa cookies | Endpoints JWT y colección Postman pendientes |

## Pruebas

```powershell
./tools/Probar.ps1                 # Unitarias; omite las que necesitan MySQL
./tools/Probar.ps1 -Integracion     # Suite completa, con Docker iniciado
```

La integración **recrea únicamente `trazo_tests` en localhost**; no usa `trazo_dev`. Resultado del primer incremento: **19 pruebas aprobadas, 0 fallidas y 0 omitidas** con MySQL real. Incluye privacidad entre cuentas, permisos por rol, CSRF, carga y lectura de avatar, registro, logout, suspensión, etiquetas, restricciones relacionales y paginación. Ver [registro de validación y capturas](docs/incremento-01.md).

Se agregó un [workflow de GitHub Actions](.github/workflows/ci.yml). Su ejecución remota queda pendiente hasta el primer push; no se presenta como CI aprobada. Estas verificaciones no equivalen a una auditoría de seguridad ni cubren funcionalidades aún no construidas.

## Documentación y pendientes de entrega

- [Entrega preliminar PDF](output/pdf/entrega-preliminar-trazo.pdf), [fuente](docs/entrega-preliminar.md) y [DER Mermaid](docs/der.mmd).
- [Plan de trabajo y antecedentes de seguridad y marco legal argentino](docs/plan-de-trabajo.md).
- [Desarrollo en VS Code y MySQL](docs/desarrollo.md), [modelo físico](docs/modelo-de-datos.md) y [dependencias de terceros](docs/dependencias.md).

Para promoción faltan los módulos señalados, funciones sociales y moderación, datos multimedia, SQL con datos ficticios completos, colección Postman, documentación y evidencias finales, y ensayo de instalación y defensa. El SQL actual contiene el **esquema**; los datos se generan con el inicializador C#.

Para producción se requiere otra etapa: recuperación y confirmación de cuentas, revisión de seguridad y operación, tratamiento de datos personales, términos y privacidad aplicables, licencias, copias de seguridad y gestión de incidentes. La página de privacidad actual informa el estado de desarrollo; no es una política legal definitiva.

## Trabajo por incrementos

Al terminar cada incremento se revisan archivos y pruebas y se **detiene el desarrollo para el commit/push de Jonathan**. No se ejecuta push automáticamente. La apertura a producción es independiente de la entrega académica.
