# Instalación de entrega y recorrido de defensa

La demostración académica usa una base y archivos separados de la instalación de desarrollo. Incluye datos ficticios suficientes para recorrer los módulos sin cargar todo a mano. No es una configuración para publicar Trazo en Internet.

## 1. Abrir una copia del repositorio en VS Code

Instalar Git, .NET SDK **10.0.400**, PowerShell **7.2 o superior** y Docker Desktop con motor Linux activo. En VS Code, usar **Git: Clone**, pegar la URL del repositorio de Jonathan y abrir la carpeta clonada. Abrir una terminal **PowerShell 7** en esa carpeta.

El bundle Vue, SQL y medios demo están versionados. **No se necesita Python, Node ni FFmpeg para navegar por esta demo.** Para modificar Vue se usa Node; para subir nuevos GIF/MP4 se necesita FFmpeg, como se explica más abajo. Docker aloja MySQL; la aplicación sigue ejecutándose en ASP.NET Core.

## 2. Importar y ejecutar

```powershell
dotnet dev-certs https --trust
./tools/Preparar-Entrega.ps1 -DesdeSql -IniciarWeb
```

Aceptar la confianza del certificado si Windows lo solicita. Abrir **https://localhost:7281** y mantener la terminal ejecutándose. `Ctrl+C` detiene la web.

El script restaura dependencias fijadas, compila en Release, inicia MySQL y, **solo si la base no tiene tablas**, importa `database/003-demo-completa.sql`. También copia y verifica por SHA-256 los 17 archivos de `src/Trazo.Web/DemoAssets`. El SQL incluye esquema, datos ficticios y las dos migraciones registradas; **no ejecutar además los SQL 001 y 002** sobre esa base. Los medios binarios están fuera del SQL.

| Instalación | MySQL local | Base | Web |
| --- | --- | --- | --- |
| Desarrollo habitual | 3307 | trazo_dev | https://localhost:7280 |
| Demo de entrega | 3308 | trazo_entrega | https://localhost:7281 |
| Verificación opcional en otra copia | 3309 | trazo_entrega, en otro contenedor | https://localhost:7282 |

La demo usa el proyecto Compose `trazo-entrega`, su propio volumen y `tmp/trazo-entrega/avatars` y `tmp/trazo-entrega/medios`. `.env.entrega` guarda contraseñas y clave JWT aleatorias, ignoradas por Git. Las variables de conexión, JWT y almacenamiento se aplican al proceso del script; no se modifican los User Secrets de desarrollo.

Al repetir el comando, se conservan la base y los archivos existentes. No se reimporta ni se reemplazan obras editadas. Si hay una importación interrumpida o faltan archivos, revisar el error y los respaldos: el script no borra tablas ni volúmenes para intentar repararlo. No borrar `.env.entrega` de una instancia existente, pues contiene sus credenciales. No limpiar `tmp/trazo-entrega` si se quieren conservar los archivos subidos a esa demo.

Para detener únicamente su MySQL, conservando los datos:

```powershell
docker compose -p trazo-entrega --env-file .env.entrega -f compose.entrega.yaml stop
```

Para retomarla, repetir `./tools/Preparar-Entrega.ps1 -DesdeSql -IniciarWeb`. Si el puerto está ocupado, cerrar la instancia anterior desde su terminal; no iniciar dos webs en el mismo puerto.

## 3. Cuentas y contenido preparado

Contraseña ficticia común: **`Trazo.Demo2026!`**.

| Email | Roles | Material de demostración |
| --- | --- | --- |
| ana@trazo.test | Creador | Perfil oscuro, avatar, contacto público, obras destacadas, imagen y GIF |
| bruno@trazo.test | Creador | Perfil claro, avatar, MP4, imagen, obra privada y obra oculta por moderación |
| admin@trazo.test | Creador y Administrador | Gestión de etiquetas, reportes y cuentas |

Hay 3 usuarios activos, 6 etiquetas, 28 publicaciones, 7 recursos de archivo, 2 favoritos, 2 seguimientos, 2 reportes y 2 entradas ficticias de historial de cuenta. Las publicaciones incluyen las 21 de la semilla básica y 7 obras multimedia adicionales. El visitante ve **22 publicaciones**, en páginas de 12 y 10. Hay obras para registrados, privadas y ocultas, además del borrador original de Ana. La historia de suspensión/reactivación de Bruno es material precargado de ejemplo, no una actuación administrativa real.

Los PNG, GIF y MP4 se generaron con geometría del propio proyecto. No se copiaron obras de artistas. Cada recurso tiene una clave independiente: eliminar uno no elimina el archivo de otra publicación. Los correos `.test` y el enlace `example.com` son ficticios y no sirven para recibir contactos reales.

## 4. Recorrido sugerido para la defensa

1. **Problema y modelo:** explicar el portafolio y la navegación sin cuenta; mostrar el DER, las 7 entidades principales, Publicación → Recursos (1:N) y Publicación ↔ Etiqueta (N:M).
2. **Visitante:** abrir Explorar, buscar por título/etiqueta/medio y pasar a página 2. Mostrar una obra, reproducir el GIF/MP4 y abrir `/p/ana`. Relacionar los destacados y el contacto con el uso por reclutadores.
3. **Creador:** ingresar como Ana. En Mi estudio abrir el editor Vue, crear una actualización temporal, buscar una etiqueta por AJAX, guardar, modificar y eliminar. En la pestaña Network mostrar JSON y que las operaciones no recargan el documento. Usar una publicación temporal para no alterar los ejemplos preparados.
4. **Archivos y perfil:** crear un borrador de tipo Obra, subir una imagen propia de prueba, agregar descripción/créditos y publicar. Editar el avatar, tema y disposición; mostrar la vista previa y el orden de destacados. Para probar nuevas cargas GIF/MP4, preparar FFmpeg antes del ensayo.
5. **Privacidad y comunidad:** mostrar favoritos y creadores seguidos. Contrastar una obra pública con una para registrados y una privada usando una ventana sin sesión. Ser administrador tampoco habilita la lectura de obras privadas ajenas.
6. **Administración:** ingresar como Admin y mostrar el reporte pendiente, la resolución ficticia y el historial de Bruno. Si se practica suspensión, reactivar a Bruno al finalizar; se revocan sus sesiones previas. Mostrar que Ana no puede abrir la administración.
7. **JWT:** importar la colección Postman, cambiar `baseUrl` a **https://localhost:7281** y ejecutar el Runner completo. Resultado esperado: 19 solicitudes y 31 aserciones. El recorrido crea/elimina una publicación temporal y revoca las sesiones de Ana al terminar. La guía de certificados y casos negativos está en [API](api.md).
8. **Cierre técnico:** mostrar README/matriz, pruebas, SQL, instrucciones de instalación y límites. Distinguir cookies + antiforgery del panel y Bearer de la API; explicar autorización antes de paginar y archivos fuera de `wwwroot`.

Para cargar nuevos GIF/MP4 en Windows:

```powershell
./tools/Preparar-Medios.ps1
```

Debe ejecutarse en la misma copia del repositorio que inicia la web. Reiniciar la web si estaba abierta. No hace falta para reproducir los archivos demo ya preparados.

## 5. Alternativas y verificación

Sin `-DesdeSql`, el script crea la misma demo mediante migraciones y `--seed-entrega`, exclusivamente en Development y sobre una base sin usuarios. La semilla básica `--seed-demo` permanece disponible para desarrollo y no agrega estos archivos a una instalación existente.

`-ExportarSql` es una opción de mantenimiento: solo permite exportar inmediatamente después de crear la demo en una base vacía. Rechaza bases con tablas, incluso si contienen datos de demostración, para evitar subir información de una instalación usada. No exporta `trazo_dev`.

Para verificar la importación en otra copia, usar `./tools/Preparar-Entrega.ps1 -Verificacion -DesdeSql -IniciarWeb`. Usa el proyecto Compose `trazo-verificacion`, puerto 3309, HTTPS 7282 y almacenamiento separado. Esa instancia también se conserva hasta detenerla; no es una orden de reinicialización.

Los recorridos de navegador requieren Node, `npm ci` y Edge instalado:

```powershell
node tools/Probar-Entrega.mjs
node tools/Probar-Entrega.mjs --restaurada
```

El primero comprueba la demo de 7281 y el segundo la copia de 7282. No crean ni eliminan publicaciones; los inicios de sesión sí tienen sus efectos normales. Guardan resultados y capturas sin cookies ni JWT. El [registro del incremento 09](incremento-09.md) identifica qué se comprobó efectivamente; no se afirma que la evaluación docente o la CI remota ya hayan aprobado la entrega.
