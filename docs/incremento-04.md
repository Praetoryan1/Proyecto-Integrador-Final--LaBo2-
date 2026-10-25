# Incremento 04 · Web API REST con JWT

Fecha: **24/10/2026**. Se parte de `5f391c4`, verificado en `main` y en GitHub. Se conserva la parada acordada para que Jonathan realice el commit/push.

## Resultado

La API `/api/v1/` permite emitir un JWT local, gestionar publicaciones propias mediante CRUD REST, paginar y consultar etiquetas relacionadas. Comparte servicios con el panel Vue. Incluye revocación de todas las sesiones de la cuenta y una colección Postman con pruebas automatizadas. La web sigue usando cookies y antiforgery; el Bearer es un esquema explícito de la API.

El emisor y todas las rutas `/api/` están limitados a Development/Testing; en otros entornos devuelven 404. La clave se prepara con `tools/Preparar-Api.ps1` en User Secrets. No se cambian tablas ni relaciones, por lo que siguen vigentes DER y migración inicial.

## Archivos principales

| Responsabilidad | Fuente |
| --- | --- |
| Emisión, validación y consulta de permisos actuales | [JwtLocal.cs](../src/Trazo.Web/Services/JwtLocal.cs) |
| Token local y revocación de sesiones | [AuthApiController.cs](../src/Trazo.Web/Controllers/Api/AuthApiController.cs) |
| CRUD REST y selector paginado | [PublicacionesApiController.cs](../src/Trazo.Web/Controllers/Api/PublicacionesApiController.cs) |
| Búsqueda compartida MVC/API | [EtiquetasConsulta.cs](../src/Trazo.Web/Services/EtiquetasConsulta.cs) |
| Esquemas de autenticación y restricción por entorno | [Program.cs](../src/Trazo.Web/Program.cs) |
| Preparación de la clave local | [Preparar-Api.ps1](../tools/Preparar-Api.ps1) |
| Colección y pasos de uso | [Postman JSON](api/Trazo.postman_collection.json), [guía de API](api.md) |
| Pruebas de integración y entorno | [ApiIntegrationTests.cs](../tests/Trazo.Tests/ApiIntegrationTests.cs) |

## Validación

**55 pruebas .NET aprobadas con MySQL real, 0 fallidas y 0 omitidas.** Se agregan 7 escenarios de integración y 3 casos de entorno a las 45 pruebas anteriores. Se verifican:

- CRUD, etiquetas, paginación, propietario y conflictos de versión.
- Separación cookie/Bearer, rechazo de tokens por URL, falta de rol y cuenta suspendida.
- Firma, algoritmo, tipo, emisor, audiencia, vencimiento, tiempo de inicio y claims de identidad/sello.
- Revocación de JWT y cookies mediante SecurityStamp, bloqueo Identity por contraseña incorrecta y límite de solicitudes.
- API deshabilitada en Production/Staging incluso con clave configurada, y respuesta 503 sin clave en Testing.

La colección Postman se ejecutó localmente con Newman 6.2.3: **19 peticiones y 31 aserciones aprobadas**. Creó y eliminó una publicación temporal en `trazo_dev`; verificó otra cuenta y administrador sin acceso, edición desactualizada, obra inválida y token revocado. Los tokens fueron temporales y el informe guardado excluye cuerpos, cabeceras y credenciales. No se presenta como una captura o ejecución de la interfaz gráfica de Postman. La guía indica cómo repetirla allí.

- [Resultados de la colección](evidencias/incremento-04/postman-resultados.json).
- [Resumen .NET sin logs sensibles](evidencias/incremento-04/pruebas-dotnet.json).

La primera ejecución se interrumpió por falta de memoria virtual en Windows y produjo fallos de asignación/procesamiento en dos pruebas multimedia. La ejecución completa posterior, incluido el control adicional del limitador, pasó sin omitir esos casos. No se modificaron los límites ni las validaciones de archivos para hacerla pasar.

La CI existente ejecuta estas pruebas con una clave efímera creada por el fixture; no necesita secretos JWT en GitHub. Su resultado remoto queda pendiente del push. No se certifica producción: faltan funcionalidades del alcance, revisión operativa y un proveedor de identidad apropiado para una API pública.

La restauración NuGet en modo bloqueado y las compilaciones Debug/Release finalizaron sin errores ni advertencias. Se repitió el recorrido de navegador del editor MVC/Vue (alta, edición, baja, selector, paginación y móvil): aprobado, sin recargas ni errores detectados. La evidencia de regresión se guardó en temporales para conservar los archivos históricos. Las dependencias npm del proyecto no cambiaron; la comprobación de npm informó 0 vulnerabilidades conocidas en su árbol final, sin que esto sustituya una auditoría.

## Preparación y parada

En este equipo ya se preparó la clave. Al clonar en otra máquina:

```powershell
./tools/Preparar-Api.ps1
dotnet run --project src/Trazo.Web --launch-profile https
```

Luego importar la colección en Postman y ejecutar el Runner completo según la guía. No hace falta crear una base ni aplicar otra migración.

Después de revisar:

```powershell
git add .
git diff --cached --stat
git commit -m "feat: API REST con JWT y coleccion Postman"
git push
```

`AGENTS.md`, `.env`, User Secrets, archivos subidos y temporales siguen fuera de Git. No se realizó commit/push automáticamente. Próximo incremento propuesto: favoritos y seguimiento de creadores, después de confirmar este push; también quedan moderación/reportes, datos finales de entrega y documentación de defensa.
