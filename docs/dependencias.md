# Dependencias y recursos de terceros

Inventario inicial para el primer incremento. Las versiones NuGet exactas y transitivas están en `packages.lock.json`. Los paquetes se restauran desde NuGet; no se copian sus binarios al repositorio.

| Componente | Uso | Información de licencia |
| --- | --- | --- |
| ASP.NET Core / Identity / EF Core | MVC, sesión, cuentas y persistencia | MIT, según los paquetes y repositorios de Microsoft |
| Microsoft.AspNetCore.Authentication.JwtBearer 10.0.13 / Microsoft.IdentityModel 8.19.2 | Validación y emisión académica de JWT; versiones transitivas en packages.lock.json | MIT según los manifiestos de los paquetes de Microsoft |
| MySql.EntityFrameworkCore 10.0.9 | Proveedor EF para MySQL | El manifiesto NuGet declara `GPL-2.0-only WITH Universal-FOSS-exception-1.0`; incluye dependencia de MySql.Data |
| SkiaSharp 4.153.1 y recursos nativos | Decodificación y conversión de avatar e imágenes de publicaciones | MIT para el wrapper; conservar y revisar avisos de componentes nativos al distribuir |
| FFmpeg/ffprobe 9.0.2, build Essentials de Gyan para Windows | Inspección y normalización de GIF/MP4; invocado como proceso por C# | La descarga incluye LICENSE y su configuración; se conserva localmente en tools/ffmpeg. No se versionan ejecutables. Revisar condiciones de la compilación y códecs antes de distribuir |
| Bootstrap local | Base de estilos | MIT; [licencia incluida](../src/Trazo.Web/wwwroot/lib/bootstrap/LICENSE) |
| Vue 3.5.44 | Editor y selector reactivos del panel | MIT; [licencia incluida con el bundle](../src/Trazo.Web/wwwroot/js/publicaciones/LICENSE.vue.txt) |
| Vite 8.3.5 y plugin Vue 6.0.10 | Compilación de componentes; no requieren un servidor Node al ejecutar Trazo | MIT; versiones fijadas en package-lock.json |
| Playwright 1.64.1 | Recorrido de navegador y captura de evidencia | Apache-2.0; herramienta de desarrollo. Usa Edge instalado y FFmpeg descargado por su instalador |
| jQuery y complementos de validación | Archivos de la plantilla MVC conservados; el layout actual no los carga | MIT; avisos incluidos en sus directorios de `wwwroot/lib` |
| xUnit, test SDK, WebApplicationFactory y coverlet | Pruebas | Consultar los manifiestos de las versiones fijadas; herramientas de desarrollo |
| Imagen MySQL 8.4 | Base de desarrollo en Docker | Imagen externa; revisar sus avisos y condiciones para la distribución elegida |

Este inventario no determina por sí solo la compatibilidad legal de una distribución comercial. Antes de producción habrá que revisar licencias de dependencias directas, transitivas y recursos nativos, junto con la licencia elegida para Trazo. No se ha elegido todavía una licencia para el código propio.

Las formas geométricas de las tarjetas se dibujan con CSS del proyecto. Los textos y usuarios de demostración son ficticios; no se copiaron obras de Amy J. Xu u otros portafolios. Las capturas de `docs/evidencias` corresponden a la aplicación local con esos datos.

El incremento 09 también distribuye PNG, GIF y MP4 geométricos propios en `DemoAssets`, generados con Canvas por `tools/Generar-Assets-Demo.mjs` y codificados con FFmpeg. El manifiesto documenta tamaño y SHA-256; el script requiere Node, Playwright/Edge y FFmpeg únicamente para regenerarlos. No se descargaron imágenes, música ni videos de terceros. No se asigna una licencia nueva a estos recursos mientras siga pendiente la decisión de licencia del proyecto.

Los archivos de terceros conservan sus avisos originales. Una publicación futura de obras de usuarios requerirá permisos y procedimientos específicos, aparte de las licencias del software.

Para integrar Vue se consultaron las guías oficiales de [herramientas y precompilación](https://vuejs.org/guide/scaling-up/tooling), [seguridad de plantillas](https://vuejs.org/guide/best-practices/security) e [integración de Vite con un backend](https://vite.dev/guide/backend-integration). Se compilan plantillas propias y se trata el texto del usuario como texto, sin `v-html`, compilación de plantillas de usuarios ni relajación de la política CSP. Se eligió una salida única con nombre estable y `asp-append-version` para servirla desde Razor.
