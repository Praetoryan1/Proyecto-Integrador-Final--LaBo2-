# Del DER a MySQL

El [DER lógico](der.png) y su [fuente Mermaid](der.mmd) describen 7 entidades principales: Usuario, Perfil, EnlacePerfil, Publicacion, RecursoMultimedia, Etiqueta y Reporte. PublicacionEtiqueta, Favorito, Seguimiento, Rol y UsuarioRol completan las asociaciones y autorización. Se conserva el alcance de la propuesta; tener persistencia no implica que todos esos módulos tengan interfaz.

El incremento 07 agrega una entidad de soporte, **CambioEstadoCuenta**, para conservar las decisiones de suspensión/reactivación. Tiene dos relaciones 1:N con Usuario: cuenta afectada y administrador que decide. Ver [vista ampliada](der-cuentas.png), [migración](../src/Trazo.Web/Data/Migrations/20261025191732_HistorialEstadoCuentas.cs) y [SQL incremental](../database/002-historial-estado-cuentas.sql). La entrega preliminar se conserva como documento histórico.

La implementación está en [Modelo.cs](../src/Trazo.Web/Domain/Modelo.cs), [TrazoDbContext](../src/Trazo.Web/Data/TrazoDbContext.cs) y su [migración](../src/Trazo.Web/Data/Migrations/20261021051003_ModeloInicial.cs). El [SQL generado](../database/001-modelo-inicial.sql) contiene la definición física exacta.

## Correspondencia

| Entidad lógica | Tabla física / observación |
| --- | --- |
| Usuario | `Usuarios`; hereda IdentityUser |
| Perfil | `Perfiles`; UsuarioId es PK y FK, relación 1:1 |
| EnlacePerfil | `EnlacesPerfil`; varios enlaces por perfil |
| Publicacion | `Publicaciones`; varias publicaciones por autor |
| RecursoMultimedia | `Recursos`; varios recursos ordenados por publicación |
| Etiqueta | `Etiquetas`; nombre normalizado único |
| PublicacionEtiqueta | `PublicacionEtiquetas`; PK compuesta, N:M entre publicación y etiqueta |
| Favorito | `Favoritos`; PK compuesta entre usuario y publicación |
| Seguimiento | `Seguimientos`; PK compuesta entre seguidor y seguido |
| Reporte | `Reportes`; publicación, denunciante y resolución opcional por usuario |
| CambioEstadoCuenta | `CambiosEstadoCuenta`; cuenta afectada, administrador, estados, motivo y fecha UTC |
| Rol | `Roles`; IdentityRole |
| UsuarioRol | `UsuarioRoles`; IdentityUserRole, PK compuesta |

Identity agrega `UsuarioClaims`, `RolClaims`, `UsuarioLogins` y `UsuarioTokens`. Son tablas técnicas; no se usan para inflar el mínimo docente. Hay **17 tablas del modelo**, más `__EFMigrationsHistory` para registrar migraciones.

Los nombres descriptivos del DER `EmailNormalizado`, `NombreUsuarioNormalizado` y `EmailConfirmado` corresponden a las propiedades Identity `NormalizedEmail`, `NormalizedUserName` y `EmailConfirmed`. El nombre normalizado de rol es `NormalizedName`. Los identificadores de usuario/rol son strings; los demás identificadores simples son bigint. Las enumeraciones se persisten como enteros y los campos terminados en Utc contienen fechas UTC. FechaAlta también se genera en UTC.

`Reporte.Motivo` se guarda como texto de hasta 80 caracteres. La fuente Mermaid refleja ese tipo; las imágenes del DER muestran el atributo sin especificar tipo. El incremento 06 valida cinco motivos de denuncia y reserva «Solicitud de revisión» al autor de una obra oculta.

## Invariantes implementadas

- Índices únicos para email normalizado, nombre de usuario, etiqueta normalizada y orden de recurso dentro de una publicación.
- Claves compuestas para evitar duplicar etiqueta de publicación, favorito, seguimiento y asignación de rol.
- CHECK de MySQL que impide seguirse a uno mismo.
- CHECK de recurso: fuente local o identificador YouTube, nunca ambos; su tipo debe corresponder a la fuente.
- Eliminación restringida de etiquetas usadas, autores con publicaciones y relaciones sensibles de reportes/seguimientos.
- Token de concurrencia `Publicacion.Version`: el editor lo recibe, devuelve al modificar/eliminar y renueva al guardar. Una versión desactualizada o una colisión real en EF devuelve 409, sin pisar el cambio anterior.
- El historial de cuentas exige transiciones Activa/Suspendida entre estados distintos y actor diferente del afectado. El servicio protege cuentas administradoras y renueva los sellos Identity al cambiar el estado; el historial y la transición son atómicos.

Usuario y Perfil se crean en una misma transacción durante el registro. La FK garantiza un propietario válido y unicidad del perfil; la obligación de crear el perfil al registrar un usuario pertenece al flujo de aplicación.

La lectura autorizada se compone con `IQueryable` antes de `Count`, `Skip` y `Take`. No basta con ocultar tarjetas en Razor. El backend respeta estado de cuenta, autoría, visibilidad de perfil/publicación y estados editorial/de moderación. El contacto tiene una restricción adicional y nunca utiliza automáticamente el correo de acceso.

## Estado funcional y pendientes

Desde el incremento 06 están implementados el envío de reportes, consulta propia, bandeja administrativa, ocultamiento, desestimación y revisión/restauración solicitada por el autor. Se registran resolutor, fecha y explicación. La regla de un pendiente por cuenta/publicación se aplica en el servicio con transacción y `Publicacion.Version`, sin un nuevo índice único. La resolución comparte la versión de publicación y se confirma atómicamente. Ver [reglas y evidencia](incremento-06.md). La política productiva de retención sigue pendiente; el incremento 07 incorpora la administración de suspensiones.

Desde el incremento 02 existen ABM, asociación de etiquetas y cambio de estado editorial de publicaciones. Se permite conservar etiquetas desactivadas ya asociadas, pero no agregar nuevas asociaciones con ellas. No se puede publicar una obra sin un recurso listo. Borrar una publicación con reportes devuelve 409 y permite optar por archivarla.

Desde el incremento 03, RecursoMultimedia tiene alta, edición, orden y baja operativas. `Bytes` registra el archivo normalizado más su miniatura; YouTube no usa almacenamiento local. La miniatura se deriva del mismo identificador de `ClaveAlmacenamiento` con sufijo `.preview.png`, sin columna adicional. Los recursos se persisten solo tras terminar el procesamiento. Las modificaciones renuevan `Publicacion.Version`; la baja de una publicación sin reportes retira sus recursos y luego elimina los archivos. Ver [coordinación y límites](medios.md).

Desde el incremento 05, Favorito y Seguimiento tienen altas/bajas y listas personales operativas. El actor se obtiene de la sesión y las claves compuestas evitan duplicados simultáneos. Las consultas y contadores filtran la disponibilidad del destino antes de paginar; las relaciones pueden conservarse mientras el destino esté oculto, salvo que su propietario elija limpiar las no disponibles. Ninguna relación concede acceso adicional. Ver [reglas y pruebas](incremento-05.md).

No hubo cambios de tablas ni relaciones en los incrementos 02 a 06. El incremento 07 incorpora el historial administrativo, su segunda migración y la extensión del DER. La administración de suspensiones/reactivaciones ya está operativa; queda precisar retención y procedimientos productivos. El estado de moderación no se modifica desde el editor, ni puede elegirse otro autor enviando campos adicionales.
