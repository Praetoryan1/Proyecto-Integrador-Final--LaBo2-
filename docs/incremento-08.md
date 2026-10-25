# Incremento 08 · Obras destacadas y apariencia del portafolio

Fecha: **25/10/2026**. Base: `4be5195`, incremento de cuentas confirmado en `main` y `origin/main`.

## Uso

1. Desde **Mi estudio → Obras destacadas**, buscar una obra propia por título y asignarle una posición del 1 al 6.
2. La posición menor aparece primero. Asignar una posición ocupada retira la obra anterior de la selección; no la elimina. Mover una obra a otra posición ocupada también reemplaza la anterior, sin intercambiar posiciones automáticamente.
3. En **Selección actual** se puede cambiar la posición o elegir **Quitar de destacados**. Hay hasta seis obras seleccionadas.
4. En **Editar mi perfil → Apariencia**, elegir tema oscuro/claro, uno de cinco acentos y cuadrícula/lista. La vista previa responde sin guardar; pulsar Guardar cambios aplica las preferencias al portafolio.

Los destacados admiten solo publicaciones de tipo **Obra**, publicadas, visibles por moderación, públicas o para registrados y con algún recurso listo. Las actualizaciones de texto permanecen en la actividad. Una obra con YouTube validado también puede destacarse; seleccionarla no carga el reproductor externo.

El catálogo propio pagina desde MySQL de a 12 y admite búsqueda. Los seis valores de posición son opciones cerradas, no un select masivo de entidades. El ABM Vue y el selector asíncrono de etiquetas siguen siendo los módulos que cubren esos requisitos docentes; los destacados usan formularios MVC con POST y antiforgery.

## Lectura y privacidad

El perfil separa **Obras destacadas** y **Actividad**, sin repetir las obras destacadas en la actividad de ese visitante. La selección se muestra en cada página del perfil y está acotada a seis por regla de producto; la actividad se pagina desde el servidor de a 12. Los resultados y contadores se filtran por permisos antes de paginar.

Una obra para registrados solo aparece en destacados para cuentas autenticadas. La privacidad del perfil sigue limitando el conjunto. Si una obra pasa a privada, borrador, archivada, oculta por moderación, cambia a Actualización o se queda sin recursos listos, deja de aparecer como destacada. Conserva su posición y puede reaparecer cuando vuelva a cumplir las reglas; el panel propio permite quitarla aunque ya no sea elegible.

Los borradores y archivados del propietario se consultan desde el editor y ya no se mezclan con el portafolio. El propietario puede seguir viendo sus publicaciones privadas publicadas en su actividad; un aviso aclara que esa vista no equivale a la de otra persona. Las obras privadas nunca se muestran como destacadas, tampoco en la vista del autor.

## Escritura y concurrencia

El backend obtiene el autor de la sesión y exige rol Creador. El rol Administrador no permite seleccionar obras ajenas. El formulario incluye versión de publicación y huella de la selección actual; ambas se comprueban antes de guardar.

Dentro de una transacción, el servicio bloquea la fila de Perfil del autor con `SELECT ... FOR UPDATE`, parametrizado por EF Core. Ese bloqueo serializa cambios de posiciones entre procesos que usen el servicio. La huella permite detectar formularios desactualizados y evita que dos solicitudes sobre obras distintas sobrescriban la misma posición inadvertidamente. Una colisión devuelve 409 y requiere volver a revisar.

Las obras afectadas renuevan `Publicacion.Version`, por lo que una pestaña antigua del editor también debe recargar antes de guardar. Destacar no cambia fecha de publicación, privacidad, estado editorial ni moderación. `OrdenDestacado` ya estaba en el modelo inicial: no hay nuevas tablas, columnas, dependencias ni migraciones. El límite y la unicidad de posiciones se mantienen en el servicio; no se agregó un índice único o CHECK para escrituras directas a MySQL.

## Apariencia

La vista previa usa clases CSS predefinidas y `textContent`; no acepta CSS/HTML libre ni scripts inline. El backend conserva la validación de tema, disposición y paleta. En tema claro, los acentos de texto/foco usan variantes más oscuras para conservar legibilidad; los botones mantienen la paleta suave con texto oscuro. El tema se aplica al portafolio, sin cambiar toda la aplicación.

## Archivos principales

| Responsabilidad | Fuente |
| --- | --- |
| Reglas y transacción de posiciones | [DestacadosService.cs](../src/Trazo.Web/Services/DestacadosService.cs) |
| Formularios y autorización | [DestacadosController.cs](../src/Trazo.Web/Controllers/DestacadosController.cs) |
| DTO y entradas | [DestacadosModels.cs](../src/Trazo.Web/Models/DestacadosModels.cs) |
| Panel de selección | [Index.cshtml](../src/Trazo.Web/Views/Destacados/Index.cshtml) |
| Lectura de perfil | [PerfilController.cs](../src/Trazo.Web/Controllers/PerfilController.cs), [Ver.cshtml](../src/Trazo.Web/Views/Perfil/Ver.cshtml) |
| Vista previa | [Editar.cshtml](../src/Trazo.Web/Views/Perfil/Editar.cshtml), [perfil-preview.js](../src/Trazo.Web/wwwroot/js/perfil-preview.js) |
| Pruebas | [DestacadosIntegrationTests.cs](../tests/Trazo.Tests/DestacadosIntegrationTests.cs) |

`GET /mi-portafolio/destacados?q=&pagina=1` devuelve el panel. `POST /mi-portafolio/destacados/{id}` asigna `Posicion` 1..6 o retira con 0; exige `Version`, `VersionSeleccion`, sesión y antiforgery. No hay escrituras por GET ni endpoints JWT nuevos.

## Validación

Compilación Release correcta, con 0 advertencias y 0 errores. EF confirmó que no hay cambios de modelo pendientes de migración.

**76 pruebas aprobadas con MySQL real, 0 fallidas y 0 omitidas.** Las cinco nuevas cubren selección, límite, orden, reemplazo, retirada, privacidad, estados/recursos, propiedad, CSRF, parámetros inválidos, concurrencia, paginación y validación de apariencia. Las pruebas anteriores de medios, moderación, suspensión y autenticación siguen aprobadas.

En Edge se crearon dos obras temporales con referencias YouTube ficticias, sin cargar videos externos. Se seleccionaron dos posiciones libres; la obra pública apareció al visitante y la de registrados solamente en la sesión. Se guardó tema claro/lista, se comprobó la vista móvil a 390 px y se pasó una obra a privada antes de quitarla de destacados.

Se midió únicamente el contraste **acento de texto contra fondo del portafolio** en las diez combinaciones de tema/paleta: entre 6,23:1 y 14,62:1. No equivale a una auditoría integral de accesibilidad. La vista previa y las capturas del panel/perfil se revisaron visualmente; no se detectaron errores JavaScript ni desbordes en el perfil móvil.

- [Video](evidencias/incremento-08/portafolio.webm), [selección](evidencias/incremento-08/seleccion.png) y [selección móvil](evidencias/incremento-08/seleccion-movil.png).
- [Vista previa](evidencias/incremento-08/vista-previa.png), [perfil claro/lista](evidencias/incremento-08/perfil-claro-lista.png), [móvil](evidencias/incremento-08/perfil-movil.png).
- [Obra privada conservada en la selección](evidencias/incremento-08/obra-privada.png).
- [Recorrido y contrastes](evidencias/incremento-08/recorrido.json), [resumen .NET](evidencias/incremento-08/pruebas-dotnet.json).

Para repetir: `./tools/Probar.ps1 -Integracion`; luego, con la web y Edge disponibles, `node tools/Probar-Portafolio.mjs`. El navegador requiere dos posiciones libres en Ana, conserva selecciones previas, restaura sus preferencias y elimina las dos obras temporales al finalizar. No guarda el formulario original ni credenciales en las evidencias. Ejecutarlo sin editar simultáneamente ese perfil demo.

## Parada para GitHub

Reiniciar la web normalmente para cargar los cambios; no ejecutar otra migración para este incremento.

```powershell
git add .
git diff --cached --stat
git commit -m "feat: obras destacadas y vista previa del portafolio"
git push
```

No se ejecuta push automático y la CI remota queda pendiente. El siguiente bloque propuesto es preparar datos reproducibles de entrega, SQL con datos ficticios, instalación desde una copia limpia y revisión final de la matriz/defensa. La apertura productiva sigue siendo un hito separado.
