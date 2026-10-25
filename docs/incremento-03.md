# Incremento 03 · Obras multimedia y acceso autorizado

Fecha: **24/10/2026**. Continuación de `fe62f2d` en `main`, comprobado también en GitHub. Se completó el trabajo interrumpido antes del commit; no se hizo push automático.

## Resultado

Las publicaciones admiten JPG/PNG/WebP, GIF, MP4 y enlaces YouTube desde Vue. Se pueden ordenar, describir, acreditar y quitar recursos sin recargar. Las obras con recursos listos ya pueden publicarse. Explorar ofrece filtro por medio y portadas reales; el detalle muestra imágenes, GIF con control de reproducción, MP4 y YouTube con carga voluntaria. Las publicaciones de texto siguen funcionando.

Se cumple el requisito de archivos **adicionales al avatar**, con almacenamiento en el servidor y lectura autorizada. Los endpoints siguen usando cookies/antiforgery; el requisito JWT todavía está pendiente. No cambian tablas ni relaciones: la entidad RecursoMultimedia del DER ya contemplaba estos datos.

## Archivos principales

| Responsabilidad | Fuente |
| --- | --- |
| Formularios, orden y edición de recursos | [RecursosEditor.vue](../src/Trazo.Web/ClientApp/publicaciones/RecursosEditor.vue) |
| Escrituras y lectura protegida, incluyendo HEAD/Range | [RecursosController.cs](../src/Trazo.Web/Controllers/RecursosController.cs) |
| Propiedad, cuotas y versión concurrente | [RecursosService.cs](../src/Trazo.Web/Services/RecursosService.cs) |
| Validación, normalización y miniaturas | [AlmacenMedios.cs](../src/Trazo.Web/Services/AlmacenMedios.cs), [ProcesoMultimedia.cs](../src/Trazo.Web/Services/ProcesoMultimedia.cs) |
| Presentación pública | [Detalle.cshtml](../src/Trazo.Web/Views/Explorar/Detalle.cshtml), [medios.js](../src/Trazo.Web/wwwroot/js/medios.js) |
| Pruebas reproducibles | [MediosIntegrationTests.cs](../tests/Trazo.Tests/MediosIntegrationTests.cs), [Probar-Medios.mjs](../tools/Probar-Medios.mjs) |
| Preparación y límites | [Guía de medios](medios.md), [Preparar-Medios.ps1](../tools/Preparar-Medios.ps1) |

## Rutas de escritura

Prefijo: `/panel-json/publicaciones/{id}/recursos`. Todas devuelven el DTO actualizado de la publicación y su nueva versión.

| Método y sufijo | Cuerpo |
| --- | --- |
| POST `/archivo` | Multipart: archivo, version, textoAlternativo, creditos |
| POST `/youtube` | JSON: url, version, textoAlternativo, creditos |
| PUT `/{recursoId}` | JSON: version, textoAlternativo, creditos |
| DELETE `/{recursoId}` | JSON: version |
| PUT `/orden` | JSON: version, recursoIds con todos los IDs actuales, sin duplicados |

Errores de negocio: 400 por formato/límites; 404 por inexistencia o propiedad ajena; 409 por versión desactualizada; 429 si los dos procesadores están ocupados; 503 si falta FFmpeg. También se aplican sesión, rol, CSRF y límites HTTP de cuerpo. No se procesa HTML proporcionado por usuarios ni se consulta la URL recibida en el servidor.

## Validación

**45 pruebas .NET aprobadas, 0 fallidas y 0 omitidas con MySQL real**, usando exclusivamente `trazo_tests`. Incluyen las 26 anteriores, 12 casos de parsing seguro de YouTube y 7 escenarios de integración multimedia. Se comprobaron PNG/GIF/MP4 reales, archivos falsos, tamaños, códecs/duración/dimensiones, cuota, máximo de recursos, conflictos de versión, orden con índice único, limpieza tras fallo/baja y permisos por cuenta/perfil/publicación. GIF/MP4 se procesaron con FFmpeg real, sin simular el servicio.

El recorrido en Edge cargó los cuatro tipos de recurso, editó una descripción, reordenó, publicó, filtró Explorar, reprodujo GIF y MP4, cambió la privacidad y eliminó recursos y la publicación temporal. Comprobó **0 recargas durante la edición**, móvil a 390 px sin desbordes, ausencia de errores JavaScript y respuestas Range 206/404 según permiso. YouTube se comprobó mediante un iframe interceptado por la prueba: no se contactó a YouTube ni se certifica la reproducción de un video externo. Las imágenes son patrones de prueba generados localmente, no obras de terceros.

- [Video del recorrido](evidencias/incremento-03/medios-abm.webm).
- [Editor](evidencias/incremento-03/editor-medios.png), [móvil](evidencias/incremento-03/editor-movil.png), [obra pública](evidencias/incremento-03/obra-publica.png), [Explorar filtrado](evidencias/incremento-03/explorar-medios.png).
- [Registro de red y comprobaciones](evidencias/incremento-03/red.json), sin cookies ni tokens.

La CI instala FFmpeg en Ubuntu antes de las pruebas. Su ejecución remota sigue pendiente del push. Las limitaciones operativas y de producción se detallan en la guía de medios; estas verificaciones no constituyen una auditoría de seguridad.

Compilaciones .NET Debug/Release sin errores ni advertencias y bundle Vue generado correctamente. Se repitió el recorrido anterior de ABM de texto, selector, paginación y móvil: aprobado, sin recargas. Sus archivos de regresión se guardaron en temporales para conservar la evidencia histórica del incremento 02. Se revisaron visualmente las capturas nuevas de editor, móvil, detalle y Explorar. Los archivos multimedia de las publicaciones de prueba se eliminaron al completar los recorridos.

## Parada para GitHub

En la terminal de VS Code, después de revisar los archivos:

```powershell
git status --short
git add .
git diff --cached --stat
git commit -m "feat: obras multimedia con validacion y acceso autorizado"
git push
```

`AGENTS.md`, `.env`, archivos subidos, FFmpeg portable, temporales, dependencias restauradas e informes TRX permanecen ignorados. No se necesitan cambios de base de datos para este incremento. El siguiente incremento previsto es la API JWT con su colección de pruebas, una vez confirmado este commit/push.
