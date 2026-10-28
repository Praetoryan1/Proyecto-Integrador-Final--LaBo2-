# API JWT de evaluación

El incremento 04 agrega una Web API REST independiente del panel Vue. La API administra **las publicaciones del creador autenticado**, con listado paginado y búsqueda de etiquetas. Reutiliza `PublicacionesService` y `EtiquetasConsulta`: mantiene propiedad, validaciones, estados editoriales, concurrencia y eliminación de archivos/reportes del recorrido MVC.

La sesión web usa cookies y antiforgery; `/api/v1/` exige **Authorization: Bearer**. Una cookie no autoriza estas rutas y el JWT no abre una sesión MVC. El panel Vue sigue usando sus endpoints `/panel-json/`.

## 1. Preparar desde VS Code

Con el entorno y MySQL ya preparados, ejecutar en PowerShell 7 desde la raíz:

```powershell
./tools/Preparar-Api.ps1
dotnet run --project src/Trazo.Web --launch-profile https
```

Si Trazo ya estaba iniciado, detenerlo con Ctrl+C antes de volver a iniciarlo. El script genera 32 bytes aleatorios y guarda la clave en **.NET User Secrets** mediante entrada estándar; no la imprime ni la incorpora a argumentos del proceso. Si ya existe una clave, la conserva. No hay claves JWT en `appsettings.json`, la colección ni el repositorio. En el equipo de Jonathan este paso ya fue realizado.

La API funciona solo en **Development** y **Testing**. En Production/Staging devuelve 404 aun si existe una clave; sin clave válida en desarrollo devuelve 503, con instrucciones de preparación. La web MVC sigue disponible sin configurar la API. No hay cambios de tablas ni migraciones. El emisor por contraseña es para evaluación local; antes de ofrecer una API pública se deberá integrar un proveedor OAuth/OIDC mantenido. No se habilita este emisor en producción.

## 2. Importar y ejecutar en Postman

1. Mantener la web y Docker/MySQL iniciados. Las cuentas demo y las 12 publicaciones originales de Ana deben existir para comprobar la segunda página.
2. En Postman de escritorio, usar **Import** y seleccionar [Trazo.postman_collection.json](api/Trazo.postman_collection.json).
3. Seleccionar la colección **Trazo · API JWT · Incremento 04** y abrir **Run / Collection Runner**, ejecución local y una iteración. Conservar el orden de las 19 solicitudes. No hace falta configurar un environment ni copiar tokens a mano.
4. Mantener `baseUrl = https://localhost:7280` para desarrollo; si se usa la [demo completa de entrega](entrega-y-defensa.md), cambiarlo a `https://localhost:7281`. Conservar la contraseña demo de la colección. Ejecutar con las cuentas ficticias, nunca con cuentas de trabajo reales. Se puede desactivar el uso de cookies almacenadas en el Runner para mostrar la independencia del Bearer.
5. El resultado esperado es **19 solicitudes y 31 aserciones aprobadas**. Los códigos 400, 401, 404 y 409 de los casos negativos son esperados, no fallos del recorrido.

La colección crea y elimina una publicación temporal privada de Ana. Al terminar **revoca todas las sesiones de Ana, tanto cookies como JWT**; será necesario volver a ingresar en la web si estabas usando esa cuenta. Los tokens y versiones pasan entre solicitudes mediante variables locales del Runner y se limpian al finalizar; no se guardan como valores de la colección. Usar el Runner completo: el envío manual aislado no conserva ese contexto local.

Si se interrumpe después del alta y antes de la baja, entrar al panel como Ana, buscar `Colección Postman` y eliminar únicamente la publicación temporal del recorrido interrumpido. No reinicializar la base para limpiarla. Si aparece 429 tras muchas repeticiones, esperar un minuto; no desactivar el limitador.

### Certificado HTTPS local

Si Postman no confía en el certificado de desarrollo, exportar **solo el certificado público**:

```powershell
New-Item -ItemType Directory -Path tmp -Force | Out-Null
dotnet dev-certs https --export-path tmp/localhost.crt --format PEM
```

En **Settings → Certificates → CA certificates**, agregar ese archivo PEM. No usar `--no-password`, `--password` ni exportar una clave privada para este paso. El archivo queda dentro de `tmp`, ignorado por Git. Postman permite agregar certificados CA para verificar servidores locales; ver su [documentación de certificados](https://learning.postman.com/docs/use/send-requests/authorization/certificates/).

La evidencia automatizada incluida se obtuvo con Newman 6.2.3 usando una excepción de validación del certificado exclusivamente contra localhost. No se afirma haber ejecutado la interfaz gráfica de Postman. Para repetirla, la colección se importa en Postman; Newman no es dependencia del proyecto ni del sitio.

## 3. Contrato HTTP

Base local: `https://localhost:7280`. Cuerpos JSON con `Content-Type: application/json`. No enviar el token por URL ni almacenarlo en el navegador o en un archivo versionado.

| Método y ruta | Autenticación | Resultado |
| --- | --- | --- |
| POST `/api/v1/auth/token` | Email/password, solo emisor local | 200 con `accessToken`, `tokenType: Bearer`, `expiresIn: 600`; no crea cookies |
| GET `/api/v1/publicaciones?q=&estado=&pagina=1` | JWT y rol Creador | Publicaciones propias, hasta 12 por página |
| GET `/api/v1/publicaciones/{id}` | JWT, Creador y propiedad | DTO editable con etiquetas y recursos asociados |
| POST `/api/v1/publicaciones` | JWT y Creador | Alta; 201 y cabecera Location |
| PUT `/api/v1/publicaciones/{id}` | JWT, Creador y propiedad | Edición con versión vigente; 200 y nueva versión |
| DELETE `/api/v1/publicaciones/{id}` | JWT, Creador y propiedad | Cuerpo `{ "version": "..." }`; 204 |
| GET `/api/v1/etiquetas?q=Anima&pagina=1` | JWT y Creador | Etiquetas activas, hasta 10 por página |
| POST `/api/v1/auth/revocar-sesiones` | JWT válido | Cambia SecurityStamp; 204, invalida todos los tokens y cookies anteriores de esa cuenta |

POST acepta `titulo`, `texto`, `tipo`, `estadoEditorial`, `visibilidad` y `etiquetaIds`; PUT agrega `version`. Tipos: Obra=0, Actualización=1; estados: Borrador=0, Publicada=1, Archivada=2; visibilidad: Pública=0, Registrados=1, Privada=2. No se aceptan cambios de autor ni de moderación. Una obra sin recurso listo no puede publicarse. `Version` es un token opaco de concurrencia: devolver el recibido, sin inventarlo.

Los listados devuelven `items`, `numero`, `tamano`, `total`, `paginas`, `anterior` y `siguiente`. La consulta filtra al propietario antes de contar/paginar. Los IDs de otros autores devuelven 404 incluso al administrador. Esta API es de gestión propia; la exploración pública sigue disponible desde MVC.

La administración de medios se realiza por la web. Las URLs de archivos incluidas en el DTO son rutas de lectura MVC: respetan sesión cookie y visibilidad pública, y **no consumen el JWT de esta API**. No se ofrece todavía una API Bearer de carga/descarga multimedia.

### Prueba manual sin imprimir el JWT

```powershell
$baseUrl = 'https://localhost:7280'
$respuesta = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/v1/auth/token" -ContentType 'application/json' -Body '{"email":"ana@trazo.test","password":"Trazo.Demo2026!"}'
$cabeceras = @{Authorization = "Bearer $($respuesta.accessToken)"}
Invoke-RestMethod -Uri "$baseUrl/api/v1/publicaciones?pagina=1" -Headers $cabeceras
Remove-Variable respuesta,cabeceras
```

El certificado debe estar confiado con `dotnet dev-certs https --trust`. Eliminar las variables locales no revoca el token: vence por tiempo o al cambiar el sello de seguridad. Cerrar sesión MVC elimina esa cookie, pero no revoca automáticamente JWT ya emitidos; la ruta `revocar-sesiones` invalida ambos tipos.

## 4. Controles y límites

- Firma HS256 con al menos 256 bits de clave; validación de algoritmo, tipo `at+jwt`, emisor `trazo-local`, audiencia `trazo-api-evaluacion` y vencimiento. Duración de 10 minutos y tolerancia temporal de 30 segundos.
- En cada solicitud se consulta usuario activo, bloqueo Identity y sello de seguridad; se sustituyen los roles del token por los vigentes de la base. Quitar Creador impide el CRUD con 403, aunque el token lo incluyera al emitirse. Una cuenta suspendida o un token revocado devuelve 401.
- Login usa verificación de contraseña y bloqueo Identity después de 5 fallos; el limitador compartido de cuentas admite 20 solicitudes por IP/minuto. El emisor no admite cuentas con segundo factor habilitado: requerirían otro flujo. No se crean refresh tokens.
- `IgnoreAntiforgeryToken` está limitado a los controladores Bearer y al emisor que no crea sesión cookie. MVC y el panel conservan antiforgery. No hay CORS abierto ni aceptación de tokens por query string.
- JSON de publicaciones limitado a 100 KB; emisión a 10 KB. Las respuestas usan `no-store`. No se guardan ni exportan JWT en las evidencias; evitar subir respuestas completas, capturas de tokens o archivos privados de Postman.

400 indica validación; 401, credencial ausente/incorrecta/vencida/revocada; 403, falta de rol; 404, entidad no disponible/ajena o API deshabilitada por entorno; 409, versión o relación incompatible; 429, límite de solicitudes; 503, clave local sin configurar. La API conserva los errores ProblemDetails de negocio y validación; los rechazos Bearer pueden tener cuerpo vacío y cabecera `WWW-Authenticate: Bearer`.

Diseño basado en las guías oficiales de [JWT Bearer en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) y [selección explícita de esquema](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/authorize-with-a-specific-scheme?view=aspnetcore-10.0). Para las comprobaciones de la colección, ver [scripts de Postman](https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/) y [Collection Runner](https://learning.postman.com/docs/tests-and-scripts/running-collections/intro-to-collection-runs/).
