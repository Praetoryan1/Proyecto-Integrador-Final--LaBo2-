START TRANSACTION;
CREATE TABLE `CambiosEstadoCuenta` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `UsuarioId` varchar(191) NOT NULL,
    `AdministradorId` varchar(191) NOT NULL,
    `EstadoAnterior` int NOT NULL,
    `EstadoNuevo` int NOT NULL,
    `Motivo` varchar(1000) NOT NULL,
    `CreadoUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `CK_CambioEstadoCuenta_Transicion` CHECK (`EstadoAnterior` IN (0, 1) AND `EstadoNuevo` IN (0, 1) AND `EstadoAnterior` <> `EstadoNuevo` AND `UsuarioId` <> `AdministradorId`),
    CONSTRAINT `FK_CambiosEstadoCuenta_Usuarios_AdministradorId` FOREIGN KEY (`AdministradorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_CambiosEstadoCuenta_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_CambiosEstadoCuenta_AdministradorId` ON `CambiosEstadoCuenta` (`AdministradorId`);

CREATE INDEX `IX_CambiosEstadoCuenta_UsuarioId_CreadoUtc_Id` ON `CambiosEstadoCuenta` (`UsuarioId`, `CreadoUtc`, `Id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261025191732_HistorialEstadoCuentas', '10.0.13');

COMMIT;

