CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `Etiquetas` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(40) NOT NULL,
    `NombreNormalizado` varchar(40) NOT NULL,
    `Activa` tinyint(1) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `Roles` (
    `Id` varchar(191) NOT NULL,
    `Name` varchar(256) NULL,
    `NormalizedName` varchar(256) NULL,
    `ConcurrencyStamp` longtext NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `Usuarios` (
    `Id` varchar(191) NOT NULL,
    `Estado` int NOT NULL,
    `FechaAlta` datetime(6) NOT NULL,
    `UserName` varchar(256) NULL,
    `NormalizedUserName` varchar(256) NULL,
    `Email` varchar(256) NULL,
    `NormalizedEmail` varchar(256) NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext NULL,
    `SecurityStamp` longtext NULL,
    `ConcurrencyStamp` longtext NULL,
    `PhoneNumber` longtext NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `RolClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(191) NOT NULL,
    `ClaimType` longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RolClaims_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Perfiles` (
    `UsuarioId` varchar(191) NOT NULL,
    `NombreVisible` varchar(80) NOT NULL,
    `Biografia` varchar(1200) NOT NULL,
    `Especialidad` varchar(100) NOT NULL,
    `DisponibleParaTrabajar` tinyint(1) NOT NULL,
    `AvatarClave` varchar(40) NULL,
    `Tema` int NOT NULL,
    `ColorAcento` varchar(7) NOT NULL,
    `Disposicion` int NOT NULL,
    `Visibilidad` int NOT NULL,
    `EmailContacto` varchar(254) NULL,
    `VisibilidadContacto` int NOT NULL,
    PRIMARY KEY (`UsuarioId`),
    CONSTRAINT `FK_Perfiles_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Publicaciones` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `AutorId` varchar(191) NOT NULL,
    `Tipo` int NOT NULL,
    `Titulo` varchar(160) NOT NULL,
    `Texto` varchar(12000) NOT NULL,
    `EstadoEditorial` int NOT NULL,
    `EstadoModeracion` int NOT NULL,
    `Visibilidad` int NOT NULL,
    `OrdenDestacado` int NULL,
    `CreadaUtc` datetime(6) NOT NULL,
    `PublicadaUtc` datetime(6) NULL,
    `ActualizadaUtc` datetime(6) NOT NULL,
    `Version` varchar(32) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Publicaciones_Usuarios_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Seguimientos` (
    `SeguidorId` varchar(191) NOT NULL,
    `SeguidoId` varchar(191) NOT NULL,
    `CreadoUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`SeguidorId`, `SeguidoId`),
    CONSTRAINT `CK_Seguimiento_Distinto` CHECK (`SeguidorId` <> `SeguidoId`),
    CONSTRAINT `FK_Seguimientos_Usuarios_SeguidoId` FOREIGN KEY (`SeguidoId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Seguimientos_Usuarios_SeguidorId` FOREIGN KEY (`SeguidorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `UsuarioClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(191) NOT NULL,
    `ClaimType` longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UsuarioClaims_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `UsuarioLogins` (
    `LoginProvider` varchar(255) NOT NULL,
    `ProviderKey` varchar(255) NOT NULL,
    `ProviderDisplayName` longtext NULL,
    `UserId` varchar(191) NOT NULL,
    PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_UsuarioLogins_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `UsuarioRoles` (
    `UserId` varchar(191) NOT NULL,
    `RoleId` varchar(191) NOT NULL,
    PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_UsuarioRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UsuarioRoles_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `UsuarioTokens` (
    `UserId` varchar(191) NOT NULL,
    `LoginProvider` varchar(255) NOT NULL,
    `Name` varchar(255) NOT NULL,
    `Value` longtext NULL,
    PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_UsuarioTokens_Usuarios_UserId` FOREIGN KEY (`UserId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `EnlacesPerfil` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PerfilUsuarioId` varchar(191) NOT NULL,
    `Etiqueta` varchar(40) NOT NULL,
    `Url` varchar(500) NOT NULL,
    `Orden` int NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_EnlacesPerfil_Perfiles_PerfilUsuarioId` FOREIGN KEY (`PerfilUsuarioId`) REFERENCES `Perfiles` (`UsuarioId`) ON DELETE CASCADE
);

CREATE TABLE `Favoritos` (
    `UsuarioId` varchar(191) NOT NULL,
    `PublicacionId` bigint NOT NULL,
    `CreadoUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`UsuarioId`, `PublicacionId`),
    CONSTRAINT `FK_Favoritos_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Favoritos_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `PublicacionEtiquetas` (
    `PublicacionId` bigint NOT NULL,
    `EtiquetaId` bigint NOT NULL,
    PRIMARY KEY (`PublicacionId`, `EtiquetaId`),
    CONSTRAINT `FK_PublicacionEtiquetas_Etiquetas_EtiquetaId` FOREIGN KEY (`EtiquetaId`) REFERENCES `Etiquetas` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PublicacionEtiquetas_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Recursos` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PublicacionId` bigint NOT NULL,
    `Tipo` int NOT NULL,
    `ClaveAlmacenamiento` varchar(120) NULL,
    `YouTubeId` varchar(11) NULL,
    `Mime` varchar(80) NULL,
    `Bytes` bigint NULL,
    `EstadoProcesamiento` int NOT NULL,
    `Orden` int NOT NULL,
    `TextoAlternativo` varchar(500) NOT NULL,
    `Creditos` varchar(300) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Recurso_Fuente` CHECK ((`ClaveAlmacenamiento` IS NOT NULL AND `YouTubeId` IS NULL AND `Tipo` <> 3) OR (`ClaveAlmacenamiento` IS NULL AND `YouTubeId` IS NOT NULL AND `Tipo` = 3)),
    CONSTRAINT `FK_Recursos_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Reportes` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PublicacionId` bigint NOT NULL,
    `DenuncianteId` varchar(191) NOT NULL,
    `ResueltoPorId` varchar(191) NULL,
    `Motivo` varchar(80) NOT NULL,
    `Detalle` varchar(2000) NOT NULL,
    `Estado` int NOT NULL,
    `CreadoUtc` datetime(6) NOT NULL,
    `ResueltoUtc` datetime(6) NULL,
    `Resolucion` varchar(2000) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Reportes_Publicaciones_PublicacionId` FOREIGN KEY (`PublicacionId`) REFERENCES `Publicaciones` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reportes_Usuarios_DenuncianteId` FOREIGN KEY (`DenuncianteId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reportes_Usuarios_ResueltoPorId` FOREIGN KEY (`ResueltoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_EnlacesPerfil_PerfilUsuarioId` ON `EnlacesPerfil` (`PerfilUsuarioId`);

CREATE UNIQUE INDEX `IX_Etiquetas_NombreNormalizado` ON `Etiquetas` (`NombreNormalizado`);

CREATE INDEX `IX_Favoritos_PublicacionId` ON `Favoritos` (`PublicacionId`);

CREATE INDEX `IX_Publicaciones_AutorId_PublicadaUtc_Id` ON `Publicaciones` (`AutorId`, `PublicadaUtc`, `Id`);

CREATE INDEX `IX_Publicaciones_EstadoEditorial_EstadoModeracion_Visibilidad_P~` ON `Publicaciones` (`EstadoEditorial`, `EstadoModeracion`, `Visibilidad`, `PublicadaUtc`, `Id`);

CREATE INDEX `IX_PublicacionEtiquetas_EtiquetaId` ON `PublicacionEtiquetas` (`EtiquetaId`);

CREATE UNIQUE INDEX `IX_Recursos_PublicacionId_Orden` ON `Recursos` (`PublicacionId`, `Orden`);

CREATE INDEX `IX_Reportes_DenuncianteId` ON `Reportes` (`DenuncianteId`);

CREATE INDEX `IX_Reportes_PublicacionId` ON `Reportes` (`PublicacionId`);

CREATE INDEX `IX_Reportes_ResueltoPorId` ON `Reportes` (`ResueltoPorId`);

CREATE INDEX `IX_RolClaims_RoleId` ON `RolClaims` (`RoleId`);

CREATE UNIQUE INDEX `RoleNameIndex` ON `Roles` (`NormalizedName`);

CREATE INDEX `IX_Seguimientos_SeguidoId` ON `Seguimientos` (`SeguidoId`);

CREATE INDEX `IX_UsuarioClaims_UserId` ON `UsuarioClaims` (`UserId`);

CREATE INDEX `IX_UsuarioLogins_UserId` ON `UsuarioLogins` (`UserId`);

CREATE INDEX `IX_UsuarioRoles_RoleId` ON `UsuarioRoles` (`RoleId`);

CREATE UNIQUE INDEX `EmailIndex` ON `Usuarios` (`NormalizedEmail`);

CREATE UNIQUE INDEX `UserNameIndex` ON `Usuarios` (`NormalizedUserName`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021051003_ModeloInicial', '10.0.13');

COMMIT;

