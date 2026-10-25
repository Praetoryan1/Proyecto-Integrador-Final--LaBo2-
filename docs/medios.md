# Medios de publicaciones

Implementados en el incremento 03. Se administran desde **Mi estudio → Administrar mis publicaciones**. Guardar primero el borrador; después agregar recursos y, finalmente, elegir Publicada. Una obra publicada siempre necesita al menos un recurso listo. Una actualización puede ser solo texto.

## Preparación en VS Code

En Windows, abrir una terminal **PowerShell 7** en la raíz del proyecto:

```powershell
./tools/Preparar-Medios.ps1
dotnet run --project src/Trazo.Web --launch-profile https
```

La primera línea descarga FFmpeg/ffprobe **9.0.2**, verifica el SHA-256 fijado y los guarda en `tools/ffmpeg/bin`, junto con su licencia. No cambia PATH ni necesita permisos de administrador. Los binarios y el ZIP quedan ignorados por Git. En el equipo de Jonathan ya se realizó este paso. No se crea otra base ni se aplica una migración nueva.

En Linux, instalar `ffmpeg` y `ffprobe` desde el gestor de paquetes (Ubuntu: `sudo apt-get install ffmpeg`). También se pueden configurar rutas absolutas mediante `Media:FfmpegPath` y `Media:FfprobePath`, por ejemplo en User Secrets. Debe ser una compilación que incluya el codificador `libx264`. La CI usa el paquete del runner Ubuntu; no se afirma que tenga la misma versión que Windows ni que genere bytes idénticos.

FFmpeg es una herramienta de procesamiento invocada por C#, necesaria para GIF/MP4. No cambia la arquitectura MVC ni requiere Python. Sin ella siguen funcionando imágenes estáticas y enlaces YouTube; GIF/MP4 devuelven un mensaje de herramienta no configurada. Está pendiente la respuesta docente sobre herramientas auxiliares.

## Límites aplicados en el servidor

Aquí MB significa 1024 × 1024 bytes. Los valores están centralizados en `AlmacenMedios`; todavía no se editan desde configuración ni desde un panel.

| Medio | Entrada admitida | Resultado |
| --- | --- | --- |
| JPG/JPEG, PNG y WebP estáticos | 10 MB; 16 millones de píxeles; decodificación completa y extensión coincidente | PNG normalizado, orientación corregida, sin copiar metadatos de origen; miniatura PNG |
| GIF | 15 MB; 30 s; 300 cuadros; 4 millones de píxeles por cuadro; 120 millones acumulados | GIF reprocesado y primer cuadro como miniatura |
| MP4 | 50 MB; 60 s; 3600 cuadros; máximo 1920 por lado y 2.073.600 píxeles; una pista H.264 y hasta una AAC | MP4 H.264/AAC normalizado y miniatura PNG |
| YouTube | URL HTTPS de youtube.com, www.youtube.com, m.youtube.com o youtu.be; ID de 11 caracteres | Solo se persiste el ID validado; nunca HTML ni una descarga del servidor |

Máximo **10 recursos por publicación** y **500 MB por cuenta**. La cuota cuenta los archivos normalizados más sus miniaturas; YouTube no almacena bytes locales. Cada imagen/GIF procesado, incluida su miniatura, debe caber en 25 MB; cada MP4 y miniatura, en 50 MB. Un original admitido puede rechazarse si su resultado supera ese límite. No hay compatibilidad universal con todos los videos: HEVC, otros códecs, pistas extra o archivos dañados se rechazan.

Cada recurso necesita una descripción accesible de hasta 500 caracteres; créditos opcionales hasta 300. Los botones ↑/↓ cambian el orden; el primer recurso local listo se usa como portada. Se puede editar descripción/créditos o eliminar un recurso. Para quitar el último de una obra publicada, guardarla primero como borrador.

## Lectura y permisos

Los archivos se guardan en `src/Trazo.Web/App_Data/medios`, fuera de `wwwroot`, con nombres aleatorios. `Media:Root` permite elegir otro directorio. **La copia de seguridad debe incluir la base y ese directorio**, además de los avatares. La ruta física nunca se devuelve al cliente.

`GET/HEAD /medios/{id}/archivo` y `/vista-previa` comprueban la visibilidad efectiva del perfil y publicación, estado editorial, moderación y cuenta antes de servir contenido. También lo hacen para rangos de video (`Range`), con respuestas `no-store`. El administrador no obtiene acceso general a borradores/privados ajenos. Un cambio de privacidad bloquea nuevas solicitudes; no recupera copias que otros ya descargaron.

Los GIF comienzan en una imagen estática y tienen controles Reproducir/Detener. Los MP4 tienen controles nativos y no se reproducen automáticamente. YouTube solo se carga cuando el visitante pulsa el botón; se utiliza `youtube-nocookie.com`, con aviso previo. La privacidad configurada en Trazo no modifica la del video en YouTube. El comportamiento externo depende también del permiso de inserción, disponibilidad y políticas de YouTube. La descripción de un video no sustituye subtítulos: la carga de pistas de subtítulos queda pendiente.

## Integridad y límites operativos

Toda escritura requiere sesión de Creador, propiedad de la publicación, antiforgery y versión vigente. Un cambio de recursos renueva `Publicacion.Version`, igual que el editor de texto. MySQL serializa modificaciones multimedia del mismo autor para comprobar su cuota; una carga fallida descarta los archivos preparados. El orden usa posiciones temporales dentro de una transacción para respetar el índice único.

Al eliminar, se retira primero la referencia de la base y después se borran los archivos. Si el sistema de archivos falla, se registra el incidente: puede quedar un archivo huérfano inaccesible por las rutas de la aplicación. Un cierre abrupto también puede dejar temporales `.upload`. No existe todavía un recolector automático; revisar esos casos con respaldo y contraste con la base, sin borrar medios referenciados. Borrar una publicación con reportes sigue bloqueado; se puede archivar.

El procesamiento admite dos operaciones simultáneas por instancia y 90 segundos por proceso externo. Valida dimensiones y códecs antes de contar cuadros, limita tamaños de entrada/salida y usa argumentos de proceso sin shell. Son controles académicos iniciales, no un aislamiento completo de archivos hostiles. Antes de abrir registros públicos faltan trabajadores aislados, cuotas de CPU/memoria/disco a nivel de sistema, cola y límites de frecuencia por usuario, observabilidad, análisis de archivos y recuperación operativa. Deben configurarse también límites del proxy/servidor de alojamiento. El incremento no habilita producción.

## Referencias técnicas

- [Documentación oficial de ffprobe](https://ffmpeg.org/ffprobe.html): inspección de streams, formato y cuadros.
- [Descargas de FFmpeg](https://ffmpeg.org/download.html) y [compilaciones Windows de Gyan](https://www.gyan.dev/ffmpeg/builds/): origen del ejecutable portable. El script fija versión y hash.
- [Inventario de dependencias](dependencias.md): revisar licencias antes de distribuir ejecutables o publicar comercialmente.
