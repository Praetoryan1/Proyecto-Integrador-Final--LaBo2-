# Trazo · Portafolios creativos

Proyecto Integrador Final de **Laboratorio II**. Autor: **Muñoz, Jonathan**.

Trazo reúne portafolios de diseño, ilustración y animación con descubrimiento por etiquetas. Los reclutadores pueden navegar sin cuenta; cada creador decide la visibilidad de su perfil, publicaciones y contacto profesional. Busca resolver la dispersión de trabajos y datos de contacto entre redes generalistas y sitios personales.


![Explorar en Trazo](docs/evidencias/explorar-desktop.png)

## Ejecución local

Requisitos: .NET SDK **10.0.400**, Docker Desktop con motor Linux activo, **PowerShell 7.2 o superior**, Git y VS Code. No hace falta instalar MySQL directamente ni modificar bases de otros proyectos. El JavaScript compilado de Vue está incluido: **Node no es necesario para ejecutar la web**. Para modificar sus componentes se requiere Node >=22.12 y `npm ci` / `npm run build`.

En una terminal PowerShell de VS Code, abierta en la raíz del repositorio:

```powershell
./tools/Preparar-Entorno.ps1
./tools/Preparar-Medios.ps1
./tools/Preparar-Api.ps1
dotnet dev-certs https --trust
dotnet run --project src/Trazo.Web --launch-profile https -- --seed-demo
dotnet run --project src/Trazo.Web --launch-profile https
```

Abrir **https://localhost:7280**. El comando `--seed-demo` aplica migraciones, crea roles y carga datos ficticios; termina sin iniciar el servidor. La última línea inicia la web. El certificado requiere la confirmación del sistema operativo la primera vez. Usar HTTPS para que funcionen las cookies de sesión.

El script genera contraseñas aleatorias en `.env` y configura la conexión mediante .NET User Secrets. Estos secretos, la base y los archivos subidos **no se versionan**. MySQL queda disponible solo en la máquina local, en `127.0.0.1:3307`. Para detenerlo conservando los datos: `docker compose stop`.

La [guía de desarrollo](docs/desarrollo.md) explica cada paso, la conexión desde Workbench, las migraciones, las pruebas y el primer push.

Para usar el editor: ingresar y abrir **Mi estudio → Administrar mis publicaciones**, o `/panel/publicaciones`. Guardar el borrador, agregar recursos y elegir Publicada. Una obra necesita al menos un recurso listo; las actualizaciones pueden ser solo texto. Este incremento no cambia el esquema ni requiere otra migración. La [guía de medios](docs/medios.md) explica formatos, cuotas y FFmpeg, necesario para GIF/MP4. El script portable es para Windows; en Linux se instala con el gestor de paquetes.

Desde **Mi estudio** también se accede a **Mis favoritos** y **Creadores que sigo**. Las dos listas son privadas, tienen búsqueda y paginación desde MySQL. Se guarda una publicación desde su detalle y se sigue a un creador desde su perfil; esas relaciones no conceden acceso adicional. [Reglas y evidencia del módulo](docs/incremento-05.md).

El módulo de reportes permite denunciar publicaciones ajenas, consultar **Mis reportes** y solicitar revisión si una obra propia fue ocultada. El administrador accede desde **Mi estudio → Moderar reportes**, con decisiones justificadas y acceso restringido al contenido. [Uso, permisos y evidencias](docs/incremento-06.md).

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
| Vue | 3.5.44, componentes precompilados e integrados en MVC |
| Vite / plugin Vue | 8.3.5 / 6.0.10, herramientas de compilación |
| Playwright | 1.64.1, recorrido local de evidencia en Edge |
| FFmpeg / ffprobe | 9.0.2 portable en Windows; procesamiento de GIF/MP4, fuera de Git |
| Web API con JWT | Microsoft.AspNetCore.Authentication.JwtBearer 10.0.13; API de evaluación local |

Las dependencias NuGet están fijadas en los proyectos y en `packages.lock.json`. Docker sigue los parches de la rama 8.4. No se utilizan ADO.NET directo, SignalR ni un frontend separado: no son necesarios para esta fase.

Las dependencias del editor están fijadas en `package-lock.json`. Los endpoints `/panel-json/` usan **cookies y antiforgery**, y devuelven JSON; no equivalen al requisito de API autenticada con JWT. Vue se compila antes de servirlo, conservando la CSP sin `unsafe-eval` ni scripts inline.

La API **`/api/v1/` usa JWT Bearer** y comparte las reglas de publicaciones con MVC. Incluye CRUD propio, paginación, consulta de etiquetas y revocación de sesiones. Importar la [colección Postman](docs/api/Trazo.postman_collection.json) y seguir la [guía de API](docs/api.md). El emisor es exclusivamente local para evaluación; la API está deshabilitada en Production/Staging. No cambia el esquema de MySQL.

```text
src/Trazo.Web/          Aplicación MVC
  Controllers/         Rutas públicas, cuenta, perfil y administración
  Domain/              Entidades y enumeraciones
  Data/                DbContext, migraciones e inicializador de datos
  Services/            Autorización de lectura, consultas y avatar
  Models/              Formularios, DTO y paginación
  ClientApp/           Componentes Vue del editor y selector de etiquetas
  Views/ y wwwroot/    Razor y recursos de interfaz
tests/Trazo.Tests/     Pruebas unitarias e integración con MySQL real
database/             SQL del esquema e inicialización de la base de pruebas
docs/                 Propuesta, DER, instrucciones y evidencias
tools/                Preparación del entorno y ejecución de pruebas
```

## Matriz de evidencias de requerimientos mínimos

| Requerimiento | Estado real | Código / controladores | Evidencia |
| --- | --- | --- | --- |
| 1. Al menos 6 entidades principales, 1:N y N:M | Implementado en persistencia: 7 principales, asociaciones y soporte Identity | [Modelo](src/Trazo.Web/Domain/Modelo.cs), [DbContext](src/Trazo.Web/Data/TrazoDbContext.cs), [migración](src/Trazo.Web/Data/Migrations/20261021051003_ModeloInicial.cs) | [DER](docs/der.png), [correspondencia física](docs/modelo-de-datos.md), [SQL](database/001-modelo-inicial.sql), pruebas con MySQL |
| 2. Login/logout, roles y avatar | Implementado y probado localmente | [Cuenta](src/Trazo.Web/Controllers/CuentaController.cs), [Perfil](src/Trazo.Web/Controllers/PerfilController.cs), [etiquetas solo administrador](src/Trazo.Web/Controllers/EtiquetasController.cs), [avatar](src/Trazo.Web/Services/AvatarService.cs), [moderación solo administrador](src/Trazo.Web/Controllers/ModeracionController.cs) | [Login](docs/evidencias/login.png), [edición de perfil](docs/evidencias/perfil-editar.png), [pruebas de acceso y subida](tests/Trazo.Tests/MySqlIntegrationTests.cs), [recorrido de moderación](docs/evidencias/incremento-06/moderacion.webm) |
| 3. Archivos adicionales al avatar | Implementado y probado localmente: imágenes, GIF y MP4 de publicaciones | [Controlador](src/Trazo.Web/Controllers/RecursosController.cs), [almacenamiento y procesamiento](src/Trazo.Web/Services/AlmacenMedios.cs) | [Video de carga y edición](docs/evidencias/incremento-03/medios-abm.webm), [captura](docs/evidencias/incremento-03/editor-medios.png), [pruebas de archivos y privacidad](tests/Trazo.Tests/MediosIntegrationTests.cs) |
| 4. ABM completo Vue + AJAX/JSON | Implementado para texto y recursos multimedia | [Componente Vue](src/Trazo.Web/ClientApp/publicaciones/PublicacionesApp.vue), [editor de recursos](src/Trazo.Web/ClientApp/publicaciones/RecursosEditor.vue), [controladores JSON](src/Trazo.Web/Controllers/PublicacionesJsonController.cs), [servicio](src/Trazo.Web/Services/PublicacionesService.cs) | [Video del ABM](docs/evidencias/incremento-02/editor-abm.webm), [medios sin recargas](docs/evidencias/incremento-03/red.json), [pruebas](tests/Trazo.Tests/PublicacionesIntegrationTests.cs) |
| 5. Paginado desde el servidor | Implementado en los listados actuales; debe mantenerse en los próximos | [Pagina](src/Trazo.Web/Models/Pagina.cs), [Explorar](src/Trazo.Web/Controllers/ExplorarController.cs), [Perfil](src/Trazo.Web/Controllers/PerfilController.cs), [servicio del panel](src/Trazo.Web/Services/PublicacionesService.cs), [favoritos y seguidos](src/Trazo.Web/Services/ComunidadService.cs), [reportes](src/Trazo.Web/Services/ReportesService.cs) | [Respuesta de página 2](docs/evidencias/incremento-02/paginacion.json), [captura](docs/evidencias/incremento-02/paginacion.png), [peticiones](docs/evidencias/incremento-02/red.json); pruebas con páginas de 12 + 1 en [listas privadas](tests/Trazo.Tests/ComunidadIntegrationTests.cs) y [reportes](tests/Trazo.Tests/ReportesIntegrationTests.cs) |
| 6. Búsqueda asíncrona de relacionados | Implementada y probada: etiquetas de publicaciones | [Selector Vue](src/Trazo.Web/ClientApp/publicaciones/SelectorEtiquetas.vue), [búsqueda JSON](src/Trazo.Web/Controllers/PublicacionesJsonController.cs) | [Captura](docs/evidencias/incremento-02/selector-etiquetas.png), [video](docs/evidencias/incremento-02/editor-abm.webm); relaciones N:M persistidas y comprobadas |
| 7. Web API REST con JWT | Implementado y probado localmente | [CRUD REST](src/Trazo.Web/Controllers/Api/PublicacionesApiController.cs), [autenticación](src/Trazo.Web/Controllers/Api/AuthApiController.cs), [validación JWT](src/Trazo.Web/Services/JwtLocal.cs) | [Colección Postman](docs/api/Trazo.postman_collection.json), [19 solicitudes y 31 aserciones](docs/evidencias/incremento-04/postman-resultados.json), [pruebas de autenticación y permisos](tests/Trazo.Tests/ApiIntegrationTests.cs) |

## Pruebas

```powershell
./tools/Probar.ps1                 # Unitarias; omite las que necesitan MySQL
./tools/Probar.ps1 -Integracion     # Suite completa, con Docker iniciado
```



El [workflow de GitHub Actions](.github/workflows/ci.yml) compila Vue, comprueba que el bundle versionado corresponda a sus fuentes y ejecuta las pruebas .NET con MySQL. La ejecución remota de esta actualización queda pendiente del push; no se presenta como CI aprobada. Estas verificaciones no equivalen a una auditoría de seguridad ni cubren funcionalidades aún no construidas.

Con la web y MySQL encendidos, se puede repetir el recorrido de navegador en Windows con Edge:

```powershell
npm ci
npx playwright install ffmpeg
npm run test:browser
node tools/Probar-Medios.mjs
node tools/Probar-Comunidad.mjs
node tools/Probar-Reportes.mjs
```

Los dos primeros recorridos usan la cuenta demo Ana y crean/eliminan sus publicaciones temporales en `trazo_dev`. El primero requiere suficientes publicaciones para una segunda página (la semilla original incluye 12 de Ana). El segundo requiere además FFmpeg de `Preparar-Medios.ps1`. Generan video, capturas y metadatos de red sin cookies ni tokens en `docs/evidencias/incremento-02/` e `incremento-03/`, respectivamente. El FFmpeg de Playwright graba las evidencias; el portable de Trazo procesa los archivos de publicaciones.

El recorrido de comunidad usa Ana y Bruno para comprobar favoritos, seguimiento, cambios de privacidad y pantallas móviles. Crea y elimina una publicación temporal y conserva los seguimientos preexistentes. Guarda las evidencias en `docs/evidencias/incremento-05/`.

El recorrido de reportes usa Ana, Bruno y Admin. Conserva dos reportes resueltos y archiva la publicación ficticia al terminar; cada ejecución agrega un caso de demostración. Guarda video, capturas y registro HTTP en `docs/evidencias/incremento-06/`.

