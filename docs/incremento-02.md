# Incremento 02 · Publicaciones en Vue y selección AJAX

Fecha: **24/10/2026**. Se continúa desde `91eab09`, verificado en `main` y en GitHub antes de comenzar. Se conserva el alcance académico y la parada para el push de Jonathan.

## Resultado

El creador accede a **Mi estudio → Administrar mis publicaciones** (`/panel/publicaciones`). Allí puede crear, listar, modificar y eliminar actualizaciones de texto sin recargar el documento; guardar borradores, publicar o archivar; elegir privacidad; y asociar hasta 8 etiquetas mediante búsqueda AJAX paginada. También puede preparar una obra como borrador. No se permite publicarla sin un recurso listo.

El listado filtra por título y estado, ordena por actualización/ID y recibe 12 entradas por página desde MySQL. El selector obtiene hasta 10 etiquetas activas por petición, espera 300 ms tras escribir y cancela solicitudes anteriores para evitar mostrar resultados desactualizados. Al editar solo se cargan las etiquetas asociadas, no el catálogo completo.

Los componentes Vue se precompilan con Vite y se sirven desde MVC. El bundle está versionado para que el evaluador pueda ejecutar la solución con .NET sin instalar Node. Los cambios en componentes sí requieren `npm ci` y `npm run build`. No hay otro servidor de aplicación ni se requiere Python para ejecutar la web.

## Archivos principales

| Responsabilidad | Archivo |
| --- | --- |
| Vista MVC y token antiforgery | [Index.cshtml](../src/Trazo.Web/Views/Publicaciones/Index.cshtml) |
| ABM y estados de interfaz | [PublicacionesApp.vue](../src/Trazo.Web/ClientApp/publicaciones/PublicacionesApp.vue) |
| Selección de entidades relacionadas | [SelectorEtiquetas.vue](../src/Trazo.Web/ClientApp/publicaciones/SelectorEtiquetas.vue) |
| Fetch, errores HTTP y CSRF | [http.js](../src/Trazo.Web/ClientApp/publicaciones/http.js) |
| Rutas JSON y manejo de errores de negocio | [PublicacionesJsonController.cs](../src/Trazo.Web/Controllers/PublicacionesJsonController.cs) |
| Autoría, estados, etiquetas y concurrencia | [PublicacionesService.cs](../src/Trazo.Web/Services/PublicacionesService.cs) |
| Validación de entradas y DTO | [PublicacionEditorModels.cs](../src/Trazo.Web/Models/PublicacionEditorModels.cs) |
| Pruebas con MySQL real | [PublicacionesIntegrationTests.cs](../tests/Trazo.Tests/PublicacionesIntegrationTests.cs) |
| Recorrido de navegador reproducible | [Probar-Editor.mjs](../tools/Probar-Editor.mjs) |

## Contrato JSON

Estos endpoints utilizan la sesión **cookie**, rol Creador y protección antiforgery. No son la API JWT comprometida para otro incremento. Un administrador con rol Creador administra sus publicaciones propias, nunca las de terceros por este recorrido.

| Método y ruta | Función |
| --- | --- |
| GET `/panel-json/publicaciones?q=&estado=&pagina=1` | Listado del creador, paginado y filtrado; omitir estado para incluir todos |
| GET `/panel-json/publicaciones/{id}` | Datos editables y etiquetas asociadas |
| POST `/panel-json/publicaciones` | Alta; devuelve 201 y Location |
| PUT `/panel-json/publicaciones/{id}` | Reemplazo de datos editables con Version; devuelve 200 |
| DELETE `/panel-json/publicaciones/{id}` | Baja con cuerpo `{ "version": "..." }`; devuelve 204 |
| GET `/panel-json/etiquetas?q=anima&pagina=1` | Selector de etiquetas activas, máximo 10 resultados |

Los listados devuelven `items`, `numero`, `tamano`, `total`, `paginas`, `anterior` y `siguiente`. Los tamaños se fijan en el backend. POST/PUT/DELETE requieren `X-CSRF-TOKEN`, obtenido de la vista, además de la cookie. Las peticiones JSON tienen un límite de cuerpo de 100 KB para publicaciones.

Errores: 400 para validación/antiforgery, 401 para sesión ausente o inválida, 403 para rol insuficiente, 404 para publicación inexistente o ajena y 409 para conflicto de versión o relaciones que impiden borrar. Los errores de negocio devuelven ProblemDetails; los de campos usan ValidationProblemDetails. La interfaz también contempla fallos de red o respuestas sin JSON, conservando el formulario.

## Integridad y privacidad

- El autor se obtiene de la sesión. No se aceptan del formulario campos de autoría, moderación, fechas ni orden destacado.
- `Version` se renueva al guardar. EF verifica el token original al escribir; dos solicitudes con la misma versión no pueden sobrescribirse silenciosamente. La baja también exige la versión actual.
- Las etiquetas recibidas se vuelven a comprobar: IDs positivos, únicos, existentes y hasta 8. Una etiqueta inactiva solo puede conservarse si ya estaba asociada.
- La publicación aplica las reglas de lectura del incremento anterior. Archivarla o volver a borrador la retira de la lectura ajena; editar una publicación moderada no cambia su moderación.
- Una publicación con reportes no se elimina físicamente: se puede archivar. La baja de publicaciones con recursos queda bloqueada hasta coordinar los archivos en el próximo módulo. Esta protección no implica que la moderación o los medios ya estén implementados.
- No cambian las tablas ni relaciones; DER y migración inicial permanecen vigentes.

## Validación y evidencia

**26 pruebas .NET aprobadas, ninguna fallida ni omitida**, con MySQL real en `trazo_tests`. Las 7 nuevas cubren ABM con persistencia N:M y estados, autoría/CSRF, escrituras simultáneas y baja desactualizada, validaciones, protección de campos privilegiados, listados/selector paginados y retención de reportes. La interfaz de moderación sigue pendiente: los reportes de esta prueba se preparan directamente en la base aislada.

Compilación .NET Debug y Release sin errores ni advertencias; compilación Vue correcta. Se repitió `npm ci` y `npm run build` y se comprobó que el SHA-256 del bundle no cambió. npm no reportó vulnerabilidades conocidas en las fuentes consultadas. No se presenta esto como auditoría de seguridad.

El recorrido automatizado en Edge creó, editó y borró una publicación temporal; seleccionó una etiqueta real; pidió la segunda página; filtró por título y comprobó el editor a 390 px. Hubo **0 recargas del documento durante el ABM**, sin errores de JavaScript o CSP detectados. Las capturas se revisaron visualmente. La publicación temporal se eliminó al finalizar.

- [Video del ABM completo](evidencias/incremento-02/editor-abm.webm).
- [Listado](evidencias/incremento-02/listado.png), [editor](evidencias/incremento-02/editor.png), [selector](evidencias/incremento-02/selector-etiquetas.png), [móvil](evidencias/incremento-02/editor-mobile.png) y [baja](evidencias/incremento-02/baja.png).
- [Registro de métodos/URL/estados HTTP](evidencias/incremento-02/red.json), sin cookies, contraseñas o tokens.
- [Metadatos de la página 2](evidencias/incremento-02/paginacion.json) y [captura](evidencias/incremento-02/paginacion.png): 13 publicaciones propias durante el recorrido, página de tamaño 12 y un registro recibido en la segunda petición.

GitHub Actions ahora recompila Vue y comprueba el bundle versionado antes de ejecutar .NET/MySQL. Su ejecución remota con estos cambios queda pendiente del push de Jonathan.

## Parada y continuación

Este incremento cumple el recorrido ABM Vue y la búsqueda asíncrona de relacionados. No completa todavía archivos adicionales, JWT, favoritos/seguimientos ni moderación. El siguiente incremento es la carga y lectura autorizada de medios.

Antes de continuar, revisar y subir:

```powershell
git add .
git diff --cached --stat
git commit -m "feat: editor Vue de publicaciones y selector AJAX de etiquetas"
git push
```

`AGENTS.md`, secretos locales, datos subidos y `node_modules` siguen fuera del repositorio. No se ejecuta push automáticamente.
