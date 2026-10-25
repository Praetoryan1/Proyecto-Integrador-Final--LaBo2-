# Incremento 07 · Administración del estado de cuentas

Fecha: **25/10/2026**. Base: `ddd309f`, incremento de reportes confirmado en `main` y `origin/main`.

## Uso

Ingresar como Admin y abrir **Mi estudio → Administrar cuentas**. El listado permite buscar por nombre de usuario, filtrar Activa/Suspendida y recorrer páginas de 12. El detalle reúne el estado y un historial también paginado.

Para suspender o reactivar, escribir un motivo de 10 a 1000 caracteres y pulsar el botón de confirmación correspondiente. El formulario explica el efecto antes de enviarlo. La propia cuenta y todas las cuentas con rol Administrador están protegidas; el panel no permite cambiarles el estado. No se agregaron asignación de roles, cambio de contraseñas, eliminación de cuentas ni acceso a perfiles privados.

Suspender impide nuevos inicios de sesión y hace que la autorización existente oculte perfil, avatar, publicaciones y medios. Las listas de favoritos/seguidos filtran ese contenido. La validación de cookies/JWT rechaza las credenciales anteriores en solicitudes posteriores; una operación ya en curso no se cancela retroactivamente.

Reactivar permite autenticarse de nuevo y vuelve a mostrar el contenido según su privacidad, estado editorial y moderación. **Las sesiones anteriores no recuperan validez.** No se alteran contraseñas, roles ni bloqueos temporales por intentos fallidos. El login mantiene un mensaje genérico; no expone el motivo administrativo a visitantes.

## Historial y consistencia

Se agregó la entidad de soporte `CambioEstadoCuenta`, tabla `CambiosEstadoCuenta`: cuenta afectada, administrador, estado anterior, estado nuevo, motivo y fecha UTC. Las FK apuntan a Usuario con borrado restringido. Un CHECK exige estados válidos y diferentes, y actor distinto de la cuenta afectada. El rol y la protección de administradores se validan en el servicio; los cambios de roles externos al panel requieren un procedimiento operativo separado.

Cada transición renueva `SecurityStamp` y `ConcurrencyStamp` de Identity. El historial, estado y sellos se guardan en una transacción de EF Core. El formulario presenta una versión y el backend la compara; una versión desactualizada o una operación simultánea devuelve 409 sin duplicar decisiones ni sobrescribir cambios. La pantalla conserva el motivo y exige volver a revisar antes de reenviar.

La aplicación solo agrega entradas al historial; no ofrece edición ni eliminación. Esto no lo hace inalterable para un operador con permisos directos en MySQL. No se afirma contar con auditoría externa o almacenamiento forense. El historial y los metadatos mínimos de cuenta son exclusivos del rol Administrador; no se muestran emails de acceso, hashes ni sellos de seguridad.

## Base de datos y DER

La migración [HistorialEstadoCuentas](../src/Trazo.Web/Data/Migrations/20261025191732_HistorialEstadoCuentas.cs) **agrega una tabla** y conserva las existentes. Está aplicada en `trazo_dev`; las pruebas también la aplicaron desde una base nueva. Hay ahora 17 tablas de modelo, más `__EFMigrationsHistory`.

- [SQL incremental](../database/002-historial-estado-cuentas.sql), posterior al SQL inicial.
- [DER actual](der.png), [Mermaid](der.mmd), [vista de cuentas](der-cuentas.png) y [fuente SVG](der-cuentas.svg).
- Regenerar la imagen actual: `node tools/Generar-Der.mjs` con Playwright/Edge instalados. El generador Python de la entrega preliminar conserva su función documental y ahora escribe `der-preliminar.png`, sin sobrescribir el DER actual.

Para actualizar otra copia local, detener Trazo y ejecutar en la terminal PowerShell de VS Code:

```powershell
dotnet run --project src/Trazo.Web --launch-profile https -- --initialize
dotnet run --project src/Trazo.Web --launch-profile https
```

La primera línea aplica las migraciones incluidas y asegura los roles; termina sin abrir la web ni sembrar otra vez los datos demo. No crear otra base ni otra migración, y no ejecutar además el SQL manualmente sobre la misma base.

## Código y rutas

| Responsabilidad | Fuente |
| --- | --- |
| Consultas, protección y transacciones | [CuentasAdminService.cs](../src/Trazo.Web/Services/CuentasAdminService.cs) |
| Rol, validación y formularios | [CuentasAdminController.cs](../src/Trazo.Web/Controllers/CuentasAdminController.cs) |
| DTO y validación de entrada | [CuentasAdminModels.cs](../src/Trazo.Web/Models/CuentasAdminModels.cs) |
| Pantallas | [Listado](../src/Trazo.Web/Views/CuentasAdmin/Index.cshtml), [detalle](../src/Trazo.Web/Views/CuentasAdmin/Detalle.cshtml) |
| Entidad y restricciones | [Modelo](../src/Trazo.Web/Domain/Modelo.cs), [DbContext](../src/Trazo.Web/Data/TrazoDbContext.cs) |
| Pruebas | [CuentasAdminIntegrationTests.cs](../tests/Trazo.Tests/CuentasAdminIntegrationTests.cs) |

`GET /administracion/cuentas` lista con `q`, `estado` y `pagina`. `GET /administracion/cuentas/{id}` muestra el detalle y la página del historial. `POST /administracion/cuentas/{id}/estado` exige cookie de administrador y antiforgery. Actor e historial no provienen de campos del cliente. No se agregó un endpoint JWT administrativo.

## Validación y evidencia

Compilación Release correcta: 0 advertencias y 0 errores. `dotnet ef migrations has-pending-model-changes` confirmó que el modelo y la última migración coinciden.

**71 pruebas aprobadas con MySQL real, 0 fallidas y 0 omitidas.** Las cinco nuevas cubren suspensión/reactivación, cookies y JWT, rechazo de login, favoritos, protección de administradores, roles, CSRF, valores inválidos, XSS, concurrencia y paginado. Las listas se comprobaron con 13 resultados en páginas de 12 + 1, tanto cuentas filtradas como historial. Se comprobó el CHECK en MySQL.

El recorrido de Edge suspendió y reactivó la cuenta demo Bruno, comprobó perfil 404/200, revocación de cookie/JWT y nuevo login. Conservó los dos registros de historial y dejó a Bruno **activo**. Cada ejecución agrega dos decisiones de demostración y revoca las sesiones de esa cuenta demo. No toca cuentas reales ni recrea la base.

- [Video](evidencias/incremento-07/cuentas.webm), [cuentas](evidencias/incremento-07/cuentas.png).
- [Confirmación](evidencias/incremento-07/confirmar-suspension.png), [cuenta suspendida](evidencias/incremento-07/cuenta-suspendida.png).
- [Móvil](evidencias/incremento-07/cuenta-movil.png), [historial](evidencias/incremento-07/historial.png), [administrador protegido](evidencias/incremento-07/administrador-protegido.png).
- [Recorrido HTTP sin credenciales](evidencias/incremento-07/recorrido.json), [resumen .NET](evidencias/incremento-07/pruebas-dotnet.json).

Las capturas de escritorio/móvil y la nueva vista del DER fueron revisadas visualmente. No se detectaron errores JavaScript ni desbordes a 390 px. Para repetir: `./tools/Probar.ps1 -Integracion` y, con la web iniciada y la API local preparada, `node tools/Probar-Cuentas.mjs`.

## Parada para GitHub

```powershell
git add .
git diff --cached --stat
git commit -m "feat: suspension de cuentas con historial y revocacion de sesiones"
git push
```

No se ejecuta push automático; la CI remota queda pendiente. Siguiente bloque propuesto: obras destacadas y revisión de la personalización del portafolio. Luego quedan datos SQL demo y verificación de instalación/defensa. La preparación productiva, canal externo de reclamos, recuperación de cuentas y políticas de retención siguen pendientes.
