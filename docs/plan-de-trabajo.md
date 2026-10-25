# Trazo: investigación y plan de trabajo

Documento complementario a la entrega preliminar. Investigación consultada el **09/10/2026**. Autor del proyecto: Jonathan Muñoz. El nombre **Trazo** es provisional y aún no se verificaron marca ni dominio.

**Actualización del 21/10/2026:** Jonathan autorizó iniciar la implementación y confirmó el vencimiento final para el **05/11/2026**. El primer incremento se amplió a entorno, persistencia, cuentas, perfiles/avatar, exploración de textos demo y administración de etiquetas. El estado comprobado está en [incremento 01](incremento-01.md) y en la [matriz del README](../README.md); las secciones de investigación siguientes incluyen funciones futuras y no certifican que estén implementadas.

**Actualización del 24/10/2026:** el primer incremento fue subido a GitHub y Jonathan pidió continuar. Se implementó el [incremento 02](incremento-02.md): editor Vue, endpoints JSON con cookies/antiforgery y selector paginado de etiquetas. No cambió el DER. Se conserva la consulta pendiente al profesor sobre herramientas auxiliares; Python no ejecuta la aplicación y Docker solo aloja MySQL localmente.

## 1. Concepto y referencias

**Actualización del 25/10/2026, incremento 06:** reportes propios y bandeja administrativa paginados, decisiones con explicación, ocultamiento y revisión/restauración del autor implementados y probados. El acceso administrativo a archivos respeta privacidad y estado actuales. [Registro y evidencia](incremento-06.md). Sin cambios del DER. La gestión de suspensiones queda para el siguiente incremento, después de la parada de push.

**Actualización del 25/10/2026, incremento 05:** favoritos privados y seguimiento de creadores implementados con listas paginadas, búsquedas y permisos aplicados también a contadores. Altas/bajas idempotentes y limpieza de relaciones no disponibles; [registro y evidencia](incremento-05.md). Sin cambios del DER ni nuevas dependencias. Próximo módulo propuesto tras el push: reportes y moderación.

**Actualización del 24/10/2026, incremento 04:** API REST JWT implementada para publicaciones propias y búsqueda de etiquetas. Incluye emisor local, revocación de sesiones y colección Postman comprobada; [registro](incremento-04.md) y [guía](api.md). La API queda deshabilitada fuera de Development/Testing. Sin cambios de DER. Próximo incremento propuesto tras el push: favoritos y seguimiento de creadores.

**Actualización del 24/10/2026, incremento 03:** carga de imágenes/GIF/MP4, enlaces YouTube, orden y descripciones en Vue, portadas, filtro por medio y lectura autorizada de archivos/miniaturas/rangos implementados y probados. [Registro y evidencia](incremento-03.md). Los límites reales están en [medios](medios.md); no son configurables desde el panel todavía. El DER no cambia. Próximo incremento previsto: JWT y colección de pruebas, después del commit/push de Jonathan.

**Propuesta:** un portafolio profesional vivo para diseñadores y animadores, con descubrimiento de obras y una comunidad sencilla. El éxito inicial será que una persona pueda publicar una obra, compartir su perfil y recibir contacto profesional; no se medirá por cantidad de funciones sociales.

La referencia [Amy J. Xu](https://amyjxu.me/) organiza trabajos de animación, ilustración y diseño, incluye categorías y una sección de información. Tomamos la organización alrededor de obras y especialidades como inspiración; no copiamos sus imágenes, identidad ni código. La inspección realizada fue del contenido y la estructura accesibles de la página, no una auditoría visual o de usabilidad completa.

[Behance describe sus proyectos](https://help.behance.net/hc/en-us/articles/204483644-Guide-Project-Basics) como conjuntos de imágenes, texto, videos y otros medios alrededor de una idea. Esto respalda una unidad editorial más rica que una imagen aislada. La adaptación propuesta para Trazo es combinar obras extensas y actualizaciones breves en una sola entidad Publicacion, distinguidas por Tipo.

La hipótesis de valor es reunir presentación profesional, exploración sin registro y privacidad explícita. Falta validarla con al menos dos creadores y un reclutador mediante un prototipo y tareas concretas: encontrar una especialidad, comprender la participación del autor y localizar contacto. No se afirma haber realizado entrevistas ni demostrado demanda comercial.

## 2. Producto que se propone construir

### Experiencia pública

Navegación: **Explorar**, perfil del creador y detalle de publicación. Explorar mostrará obras y actualizaciones permitidas para el visitante, con filtros combinables por texto, etiqueta, tipo y medio. El orden inicial será reciente; no habrá ranking de popularidad. Cada perfil tendrá una selección de trabajos destacados y debajo su actividad.

Una obra incluirá título, descripción, participación del autor, créditos y recursos ordenados; en la primera versión, participación y contexto se escribirán en Texto y los créditos del archivo en RecursoMultimedia.Creditos. Una actualización podrá ser solo texto. No se obliga a subir una imagen ficticia para poder publicar.

El contacto se resuelve mediante correo profesional opcional y enlaces elegidos por el creador. No habrá formulario de contratación ni mensajería interna en el MVP. Un email publicado puede recibir spam: el editor mostrará claramente cuándo quedará visible y permitirá ocultarlo. Nunca se usará automáticamente el correo de inicio de sesión.

### Personalización

Oscuro por defecto, modo claro, color de acento validado, dos disposiciones de galería, biografía, avatar y orden de obras destacadas. Los controles serán predefinidos; la personalización libre con HTML, JavaScript o CSS queda fuera. Así se mantiene una experiencia reconocible para reclutadores, una interfaz accesible y un alcance defendible.

Diseño previsto: fondo grafito, tarjetas de obra con bastante espacio, tipografía legible y acento elegido por el usuario. Deben mantenerse contraste, foco visible y navegación por teclado. Se evitará la reproducción automática de videos; los GIF tendrán una vista previa estática o control de reproducción, y se respetará la preferencia de movimiento reducido. El texto alternativo será parte del editor. Esto es una dirección de diseño, no una interfaz ya realizada.

### Comunidad acotada

Seguir/dejar de seguir a creadores y guardar/quitar favoritos. Favoritos será una lista privada y paginada. El listado de creadores seguidos será personal, sin exponer por defecto el grafo social completo. Los autores no reciben acceso a una lista identificable de quienes guardaron sus publicaciones. No se implementarán likes adicionales, comentarios, chat, notificaciones en tiempo real ni SignalR en el MVP.

### Límites iniciales propuestos

Valores configurables, a validar con ejemplos reales y presupuesto de alojamiento: avatar 2 MB; imágenes hasta 10 MB; GIF hasta 15 MB; MP4 hasta 50 MB y 60 segundos; máximo 10 recursos por publicación y 500 MB por cuenta en la beta. Limitar también resolución y cantidad total de fotogramas procesados para no agotar memoria. Un archivo no compatible se rechaza con una explicación; no se promete transcodificación universal.

MP4 se comprobará por contenedor y códecs compatibles (objetivo H.264/AAC), no solo por extensión. Para trabajos largos se ofrecerá YouTube. La beta necesita un presupuesto de almacenamiento, transferencia y moderación antes de fijar cuotas comerciales. Estos límites son decisiones de producto propuestas, no requisitos de la cátedra.

## 3. Privacidad sin ambigüedades

| Visibilidad efectiva | Visitante | Otra cuenta activa | Propietario |
| --- | --- | --- | --- |
| Pública | Sí | Sí | Sí |
| Usuarios registrados | No | Sí | Sí |
| Privada | No | No | Sí |
| Borrador / archivada | No | No | Sí |

Se aplica la restricción más fuerte entre perfil y publicación. El estado de cuenta y la moderación pueden impedir la difusión aunque la visibilidad sea pública. Una publicación oculta por moderación no reaparece en Explorar por editarla; el propietario puede consultar el motivo y solicitar revisión.

**Administrador no significa lector universal.** Podrá moderar publicaciones públicas y de usuarios registrados y ver los reportes por su función. Las obras privadas y borradores ajenos no se abrirán automáticamente con ese rol. Un requerimiento legal excepcional exige un procedimiento documentado; no se incorpora un acceso indiscriminado en la interfaz.

EmailContacto y EnlacePerfil tendrán como techo la visibilidad del perfil, y además VisibilidadContacto (Pública / Usuarios registrados / Oculta). Los DTO públicos excluyen Email de acceso, hash, estado interno, rutas físicas, datos de reportes y demás información innecesaria.

La autorización se aplica en el servidor **antes de contar o paginar**, en detalle, miniaturas, descarga, metadatos, favoritos, resultados AJAX y API. Un recurso sin acceso no filtra título, existencia o URL mediante una tarjeta. Cambiar de público a privado invalida cachés de la aplicación y bloquea nuevas solicitudes; no puede recuperar copias que terceros ya hayan descargado. El atributo noindex y las URL difíciles de adivinar no constituyen control de acceso.

Los medios se guardan fuera de `wwwroot`. Un controlador o servicio comprueba la publicación antes de servirlos. Para contenido restringido se usarán respuestas sin caché compartida; las solicitudes parciales de video también requieren autorización. Si luego se usan URLs firmadas, deben expirar rápido y respetar cambios de visibilidad. La fase académica puede usar siempre lectura autorizada desde el servidor.

YouTube conserva sus propios permisos. Un video público o no listado puede circular fuera de Trazo: poner privada la publicación no lo protege en YouTube. La interfaz deberá explicar esa diferencia. Para contenidos confidenciales no se recomienda depender de un enlace no listado.

## 4. Arquitectura y versiones

Se propone un **monolito modular**: un proyecto ASP.NET Core MVC con controladores MVC, controladores JSON para Vue y controladores REST con JWT; servicios compartidos para reglas de negocio y autorización; EF Core para persistencia. No se necesitan microservicios ni un frontend completo separado para satisfacer el requisito SPA.

| Componente | Decisión inicial |
| --- | --- |
| Backend | C#, ASP.NET Core 10 LTS, MVC/Razor y Web API |
| Persistencia | EF Core 10, MySQL; candidato de servidor MySQL 8.4 LTS |
| Proveedor EF | Candidato oficial MySql.EntityFrameworkCore 10; fijar versión exacta tras prueba de compatibilidad y revisión de licencia |
| Identidad | ASP.NET Core Identity; roles Creador y Administrador |
| Web | Cookies de sesión seguras; Vue 3 en el panel; Bootstrap 5 |
| API | Esquema JwtBearer explícito y colección Postman |
| Archivos | Disco del servidor fuera de la raíz pública en local; abstracción de almacenamiento para futura nube |
| Fuera del alcance inicial | SignalR, ADO.NET directo, .NET Framework clásico, microservicios |

[Microsoft fija el fin de soporte de .NET 8 en el 10/11/2026 y el de .NET 10 en el 14/11/2028](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Por eso se propone .NET 10 para este proyecto nuevo. El proveedor oficial publica soporte para EF Core 10 en [MySQL Connector/NET](https://github.com/mysql/mysql-connector-net) y su [paquete NuGet](https://www.nuget.org/packages/MySql.EntityFrameworkCore/). No se debe asumir que cambiar la versión del SDK actualiza por sí solo la compatibilidad del proveedor.

[EF Core advierte que los proveedores normalmente no funcionan entre versiones mayores diferentes](https://learn.microsoft.com/en-us/ef/core/providers/). Antes de fijar paquetes se probarán migraciones, Identity, índices, claves compuestas, paginación y persistencia de fechas con la combinación real. Se usarán versiones estables y soportadas, nunca prereleases para resolver apresuradamente una incompatibilidad.

Revisión local del 09/10/2026: SDK **10.0.400** disponible; Git disponible; carpeta de **MySQL Workbench 8.0 CE** presente. No se detectó un comando mysql ni un servicio con nombre MySQL/MariaDB. Esto no descarta Docker, XAMPP, otro puerto o un servidor remoto; todavía no se inspeccionaron ni modificaron. Workbench es un cliente y no prueba que exista un servidor local en ejecución.

### Cookies y JWT tienen recorridos separados

- MVC y el panel Vue del mismo origen usan cookies HttpOnly/Secure y protección antiforgery en toda operación de escritura. El panel consume `/panel/publicaciones` como interfaz y endpoints JSON bajo `/panel-json/`.
- `/api/v1/` acepta el esquema Bearer explícitamente. No basta con estar logueado por cookies. Los endpoints comprueban firma, emisor, audiencia, vencimiento, estado de la cuenta, rol y propietario.
- MVC, AJAX y JWT invocan los mismos servicios y políticas para evitar diferencias de permisos.
- La colección académica probará CRUD de publicaciones, consulta paginada y escenarios de error. Podrá usar emisión de JWT restringida a desarrollo, con credenciales de prueba verificadas mediante Identity y claves fuera del repositorio. No se desplegará ese emisor de demostración en producción.
- Antes de exponer una API pública se adoptará un flujo estándar OAuth/OIDC con un emisor mantenido, PKCE para clientes interactivos, revocación y rotación apropiadas. Si la API no se ofrece todavía, se deshabilita en el despliegue público; la web MVC conserva su funcionamiento con cookies.

[Microsoft recomienda validar íntegramente los JWT, mantener los tokens fuera del almacenamiento accesible al JavaScript del navegador y emplear estándares para su emisión en producción](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0). El bearer incorporado de ciertas APIs de Identity no debe confundirse con el requisito específico de JWT.

## 5. Cómo se demostrará cada requisito

| Consigna | Implementación prevista | Comprobación concreta |
| --- | --- | --- |
| 6 entidades, 1:N y N:M | 7 principales; publicación-recursos y publicación-etiquetas | DER coherente, migraciones y registros relacionados reales |
| Login/logout, roles y avatar | Identity, perfil editable, administración exclusiva | Una cuenta creadora recibe 403 en administración; login y cierre efectivos |
| Archivo adicional al avatar | Recursos multimedia de publicaciones | Carga, validación, lectura autorizada y eliminación |
| ABM Vue con AJAX/JSON | Panel de publicaciones completo | Alta, listado, edición y baja sin recarga; errores de validación visibles |
| Paginación backend | Explorar, publicaciones propias, favoritos, seguidos, cuentas, etiquetas administrativas y reportes | Red muestra solo la página pedida; no hay descarga inicial de todo el conjunto |
| Relacionados por AJAX | Selector de etiquetas en el editor | Buscar término, recibir coincidencias limitadas, seleccionar y persistir la asociación |
| REST + JWT | `/api/v1/publicaciones` y consulta relacionada | Postman: token válido, vencido, alterado, sin token, rol incorrecto y otro propietario |

Contrato implementado para el panel: parámetros `pagina`, `q` y `estado`; respuesta `items`, `numero`, `tamano`, `total`, `paginas`, `anterior` y `siguiente`. Tamaño fijo de 12 publicaciones y 10 etiquetas por petición; el cliente no puede solicitar el catálogo completo mediante pageSize. Consulta EF con filtro de autorización, proyección, orden estable, `Skip`/`Take` y ejecución en base de datos. No hacer `ToList()` antes de paginar. Revisar índices según consultas: autor/fecha, visibilidad/estado/fecha, etiqueta normalizada y ambos sentidos útiles de las asociaciones.

Selector: solicitud con término y página, máximo 20 coincidencias, búsqueda con una breve espera tras teclear y cancelación de respuestas antiguas. Cargar solo etiquetas seleccionadas al editar. Los IDs recibidos se vuelven a validar en el servidor. El CRUD de etiquetas es administrativo; crear nuevas etiquetas desde el selector no es parte del MVP.

Datos ficticios para defensa: dos creadores, un administrador, contenido público, de usuarios registrados y privado; suficientes publicaciones para varias páginas; obra multimedia, actualización textual, favoritos, seguimientos y reporte resuelto. No utilizar obras ajenas sin permiso. Las credenciales de prueba se documentan solo para el entorno académico y el sembrado queda bloqueado en producción.

## 6. Seguridad desde el primer incremento

No existe una garantía de ausencia de ciberataques. Se define una base verificable y se requiere operación continua. [OWASP ASVS](https://owasp.org/projects/asvs) servirá como referencia de requisitos; se priorizarán los controles aplicables y una revisión equivalente al nivel 2 antes de abrir registros públicos, sin afirmar certificación.

| Riesgo del producto | Control previsto | Prueba de aceptación |
| --- | --- | --- |
| Acceder a obras ajenas cambiando IDs | Política por recurso; negar por defecto; DTO mínimos | Cuenta A no lee ni edita el privado de B, tampoco archivo o miniatura |
| XSS mediante texto, enlaces o tema | Codificación Razor/Vue; no HTML crudo; esquemas URL permitidos; CSP | Texto con etiquetas se muestra como texto; se rechaza javascript: |
| CSRF en MVC y AJAX | Antiforgery en escrituras con cookies; SameSite adecuado | Petición sin token antiforgery rechazada; logout por POST |
| Inyección SQL / sobreasignación | EF parametrizado; DTO explícito; AutorId desde sesión | No se acepta cambiar autor, rol ni estado administrativo por JSON |
| Robo de cuentas | Identity, errores genéricos, limitación de intentos, recuperación de un uso | No enumerar emails; tokens de recuperación vencen y no se reutilizan |
| Cuenta suspendida con sesión vigente | Revalidar estado/versión de seguridad en accesos protegidos | Cookie o JWT previo deja de permitir acciones al suspender la cuenta |
| JWT falsificado | Validación criptográfica y de claims; algoritmos permitidos explícitos | Token alterado, vencido, de otro emisor o audiencia devuelve 401 |
| Agotamiento de disco/ancho de banda | Cuotas por usuario, límite de petición y frecuencia, limpieza de huérfanos | Se rechaza exceso sin dejar archivos accesibles ni operaciones incompletas |
| Filtración operativa | Secretos fuera de Git, logs mínimos, HTTPS, copias protegidas | Repositorio/colección sin claves; restauración de backup ensayada |

La autorización se verificará en cada petición y también al servir archivos; ocultar un botón no protege un endpoint. Estas decisiones siguen los principios de [OWASP Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html).

Para subidas: lista cerrada de formatos, comprobar firma y contenido decodificado además de MIME/extensión, renombrar con identificadores del servidor, limitar tamaño y trabajo de procesamiento, cuarentena, permisos mínimos y escaneo en producción. No aceptar SVG, HTML, ejecutables o archivos comprimidos en el MVP. Quitar metadatos sensibles cuando sea posible. Rechazar recursos no procesados; nunca publicar un archivo solo porque la carga terminó. Basado en [OWASP File Upload](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html).

No descargar URL arbitrarias desde el servidor para producir vistas previas. En YouTube se valida un host exacto permitido y se extrae el ID; en el resto de enlaces se almacena la URL sin consultar recursos internos. Enlaces externos con protección frente al acceso a la ventana de origen. Los reproductores externos se cargan tras acción del visitante, indicando el tercero que recibirá la conexión.

Antes de producción: email confirmado para publicar, MFA para administradores, dependencias actualizadas, claves de Data Protection persistidas y protegidas, usuario MySQL con permisos mínimos, copias y restauración probadas, almacenamiento aislado, alertas, límites de gastos, plan de incidentes y revisión externa de autorización y carga de archivos. Estos controles no se consideran ya implementados.

## 7. Marco argentino para preparar el lanzamiento

Esta sección es una investigación inicial de obligaciones y decisiones de producto, **no un dictamen jurídico ni una garantía contra reclamos**. Antes de abrir el servicio se necesita revisión legal argentina sobre el funcionamiento real, el responsable y los proveedores elegidos. Se citan normas publicadas y fuentes oficiales; un proyecto de reforma no se trata como ley vigente.

### Datos personales

La [Ley 25.326, texto actualizado](https://www.argentina.gob.ar/normativa/nacional/ley-25326-64790/actualizacion) exige finalidad y calidad de datos, información al titular, consentimiento cuando corresponda, seguridad y confidencialidad. Regula el acceso, rectificación y supresión: el artículo 14 contempla **10 días corridos** para responder al acceso y el 16, **5 días hábiles** para rectificación, actualización o supresión en sus condiciones legales. No basta con un enlace genérico a “privacidad”.

Traducción al producto: recopilar lo necesario, contacto público opcional, canal verificable de solicitudes, exportación de los datos propios y proceso real de cierre de cuenta. Separar aceptación de términos, información de privacidad y permisos opcionales; no condicionar funciones básicas a publicidad. Documentar qué se elimina y qué debe conservarse legítimamente, durante cuánto tiempo y con qué acceso. Un borrado lógico indefinido no resuelve la supresión.

La [AAIP explica las obligaciones de responsables](https://www.argentina.gob.ar/aaip/datospersonales/responsables/obligaciones) y la [inscripción de bases](https://www.argentina.gob.ar/aaip/datospersonales/tramites). Un servicio público con perfiles de personas requiere evaluar y tramitar la inscripción del responsable y las bases que correspondan; no debe asumirse la excepción de agenda personal. La política informará identidad y domicilio del responsable, finalidades, destinatarios, datos obligatorios/opcionales y vías para ejercer derechos.

### Alojamiento y servicios extranjeros

Hosting, backups, correo, analítica y soporte pueden implicar transferencias internacionales, aunque la web esté dirigida a Argentina. La [AAIP detalla mecanismos, países adecuados y cláusulas modelo](https://www.argentina.gob.ar/transferencias-internacionales). Antes de contratar se registrarán país, subencargados y garantías; se elegirá el mecanismo legal aplicable, en lugar de asumir que cualquier nube es válida. Para reducir exposición inicial: sin publicidad ni analítica de terceros y con cargas de medios externos a demanda.

### Obras, licencias y derechos de imagen

La [Ley 11.723, texto actualizado](https://www.argentina.gob.ar/normativa/nacional/ley-11723-42755/actualizacion) protege obras artísticas, fotografías, audiovisuales y otras creaciones. La propuesta es que el autor conserve sus derechos y otorgue una licencia no exclusiva limitada a alojar, procesar técnicamente y mostrar la obra conforme a su visibilidad. Publicar no significa autorizar su reventa ni su entrenamiento de IA. Estos usos adicionales requerirían un análisis y acuerdo propios; no están previstos.

El [artículo 53 del Código Civil y Comercial](https://www.argentina.gob.ar/normativa/nacional/ley-26994-235975/actualizacion) regula el consentimiento para captar o reproducir imagen o voz y sus excepciones. Ser titular de una ilustración no prueba tener permisos sobre música, fotografías, personas retratadas o material de un cliente. El editor debe permitir créditos y exigir que quien publica tenga las autorizaciones pertinentes, incluso frente a acuerdos de confidencialidad.

Se prepararán normas de contenido y un procedimiento de denuncia, revisión, retiro cuando corresponda y respuesta al autor. Habrá un canal accesible sin cuenta para titulares de derechos; los reportes internos autenticados no lo reemplazan. No se copiará un procedimiento DMCA estadounidense como si fuera automáticamente el régimen argentino. Tampoco una cláusula de exención elimina por sí sola la responsabilidad de la plataforma.

### Decisiones previas a la apertura

Se propone una beta para mayores de 18 años, como decisión de alcance y moderación, no como afirmación de que toda plataforma argentina deba aplicar ese límite. Incorporar menores requiere revisar consentimiento, resguardo y procedimientos adicionales. Sin pagos ni marketplace en el MVP; si se incorporan suscripciones, contratación o comercialización, habrá que analizar específicamente consumo, cancelación, facturación e impuestos. La marca y el dominio también deben verificarse antes de invertir en identidad pública.

Se deberán aprobar documentos y procesos concretos: términos de uso, privacidad, normas de contenido, política de retención, permisos de publicación, contacto para derechos y reclamos, proveedores y respuesta a incidentes. El modelo de producción probablemente agregue historial de aceptación de versiones, auditoría de moderación y solicitudes de derechos; esas tablas de soporte no están comprometidas en el DER académico inicial y deberán modelarse antes de implementar esos procesos. El historial no debe depender de un solo campo que se sobrescribe.

## 8. Incrementos y paradas para GitHub

**Acuerdo:** al finalizar cada incremento, presentar cambios, prueba breve y mensaje de commit sugerido; detenerse para que Jonathan revise y haga commit/push. No avanzar al siguiente bloque hasta que lo confirme. No ejecutar push por su cuenta. La publicación a producción es una etapa posterior.

| Incremento | Resultado verificable | Parada |
| --- | --- | --- |
| 0 - Actividad preliminar | PDF, DER, narrativa y alcance preparados; implementación autorizada por Jonathan | No se infiere aprobación docente a partir de esa autorización |
| 1 - Base funcional ampliada | MVC, MySQL, migración, Identity, perfiles/avatar, exploración paginada, etiquetas por rol y pruebas | Push verificado el 24/10 |
| 2 - Editor Vue y relacionados | ABM de publicaciones propio vía AJAX/JSON, selector asíncrono de etiquetas y privacidad | Implementado y probado el 24/10; espera revisión y push |
| 3 - Archivos y medios | Imágenes, GIF, video corto y YouTube con lectura autorizada, límites y créditos | Pruebas de subida/descarga y push |
| 4 - API REST con JWT | Autenticación y endpoints, permisos compartidos, colección Postman | Pruebas positivas/negativas y push |
| 5 - Comunidad y moderación | Destacados, favoritos, seguimientos, reportes y suspensión administrable | Pruebas de privacidad, paginado y roles; push |
| 6 - Promoción | SQL con datos ficticios, instrucciones desde clon limpio, matriz real y defensa | Ensayo de instalación y push final académico |
| 7 - Preparación productiva | Seguridad, operación, contratos, derechos, proveedores y revisión legal | Decisión separada de apertura de beta |

La seguridad y autorización se incorporan en cada bloque; la preparación productiva no posterga su implementación básica. La fecha **15/10/2026** corresponde a la propuesta y **05/11/2026** al proyecto completo. Se avanzará con prioridad en los mínimos evaluables, reservando el cierre para reproducibilidad, evidencias y defensa. Cada bloque puede dividirse si la validación lo necesita; no se avanzará por encima de una parada de push pendiente.

## 9. Próximas decisiones

El siguiente paso es revisar y subir el incremento 06. Luego se incorporará la gestión administrativa de suspensiones de cuentas. Quedan también destacados/personalización pendiente y el cierre reproducible de la entrega. Las versiones probadas y los pasos de MySQL/VS Code figuran en el README y en desarrollo.md. Queda verificar con el docente cualquier observación sobre el alcance y las herramientas auxiliares. El nombre de trabajo puede cambiar sin afectar el modelo. No publicar contraseñas reales, claves JWT, datos de usuarios ni archivos sin licencia en GitHub.
