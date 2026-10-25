# Desarrollo local: VS Code, MySQL y GitHub

Guía actualizada al incremento 08, 25/10/2026. Todos los comandos se ejecutan en la carpeta que contiene `Trazo.sln`, salvo indicación explícita.

## 1. Abrir el proyecto

En VS Code: **Archivo → Abrir carpeta** y seleccionar `Proyecto Integrador Final (LaBo2)`. Aceptar las extensiones recomendadas de C# y EditorConfig. Abrir **Terminal → Nueva terminal** y seleccionar **PowerShell 7** (comando `pwsh`), no Windows PowerShell 5.1.

Comprobar las herramientas:

```powershell
dotnet --version
$PSVersionTable.PSVersion
docker version
git --version
```

El SDK fijado es 10.0.400; `global.json` permite parches posteriores de esa banda. Los scripts requieren PowerShell 7.2 o superior. Docker Desktop debe mostrar el motor Linux funcionando. Vue ya está integrado; La API JWT y la colección Postman ya están disponibles; ver docs/api.md.

## 2. Preparar la base de datos

```powershell
./tools/Preparar-Entorno.ps1
docker compose ps
```

El script crea `.env` con contraseñas aleatorias si aún no existe, configura `ConnectionStrings:Trazo` en User Secrets, restaura herramientas y paquetes y levanta MySQL 8.4. Esperar a que `docker compose ps` indique `healthy`.

| Configuración | Valor |
| --- | --- |
| Host local | `127.0.0.1` |
| Puerto publicado | `3307` |
| Usuario de la aplicación | `trazo` |
| Contraseña | Valor de `TRAZO_DB_PASSWORD` en tu `.env` local |
| Base de desarrollo | `trazo_dev` |
| Base descartable de pruebas | `trazo_tests` |
| Persistencia Docker | Volumen `trazo_mysql_data` |

No modifica Inmobiliaria_JM ni las bases de XAMPP. Dentro del contenedor MySQL usa 3306; desde Windows se usa 3307 para evitar conflictos.

**Workbench:** crear una conexión Standard TCP/IP con esos datos, pulsar Test Connection y guardar la contraseña en su almacén local si se desea. Después de inicializar el modelo, actualizar Schemas y abrir `trazo_dev`. No incluir la contraseña en capturas de evidencia.

No editar ni borrar `.env` para cambiar la contraseña de una base ya creada: MySQL solo toma las variables iniciales al crear el volumen. Si se pierde el archivo, primero recuperar las credenciales locales; regenerarlo no cambia la contraseña almacenada en MySQL.

## 3. Crear tablas y datos de demostración

```powershell
dotnet dev-certs https --trust
dotnet run --project src/Trazo.Web --launch-profile https -- --seed-demo
```

La primera línea confía el certificado HTTPS de desarrollo y puede abrir una confirmación de Windows. La segunda aplica la migración y crea roles, usuarios y publicaciones ficticias; al terminar devuelve el control a la terminal.

Las credenciales están en el [README](../README.md#cuentas-de-demostración). Volver a ejecutar la semilla conserva usuarios existentes y sus contraseñas; no es un comando para restablecer datos editados. El modo demo solo se permite en Development. Para inicializar esquema y roles sin datos demo, usar `--initialize` en lugar de `--seed-demo`.

La fuente del esquema son las migraciones de EF. No ejecutar además el SQL a mano en la misma base: `database/001-modelo-inicial.sql` es una exportación de consulta/entrega del esquema inicial, no un segundo paso de instalación.

## 4. Ejecutar y depurar

```powershell
dotnet run --project src/Trazo.Web --launch-profile https
```

Abrir **https://localhost:7280**. HTTP en 5079 redirige a HTTPS. Mantener esa terminal abierta; **Ctrl+C** detiene la web.

Alternativamente, detener antes la ejecución de terminal y pulsar **F5 → Trazo: depurar MVC**. VS Code compila e inicia la aplicación con el entorno Development. No ejecutar ambas opciones al mismo tiempo en los mismos puertos. **Ctrl+Shift+B** compila sin iniciar el servidor.

Recorrido sugerido:

1. Sin sesión, recorrer Explorar, buscar una etiqueta y abrir la segunda página.
2. Ingresar como Ana, abrir Mi estudio y Editar mi perfil. Cambiar presentación, tema y contacto; cargar un PNG/JPG/WebP propio de hasta 2 MB.
3. Abrir `/p/ana` con y sin sesión para observar los permisos. Ana ve sus borradores y privados; otra cuenta no.
4. Ingresar como administrador y usar Administrar para crear, editar y eliminar una etiqueta nueva. Una etiqueta asociada a publicaciones no se elimina: puede desactivarse.

Desde **Mi estudio → Administrar mis publicaciones** se accede a `/panel/publicaciones`. Crear una actualización, completar título/texto, buscar etiquetas y guardar. Elegir Borrador para conservarla sin difusión, Publicada para habilitar su lectura según privacidad, o Archivada para retirarla sin eliminarla. Todas esas operaciones, los filtros y el paginado se resuelven vía AJAX sin recargar.

Eliminar requiere confirmación y borra de forma permanente una publicación sin reportes, incluidos sus recursos locales. Si otra pestaña ya la cambió, se muestra un conflicto y se conserva el texto local para poder copiarlo antes de recargar su versión actual. Las obras necesitan al menos un recurso listo para publicarse. Guardar el borrador, agregar imágenes/GIF/MP4 o YouTube debajo del formulario y después publicar. Para GIF/MP4 ejecutar antes `./tools/Preparar-Medios.ps1`; ver [preparación, límites y almacenamiento](medios.md).

### Editar los componentes Vue

Los componentes están en `src/Trazo.Web/ClientApp/publicaciones/`. VS Code recomienda la extensión Vue Official. Para ejecutar el proyecto como evaluador no hace falta Node: el bundle compilado está incluido. Para modificar esos componentes se necesita Node >=22.12:

```powershell
node --version
npm ci
npm run build
```

Vite genera `src/Trazo.Web/wwwroot/js/publicaciones/editor.js`. No editar ese archivo a mano: editar `.vue` / `.js` en ClientApp, recompilar y versionar tanto fuentes como bundle. No hay que dejar un servidor Node funcionando.

Durante el desarrollo, `npm run watch` recompila al guardar; mantenerlo en otra terminal y recargar el navegador después de cambiar el código. Esto es independiente del funcionamiento AJAX del editor, que no recarga al operar publicaciones. Los estilos específicos están en `wwwroot/css/publicaciones.css`.

Los incrementos 02 a 06 no cambiaron el esquema. El incremento 07 agrega el historial de estados de cuentas. Para actualizar una instalación existente, detener la web y ejecutar `dotnet run --project src/Trazo.Web --launch-profile https -- --initialize`; luego iniciarla normalmente. Aplica las migraciones incluidas sin recrear la base ni sembrar datos demo. No ejecutar los SQL manualmente además de las migraciones.

## 5. Probar cambios

```powershell
./tools/Probar.ps1
./tools/Probar.ps1 -Integracion
```

El segundo comando usa MySQL real y **borra/recrea `trazo_tests`**. No guardar información útil en esa base. El código rechaza otra base o un servidor remoto. La aplicación puede seguir ejecutándose contra `trazo_dev` mientras se prueban las integraciones.

Resultados esperados de este incremento: sin `-Integracion`, 27 aprobadas y 49 omitidas; con `-Integracion`, 76 aprobadas y ninguna omitida. Los informes TRX se generan en `tests/Trazo.Tests/TestResults/` y no se suben al repositorio. La versión del runtime puede ser un parche anterior a la de paquetes; la compatibilidad se comprueba mediante compilación y pruebas.

### Recorrido de navegador y evidencia

Con Windows, Edge instalado, la aplicación ejecutándose en HTTPS 7280, la cuenta demo Ana sin cambiar su contraseña y la semilla con sus 12 publicaciones:

```powershell
npm ci
npx playwright install ffmpeg
npm run test:browser
```

El script crea una publicación temporal en `trazo_dev`, la edita, busca etiquetas, prueba la segunda página, comprueba el ancho móvil y la elimina al terminar. No recrea la base. Necesita suficientes entradas de Ana para paginar; repetir la semilla no restablece entradas eliminadas a mano. Usar los datos demo para este recorrido. En caso de interrupción abrupta, revisar si quedó una entrada con el título “Estudio de movimiento · prueba …”.

Genera capturas, video WebM y un registro de métodos/URL/estados HTTP sin contraseñas ni tokens en `docs/evidencias/incremento-02/`. El contexto de pruebas acepta únicamente para ese recorrido el certificado local de desarrollo; el navegador habitual sigue usando el certificado confiado en el paso 3.

Para restaurar exactamente las dependencias fijadas:

```powershell
dotnet restore --locked-mode Trazo.sln
dotnet build Trazo.sln --no-restore
```

## 6. Migraciones futuras

Después de un cambio acordado del modelo, detener la web, generar una migración con nombre descriptivo y revisar su SQL antes de aplicarla:

```powershell
dotnet tool restore
dotnet ef migrations add NombreDelCambio --project src/Trazo.Web
dotnet run --project src/Trazo.Web --launch-profile https -- --initialize
```

No hace falta crear otra migración para instalar el proyecto: ambas están incluidas. Actualizar el DER y la documentación junto con los cambios de relaciones.

## 7. Detener y volver a trabajar

Cerrar la web con Ctrl+C o detener el depurador. Para parar MySQL sin borrar datos:

```powershell
docker compose stop
```

Para retomar: abrir Docker Desktop, ejecutar `docker compose up -d --wait` y luego iniciar la web. Los avatares quedan en `src/Trazo.Web/App_Data/avatars`; no forman parte de Git. La base y los archivos deben respaldarse por separado cuando haya datos que conservar.

Si aparece “address already in use”, comprobar si otra terminal o el depurador ya ejecutan Trazo. Si aparece “access denied” de MySQL, revisar la correspondencia entre `.env`, User Secrets y el volumen, sin borrar datos para resolverlo. Si el certificado no es confiable, repetir el paso del certificado y reabrir el navegador. Si falta la conexión, ejecutar nuevamente el script de preparación.

## 8. Commit y push del incremento actual

El repositorio ya está creado y `origin` apunta al proyecto de GitHub. El primer incremento fue subido; no volver a ejecutar `git init` ni `git remote add`.

Antes de subir, revisar `git status --short`: no deben aparecer `AGENTS.md`, `.env`, `tools/ffmpeg`, `node_modules`, `bin`, `obj`, `App_Data`, `TestResults` ni archivos con contraseñas reales. Los datos demo del README son deliberadamente públicos. La licencia del código propio se decidirá por separado; publicar el repositorio no concede automáticamente una licencia de reutilización.

```powershell
git add .
git diff --cached --stat
git commit -m "feat: obras destacadas y vista previa del portafolio"
git push
```

El acceso a GitHub se completa mediante su autenticación habitual, nunca guardando un token en la URL. Después, revisar la pestaña Actions: la suite incorpora comprobaciones JWT con una clave efímera de pruebas; conserva compilación Vue, verificación del bundle y pruebas .NET/MySQL/FFmpeg.

**Parada acordada:** confirmar el commit/push antes de comenzar el próximo incremento. No realizar todavía un despliegue público de la aplicación.

Para repetir la evidencia multimedia, con FFmpeg portable preparado y la web encendida, ejecutar `node tools/Probar-Medios.mjs`. Crea y elimina una publicación temporal propia y guarda capturas/video/red en `docs/evidencias/incremento-03/`. El iframe de YouTube se simula para comprobar la carga voluntaria sin contactar al proveedor.

Para la API, ejecutar una vez `./tools/Preparar-Api.ps1` y reiniciar Trazo. La clave queda en User Secrets. Importar `docs/api/Trazo.postman_collection.json` en Postman y ejecutar el Runner completo según [la guía de API](api.md); el recorrido final revoca las sesiones demo de Ana. La web continúa usando cookies. La API no necesita tablas adicionales; aplicar las migraciones generales del proyecto como se indicó arriba.

Favoritos y seguidos se administran desde Mi estudio. No requieren migraciones ni herramientas adicionales. Para repetir el recorrido local de este módulo: `node tools/Probar-Comunidad.mjs`. Ver [reglas, pantallas y pruebas](incremento-05.md).

Obras destacadas se administra desde Mi estudio. El catálogo admite obras publicadas con recursos listos, públicas o para registrados; permite elegir seis posiciones. Editar mi perfil incluye una vista previa de apariencia. El incremento 08 no cambia el esquema. Recorrido local: `node tools/Probar-Portafolio.mjs`; ver [reglas y evidencias](incremento-08.md).

Reportes: desde una publicación ajena, elegir Reportar publicación. Cada creador consulta Mis reportes; Admin accede a Moderar reportes desde Mi estudio. Una obra oculta permite al autor solicitar revisión si sigue publicada y con privacidad pública/registrados. Ver [reglas y evidencias](incremento-06.md). El recorrido `node tools/Probar-Reportes.mjs` conserva dos reportes resueltos y archiva su publicación demo; no borra el historial. En la bandeja administrativa seleccionar estado Todos para ver esos casos.
