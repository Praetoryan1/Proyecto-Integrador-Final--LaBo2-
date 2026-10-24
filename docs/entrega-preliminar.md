# 1. Narrativa del Proyecto y Descripción del Problema

## Datos del grupo

**Materia:** Laboratorio II - Proyecto Integrador Final.  
**Modalidad:** individual.  
**Integrante (Apellido, Nombre):** Muñoz, Jonathan.  
**Email:** jonathanezequielm20@gmail.com.  
**Fecha prevista de entrega preliminar:** 15/10/2026.

## Título y temática

**Trazo - Plataforma de portafolios creativos.** Nombre de trabajo, sujeto a revisión antes de su lanzamiento comercial.

**Temática:** exhibición profesional de obras de diseño gráfico, ilustración y animación, con descubrimiento de contenido e interacción social básica.

## Descripción de la problemática

Los diseñadores y animadores necesitan presentar sus trabajos de forma visual, explicar su participación en cada obra y facilitar el contacto con potenciales empleadores. Una red social generalista privilegia la publicación reciente, mientras que un sitio personal aislado requiere mantenimiento y ofrece pocas herramientas para descubrir otros creadores. Como resultado, las obras, los procesos de trabajo y los datos de contacto suelen quedar dispersos entre distintas plataformas.

Los reclutadores necesitan recorrer trabajos y perfiles sin registrarse obligatoriamente, localizar especialidades mediante búsquedas y acceder a información profesional clara. A su vez, los creadores necesitan decidir qué compartir públicamente, qué mostrar a usuarios registrados y qué conservar en privado.

## Propuesta de solución

Se propone un sistema web MVC que centralice portafolios personalizables y publicaciones multimedia. Cada creador tendrá un perfil con presentación, avatar, enlaces de contacto y trabajos destacados. Podrá publicar obras o actualizaciones de texto, acompañarlas con imágenes, GIF, videos cortos y enlaces de YouTube, e identificarlas mediante etiquetas.

La sección Explorar ofrecerá búsqueda y filtros con paginación desde el servidor. Los visitantes podrán consultar el contenido público sin cuenta. Los usuarios registrados podrán administrar su portafolio, seguir creadores, guardar favoritos y reportar publicaciones. Un administrador gestionará etiquetas, reportes y suspensiones de cuentas.

Se utilizarán C#, ASP.NET Core MVC y Web API, Entity Framework Core con MySQL, cookies para la sesión web y JWT para endpoints de API. Vue 3 resolverá el ABM de publicaciones mediante AJAX/JSON; Bootstrap 5 se utilizará como base de interfaz. Se propone .NET 10 LTS, sin cambiar la arquitectura solicitada por la cátedra.

<!-- PAGEBREAK -->

## Alcance funcional de la solución

**Cuenta y perfil.** Registro, inicio y cierre de sesión; edición de avatar, biografía, especialidad, disponibilidad laboral y enlaces profesionales. El correo de acceso no se publicará automáticamente. El perfil permitirá elegir tema oscuro o claro, color de acento, disposición de galería y orden de obras destacadas mediante opciones predefinidas.

**Publicaciones.** Alta, listado, modificación y baja desde un panel Vue 3 sin recargar la página. Se admitirán publicaciones de tipo Obra y Actualización, con título, texto y recursos ordenados. Una actualización podrá contener solo texto. Las obras requerirán al menos un recurso listo para publicarse. Se podrán guardar borradores y archivar publicaciones.

**Archivos y enlaces.** Carga de imágenes JPG, PNG, WebP, GIF y videos cortos MP4 compatibles, además del avatar. Se validarán tipos, tamaños y permisos en el servidor. Para YouTube se guardará un identificador validado del video, sin aceptar código HTML arbitrario. El almacenamiento local del servidor será suficiente para la demostración académica.

**Explorar.** Búsqueda por texto y filtros por etiquetas, tipo de publicación y medio. Los resultados se ordenarán por fecha e identificador, con paginación en el backend. La selección de etiquetas en el editor se hará mediante búsqueda AJAX al servidor.

**Funciones sociales.** Seguimiento de creadores y favoritos personales, sin chat ni comentarios en esta versión. Seguir a alguien o guardar una publicación no concederá acceso adicional a contenido restringido.

**Administración.** Solo el rol Administrador podrá mantener el catálogo de etiquetas, resolver reportes, ocultar publicaciones infractoras y suspender cuentas. Los creadores solo podrán modificar sus propios recursos. El rol no reemplazará la comprobación de propiedad.

## Actores y reglas de visibilidad

- **Visitante:** consulta perfiles, obras y contactos públicos sin iniciar sesión. Puede ser un reclutador; no se requiere un rol especial para contratar o contactar.
- **Creador:** usuario registrado que administra su perfil y publicaciones, sigue creadores, guarda favoritos y presenta reportes.
- **Administrador:** usuario con permisos exclusivos de moderación y gestión del catálogo. La administración no habilita por defecto la lectura de obras privadas ajenas.

El perfil y cada publicación tendrán visibilidad **Pública**, **Usuarios registrados** o **Privada**. Se aplicará la más restrictiva de ambas. Privada significa acceso exclusivo del autor; Usuarios registrados permite el acceso a cualquier cuenta activa autenticada. Borradores y archivos en procesamiento serán visibles únicamente para su propietario.

Explorar, búsquedas, favoritos y archivos devolverán únicamente contenido autorizado. La restricción abarcará también miniaturas, metadatos y enlaces internos. La privacidad de una publicación no modifica los permisos de un video alojado en YouTube.

Quedan fuera de esta entrega: mensajería, comentarios, pagos, ofertas laborales, recomendaciones algorítmicas, edición libre de HTML/CSS y procesamiento avanzado de video. La aplicación académica no se considerará automáticamente habilitada para producción pública.

<!-- PAGEBREAK -->

# 2. Modelo de Datos y Diagrama Entidad-Relación (DER)

## DER - Vista de portafolios y publicaciones

<!-- DIAGRAM CORE -->

El modelo se presenta en dos vistas complementarias para conservar su legibilidad. Las tablas Usuario y Publicacion repetidas en la segunda vista son las mismas entidades, no tablas adicionales. PK: clave primaria; FK: clave foránea; UQ: valor único; ?: admite nulo. Los números junto a cada extremo indican la cardinalidad.

<!-- PAGEBREAK -->

## DER - Vista de comunidad y permisos

<!-- DIAGRAM SOCIAL -->

Cada favorito pertenece a un usuario y una publicación. Seguimiento relaciona dos usuarios con funciones diferentes. Reporte vincula una publicación con su denunciante y, opcionalmente, con el administrador que lo resuelve. Las asociaciones de roles admiten que una cuenta tenga más de un rol.

<!-- PAGEBREAK -->

## Listado y descripción de entidades principales

El diseño incluye **7 entidades principales de dominio**, más **5 tablas asociativas o de autorización**. No se utilizan tablas auxiliares de autenticación para alcanzar el mínimo de seis entidades principales.

| Entidad | Finalidad y atributos principales |
| --- | --- |
| **Usuario** | Cuenta de acceso y estado. Id (PK), EmailNormalizado (UQ), NombreUsuarioNormalizado (UQ), PasswordHash, EmailConfirmado, Estado y FechaAlta. Se implementará extendiendo ASP.NET Core Identity; no se guardarán contraseñas en texto plano. |
| **Perfil** | Identidad profesional y personalización. UsuarioId (PK/FK), NombreVisible, Biografia, Especialidad, DisponibleParaTrabajar, AvatarClave, Tema, ColorAcento, Disposicion, Visibilidad, EmailContacto opcional y VisibilidadContacto. Un perfil por cuenta. |
| **EnlacePerfil** | Enlaces profesionales del perfil. Id (PK), PerfilUsuarioId (FK), Etiqueta, Url y Orden. Permite varios enlaces sin columnas repetidas; solo URL HTTPS validada. Hereda la visibilidad del contacto y del perfil. |
| **Publicacion** | Obra o actualización del creador. Id (PK), AutorId (FK), Tipo, Titulo, Texto, EstadoEditorial, EstadoModeracion, Visibilidad, OrdenDestacado opcional, CreadaUtc, PublicadaUtc opcional, ActualizadaUtc y Version. El estado editorial se separa de la privacidad y la moderación. |
| **RecursoMultimedia** | Archivo o video externo asociado a una publicación. Id (PK), PublicacionId (FK), Tipo, ClaveAlmacenamiento opcional, YouTubeId opcional, Mime, Bytes, EstadoProcesamiento, Orden, TextoAlternativo y Creditos. El binario se almacena fuera de la base de datos. |
| **Etiqueta** | Clasificación para descubrir obras. Id (PK), Nombre, NombreNormalizado (UQ) y Activa. Se selecciona con búsqueda asíncrona y se administra por rol. |
| **Reporte** | Denuncia y resolución administrativa sobre una publicación. Id (PK), PublicacionId (FK), DenuncianteId (FK), Motivo, Detalle, Estado, CreadoUtc, ResueltoPorId (FK opcional), ResueltoUtc y Resolucion opcionales. Los reclamos externos sin cuenta tendrán un canal de contacto independiente. |

Las claves de usuario y rol utilizarán el tipo configurado en Identity; las restantes podrán ser enteros de 64 bits. Las fechas se almacenarán en UTC. Las opciones pequeñas y cerradas (visibilidad, tipo y estado) serán enumeraciones validadas, no tablas artificiales.

<!-- PAGEBREAK -->

## Tablas asociativas y de autorización

| Entidad | Finalidad y claves |
| --- | --- |
| **PublicacionEtiqueta** | Asociación N:M. PublicacionId y EtiquetaId son FK y forman una PK compuesta, para impedir etiquetas duplicadas en una publicación. |
| **Favorito** | Guardado personal de una publicación. UsuarioId y PublicacionId son FK y PK compuesta; incluye CreadoUtc. Los favoritos no serán públicos. |
| **Seguimiento** | Relación dirigida entre cuentas. SeguidorId y SeguidoId son FK a Usuario y PK compuesta; incluye CreadoUtc. Se prohíbe seguirse a uno mismo. |
| **Rol** | Permisos globales: Creador y Administrador. Id (PK) y NombreNormalizado (UQ), administrados mediante Identity. |
| **UsuarioRol** | Asociación N:M entre Usuario y Rol. UsuarioId y RolId son FK y PK compuesta. Los roles privilegiados no podrán elegirse durante el registro. |

Identity incorporará, cuando corresponda, sus tablas técnicas de claims, tokens y accesos externos. Se omiten en este DER preliminar de dominio y se documentarán en el esquema físico de las migraciones.

## Descripción de relaciones y restricciones relevantes

- **Usuario 1:1 Perfil; Perfil 1:N EnlacePerfil.** El perfil se crea junto con la cuenta en una transacción. Sus enlaces profesionales pueden agregarse, ordenarse y eliminarse sin modificar la estructura del perfil.
- **Usuario 1:N Publicacion; Publicacion 1:N RecursoMultimedia.** Cada obra tiene un único autor y cero o más recursos. Una actualización admite cero recursos; publicar una obra exige al menos uno listo. La primera imagen válida por orden se podrá usar como portada.
- **Publicacion N:M Etiqueta.** PublicacionEtiqueta permite clasificar una obra con varias etiquetas y reutilizar cada etiqueta en muchas publicaciones.
- **Usuario N:M Publicacion por Favorito; Usuario N:M Usuario por Seguimiento.** Las claves compuestas evitan duplicados. Estas asociaciones nunca modifican la autorización del contenido.
- **Reporte depende de Publicacion y Usuario.** Debe identificar la publicación denunciada y al denunciante. El resolutor es opcional hasta que un administrador actúe; el backend comprobará ese rol y registrará la resolución.
- **Usuario N:M Rol.** Separa permisos generales de la propiedad de cada publicación. La edición y la lectura aplicarán comprobaciones de rol, propietario, estado y visibilidad.

Para cada recurso se exigirá una sola fuente: archivo almacenado o identificador de YouTube. Habrá unicidad de (PublicacionId, Orden). Las consultas filtrarán autorización y estado antes de contar y paginar. Las claves foráneas preservarán la integridad; la baja de contenido bloqueará su acceso de inmediato y la eliminación física de archivos se coordinará con la base de datos. La retención de reportes y la supresión de cuentas se precisarán antes del lanzamiento público.
