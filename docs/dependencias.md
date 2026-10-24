# Dependencias y recursos de terceros

Inventario inicial para el primer incremento. Las versiones NuGet exactas y transitivas están en `packages.lock.json`. Los paquetes se restauran desde NuGet; no se copian sus binarios al repositorio.

| Componente | Uso | Información de licencia |
| --- | --- | --- |
| ASP.NET Core / Identity / EF Core | MVC, sesión, cuentas y persistencia | MIT, según los paquetes y repositorios de Microsoft |
| MySql.EntityFrameworkCore 10.0.9 | Proveedor EF para MySQL | El manifiesto NuGet declara `GPL-2.0-only WITH Universal-FOSS-exception-1.0`; incluye dependencia de MySql.Data |
| SkiaSharp 4.153.1 y recursos nativos | Decodificación y conversión de avatar | MIT para el wrapper; conservar y revisar avisos de componentes nativos al distribuir |
| Bootstrap local | Base de estilos | MIT; [licencia incluida](../src/Trazo.Web/wwwroot/lib/bootstrap/LICENSE) |
| jQuery y complementos de validación | Archivos de la plantilla MVC conservados; el layout actual no los carga | MIT; avisos incluidos en sus directorios de `wwwroot/lib` |
| xUnit, test SDK, WebApplicationFactory y coverlet | Pruebas | Consultar los manifiestos de las versiones fijadas; herramientas de desarrollo |
| Imagen MySQL 8.4 | Base de desarrollo en Docker | Imagen externa; revisar sus avisos y condiciones para la distribución elegida |

Este inventario no determina por sí solo la compatibilidad legal de una distribución comercial. Antes de producción habrá que revisar licencias de dependencias directas, transitivas y recursos nativos, junto con la licencia elegida para Trazo. No se ha elegido todavía una licencia para el código propio.

Las formas geométricas de las tarjetas se dibujan con CSS del proyecto. Los textos y usuarios de demostración son ficticios; no se copiaron obras de Amy J. Xu u otros portafolios. Las capturas de `docs/evidencias` corresponden a la aplicación local con esos datos.

La [investigación preliminar](plan-de-trabajo.md) conserva las referencias técnicas y legales consultadas. Los archivos de terceros conservan sus avisos originales. Una publicación futura de obras de usuarios requerirá permisos y procedimientos específicos, aparte de las licencias del software.
