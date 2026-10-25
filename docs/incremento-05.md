# Incremento 05 · Favoritos y seguimiento de creadores

Fecha: **25/10/2026**. Inicio desde `ded7644`, comprobado en `main` y GitHub. No cambian tablas ni relaciones: Favorito y Seguimiento ya estaban en el DER y en la migración inicial.

## Uso desde la web

1. Ingresar con una cuenta de creador y abrir una publicación accesible. **Guardar en favoritos** la agrega a la colección personal; **Quitar de favoritos** retira únicamente ese guardado.
2. En **Mi estudio → Mis favoritos**, buscar por título y recorrer páginas de 12 registros. Se puede quitar cada favorito desde su tarjeta.
3. Visitar el perfil de otra persona y elegir **Seguir creador**. **Mi estudio → Creadores que sigo** reúne esos portafolios y permite buscar por nombre, usuario o especialidad. También pagina de a 12 desde MySQL.
4. Elegir **Dejar de seguir** desde el perfil o desde la lista. No hay autoseguimiento, solicitudes de amistad, notificaciones ni acceso adicional a contenido privado.

Los visitantes siguen navegando sin cuenta. En el detalle/perfil tienen un enlace para ingresar si quieren guardar o seguir; al autenticarse vuelven al contenido. Los formularios usan MVC con POST, antiforgery y redirección local. El ABM de publicaciones continúa en Vue/AJAX; no se agrega otra dependencia de frontend.

## Privacidad y consistencia

Las dos listas son **personales**, sin rutas para consultar la colección o los seguimientos de otra cuenta. No se publican conteos de seguidores ni identidades de quienes guardaron una obra. Los IDs del actor se obtienen de la sesión, nunca de campos enviados por el cliente.

Solo se pueden guardar publicaciones publicadas, visibles por moderación y accesibles para la cuenta. Los borradores y archivados no se guardan, incluso si son propios. Una publicación privada propia que esté publicada puede guardarse para uso personal. Seguir requiere un perfil actualmente accesible y una cuenta activa; no concede permisos sobre sus publicaciones.

Si cambia la privacidad del perfil/publicación, se archiva u oculta una obra o se suspende su autor, el contenido deja de aparecer en las listas y sus contadores. El filtrado ocurre **en MySQL antes de contar y paginar**. No hay tarjetas ni miniaturas que revelen destinos inaccesibles. Los contadores de Mi estudio también cuentan solo relaciones cuyo destino sigue disponible.

La relación guardada se conserva y puede reaparecer si vuelve a ser accesible. Cada lista permite **quitar relaciones no disponibles**; esta limpieza afecta exclusivamente a la cuenta autenticada y no elimina obras ni usuarios. También se puede quitar una relación propia por su identificador aunque el destino se haya ocultado. Si la publicación se elimina, su favorito se elimina por la FK en cascada.

Guardar o seguir repetidamente deja una sola relación. Las claves compuestas de MySQL resuelven incluso altas simultáneas; las bajas son idempotentes. Una baja de seguimiento usa una subconsulta de IDs para que el proveedor genere un DELETE compatible con MySQL. No se ofrecen todavía endpoints JWT para estas funciones; sus rutas usan la sesión web y rol Creador.

## Archivos y rutas

| Responsabilidad | Fuente |
| --- | --- |
| Consultas autorizadas, altas, bajas y limpieza | [ComunidadService.cs](../src/Trazo.Web/Services/ComunidadService.cs) |
| Listas privadas y acciones MVC | [ComunidadController.cs](../src/Trazo.Web/Controllers/ComunidadController.cs) |
| DTO específicos | [ComunidadModels.cs](../src/Trazo.Web/Models/ComunidadModels.cs) |
| Vistas personales | [Favoritos.cshtml](../src/Trazo.Web/Views/Comunidad/Favoritos.cshtml), [Seguidos.cshtml](../src/Trazo.Web/Views/Comunidad/Seguidos.cshtml) |
| Botones y contadores | [Detalle](../src/Trazo.Web/Views/Explorar/Detalle.cshtml), [perfil](../src/Trazo.Web/Views/Perfil/Ver.cshtml), [Mi estudio](../src/Trazo.Web/Controllers/PanelController.cs) |
| Pruebas MySQL | [ComunidadIntegrationTests.cs](../tests/Trazo.Tests/ComunidadIntegrationTests.cs) |
| Recorrido de navegador | [Probar-Comunidad.mjs](../tools/Probar-Comunidad.mjs) |

| Método | Ruta | Uso |
| --- | --- | --- |
| GET | `/mis-favoritos?q=&pagina=1` | Favoritos disponibles de la sesión |
| GET | `/mis-seguidos?q=&pagina=1` | Perfiles seguidos disponibles de la sesión |
| POST | `/favoritos/{id}/guardar` / `/quitar` | Agregar/quitar guardado propio |
| POST | `/seguimientos/{usuario}/seguir` / `/quitar` | Seguir/dejar de seguir un nombre de usuario |
| POST | `/mis-favoritos/limpiar` | Retirar favoritos propios no disponibles |
| POST | `/mis-seguidos/limpiar` | Retirar seguimientos propios no disponibles |

Las escrituras admiten antiforgery de formulario o cabecera `X-CSRF-TOKEN`. `returnUrl` solo se usa si es local; en caso contrario se vuelve a la lista personal. Un GET no modifica relaciones. Los errores de negocio mantienen 400 para autoseguimiento y 404 para destinos inaccesibles; la ausencia de sesión MVC redirige al login.

## Validación y evidencia

Compilación Release comprobada con `dotnet build Trazo.sln -c Release --no-restore`: 0 errores y 0 advertencias.

**61 pruebas .NET aprobadas con MySQL real; 0 fallidas y 0 omitidas.** Se agregaron 6 escenarios de integración a las 55 pruebas anteriores. Cubren operaciones repetidas/simultáneas, aislamiento entre cuentas, campos de actor manipulados, CSRF, métodos HTTP, redirecciones, autoseguimiento, contenido restringido, cambios de privacidad, moderación, archivo, suspensión, limpieza y cascada de eliminación. Para cada lista se prepararon 14 destinos, uno privado: el servidor contó 13 y devolvió 12 en la primera página y 1 en la segunda. Se verificaron búsquedas filtradas.

El recorrido de Edge guarda una publicación temporal de Bruno como Ana, la busca en favoritos, sigue a Bruno y recorre la lista de seguidos. Luego Bruno vuelve privada esa publicación: Ana recibe 404 y su favorito deja de mostrarse. También comprueba visitantes sin acceso a las listas y ambas pantallas a 390 px sin desbordes ni errores JavaScript detectados. Al finalizar elimina el guardado y la publicación temporal; conserva cualquier seguimiento que ya existiera antes del recorrido. En la ejecución registrada no existía el seguimiento, así que se comprobaron tanto su alta como su baja.

- [Video del recorrido](evidencias/incremento-05/comunidad.webm).
- [Favoritos](evidencias/incremento-05/favoritos.png) y [móvil](evidencias/incremento-05/favoritos-movil.png).
- [Seguidos](evidencias/incremento-05/seguidos.png), [móvil](evidencias/incremento-05/seguidos-movil.png) y [perfil](evidencias/incremento-05/perfil-seguido.png).
- [Obra guardada](evidencias/incremento-05/publicacion-guardada.png) y [favorito oculto tras cambiar la privacidad](evidencias/incremento-05/favorito-oculto.png).
- [Métodos, rutas y estados del navegador](evidencias/incremento-05/recorrido.json), sin cookies/tokens.
- [Resumen de pruebas .NET](evidencias/incremento-05/pruebas-dotnet.json).

Para repetir con MySQL y la web encendidos, cuentas demo Ana/Bruno disponibles y Edge instalado:

```powershell
./tools/Probar.ps1 -Integracion
node tools/Probar-Comunidad.mjs
```

Las pruebas .NET recrean solo `trazo_tests`. El navegador usa una publicación temporal en `trazo_dev`, sin reinicializar esa base. La CI existente ejecutará los nuevos tests después del push; el resultado remoto todavía no se certifica.

## Parada para GitHub

No hay paquetes nuevos, preparación adicional ni migraciones. Iniciar la web normalmente y entrar a Mi estudio.

```powershell
git add .
git diff --cached --stat
git commit -m "feat: favoritos privados y seguimiento de creadores"
git push
```

`AGENTS.md`, secretos, archivos subidos y temporales permanecen fuera de Git. No se ejecutó push automático. Después de confirmar este incremento, el siguiente módulo propuesto es reportes y moderación; también quedan los datos finales de entrega, ajustes pendientes del portafolio y la preparación de la defensa. La apertura pública sigue siendo un hito separado.
