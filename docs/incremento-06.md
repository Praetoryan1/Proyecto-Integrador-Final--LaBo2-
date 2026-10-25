# Incremento 06 · Reportes y moderación

Fecha: **25/10/2026**. Base: `4700101`, incremento de comunidad confirmado en `main` y `origin/main`. No cambia el esquema: se utiliza Reporte y sus relaciones del DER inicial.

## Recorrido funcional

1. Un creador abre una publicación ajena disponible y elige **Reportar publicación**. Selecciona motivo y escribe una explicación de 10 a 2000 caracteres.
2. **Mi estudio → Mis reportes** muestra únicamente sus solicitudes, con búsqueda, filtro de estado y páginas de 12. Presentar un reporte no oculta automáticamente la obra.
3. Una cuenta con rol Administrador entra a **Mi estudio → Moderar reportes**. Puede desestimar o, cuando corresponde, ocultar la publicación. Debe registrar una explicación.
4. El autor ve el motivo de ocultamiento en el detalle de su publicación, sin identidad ni descripción original del denunciante. Editar el contenido no revierte la moderación.
5. Si la obra sigue publicada y tanto ella como el perfil admiten acceso público o a registrados, el autor puede **Solicitar revisión**. El administrador puede desestimar esa solicitud o restaurar la visibilidad.

Las decisiones registran `ResueltoPorId`, `ResueltoUtc`, `Resolucion` y estado. Un reporte resuelto no se modifica: una revisión posterior es otro registro. Restaurar se permite únicamente al resolver una solicitud de revisión del autor de una obra oculta. No se modifican visibilidad ni estado editorial al moderar.

## Permisos y reglas

- El actor proviene de la sesión. No se reciben denunciante, resolutor ni estado final desde el formulario.
- Solo el rol Administrador accede a la bandeja, resuelve y consulta los medios de moderación. Las rutas no usan JWT; mantienen cookies y antiforgery.
- Hay cinco motivos de denuncia: derechos de autor, contenido ofensivo, spam o engaño, datos personales y otro. «Solicitud de revisión» queda reservada al autor de una obra oculta.
- Cada cuenta mantiene como máximo un reporte pendiente por publicación mediante el servicio. Un envío repetido vuelve al registro existente; altas simultáneas pueden devolver 409 para recargar, sin duplicarlo. No hay una nueva restricción única en MySQL para este caso: escrituras directas a la base no pasan por la regla.
- Las altas están limitadas a 20 solicitudes por minuto y dirección IP en el proceso actual. Este límite es básico, compartido por usuarios detrás de una misma IP y no distribuido entre servidores.
- Motivos, detalles y resoluciones se muestran como texto codificado, sin HTML ejecutable. La interfaz recuerda no incluir datos personales del denunciante en una resolución compartida.
- Los reportes históricos siguen consultables aunque su publicación ya no pueda leerse; el título, texto y recursos actuales se omiten según permisos. El texto que el usuario ya incluyó en su denuncia permanece como parte del registro.
- La administración puede revisar contenido publicado público o para registrados, incluso oculto por moderación. No accede mediante estas rutas a obras privadas, perfiles privados, borradores, archivados o contenido de cuentas suspendidas. Esta restricción se aplica también a búsqueda, archivos, miniaturas, HEAD y rangos de video.
- La vista administrativa muestra el contenido **actual**, no una copia histórica. Una edición posterior puede diferir de lo que vio el denunciante. No se implementa un archivo forense de versiones.

## Concurrencia y persistencia

Crear una solicitud toma la fila de la publicación mediante una actualización condicionada por `Publicacion.Version`, antes de insertar su FK. El alta y el cambio de versión se confirman en una transacción. Así se evita duplicar solicitudes pendientes o aceptar un alta sobre una publicación que se modificó/eliminó simultáneamente.

Resolver comprueba la versión presentada y el estado pendiente. La modificación del reporte y la publicación se guarda en una única transacción de EF Core; una colisión devuelve 409 y revierte el conjunto. Un cambio de perfil se vuelve a comprobar al decidir si el contenido es moderable. Los formularios no ejecutan cambios por GET y no permiten decisiones arbitrarias enviando otro valor numérico.

Las obras con reportes siguen sin poder eliminarse desde el editor; pueden archivarse. La retención legal definitiva de reportes y los procedimientos excepcionales quedan pendientes de la preparación productiva.

## Archivos principales

| Función | Archivo |
| --- | --- |
| Reglas, consultas y transacciones | [ReportesService.cs](../src/Trazo.Web/Services/ReportesService.cs) |
| Envío y consulta personal | [ReportesController.cs](../src/Trazo.Web/Controllers/ReportesController.cs) |
| Resolución y medios autorizados | [ModeracionController.cs](../src/Trazo.Web/Controllers/ModeracionController.cs) |
| Validación de entradas y proyecciones | [ReporteModels.cs](../src/Trazo.Web/Models/ReporteModels.cs) |
| Errores MVC con estado HTTP | [ReporteExceptionFilter.cs](../src/Trazo.Web/Controllers/ReporteExceptionFilter.cs) |
| Pantallas | [Reportes](../src/Trazo.Web/Views/Reportes), [Moderación](../src/Trazo.Web/Views/Moderacion/Detalle.cshtml) |
| Pruebas de integración | [ReportesIntegrationTests.cs](../tests/Trazo.Tests/ReportesIntegrationTests.cs) |
| Recorrido Edge | [Probar-Reportes.mjs](../tools/Probar-Reportes.mjs) |

| Método | Ruta | Uso |
| --- | --- | --- |
| GET / POST | `/publicaciones/{id}/reportar` | Formulario y alta de denuncia/revisión |
| GET | `/mis-reportes` / `/mis-reportes/{id}` | Listado y detalle propios |
| GET | `/administracion/reportes` / `/administracion/reportes/{id}` | Bandeja y detalle administrativo |
| POST | `/administracion/reportes/{id}/resolver` | Decisión con explicación y versión |
| GET / HEAD | `/administracion/reportes/{id}/medios/{recursoId}/archivo` | Archivo asociado a ese reporte, con autorización actual |
| GET / HEAD | `/administracion/reportes/{id}/medios/{recursoId}/vista-previa` | Miniatura con la misma autorización |

## Validación

`dotnet build Trazo.sln -c Release --no-restore`: compilación correcta, sin advertencias ni errores.

**66 pruebas aprobadas con MySQL real, 0 fallidas y 0 omitidas.** Las 5 pruebas nuevas comprueban el recorrido completo, roles/CSRF/validaciones, privacidad de contenido y medios, concurrencia, paginación, aislamiento entre cuentas y codificación HTML. Las listas se verifican con 13 pendientes: 12 resultados en la primera página y 1 en la segunda.

El recorrido de Edge comprobó denuncia → ocultamiento → solicitud del autor → restauración. El acceso anónimo a la publicación pasó de 200 a 404 y volvió a 200. Se revisaron visualmente las pantallas en escritorio y móvil de 390 px, sin desbordes ni errores JavaScript detectados.

- [Video de moderación](evidencias/incremento-06/moderacion.webm).
- [Formulario](evidencias/incremento-06/formulario-reporte.png), [consulta propia](evidencias/incremento-06/mi-reporte.png).
- [Bandeja](evidencias/incremento-06/bandeja.png) y [móvil](evidencias/incremento-06/bandeja-movil.png).
- [Decisión](evidencias/incremento-06/decision.png), [aviso al autor](evidencias/incremento-06/autor-oculta.png).
- [Revisión](evidencias/incremento-06/revision.png), [móvil](evidencias/incremento-06/revision-movil.png), [resolución](evidencias/incremento-06/resolucion.png).
- [Registro HTTP sin secretos](evidencias/incremento-06/recorrido.json), [resultados .NET](evidencias/incremento-06/pruebas-dotnet.json).

Para repetir:

```powershell
./tools/Probar.ps1 -Integracion
# Con la web iniciada, Node/Playwright y Edge disponibles:
node tools/Probar-Reportes.mjs
```

La suite .NET recrea únicamente `trazo_tests`. El navegador utiliza las cuentas demo Ana, Bruno y Admin de `trazo_dev`. **Conserva dos reportes resueltos y archiva su publicación ficticia al terminar**, porque las obras con reportes no se eliminan. Cada ejecución agrega un caso de demostración; no reinicializa la base. Los identificadores quedan en `recorrido.json`. Para ver este historial en la bandeja, seleccionar estado «Todos»; la vista inicial muestra pendientes.

## Parada para GitHub

No hay paquetes ni migraciones nuevos. Reiniciar la aplicación con el comando habitual para cargar los cambios:

```powershell
dotnet run --project src/Trazo.Web --launch-profile https
```

Después de revisar:

```powershell
git add .
git diff --cached --stat
git commit -m "feat: reportes y moderacion con revision del autor"
git push
```

No se ejecuta push automático. La CI remota queda pendiente. Próximo incremento propuesto: administración de suspensiones de cuentas; quedan también destacados/personalización pendiente y cierre académico (SQL demo, instalación desde clon limpio y defensa). Este bloque no habilita la apertura pública ni reemplaza la revisión legal y operativa.
