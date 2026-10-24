# Incremento 01 · Base funcional de Trazo

Fecha: **21/10/2026**. Primer incremento ampliado a pedido de Jonathan. Entrega final confirmada para el **05/11/2026**. Esta evidencia corresponde a ejecución local; no a un sitio productivo ni a una evaluación docente.

## Cambios incluidos

- Solución .NET 10 MVC, herramientas y dependencias fijadas, configuración de VS Code, Docker Compose y scripts PowerShell.
- Modelo relacional completo de la propuesta, soporte Identity, migración inicial y SQL del esquema.
- Registro transaccional con rol Creador, login, logout POST y administración de etiquetas exclusiva de Administrador.
- Edición de perfil, tema, acento, galería, contacto y privacidad. Avatar JPG/PNG/WebP validado, recortado y convertido a PNG fuera de la carpeta pública.
- Explorar sin login, filtros por texto/etiqueta/tipo, perfiles y detalle, paginación en MySQL con permisos aplicados antes del conteo.
- Datos ficticios: 3 cuentas, 6 etiquetas, 18 textos públicos y 3 casos de visibilidad/estado. Diseño adaptable con fondo oscuro y acento lima.
- Pruebas unitarias y de integración; workflow para GitHub aún sin ejecutar remotamente.

## Comprobaciones realizadas

| Comprobación | Resultado observado |
| --- | --- |
| Compilación .NET | Debug y Release sin errores ni advertencias; restauración con lockfiles comprobada |
| Migración y semilla en MySQL 8.4.11 | Aplicadas a `trazo_dev`; contenedor healthy |
| Suite completa `tools/Probar.ps1 -Integracion` | **19 aprobadas, 0 fallidas, 0 omitidas** |
| Dependencias NuGet, consulta de vulnerabilidades conocidas | Sin paquetes reportados por las fuentes consultadas; no equivale a ausencia de vulnerabilidades |
| Navegación en Edge mediante Playwright | Portada, siguiente página, login, perfil y administración completados; sin errores de página JavaScript ni respuestas 5xx registradas |
| Paginación pública en navegador | 12 tarjetas en primera página y 6 en segunda |
| Vista móvil de 390 px | Sin desbordamiento horizontal; menú e ingreso comprobados |
| Inspección visual | Capturas de escritorio, móvil, login, perfil, edición y etiquetas revisadas |
| GitHub Actions | Configurado; pendiente del primer push |

Las pruebas de navegador usaron un contexto local que tolera el certificado de desarrollo. En el navegador habitual se debe confiar el certificado con el comando documentado. La inspección cubre estas pantallas y recorridos, no una auditoría integral de accesibilidad ni todos los navegadores.

## Cobertura automatizada relevante

Las [pruebas unitarias](../tests/Trazo.Tests/VisibilidadTests.cs) recorren combinaciones de privacidad, estado editorial, moderación, suspensión y acceso al contacto. Las [pruebas con MySQL](../tests/Trazo.Tests/MySqlIntegrationTests.cs) comprueban:

1. Restricciones SQL de autoseguimiento y fuentes incompatibles de un recurso.
2. Login con ReturnUrl externo sin redirección abierta, cookie segura y logout solo POST.
3. Registro con CSRF, creación de perfil y prohibición de elegir rol Administrador mediante campos extra.
4. Avatar real permitido, archivo falso rechazado y lectura bloqueada cuando el perfil pasa a privado; rechazo de enlaces profesionales inseguros.
5. ABM de etiquetas y rechazo de baja de una etiqueta asociada.
6. Suspensión que invalida una sesión existente.
7. Lectura pública paginada y filtrada, rechazo de parámetros inválidos y bloqueo de contenido privado incluso para otro administrador.

El test de suspensión modifica la cuenta directamente en su base aislada: la pantalla de moderación no está implementada. Las integraciones recrean `trazo_tests`; no borran ni reemplazan la base de desarrollo.

## Capturas

- [Explorar en escritorio](evidencias/explorar-desktop.png).
- [Explorar en móvil](evidencias/explorar-mobile.png).
- [Login](evidencias/login.png).
- [Edición de perfil](evidencias/perfil-editar.png).
- [Perfil de Ana visto por su propia cuenta](evidencias/perfil.png): por eso incluye su publicación privada y su borrador.
- [Etiquetas como administrador](evidencias/etiquetas-admin.png).

La captura del editor de perfil muestra el estado sin avatar personalizado; la subida y lectura de un PNG sí se comprueban en la integración. Queda incorporar evidencia visual específica del avatar personalizado y del inspector de red para la entrega final.

## Límites y siguiente incremento

Faltan el ABM de publicaciones en Vue, selección AJAX de etiquetas, carga de medios adicionales, Web API con JWT, colección Postman, favoritos/seguimientos y moderación. La base de seguridad existente tampoco completa los requisitos operativos y legales de producción. El SQL incluido exporta únicamente el esquema; el dump académico con datos completos queda para el cierre.

El siguiente incremento propuesto es el editor de publicaciones Vue y su selector asíncrono. **Se espera el commit/push de Jonathan antes de continuarlo.** Mensaje sugerido:

```text
feat: base MVC, identidad, perfiles y persistencia MySQL
```
