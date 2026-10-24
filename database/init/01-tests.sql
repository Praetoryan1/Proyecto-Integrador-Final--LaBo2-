-- Base separada para pruebas de integración, dentro del contenedor local.
CREATE DATABASE IF NOT EXISTS trazo_tests CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
GRANT ALL PRIVILEGES ON trazo_tests.* TO 'trazo'@'%';
