
/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CambiosEstadoCuenta` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UsuarioId` varchar(191) NOT NULL,
  `AdministradorId` varchar(191) NOT NULL,
  `EstadoAnterior` int NOT NULL,
  `EstadoNuevo` int NOT NULL,
  `Motivo` varchar(1000) NOT NULL,
  `CreadoUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CambiosEstadoCuenta_AdministradorId` (`AdministradorId`),
  KEY `IX_CambiosEstadoCuenta_UsuarioId_CreadoUtc_Id` (`UsuarioId`,`CreadoUtc`,`Id`),
  CONSTRAINT `FK_CambiosEstadoCuenta_Usuarios_AdministradorId` FOREIGN KEY (`AdministradorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_CambiosEstadoCuenta_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `CK_CambioEstadoCuenta_Transicion` CHECK (((`EstadoAnterior` in (0,1)) and (`EstadoNuevo` in (0,1)) and (`EstadoAnterior` <> `EstadoNuevo`) and (`UsuarioId` <> `AdministradorId`)))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `CambiosEstadoCuenta` WRITE;
/*!40000 ALTER TABLE `CambiosEstadoCuenta` DISABLE KEYS */;
INSERT INTO `CambiosEstadoCuenta` VALUES (1,'25cb06c2-e784-4e67-970f-d134cb221670','0cd74db8-61c7-487f-9059-5a2c2d46d563',0,1,'Historial ficticio de suspensión para la defensa académica.','2026-10-18 12:00:00.000000'),(2,'25cb06c2-e784-4e67-970f-d134cb221670','0cd74db8-61c7-487f-9059-5a2c2d46d563',1,0,'Historial ficticio de reactivación: la cuenta demo queda activa.','2026-10-19 12:00:00.000000');
/*!40000 ALTER TABLE `CambiosEstadoCuenta` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `EnlacesPerfil` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PerfilUsuarioId` varchar(191) NOT NULL,
  `Etiqueta` varchar(40) NOT NULL,
  `Url` varchar(500) NOT NULL,
  `Orden` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EnlacesPerfil_PerfilUsuarioId` (`PerfilUsuarioId`),
  CONSTRAINT `FK_EnlacesPerfil_Perfiles_PerfilUsuarioId` FOREIGN KEY (`PerfilUsuarioId`) REFERENCES `Perfiles` (`UsuarioId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `EnlacesPerfil` WRITE;
/*!40000 ALTER TABLE `EnlacesPerfil` DISABLE KEYS */;
INSERT INTO `EnlacesPerfil` VALUES (1,'19abd741-a7cd-471d-8932-75b6d8c1b826','Sitio de ejemplo','https://example.com',0);
/*!40000 ALTER TABLE `EnlacesPerfil` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Etiquetas` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(40) NOT NULL,
  `NombreNormalizado` varchar(40) NOT NULL,
  `Activa` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Etiquetas_NombreNormalizado` (`NombreNormalizado`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Etiquetas` WRITE;
/*!40000 ALTER TABLE `Etiquetas` DISABLE KEYS */;
INSERT INTO `Etiquetas` VALUES (1,'Ilustración','ILUSTRACIÓN',1),(2,'Animación','ANIMACIÓN',1),(3,'Diseño','DISEÑO',1),(4,'Proceso','PROCESO',1),(5,'Identidad','IDENTIDAD',1),(6,'3D','3D',1);
/*!40000 ALTER TABLE `Etiquetas` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Favoritos` (
  `UsuarioId` varchar(191) NOT NULL,
  `PublicacionId` bigint NOT NULL,
  `CreadoUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`UsuarioId`,`PublicacionId`),
  KEY `IX_Favoritos_PublicacionId` (`PublicacionId`),
  CONSTRAINT `FK_Favoritos_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Favoritos_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Favoritos` WRITE;
/*!40000 ALTER TABLE `Favoritos` DISABLE KEYS */;
INSERT INTO `Favoritos` VALUES ('19abd741-a7cd-471d-8932-75b6d8c1b826',25,'2026-10-20 12:00:00.000000'),('25cb06c2-e784-4e67-970f-d134cb221670',22,'2026-10-20 12:00:00.000000');
/*!40000 ALTER TABLE `Favoritos` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Perfiles` (
  `UsuarioId` varchar(191) NOT NULL,
  `NombreVisible` varchar(80) NOT NULL,
  `Biografia` varchar(1200) NOT NULL,
  `Especialidad` varchar(100) NOT NULL,
  `DisponibleParaTrabajar` tinyint(1) NOT NULL,
  `AvatarClave` varchar(40) DEFAULT NULL,
  `Tema` int NOT NULL,
  `ColorAcento` varchar(7) NOT NULL,
  `Disposicion` int NOT NULL,
  `Visibilidad` int NOT NULL,
  `EmailContacto` varchar(254) DEFAULT NULL,
  `VisibilidadContacto` int NOT NULL,
  PRIMARY KEY (`UsuarioId`),
  CONSTRAINT `FK_Perfiles_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Perfiles` WRITE;
/*!40000 ALTER TABLE `Perfiles` DISABLE KEYS */;
INSERT INTO `Perfiles` VALUES ('0cd74db8-61c7-487f-9059-5a2c2d46d563','Equipo Trazo','Me interesa encontrar historias en las formas, los colores y el movimiento. Este es mi espacio para compartir el proceso.','Administración',0,'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa3.png',0,'#c4f566',0,0,NULL,2),('19abd741-a7cd-471d-8932-75b6d8c1b826','Ana Rivera','Me interesa encontrar historias en las formas, los colores y el movimiento. Este es mi espacio para compartir el proceso.','Ilustración y dirección de arte',1,'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1.png',0,'#c4f566',0,0,'contacto@ana.trazo.test',0),('25cb06c2-e784-4e67-970f-d134cb221670','Bruno Costa','Me interesa encontrar historias en las formas, los colores y el movimiento. Este es mi espacio para compartir el proceso.','Animación y diseño de personajes',1,'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2.png',1,'#7dd3fc',0,0,NULL,2);
/*!40000 ALTER TABLE `Perfiles` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PublicacionEtiquetas` (
  `PublicacionId` bigint NOT NULL,
  `EtiquetaId` bigint NOT NULL,
  PRIMARY KEY (`PublicacionId`,`EtiquetaId`),
  KEY `IX_PublicacionEtiquetas_EtiquetaId` (`EtiquetaId`),
  CONSTRAINT `FK_PublicacionEtiquetas_Etiquetas_EtiquetaId` FOREIGN KEY (`EtiquetaId`) REFERENCES `Etiquetas` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_PublicacionEtiquetas_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `PublicacionEtiquetas` WRITE;
/*!40000 ALTER TABLE `PublicacionEtiquetas` DISABLE KEYS */;
INSERT INTO `PublicacionEtiquetas` VALUES (1,1),(7,1),(13,1),(27,1),(2,2),(8,2),(14,2),(22,2),(28,2),(3,3),(9,3),(15,3),(26,3),(4,4),(10,4),(16,4),(23,4),(5,5),(11,5),(17,5),(25,5),(6,6),(12,6),(18,6),(24,6);
/*!40000 ALTER TABLE `PublicacionEtiquetas` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Publicaciones` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AutorId` varchar(191) NOT NULL,
  `Tipo` int NOT NULL,
  `Titulo` varchar(160) NOT NULL,
  `Texto` varchar(12000) NOT NULL,
  `EstadoEditorial` int NOT NULL,
  `EstadoModeracion` int NOT NULL,
  `Visibilidad` int NOT NULL,
  `OrdenDestacado` int DEFAULT NULL,
  `CreadaUtc` datetime(6) NOT NULL,
  `PublicadaUtc` datetime(6) DEFAULT NULL,
  `ActualizadaUtc` datetime(6) NOT NULL,
  `Version` varchar(32) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Publicaciones_AutorId_PublicadaUtc_Id` (`AutorId`,`PublicadaUtc`,`Id`),
  KEY `IX_Publicaciones_EstadoEditorial_EstadoModeracion_Visibilidad_P~` (`EstadoEditorial`,`EstadoModeracion`,`Visibilidad`,`PublicadaUtc`,`Id`),
  CONSTRAINT `FK_Publicaciones_Usuarios_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Publicaciones` WRITE;
/*!40000 ALTER TABLE `Publicaciones` DISABLE KEYS */;
INSERT INTO `Publicaciones` VALUES (1,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'La forma de una idea','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-01 12:00:00.000000','2026-10-01 12:00:00.000000','2026-10-01 12:00:00.000000','8e92d21ef7cf4ff7996b7d3ca9a8aa0c'),(2,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Cuaderno de exploración','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-01 19:00:00.000000','2026-10-01 19:00:00.000000','2026-10-01 19:00:00.000000','fa0c7e4c6d5842babd064b188c82dd4c'),(3,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Un mundo en dos colores','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-02 02:00:00.000000','2026-10-02 02:00:00.000000','2026-10-02 02:00:00.000000','eeff1658ce8d4d66a50704f9ba22f763'),(4,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Letras en movimiento','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-02 09:00:00.000000','2026-10-02 09:00:00.000000','2026-10-02 09:00:00.000000','e0a9a966b8c940249c853cdca7a07227'),(5,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Detrás de cada personaje','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-02 16:00:00.000000','2026-10-02 16:00:00.000000','2026-10-02 16:00:00.000000','24db81d9887549e6a56eb4239cc99ec2'),(6,'25cb06c2-e784-4e67-970f-d134cb221670',1,'El ritmo de lo cotidiano','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-02 23:00:00.000000','2026-10-02 23:00:00.000000','2026-10-02 23:00:00.000000','f2670d26acad4b719b013fba77aadab1'),(7,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Volver a mirar','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-03 06:00:00.000000','2026-10-03 06:00:00.000000','2026-10-03 06:00:00.000000','8718db16c39b4632a3153f542c2c8024'),(8,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Pequeños universos','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-03 13:00:00.000000','2026-10-03 13:00:00.000000','2026-10-03 13:00:00.000000','ed09f8374d3c457b9c52ed58ea460204'),(9,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Una pausa entre fotogramas','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-03 20:00:00.000000','2026-10-03 20:00:00.000000','2026-10-03 20:00:00.000000','97bcd2b0167542fcb07efe2e3eaafbed'),(10,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Geometría sensible','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-04 03:00:00.000000','2026-10-04 03:00:00.000000','2026-10-04 03:00:00.000000','97b01f6b88f84cfb960de5be5372f7d7'),(11,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Historias sin palabras','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-04 10:00:00.000000','2026-10-04 10:00:00.000000','2026-10-04 10:00:00.000000','0a81876cdd304d6f8723cff1e37b81c9'),(12,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Encontrar la paleta','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-04 17:00:00.000000','2026-10-04 17:00:00.000000','2026-10-04 17:00:00.000000','120fa17f18774e71aa2021d4c58c7b20'),(13,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'La ciudad imaginada','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-05 00:00:00.000000','2026-10-05 00:00:00.000000','2026-10-05 00:00:00.000000','fb8907eaccb64737acf516ec5bb9b92b'),(14,'25cb06c2-e784-4e67-970f-d134cb221670',1,'De boceto a sistema','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-05 07:00:00.000000','2026-10-05 07:00:00.000000','2026-10-05 07:00:00.000000','6ad2ba157f7a4a8a9ab0b76225476c9d'),(15,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Ensayos de luz','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-05 14:00:00.000000','2026-10-05 14:00:00.000000','2026-10-05 14:00:00.000000','6d8aa34f123f4a71b09c4b00279e0915'),(16,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Lo que cuenta una línea','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-05 21:00:00.000000','2026-10-05 21:00:00.000000','2026-10-05 21:00:00.000000','f328cf7c41f44aaa89a998cefad1822a'),(17,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Un gesto, mil historias','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-06 04:00:00.000000','2026-10-06 04:00:00.000000','2026-10-06 04:00:00.000000','7b40ba6e0b914cc295c37aba09e8aa61'),(18,'25cb06c2-e784-4e67-970f-d134cb221670',1,'Notas de un nuevo comienzo','Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.',1,0,0,NULL,'2026-10-06 11:00:00.000000','2026-10-06 11:00:00.000000','2026-10-06 11:00:00.000000','2adeb67dadf642ef8a6b51ee33498b11'),(19,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Apunte para la comunidad','Esta actualización requiere una cuenta activa.',1,0,1,NULL,'2026-10-28 03:02:17.332680','2026-10-28 03:02:17.332682','2026-10-28 03:02:17.332680','b5e5412666664812a636b5ad4ca3811a'),(20,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Exploración privada de Ana','Solo Ana puede consultar este contenido.',1,0,2,NULL,'2026-10-28 03:02:17.332682','2026-10-28 03:02:17.332682','2026-10-28 03:02:17.332682','4cec1948bb304264b51a92907af865cb'),(21,'19abd741-a7cd-471d-8932-75b6d8c1b826',1,'Borrador de Ana','Todavía no está publicado.',0,0,0,NULL,'2026-10-28 03:02:17.332682',NULL,'2026-10-28 03:02:17.332682','75d5d31fc7de4bc3bd68622ed5401588'),(22,'19abd741-a7cd-471d-8932-75b6d8c1b826',0,'Atlas de formas','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,0,1,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','88cfc61d1c8241c98b875e9e759f7e02'),(23,'19abd741-a7cd-471d-8932-75b6d8c1b826',0,'Ritmo circular','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,0,2,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','98f3cbba2e6d44c985fab3889e86c6c5'),(24,'19abd741-a7cd-471d-8932-75b6d8c1b826',0,'Proceso para la comunidad','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,1,3,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','b86db57466bb4e0ba081d07d7f24d3ef'),(25,'25cb06c2-e784-4e67-970f-d134cb221670',0,'Movimiento en un segundo','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,0,1,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','024cb898d5ba466b86175ec07395300a'),(26,'25cb06c2-e784-4e67-970f-d134cb221670',0,'Identidad en color','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,0,2,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2fa05dce013444368dc46867564d4737'),(27,'25cb06c2-e784-4e67-970f-d134cb221670',0,'Propuesta reservada','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,0,2,NULL,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','f9f503a7be624689bda062d36897edba'),(28,'25cb06c2-e784-4e67-970f-d134cb221670',0,'Obra en revisión','Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.',1,1,0,NULL,'2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','2026-10-20 12:00:00.000000','c2001ce98f9d4916b341d27affaed3ae');
/*!40000 ALTER TABLE `Publicaciones` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Recursos` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PublicacionId` bigint NOT NULL,
  `Tipo` int NOT NULL,
  `ClaveAlmacenamiento` varchar(120) DEFAULT NULL,
  `YouTubeId` varchar(11) DEFAULT NULL,
  `Mime` varchar(80) DEFAULT NULL,
  `Bytes` bigint DEFAULT NULL,
  `EstadoProcesamiento` int NOT NULL,
  `Orden` int NOT NULL,
  `TextoAlternativo` varchar(500) NOT NULL,
  `Creditos` varchar(300) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Recursos_PublicacionId_Orden` (`PublicacionId`,`Orden`),
  CONSTRAINT `FK_Recursos_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `CK_Recurso_Fuente` CHECK ((((`ClaveAlmacenamiento` is not null) and (`YouTubeId` is null) and (`Tipo` <> 3)) or ((`ClaveAlmacenamiento` is null) and (`YouTubeId` is not null) and (`Tipo` = 3))))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Recursos` WRITE;
/*!40000 ALTER TABLE `Recursos` DISABLE KEYS */;
INSERT INTO `Recursos` VALUES (1,22,0,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb1.png',NULL,'image/png',48224,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(2,23,1,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb3.gif',NULL,'image/gif',2122526,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(3,24,0,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb5.png',NULL,'image/png',47766,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(4,25,2,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb4.mp4',NULL,'video/mp4',55255,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(5,26,0,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb2.png',NULL,'image/png',47766,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(6,27,0,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb6.png',NULL,'image/png',48307,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs'),(7,28,0,'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb7.png',NULL,'image/png',48224,1,0,'Anillos y bandas geométricas en una composición de colores','Material demo generado por tools/Generar-Assets-Demo.mjs');
/*!40000 ALTER TABLE `Recursos` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Reportes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PublicacionId` bigint NOT NULL,
  `DenuncianteId` varchar(191) NOT NULL,
  `ResueltoPorId` varchar(191) DEFAULT NULL,
  `Motivo` varchar(80) NOT NULL,
  `Detalle` varchar(2000) NOT NULL,
  `Estado` int NOT NULL,
  `CreadoUtc` datetime(6) NOT NULL,
  `ResueltoUtc` datetime(6) DEFAULT NULL,
  `Resolucion` varchar(2000) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Reportes_DenuncianteId` (`DenuncianteId`),
  KEY `IX_Reportes_PublicacionId` (`PublicacionId`),
  KEY `IX_Reportes_ResueltoPorId` (`ResueltoPorId`),
  CONSTRAINT `FK_Reportes_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Reportes_Usuarios_DenuncianteId` FOREIGN KEY (`DenuncianteId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Reportes_Usuarios_ResueltoPorId` FOREIGN KEY (`ResueltoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Reportes` WRITE;
/*!40000 ALTER TABLE `Reportes` DISABLE KEYS */;
INSERT INTO `Reportes` VALUES (1,26,'19abd741-a7cd-471d-8932-75b6d8c1b826',NULL,'Otro','Caso ficticio pendiente para practicar la revisión administrativa.',0,'2026-10-20 12:00:00.000000',NULL,NULL),(2,28,'19abd741-a7cd-471d-8932-75b6d8c1b826','0cd74db8-61c7-487f-9059-5a2c2d46d563','Derechos de autor','Caso ficticio: se solicita aclarar la autoría del material.',1,'2026-10-20 12:00:00.000000','2026-10-20 13:00:00.000000','Decisión ficticia: obra oculta mientras se revisan sus créditos. El autor puede solicitar revisión.');
/*!40000 ALTER TABLE `Reportes` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RolClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(191) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_RolClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_RolClaims_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `RolClaims` WRITE;
/*!40000 ALTER TABLE `RolClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `RolClaims` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Roles` (
  `Id` varchar(191) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Roles` WRITE;
/*!40000 ALTER TABLE `Roles` DISABLE KEYS */;
INSERT INTO `Roles` VALUES ('64d7d909-9084-4284-8e3c-1830b8e50e77','Administrador','ADMINISTRADOR','221eaead-eb34-4597-b76c-c9818055b3ec'),('e4170f12-7525-45cc-b23c-c34bfa6c6340','Creador','CREADOR','1acd531d-d91e-4537-9786-c02384b85fa8');
/*!40000 ALTER TABLE `Roles` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Seguimientos` (
  `SeguidorId` varchar(191) NOT NULL,
  `SeguidoId` varchar(191) NOT NULL,
  `CreadoUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`SeguidorId`,`SeguidoId`),
  KEY `IX_Seguimientos_SeguidoId` (`SeguidoId`),
  CONSTRAINT `FK_Seguimientos_Usuarios_SeguidoId` FOREIGN KEY (`SeguidoId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Seguimientos_Usuarios_SeguidorId` FOREIGN KEY (`SeguidorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `CK_Seguimiento_Distinto` CHECK ((`SeguidorId` <> `SeguidoId`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Seguimientos` WRITE;
/*!40000 ALTER TABLE `Seguimientos` DISABLE KEYS */;
INSERT INTO `Seguimientos` VALUES ('19abd741-a7cd-471d-8932-75b6d8c1b826','25cb06c2-e784-4e67-970f-d134cb221670','2026-10-20 12:00:00.000000'),('25cb06c2-e784-4e67-970f-d134cb221670','19abd741-a7cd-471d-8932-75b6d8c1b826','2026-10-20 12:00:00.000000');
/*!40000 ALTER TABLE `Seguimientos` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UsuarioClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(191) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_UsuarioClaims_UserId` (`UserId`),
  CONSTRAINT `FK_UsuarioClaims_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `UsuarioClaims` WRITE;
/*!40000 ALTER TABLE `UsuarioClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `UsuarioClaims` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UsuarioLogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext,
  `UserId` varchar(191) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_UsuarioLogins_UserId` (`UserId`),
  CONSTRAINT `FK_UsuarioLogins_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `UsuarioLogins` WRITE;
/*!40000 ALTER TABLE `UsuarioLogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `UsuarioLogins` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UsuarioRoles` (
  `UserId` varchar(191) NOT NULL,
  `RoleId` varchar(191) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_UsuarioRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_UsuarioRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UsuarioRoles_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `UsuarioRoles` WRITE;
/*!40000 ALTER TABLE `UsuarioRoles` DISABLE KEYS */;
INSERT INTO `UsuarioRoles` VALUES ('0cd74db8-61c7-487f-9059-5a2c2d46d563','64d7d909-9084-4284-8e3c-1830b8e50e77'),('0cd74db8-61c7-487f-9059-5a2c2d46d563','e4170f12-7525-45cc-b23c-c34bfa6c6340'),('19abd741-a7cd-471d-8932-75b6d8c1b826','e4170f12-7525-45cc-b23c-c34bfa6c6340'),('25cb06c2-e784-4e67-970f-d134cb221670','e4170f12-7525-45cc-b23c-c34bfa6c6340');
/*!40000 ALTER TABLE `UsuarioRoles` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UsuarioTokens` (
  `UserId` varchar(191) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_UsuarioTokens_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `UsuarioTokens` WRITE;
/*!40000 ALTER TABLE `UsuarioTokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `UsuarioTokens` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Usuarios` (
  `Id` varchar(191) NOT NULL,
  `Estado` int NOT NULL,
  `FechaAlta` datetime(6) NOT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `ConcurrencyStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `EmailIndex` (`NormalizedEmail`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `Usuarios` WRITE;
/*!40000 ALTER TABLE `Usuarios` DISABLE KEYS */;
INSERT INTO `Usuarios` VALUES ('0cd74db8-61c7-487f-9059-5a2c2d46d563',0,'2026-10-10 12:00:00.000000','admin','ADMIN','admin@trazo.test','ADMIN@TRAZO.TEST',1,'AQAAAAIAAYagAAAAEKwJaxJ4Pnew7MdawjdHBMl9or7Q6fDLEHLrt+12soaKFDAOUjPoIEeU/9Y4hTg2bw==','KCA35LNH3K3QQBE55RETPH6XU3OXPOA2','d1b41ad7-0b2e-4a4e-ac43-896c8e263f16',NULL,0,0,NULL,1,0),('19abd741-a7cd-471d-8932-75b6d8c1b826',0,'2026-10-10 12:00:00.000000','ana','ANA','ana@trazo.test','ANA@TRAZO.TEST',1,'AQAAAAIAAYagAAAAEP3Xuve8p6VoqKhrUOPw1EeZwY9lS2e3U9Hr7Rdwo4n4ddQ1UHQvJgvEMSWwoQUx9Q==','K4JPMDXRRKBKGUJWYWN7QDGN7E6TLIUW','0421e9c2-bfb2-44c3-b1ee-ed283ce09975',NULL,0,0,NULL,1,0),('25cb06c2-e784-4e67-970f-d134cb221670',0,'2026-10-10 12:00:00.000000','bruno','BRUNO','bruno@trazo.test','BRUNO@TRAZO.TEST',1,'AQAAAAIAAYagAAAAEDZRmNb5y+oDvqqdENn6e2qIotVbbc7Lh9Nojeltm//8qXoHQHYyPyaVJ2Z8O5K47g==','KMUBZ24K2XLB2NU5MBFS2SRTBCHMLXVG','eaa76d92-80c0-4e57-a1fa-e2a52e7398b9',NULL,0,0,NULL,1,0);
/*!40000 ALTER TABLE `Usuarios` ENABLE KEYS */;
UNLOCK TABLES;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `__EFMigrationsHistory` WRITE;
/*!40000 ALTER TABLE `__EFMigrationsHistory` DISABLE KEYS */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20261021051003_ModeloInicial','10.0.13'),('20261025191732_HistorialEstadoCuentas','10.0.13');
/*!40000 ALTER TABLE `__EFMigrationsHistory` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

