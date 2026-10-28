# Incremento 09 · Demo completa e instalación reproducible

Fecha de cierre: **28/10/2026**. Se agrega un paquete de entrega académica separado de la base de trabajo. No cambia el esquema ni el DER; continúan las dos migraciones existentes.

## Cambios

- `--seed-entrega`: amplía la semilla básica con avatares, obras PNG/GIF/MP4, destacados, contacto, comunidad y casos de moderación. Exige Development y base sin usuarios; no reemplaza una instalación usada.
- `DemoAssets`: 17 archivos geométricos propios con manifiesto SHA-256. Cada recurso posee archivo y miniatura independientes, fuera de la raíz web pública.
- `compose.entrega.yaml` y `Preparar-Entrega.ps1`: MySQL, secretos y almacenamiento separados; importación en base vacía, ejecución y exportación limitada a una demo recién sembrada.
- `database/003-demo-completa.sql`: esquema completo, historial de migraciones y datos ficticios. Se acompaña de los medios binarios; no contiene credenciales de MySQL/JWT ni datos de `trazo_dev`.
- [Guía de instalación y defensa](entrega-y-defensa.md), recorrido automatizado y nueva prueba de integración.

## Validación realizada

La solución compiló en Release sin advertencias ni errores. La suite .NET con MySQL real aprobó **77 pruebas, sin omisiones ni fallos**. La prueba agregada comprueba relaciones demo, tamaños, archivos independientes, lectura autorizada, rangos MP4, avatares y rechazo de una segunda carga sobre usuarios existentes.

Se exportó SQL desde `trazo-entrega`, recién creado. Luego se copió el conjunto de archivos versionables a una carpeta nueva, **sin `.env`, `.env.entrega`, AGENTS.md, App_Data, bin, obj ni node_modules**, y se importó el SQL en `trazo-verificacion`, otro contenedor y volumen. Allí se restauraron paquetes con lockfile, se compiló y se inició la aplicación desde esa copia. Los ajustes de conexión/JWT/almacenamiento fueron generados por el script. La prueba usó el SDK, Docker, certificado HTTPS y caché NuGet del equipo: no equivale a instalar en una máquina sin herramientas previas.

La copia contenía los cambios pendientes de este incremento; **no fue un clon del commit remoto**, que todavía no incluye estos cambios. El comando de instalación quedó ensayado sobre esos archivos. La ejecución de GitHub Actions queda pendiente del push.

En Edge pasaron **51 comprobaciones en la semilla y 51 en la copia restaurada**: paginación pública, portadas decodificadas, login de los tres usuarios, roles, bundle Vue servido, páginas de comunidad, avatares, acceso público/registrado/privado, miniaturas, reproducción de GIF/MP4, rango de 100 bytes, paquete demo no público y lectura JWT. Las capturas de Explorar y perfil móvil se inspeccionaron visualmente. No se detectaron errores JavaScript ni desborde en el perfil de 390 px.

La colección Postman también se ejecutó mediante Newman contra la copia restaurada: **19 solicitudes y 31 aserciones aprobadas, sin fallos**. La excepción TLS se limitó al certificado local de desarrollo. No se ejecutó la interfaz gráfica de Postman. Además, se intentó exportar la base ya utilizada: el script lo rechazó y el hash del SQL permaneció intacto.

- [.NET](evidencias/incremento-09/pruebas-dotnet.json).
- [Semilla](evidencias/incremento-09/semilla/resultado.json) y [restauración](evidencias/incremento-09/restaurada/resultado.json).
- [Explorar restaurado](evidencias/incremento-09/restaurada/explorar.png) y [perfil móvil](evidencias/incremento-09/restaurada/perfil-movil.png).
- [Colección sobre SQL restaurado](evidencias/incremento-09/postman-restaurada.json).
- [Procedencia y hashes](evidencias/incremento-09/instalacion.json).

## Estado y siguiente parada

El paquete académico ya incluye SQL con datos, credenciales demo por rol, colección y evidencia de instalación. Esto no certifica preparación productiva ni aprobación docente. Sigue pendiente el ensayo oral de Jonathan, revisar Actions después del push y resolver cualquier observación del profesor. La apertura pública continúa como hito separado de seguridad, operación y privacidad.

**Parada de commit/push:** `feat: demo completa y entrega reproducible con SQL`. No continuar con otro incremento hasta que Jonathan confirme la subida.
