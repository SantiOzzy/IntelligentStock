USE [master]
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'IntelligentStock')
BEGIN
    ALTER DATABASE [IntelligentStock] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [IntelligentStock];
END
GO

CREATE DATABASE [IntelligentStock]
GO

USE [IntelligentStock]
GO

-- =============================================
-- TABLAS DE SEGURIDAD Y CONTROL
-- =============================================

-- Tabla Rol
CREATE TABLE Rol (
    id_rol INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(50) NOT NULL
)
GO

-- Tabla Usuario
CREATE TABLE Usuario (
    dni INT PRIMARY KEY,
    id_rol INT NOT NULL FOREIGN KEY REFERENCES Rol(id_rol),
    nombre VARCHAR(50) NOT NULL,
    apellido VARCHAR(50) NOT NULL,
    fechaNacimiento DATE NULL,
    correoElectronico VARCHAR(100) NULL,
    numeroTelefono VARCHAR(20) NULL,
    nombreUsuario VARCHAR(50) NOT NULL UNIQUE,
    contraseña VARCHAR(64) NOT NULL,
    activo BIT DEFAULT 1,
    bloqueado BIT DEFAULT 0,
    intentosFallidos INT DEFAULT 0,
    DVH VARCHAR(64) NULL
)
GO

-- Tabla Familia (Composite pattern: tipo 'P' = Patente/hoja, tipo 'F' = Familia/compuesto)
CREATE TABLE Familia (
    id_familia INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100) NOT NULL,
    tipo CHAR(1) NOT NULL CHECK (tipo IN ('P', 'F'))
)
GO

-- Tabla RolFamilia (asignación de familias/patentes a roles)
CREATE TABLE RolFamilia (
    id_rol INT FOREIGN KEY REFERENCES Rol(id_rol),
    id_familia INT FOREIGN KEY REFERENCES Familia(id_familia),
    PRIMARY KEY (id_rol, id_familia)
)
GO

-- Tabla RelacionFamilia (jerarquía padre-hijo del Composite)
CREATE TABLE RelacionFamilia (
    id_familiaPadre INT FOREIGN KEY REFERENCES Familia(id_familia),
    id_familiaHija INT FOREIGN KEY REFERENCES Familia(id_familia),
    PRIMARY KEY (id_familiaPadre, id_familiaHija)
)
GO

-- Tabla Evento (bitácora de eventos)
CREATE TABLE Evento (
    id_evento INT PRIMARY KEY IDENTITY(1,1),
    dni_usuario INT NULL FOREIGN KEY REFERENCES Usuario(dni),
    fecha DATETIME NOT NULL DEFAULT GETDATE(),
    accion VARCHAR(100) NOT NULL,
    descripcion VARCHAR(500) NULL,
    -- Informativa / Advertencia / Grave (BE.CriticidadEvento). Se usa para
    -- filtrar la bitácora; ver 07-Plan-pendiente / apéndice de políticas.
    criticidad VARCHAR(20) NOT NULL DEFAULT 'Informativa'
)
GO

-- Tabla DV (dígito verificador)
CREATE TABLE DV (
    tabla NVARCHAR(50) PRIMARY KEY,
    DVV VARCHAR(64) NULL,
    DVH VARCHAR(64) NULL
)
GO

-- DVH de cada registro. Las tablas de negocio no tienen columna DVH propia
-- (solo Usuario la tiene), así que el dígito por fila se guarda acá.
-- La columna "clave" guarda la PK del registro con formato "columna=valor".
CREATE TABLE DVH_Fila (
    tabla NVARCHAR(50) NOT NULL,
    clave NVARCHAR(400) NOT NULL,
    DVH VARCHAR(64) NULL,
    PRIMARY KEY (tabla, clave)
)
GO

-- DVV de cada columna. Cruzando las filas mal contra las columnas mal
-- se obtiene la celda exacta donde está la inconsistencia.
CREATE TABLE DVV_Columna (
    tabla NVARCHAR(50) NOT NULL,
    columna NVARCHAR(128) NOT NULL,
    DVV VARCHAR(64) NULL,
    PRIMARY KEY (tabla, columna)
)
GO

-- Recuperación de contraseña sin intervención del administrador: el usuario
-- pide un enlace por correo y con ese enlace define una contraseña nueva.
-- Se guarda el SHA-256 del token, no el token en claro: si alguien leyera
-- esta tabla no podría usar los enlaces pendientes.
-- Igual que Evento y DV, no se somete al control de dígito verificador:
-- cambia en cada solicitud y es información transitoria.
CREATE TABLE TokenRecuperacion (
    tokenHash VARCHAR(64) PRIMARY KEY,
    dni_usuario INT NOT NULL FOREIGN KEY REFERENCES Usuario(dni),
    fechaExpiracion DATETIME NOT NULL,
    usado BIT NOT NULL DEFAULT 0
)
GO

-- =============================================
-- TABLAS DE NEGOCIO
-- =============================================

-- Tabla Proveedor
CREATE TABLE Proveedor (
    cuit VARCHAR(20) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    razonSocial VARCHAR(100) NULL,
    correo VARCHAR(100) NULL,
    numero VARCHAR(20) NULL,
    activo BIT DEFAULT 1
)
GO

-- Tabla Insumo
CREATE TABLE Insumo (
    id_insumo INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(500) NULL,
    unidadDeMedida VARCHAR(50) NULL,
    -- DECIMAL como en el diccionario de datos: los insumos se miden en kg, lt,
    -- etc., y el stock tiene que poder quedar en valores como 2,5.
    stock DECIMAL(10,2) DEFAULT 0,
    stockMinimo DECIMAL(10,2) DEFAULT 0,
    diasDeVidaUtil INT NULL,
    activo BIT DEFAULT 1,
    -- Costo de una unidad de medida. Se usa para valorizar las pérdidas
    -- (control ImpactoEconomico en el ajuste de stock y en las alertas IoT).
    precioUnitario DECIMAL(10,2) NOT NULL DEFAULT 0
)
GO

-- Tabla OrdenCompra
CREATE TABLE OrdenCompra (
    id_ordenCompra INT PRIMARY KEY IDENTITY(1,1),
    cuitProveedor VARCHAR(20) FOREIGN KEY REFERENCES Proveedor(cuit),
    fecha DATE NULL,
    estado VARCHAR(50) NULL
)
GO

-- Tabla DetalleOrdenCompra
CREATE TABLE DetalleOrdenCompra (
    id_ordenCompra INT FOREIGN KEY REFERENCES OrdenCompra(id_ordenCompra),
    id_insumo INT FOREIGN KEY REFERENCES Insumo(id_insumo),
    cantidad DECIMAL(8,2) NULL,
    PRIMARY KEY (id_ordenCompra, id_insumo)
)
GO

-- Tabla Receta
CREATE TABLE Receta (
    id_receta INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100) NOT NULL,
    activo BIT DEFAULT 1
)
GO

-- Tabla DetalleReceta
CREATE TABLE DetalleReceta (
    id_receta INT FOREIGN KEY REFERENCES Receta(id_receta),
    id_insumo INT FOREIGN KEY REFERENCES Insumo(id_insumo),
    cantidad DECIMAL(8,2) NULL,
    PRIMARY KEY (id_receta, id_insumo)
)
GO

-- Tabla Sensor
CREATE TABLE Sensor (
    id_sensor INT PRIMARY KEY IDENTITY(1,1),
    macAddress VARCHAR(50) NULL,
    modelo VARCHAR(100) NULL,
    temperaturaMaximaPermitida FLOAT NULL,
    activo BIT DEFAULT 1
)
GO

-- Tabla Alerta
CREATE TABLE Alerta (
    id_alerta INT PRIMARY KEY IDENTITY(1,1),
    id_sensor INT FOREIGN KEY REFERENCES Sensor(id_sensor),
    fechaHora DATETIME NULL,
    temperaturaAlcanzada FLOAT NULL,
    estado VARCHAR(50) NULL,
    observaciones VARCHAR(500) NULL
)
GO

-- Tabla Suscripcion
CREATE TABLE Suscripcion (
    id_suscripcion INT PRIMARY KEY IDENTITY(1,1),
    tipoPlan VARCHAR(50) NULL,
    tokenPago VARCHAR(200) NULL,
    estado VARCHAR(50) NULL,
    fechaInicio DATE NULL
)
GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- Validar usuario (login)
CREATE PROCEDURE ValidarUsuario
    @NombreUsuario VARCHAR(50)
AS
BEGIN
    -- El alias sin eñe evita tener que escribir "contraseña" desde C#.
    SELECT U.dni, U.id_rol, U.nombre, U.apellido, U.fechaNacimiento,
           U.correoElectronico, U.numeroTelefono, U.nombreUsuario,
           U.contraseña AS contrasena, U.activo, U.bloqueado, U.intentosFallidos,
           R.id_rol AS rolId, R.nombre AS rolNombre
    FROM Usuario U
    INNER JOIN Rol R ON U.id_rol = R.id_rol
    WHERE U.nombreUsuario = @NombreUsuario
END
GO

-- Igual que ValidarUsuario pero buscando por DNI y sin traer la clave.
CREATE PROCEDURE TraerUsuarioPorDNI
    @DNI INT
AS
BEGIN
    SELECT U.dni, U.id_rol, U.nombre, U.apellido, U.fechaNacimiento,
           U.correoElectronico, U.numeroTelefono, U.nombreUsuario,
           U.activo, U.bloqueado, U.intentosFallidos,
           R.nombre AS rolNombre
    FROM Usuario U
    INNER JOIN Rol R ON U.id_rol = R.id_rol
    WHERE U.dni = @DNI
END
GO

-- Modificar bloqueo de usuario
CREATE PROCEDURE ModificarBloquearUsuario
    @DNI INT,
    @Bloqueo BIT
AS
BEGIN
    UPDATE Usuario SET bloqueado = @Bloqueo WHERE dni = @DNI
END
GO

-- Modificar contador de intentos fallidos
CREATE PROCEDURE ModificarContFallido
    @NombreUsuario VARCHAR(50),
    @ContClaveIncorrecta INT
AS
BEGIN
    UPDATE Usuario SET intentosFallidos = @ContClaveIncorrecta WHERE nombreUsuario = @NombreUsuario
END
GO

-- Registrar evento en bitácora
CREATE PROCEDURE RegistrarEvento
    @DniUsuario INT,
    @Accion VARCHAR(100),
    @Descripcion VARCHAR(500),
    @Criticidad VARCHAR(20) = 'Informativa'
AS
BEGIN
    INSERT INTO Evento (dni_usuario, fecha, accion, descripcion, criticidad)
    VALUES (@DniUsuario, GETDATE(), @Accion, @Descripcion, @Criticidad)
END
GO

-- Filtrar eventos por fecha
CREATE PROCEDURE FiltrarEventos
    @FechaInicio DATETIME,
    @FechaFin DATETIME
AS
BEGIN
    SELECT E.id_evento, E.dni_usuario, U.nombreUsuario, E.fecha, E.accion, E.descripcion, E.criticidad
    FROM Evento E
    LEFT JOIN Usuario U ON E.dni_usuario = U.dni
    WHERE E.fecha >= @FechaInicio AND E.fecha <= @FechaFin
    ORDER BY E.id_evento DESC
END
GO

-- Traer todos los eventos
CREATE PROCEDURE TraerEventos
AS
BEGIN
    SELECT E.id_evento, E.dni_usuario, U.nombreUsuario, E.fecha, E.accion, E.descripcion, E.criticidad
    FROM Evento E
    LEFT JOIN Usuario U ON E.dni_usuario = U.dni
    ORDER BY E.id_evento DESC
END
GO

-- Vista inicial de la bitácora: los últimos N eventos (la pantalla pide 200).
CREATE PROCEDURE TraerUltimosEventos
    @Cantidad INT
AS
BEGIN
    SELECT TOP (@Cantidad) E.id_evento, E.dni_usuario, U.nombreUsuario,
           E.fecha, E.accion, E.descripcion, E.criticidad
    FROM Evento E
    LEFT JOIN Usuario U ON E.dni_usuario = U.dni
    ORDER BY E.id_evento DESC
END
GO

-- Cantidad total de eventos, para decidir si hace falta purgar los mas viejos.
-- Cuenta los eventos sujetos a la política de retención: las ventas del
-- último año no cuentan (ver PurgarEventosAntiguos).
CREATE PROCEDURE ContarEventos
AS
BEGIN
    SELECT COUNT(1) FROM Evento
    WHERE NOT (accion = 'Venta' AND fecha >= DATEADD(DAY, -400, GETDATE()))
END
GO

-- Política de retención de la bitácora: conserva solo los @CantidadARetener
-- eventos mas recientes (orden descendente por fecha) y borra el resto.
-- Las ventas del último año no se borran: son el historial con el que se
-- entrena la predicción de demanda (CU-006-033).
CREATE PROCEDURE PurgarEventosAntiguos
    @CantidadARetener INT
AS
BEGIN
    -- Las ventas del último año no se borran ni cuentan para el límite: son
    -- el historial de consumo de la predicción de demanda (CU-006-033).
    ;WITH Ordenados AS (
        SELECT id_evento, ROW_NUMBER() OVER (ORDER BY fecha DESC, id_evento DESC) AS Posicion
        FROM Evento
        WHERE NOT (accion = 'Venta' AND fecha >= DATEADD(DAY, -400, GETDATE()))
    )
    DELETE E
    FROM Evento E
    INNER JOIN Ordenados O ON E.id_evento = O.id_evento
    WHERE O.Posicion > @CantidadARetener
END
GO

-- Recalcular DVH de una tabla
CREATE PROCEDURE RecalcularDVHTabla
    @DVH VARCHAR(64),
    @Tabla NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM DV WHERE Tabla = @Tabla)
        UPDATE DV SET DVH = @DVH WHERE Tabla = @Tabla
    ELSE
        INSERT INTO DV (Tabla, DVH, DVV) VALUES (@Tabla, @DVH, '')
END
GO

-- Recalcular DVV de una tabla
CREATE PROCEDURE RecalcularDVVTabla
    @DVV VARCHAR(64),
    @Tabla NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM DV WHERE Tabla = @Tabla)
        UPDATE DV SET DVV = @DVV WHERE Tabla = @Tabla
    ELSE
        INSERT INTO DV (Tabla, DVH, DVV) VALUES (@Tabla, '', @DVV)
END
GO

-- Tablas que tienen fila en DV. La lista de tablas controladas está fija en
-- BLLDigitoVerificador (así, borrar la fila de una tabla acá no la saca del
-- control); este procedimiento solo suma las que se hayan agregado aparte.
-- Evento y DV quedan afuera a propósito: la bitácora cambia todo el tiempo
-- y DV es justamente la tabla de control.
CREATE PROCEDURE TraerTablasDV
AS
BEGIN
    SELECT tabla FROM DV ORDER BY tabla
END
GO

CREATE PROCEDURE TraerDVTabla
    @Tabla NVARCHAR(50)
AS
BEGIN
    SELECT tabla, DVH, DVV FROM DV WHERE tabla = @Tabla
END
GO

CREATE PROCEDURE TraerDVHFilas
    @Tabla NVARCHAR(50)
AS
BEGIN
    SELECT clave, DVH FROM DVH_Fila WHERE tabla = @Tabla
END
GO

-- El recálculo graba solo los dígitos que cambiaron (DALDigitoVerificador);
-- este procedimiento queda para vaciar los de una tabla de una sola vez.
CREATE PROCEDURE LimpiarDVHFilas
    @Tabla NVARCHAR(50)
AS
BEGIN
    DELETE FROM DVH_Fila WHERE tabla = @Tabla
END
GO

CREATE PROCEDURE TraerDVVColumnas
    @Tabla NVARCHAR(50)
AS
BEGIN
    SELECT columna, DVV FROM DVV_Columna WHERE tabla = @Tabla
END
GO

CREATE PROCEDURE LimpiarDVVColumnas
    @Tabla NVARCHAR(50)
AS
BEGIN
    DELETE FROM DVV_Columna WHERE tabla = @Tabla
END
GO

-- Backup de base de datos
CREATE PROCEDURE RealizarBackUp
    @RutaBackUp VARCHAR(260)
AS
BEGIN
    BACKUP DATABASE IntelligentStock TO DISK = @RutaBackUp
END
GO

-- Carpeta de backups que tiene configurada la instancia. Es la única sobre la
-- que la cuenta del servicio de SQL Server tiene permiso de escritura seguro,
-- así que se usa como valor por defecto en la pantalla de respaldo.
CREATE PROCEDURE TraerRutaBackUpPorDefecto
AS
BEGIN
    SELECT CONVERT(NVARCHAR(260), SERVERPROPERTY('InstanceDefaultBackupPath')) AS ruta
END
GO

-- CRUD Usuarios
CREATE PROCEDURE CrearUsuario
    @DNI INT,
    @IdRol INT,
    @Nombre VARCHAR(50),
    @Apellido VARCHAR(50),
    @FechaNacimiento DATE,
    @CorreoElectronico VARCHAR(100),
    @NumeroTelefono VARCHAR(20),
    @NombreUsuario VARCHAR(50),
    @Contrasena VARCHAR(64)
AS
BEGIN
    INSERT INTO Usuario (dni, id_rol, nombre, apellido, fechaNacimiento, correoElectronico,
                         numeroTelefono, nombreUsuario, contraseña, activo, bloqueado, intentosFallidos)
    VALUES (@DNI, @IdRol, @Nombre, @Apellido, @FechaNacimiento, @CorreoElectronico,
            @NumeroTelefono, @NombreUsuario, @Contrasena, 1, 0, 0)
END
GO

CREATE PROCEDURE ModificarUsuario
    @DNI INT,
    @IdRol INT,
    @Nombre VARCHAR(50),
    @Apellido VARCHAR(50),
    @FechaNacimiento DATE,
    @CorreoElectronico VARCHAR(100),
    @NumeroTelefono VARCHAR(20),
    @NombreUsuario VARCHAR(50)
AS
BEGIN
    UPDATE Usuario SET id_rol = @IdRol, nombre = @Nombre, apellido = @Apellido,
        fechaNacimiento = @FechaNacimiento, correoElectronico = @CorreoElectronico,
        numeroTelefono = @NumeroTelefono, nombreUsuario = @NombreUsuario
    WHERE dni = @DNI
END
GO

CREATE PROCEDURE BajaUsuario
    @DNI INT
AS
BEGIN
    UPDATE Usuario SET activo = 0 WHERE dni = @DNI
END
GO

-- Reactivar una cuenta dada de baja. Se aprovecha para limpiar el bloqueo
-- y el contador de intentos, si no el usuario vuelve trabado.
CREATE PROCEDURE ActivarUsuario
    @DNI INT
AS
BEGIN
    UPDATE Usuario SET activo = 1, bloqueado = 0, intentosFallidos = 0 WHERE dni = @DNI
END
GO

CREATE PROCEDURE CambiarContrasena
    @NombreUsuario VARCHAR(50),
    @NuevaContrasena VARCHAR(64)
AS
BEGIN
    UPDATE Usuario SET contraseña = @NuevaContrasena WHERE nombreUsuario = @NombreUsuario
END
GO

-- Recuperación de contraseña autoservicio
CREATE PROCEDURE CrearTokenRecuperacion
    @TokenHash VARCHAR(64),
    @DniUsuario INT,
    @FechaExpiracion DATETIME
AS
BEGIN
    INSERT INTO TokenRecuperacion (tokenHash, dni_usuario, fechaExpiracion, usado)
    VALUES (@TokenHash, @DniUsuario, @FechaExpiracion, 0)
END
GO

CREATE PROCEDURE TraerTokenRecuperacion
    @TokenHash VARCHAR(64)
AS
BEGIN
    SELECT tokenHash, dni_usuario, fechaExpiracion, usado
    FROM TokenRecuperacion
    WHERE tokenHash = @TokenHash
END
GO

CREATE PROCEDURE MarcarTokenRecuperacionUsado
    @TokenHash VARCHAR(64)
AS
BEGIN
    UPDATE TokenRecuperacion SET usado = 1 WHERE tokenHash = @TokenHash
END
GO

-- Al pedir un enlace nuevo se anulan los anteriores del mismo usuario.
CREATE PROCEDURE InvalidarTokensDeUsuario
    @DniUsuario INT
AS
BEGIN
    UPDATE TokenRecuperacion SET usado = 1 WHERE dni_usuario = @DniUsuario AND usado = 0
END
GO

CREATE PROCEDURE ActualizarDVHUsuario
    @DNI INT,
    @DVH VARCHAR(64)
AS
BEGIN
    UPDATE Usuario SET DVH = @DVH WHERE dni = @DNI
END
GO

CREATE PROCEDURE TraerUsuarios
AS
BEGIN
    SELECT U.dni, U.id_rol, U.nombre, U.apellido, U.fechaNacimiento, U.correoElectronico,
           U.numeroTelefono, U.nombreUsuario, U.activo, U.bloqueado, U.intentosFallidos,
           R.nombre AS rolNombre
    FROM Usuario U INNER JOIN Rol R ON U.id_rol = R.id_rol
END
GO

-- CRUD Familias y Patentes
CREATE PROCEDURE CrearFamilia
    @Nombre VARCHAR(100),
    @Tipo CHAR(1)
AS
BEGIN
    INSERT INTO Familia (nombre, tipo) VALUES (@Nombre, @Tipo)
    SELECT SCOPE_IDENTITY()
END
GO

-- Alta de familia con sus componentes (CU-003-004) en un solo procedimiento:
-- DALConexion lo ejecuta dentro de una transacción, así que o queda la familia
-- con todos sus hijos o no se graba nada. Los hijos llegan como XML:
-- <Componentes><Componente id="5" /></Componentes>
CREATE PROCEDURE CrearFamiliaConHijos
    @Nombre VARCHAR(100),
    @Hijos XML
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdFamilia INT;

    INSERT INTO Familia (nombre, tipo) VALUES (@Nombre, 'F');
    SET @IdFamilia = SCOPE_IDENTITY();

    INSERT INTO RelacionFamilia (id_familiaPadre, id_familiaHija)
    SELECT DISTINCT @IdFamilia, X.C.value('@id', 'INT')
    FROM @Hijos.nodes('/Componentes/Componente') AS X(C);

    SELECT @IdFamilia;
END
GO

CREATE PROCEDURE EliminarFamilia
    @IdFamilia INT
AS
BEGIN
    DELETE FROM RelacionFamilia WHERE id_familiaPadre = @IdFamilia OR id_familiaHija = @IdFamilia
    DELETE FROM RolFamilia WHERE id_familia = @IdFamilia
    DELETE FROM Familia WHERE id_familia = @IdFamilia
END
GO

CREATE PROCEDURE TraerFamilias
AS
BEGIN
    SELECT id_familia, nombre, tipo FROM Familia ORDER BY tipo, nombre
END
GO

CREATE PROCEDURE TraerFamiliasPorTipo
    @Tipo CHAR(1)
AS
BEGIN
    SELECT id_familia, nombre, tipo FROM Familia WHERE tipo = @Tipo ORDER BY nombre
END
GO

CREATE PROCEDURE AgregarHijoAFamilia
    @IdPadre INT,
    @IdHijo INT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM RelacionFamilia WHERE id_familiaPadre = @IdPadre AND id_familiaHija = @IdHijo)
        INSERT INTO RelacionFamilia (id_familiaPadre, id_familiaHija) VALUES (@IdPadre, @IdHijo)
END
GO

CREATE PROCEDURE QuitarHijoDeFamilia
    @IdPadre INT,
    @IdHijo INT
AS
BEGIN
    DELETE FROM RelacionFamilia WHERE id_familiaPadre = @IdPadre AND id_familiaHija = @IdHijo
END
GO

CREATE PROCEDURE TraerHijosDeFamilia
    @IdPadre INT
AS
BEGIN
    SELECT F.id_familia, F.nombre, F.tipo
    FROM RelacionFamilia RF INNER JOIN Familia F ON RF.id_familiaHija = F.id_familia
    WHERE RF.id_familiaPadre = @IdPadre
END
GO

CREATE PROCEDURE AsignarFamiliaARol
    @IdRol INT,
    @IdFamilia INT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM RolFamilia WHERE id_rol = @IdRol AND id_familia = @IdFamilia)
        INSERT INTO RolFamilia (id_rol, id_familia) VALUES (@IdRol, @IdFamilia)
END
GO

CREATE PROCEDURE QuitarFamiliaDeRol
    @IdRol INT,
    @IdFamilia INT
AS
BEGIN
    DELETE FROM RolFamilia WHERE id_rol = @IdRol AND id_familia = @IdFamilia
END
GO

CREATE PROCEDURE TraerFamiliasDeRol
    @IdRol INT
AS
BEGIN
    SELECT F.id_familia, F.nombre, F.tipo
    FROM RolFamilia RF INNER JOIN Familia F ON RF.id_familia = F.id_familia
    WHERE RF.id_rol = @IdRol
END
GO

CREATE PROCEDURE TraerRoles
AS
BEGIN
    SELECT id_rol, nombre FROM Rol
END
GO

CREATE PROCEDURE ModificarFamilia
    @IdFamilia INT,
    @Nombre VARCHAR(100)
AS
BEGIN
    UPDATE Familia SET nombre = @Nombre WHERE id_familia = @IdFamilia
END
GO

-- El árbol del Composite se arma en memoria con dos consultas: todos los
-- componentes (TraerFamilias) y todas las relaciones padre-hijo. Así se evita
-- pegarle a la base una vez por cada nodo.
CREATE PROCEDURE TraerRelacionesFamilia
AS
BEGIN
    SELECT id_familiaPadre, id_familiaHija FROM RelacionFamilia
END
GO

CREATE PROCEDURE TraerRolFamilia
AS
BEGIN
    SELECT id_rol, id_familia FROM RolFamilia
END
GO

-- Se usan antes de dar de baja una familia, para avisar dónde está en uso.
CREATE PROCEDURE ContarRolesDeFamilia
    @IdFamilia INT
AS
BEGIN
    SELECT COUNT(1) FROM RolFamilia WHERE id_familia = @IdFamilia
END
GO

CREATE PROCEDURE ContarPadresDeFamilia
    @IdFamilia INT
AS
BEGIN
    SELECT COUNT(1) FROM RelacionFamilia WHERE id_familiaHija = @IdFamilia
END
GO

-- CRUD de roles (alta/modificación/baja, análogo al de familias).
CREATE PROCEDURE CrearRol
    @Nombre VARCHAR(50)
AS
BEGIN
    INSERT INTO Rol (nombre) VALUES (@Nombre)
    SELECT SCOPE_IDENTITY()
END
GO

-- Alta de rol con sus familias y patentes, igual que la de familia: todo o nada.
CREATE PROCEDURE CrearRolConComponentes
    @Nombre VARCHAR(50),
    @Componentes XML
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdRol INT;

    INSERT INTO Rol (nombre) VALUES (@Nombre);
    SET @IdRol = SCOPE_IDENTITY();

    INSERT INTO RolFamilia (id_rol, id_familia)
    SELECT DISTINCT @IdRol, X.C.value('@id', 'INT')
    FROM @Componentes.nodes('/Componentes/Componente') AS X(C);

    SELECT @IdRol;
END
GO

CREATE PROCEDURE RenombrarRol
    @IdRol INT,
    @Nombre VARCHAR(50)
AS
BEGIN
    UPDATE Rol SET nombre = @Nombre WHERE id_rol = @IdRol
END
GO

CREATE PROCEDURE EliminarRol
    @IdRol INT
AS
BEGIN
    DELETE FROM RolFamilia WHERE id_rol = @IdRol
    DELETE FROM Rol WHERE id_rol = @IdRol
END
GO

-- Se usa antes de dar de baja un rol, para no dejar usuarios sin rol asignado.
CREATE PROCEDURE ContarUsuariosConRol
    @IdRol INT
AS
BEGIN
    SELECT COUNT(1) FROM Usuario WHERE id_rol = @IdRol
END
GO

-- =============================================
-- CRUD Proveedores (CU-004-020 a CU-004-023)
-- =============================================

CREATE PROCEDURE CrearProveedor
    @Cuit VARCHAR(20),
    @Nombre VARCHAR(100),
    @RazonSocial VARCHAR(100),
    @Correo VARCHAR(100),
    @Numero VARCHAR(20)
AS
BEGIN
    INSERT INTO Proveedor (cuit, nombre, razonSocial, correo, numero, activo)
    VALUES (@Cuit, @Nombre, @RazonSocial, @Correo, @Numero, 1)
END
GO

-- El CUIT es la clave del registro y no se modifica.
CREATE PROCEDURE ModificarProveedor
    @Cuit VARCHAR(20),
    @Nombre VARCHAR(100),
    @RazonSocial VARCHAR(100),
    @Correo VARCHAR(100),
    @Numero VARCHAR(20)
AS
BEGIN
    UPDATE Proveedor SET nombre = @Nombre, razonSocial = @RazonSocial,
        correo = @Correo, numero = @Numero
    WHERE cuit = @Cuit
END
GO

-- Baja lógica: el registro se conserva para no perder sus órdenes de compra.
CREATE PROCEDURE BajaProveedor
    @Cuit VARCHAR(20)
AS
BEGIN
    UPDATE Proveedor SET activo = 0 WHERE cuit = @Cuit
END
GO

-- Deshace la baja lógica (reactivación desde Baja de proveedor).
CREATE PROCEDURE ActivarProveedor
    @Cuit VARCHAR(20)
AS
BEGIN
    UPDATE Proveedor SET activo = 1 WHERE cuit = @Cuit
END
GO

CREATE PROCEDURE TraerProveedores
AS
BEGIN
    SELECT cuit, nombre, razonSocial, correo, numero, activo
    FROM Proveedor
    ORDER BY nombre
END
GO

CREATE PROCEDURE TraerProveedorPorCuit
    @Cuit VARCHAR(20)
AS
BEGIN
    SELECT cuit, nombre, razonSocial, correo, numero, activo
    FROM Proveedor
    WHERE cuit = @Cuit
END
GO

-- Se usa antes de la baja: un proveedor con órdenes pendientes de recibir
-- no se puede inhabilitar, porque esas órdenes quedarían sin proveedor activo.
CREATE PROCEDURE ContarOrdenesPendientesDeProveedor
    @Cuit VARCHAR(20)
AS
BEGIN
    SELECT COUNT(1) FROM OrdenCompra WHERE cuitProveedor = @Cuit AND estado = 'Pendiente'
END
GO

-- =============================================
-- CRUD Insumos y Recetas (CU-005-024 a CU-005-032)
-- =============================================

-- Recalcular stock físico (R8): suma o resta la cantidad indicada. La
-- condición del WHERE impide dejar el stock en negativo aunque dos usuarios
-- descuenten al mismo tiempo. Devuelve 1 si actualizó y 0 si no.
CREATE PROCEDURE AjustarStockInsumo
    @IdInsumo INT,
    @Cantidad DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumo SET stock = ISNULL(stock, 0) + @Cantidad
    WHERE id_insumo = @IdInsumo AND activo = 1 AND ISNULL(stock, 0) + @Cantidad >= 0;
    SELECT @@ROWCOUNT;
END
GO

CREATE PROCEDURE CrearInsumo
    @Nombre VARCHAR(100),
    @Descripcion VARCHAR(500),
    @UnidadDeMedida VARCHAR(50),
    @Stock DECIMAL(10,2),
    @StockMinimo DECIMAL(10,2),
    @DiasDeVidaUtil INT,
    @PrecioUnitario DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Insumo (nombre, descripcion, unidadDeMedida, stock, stockMinimo, diasDeVidaUtil, activo, precioUnitario)
    VALUES (@Nombre, @Descripcion, @UnidadDeMedida, @Stock, @StockMinimo, @DiasDeVidaUtil, 1, @PrecioUnitario);
    SELECT SCOPE_IDENTITY();
END
GO

-- El stock no se modifica desde acá: solo cambia por ajuste, venta o compra.
CREATE PROCEDURE ModificarInsumo
    @IdInsumo INT,
    @Nombre VARCHAR(100),
    @Descripcion VARCHAR(500),
    @UnidadDeMedida VARCHAR(50),
    @StockMinimo DECIMAL(10,2),
    @DiasDeVidaUtil INT,
    @PrecioUnitario DECIMAL(10,2)
AS
BEGIN
    UPDATE Insumo SET nombre = @Nombre, descripcion = @Descripcion,
        unidadDeMedida = @UnidadDeMedida, stockMinimo = @StockMinimo,
        diasDeVidaUtil = @DiasDeVidaUtil, precioUnitario = @PrecioUnitario
    WHERE id_insumo = @IdInsumo
END
GO

-- Baja lógica: se conserva el historial de movimientos del insumo.
CREATE PROCEDURE BajaInsumo
    @IdInsumo INT
AS
BEGIN
    UPDATE Insumo SET activo = 0 WHERE id_insumo = @IdInsumo
END
GO

-- Deshace la baja lógica (reactivación desde Baja de insumo).
CREATE PROCEDURE ActivarInsumo
    @IdInsumo INT
AS
BEGIN
    UPDATE Insumo SET activo = 1 WHERE id_insumo = @IdInsumo
END
GO

CREATE PROCEDURE TraerInsumos
AS
BEGIN
    SELECT id_insumo, nombre, descripcion, unidadDeMedida,
           ISNULL(stock, 0) AS stock, ISNULL(stockMinimo, 0) AS stockMinimo,
           diasDeVidaUtil, activo, precioUnitario
    FROM Insumo
    ORDER BY nombre
END
GO

CREATE PROCEDURE TraerInsumoPorId
    @IdInsumo INT
AS
BEGIN
    SELECT id_insumo, nombre, descripcion, unidadDeMedida,
           ISNULL(stock, 0) AS stock, ISNULL(stockMinimo, 0) AS stockMinimo,
           diasDeVidaUtil, activo, precioUnitario
    FROM Insumo
    WHERE id_insumo = @IdInsumo
END
GO

-- Se usan antes de dar de baja un insumo: no puede quedar dentro de una receta
-- activa ni de una orden de compra que todavía no se recibió.
CREATE PROCEDURE TraerRecetasActivasConInsumo
    @IdInsumo INT
AS
BEGIN
    SELECT R.id_receta, R.nombre
    FROM DetalleReceta D
    INNER JOIN Receta R ON D.id_receta = R.id_receta
    WHERE D.id_insumo = @IdInsumo AND R.activo = 1
    ORDER BY R.nombre
END
GO

CREATE PROCEDURE ContarOrdenesPendientesConInsumo
    @IdInsumo INT
AS
BEGIN
    SELECT COUNT(1)
    FROM DetalleOrdenCompra D
    INNER JOIN OrdenCompra O ON D.id_ordenCompra = O.id_ordenCompra
    WHERE D.id_insumo = @IdInsumo AND O.estado = 'Pendiente'
END
GO

-- La receta y su detalle se graban en un solo procedimiento, dentro de la misma
-- transacción: o se guarda todo o no se guarda nada. El detalle llega como XML:
-- <Detalle><Insumo idInsumo="1" cantidad="0.25" /></Detalle>
CREATE PROCEDURE CrearReceta
    @Nombre VARCHAR(100),
    @Detalle XML
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdReceta INT;

    INSERT INTO Receta (nombre, activo) VALUES (@Nombre, 1);
    SET @IdReceta = SCOPE_IDENTITY();

    INSERT INTO DetalleReceta (id_receta, id_insumo, cantidad)
    SELECT @IdReceta,
           X.I.value('@idInsumo', 'INT'),
           X.I.value('@cantidad', 'DECIMAL(8,2)')
    FROM @Detalle.nodes('/Detalle/Insumo') AS X(I);

    SELECT @IdReceta;
END
GO

-- El detalle se reemplaza completo por el que llega en el XML.
CREATE PROCEDURE ModificarReceta
    @IdReceta INT,
    @Nombre VARCHAR(100),
    @Detalle XML
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Receta SET nombre = @Nombre WHERE id_receta = @IdReceta;

    DELETE FROM DetalleReceta WHERE id_receta = @IdReceta;

    INSERT INTO DetalleReceta (id_receta, id_insumo, cantidad)
    SELECT @IdReceta,
           X.I.value('@idInsumo', 'INT'),
           X.I.value('@cantidad', 'DECIMAL(8,2)')
    FROM @Detalle.nodes('/Detalle/Insumo') AS X(I);
END
GO

-- Baja lógica: la receta deja de poder venderse pero se conserva su detalle.
CREATE PROCEDURE BajaReceta
    @IdReceta INT
AS
BEGIN
    UPDATE Receta SET activo = 0 WHERE id_receta = @IdReceta
END
GO

-- Deshace la baja lógica (reactivación desde Baja de receta).
CREATE PROCEDURE ActivarReceta
    @IdReceta INT
AS
BEGIN
    UPDATE Receta SET activo = 1 WHERE id_receta = @IdReceta
END
GO

CREATE PROCEDURE TraerRecetas
AS
BEGIN
    SELECT R.id_receta, R.nombre, R.activo,
           (SELECT COUNT(1) FROM DetalleReceta D WHERE D.id_receta = R.id_receta) AS cantidadInsumos
    FROM Receta R
    ORDER BY R.nombre
END
GO

CREATE PROCEDURE TraerRecetaPorId
    @IdReceta INT
AS
BEGIN
    SELECT R.id_receta, R.nombre, R.activo,
           (SELECT COUNT(1) FROM DetalleReceta D WHERE D.id_receta = R.id_receta) AS cantidadInsumos
    FROM Receta R
    WHERE R.id_receta = @IdReceta
END
GO

CREATE PROCEDURE TraerDetalleReceta
    @IdReceta INT
AS
BEGIN
    SELECT D.id_receta, D.id_insumo, I.nombre AS nombreInsumo, I.unidadDeMedida,
           ISNULL(D.cantidad, 0) AS cantidad
    FROM DetalleReceta D
    INNER JOIN Insumo I ON D.id_insumo = I.id_insumo
    WHERE D.id_receta = @IdReceta
    ORDER BY I.nombre
END
GO

-- Detalle de todas las recetas de una sola vez, para armar el catálogo XML.
CREATE PROCEDURE TraerDetallesDeRecetas
AS
BEGIN
    SELECT D.id_receta, D.id_insumo, I.nombre AS nombreInsumo, I.unidadDeMedida,
           ISNULL(D.cantidad, 0) AS cantidad
    FROM DetalleReceta D
    INNER JOIN Insumo I ON D.id_insumo = I.id_insumo
    ORDER BY D.id_receta, I.nombre
END
GO

-- Movimientos de stock registrados en la bitácora (por ejemplo, los últimos
-- ajustes de stock que muestra la pantalla de ajuste).
CREATE PROCEDURE TraerUltimosEventosPorAccion
    @Accion VARCHAR(100),
    @Cantidad INT
AS
BEGIN
    SELECT TOP (@Cantidad) E.id_evento, E.dni_usuario, U.nombreUsuario,
           E.fecha, E.accion, E.descripcion, E.criticidad
    FROM Evento E
    LEFT JOIN Usuario U ON E.dni_usuario = U.dni
    WHERE E.accion = @Accion
    ORDER BY E.id_evento DESC
END
GO

-- =============================================
-- Telemetría IoT (CU-007-039 a CU-007-043)
-- =============================================

-- Los procedimientos que traen sensores devuelven, además de las columnas de
-- la tabla, la fecha de la última alerta y cuántas alertas siguen abiertas
-- (las que no están Resueltas ni Descartadas).
CREATE PROCEDURE CrearSensor
    @MacAddress VARCHAR(50),
    @Modelo VARCHAR(100),
    @TemperaturaMaxima FLOAT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Sensor (macAddress, modelo, temperaturaMaximaPermitida, activo)
    VALUES (@MacAddress, @Modelo, @TemperaturaMaxima, 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE ModificarSensor
    @IdSensor INT,
    @MacAddress VARCHAR(50),
    @Modelo VARCHAR(100),
    @TemperaturaMaxima FLOAT
AS
BEGIN
    UPDATE Sensor SET macAddress = @MacAddress, modelo = @Modelo,
        temperaturaMaximaPermitida = @TemperaturaMaxima
    WHERE id_sensor = @IdSensor
END
GO

-- Baja lógica: se conservan las alertas que generó el sensor.
CREATE PROCEDURE BajaSensor
    @IdSensor INT
AS
BEGIN
    UPDATE Sensor SET activo = 0 WHERE id_sensor = @IdSensor
END
GO

-- Deshace la baja lógica (reactivación desde Baja de sensor).
CREATE PROCEDURE ActivarSensor
    @IdSensor INT
AS
BEGIN
    UPDATE Sensor SET activo = 1 WHERE id_sensor = @IdSensor
END
GO

CREATE PROCEDURE TraerSensores
AS
BEGIN
    SELECT S.id_sensor, S.macAddress, S.modelo, S.temperaturaMaximaPermitida, S.activo,
           (SELECT MAX(A.fechaHora) FROM Alerta A WHERE A.id_sensor = S.id_sensor) AS ultimaAlerta,
           (SELECT COUNT(*) FROM Alerta A WHERE A.id_sensor = S.id_sensor
               AND ISNULL(A.estado, '') NOT IN ('Resuelta', 'Descartada')) AS alertasAbiertas
    FROM Sensor S
    ORDER BY S.macAddress
END
GO

CREATE PROCEDURE TraerSensorPorId
    @IdSensor INT
AS
BEGIN
    SELECT S.id_sensor, S.macAddress, S.modelo, S.temperaturaMaximaPermitida, S.activo,
           (SELECT MAX(A.fechaHora) FROM Alerta A WHERE A.id_sensor = S.id_sensor) AS ultimaAlerta,
           (SELECT COUNT(*) FROM Alerta A WHERE A.id_sensor = S.id_sensor
               AND ISNULL(A.estado, '') NOT IN ('Resuelta', 'Descartada')) AS alertasAbiertas
    FROM Sensor S
    WHERE S.id_sensor = @IdSensor
END
GO

-- La MAC no se repite entre los sensores activos (#ERR023). Lo usan el alta y
-- la modificación, y el servicio web de telemetría para identificar el sensor.
CREATE PROCEDURE TraerSensorActivoPorMac
    @MacAddress VARCHAR(50)
AS
BEGIN
    SELECT S.id_sensor, S.macAddress, S.modelo, S.temperaturaMaximaPermitida, S.activo,
           (SELECT MAX(A.fechaHora) FROM Alerta A WHERE A.id_sensor = S.id_sensor) AS ultimaAlerta,
           (SELECT COUNT(*) FROM Alerta A WHERE A.id_sensor = S.id_sensor
               AND ISNULL(A.estado, '') NOT IN ('Resuelta', 'Descartada')) AS alertasAbiertas
    FROM Sensor S
    WHERE S.activo = 1 AND S.macAddress = @MacAddress
END
GO

-- Lectura por encima de la temperatura máxima del sensor (la llama el servicio
-- web de telemetría). Si el sensor no tiene una alerta abierta se crea una; si
-- ya la tiene no se duplica: se actualiza la temperatura alcanzada cuando la
-- lectura nueva es más alta. El bloqueo evita que dos lecturas simultáneas
-- generen dos alertas. Devuelve el id de la alerta y qué se hizo:
-- N = alerta nueva, A = alerta actualizada, S = sin cambios.
CREATE PROCEDURE RegistrarLecturaFueraDeRango
    @IdSensor INT,
    @Temperatura FLOAT,
    @EstadoInicial VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @IdAlerta INT, @TemperaturaAnterior FLOAT;

    BEGIN TRANSACTION;

    SELECT TOP 1 @IdAlerta = id_alerta, @TemperaturaAnterior = temperaturaAlcanzada
    FROM Alerta WITH (UPDLOCK, HOLDLOCK)
    WHERE id_sensor = @IdSensor AND ISNULL(estado, '') NOT IN ('Resuelta', 'Descartada')
    ORDER BY fechaHora DESC, id_alerta DESC;

    IF @IdAlerta IS NULL
    BEGIN
        INSERT INTO Alerta (id_sensor, fechaHora, temperaturaAlcanzada, estado, observaciones)
        VALUES (@IdSensor, GETDATE(), @Temperatura, @EstadoInicial, NULL);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS id_alerta, 'N' AS resultado;
    END
    ELSE IF ISNULL(@TemperaturaAnterior, -1000) < @Temperatura
    BEGIN
        UPDATE Alerta SET temperaturaAlcanzada = @Temperatura WHERE id_alerta = @IdAlerta;
        SELECT @IdAlerta AS id_alerta, 'A' AS resultado;
    END
    ELSE
    BEGIN
        SELECT @IdAlerta AS id_alerta, 'S' AS resultado;
    END

    COMMIT TRANSACTION;
END
GO

-- CU-007-043: el técnico cambia el estado de la alerta y deja sus observaciones.
CREATE PROCEDURE ProcesarAlerta
    @IdAlerta INT,
    @Estado VARCHAR(50),
    @Observaciones VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    -- Solo si la alerta sigue abierta: si otro técnico la cerró mientras
    -- tanto, no se reabre. Devuelve 1 si actualizó y 0 si no.
    UPDATE Alerta SET estado = @Estado, observaciones = @Observaciones
    WHERE id_alerta = @IdAlerta AND ISNULL(estado, '') NOT IN ('Resuelta', 'Descartada');
    SELECT @@ROWCOUNT;
END
GO

CREATE PROCEDURE TraerAlertas
AS
BEGIN
    SELECT A.id_alerta, A.id_sensor, A.fechaHora, A.temperaturaAlcanzada, A.estado, A.observaciones,
           S.macAddress, S.modelo, S.temperaturaMaximaPermitida
    FROM Alerta A
    LEFT JOIN Sensor S ON S.id_sensor = A.id_sensor
    ORDER BY A.fechaHora DESC, A.id_alerta DESC
END
GO

CREATE PROCEDURE TraerAlertaPorId
    @IdAlerta INT
AS
BEGIN
    SELECT A.id_alerta, A.id_sensor, A.fechaHora, A.temperaturaAlcanzada, A.estado, A.observaciones,
           S.macAddress, S.modelo, S.temperaturaMaximaPermitida
    FROM Alerta A
    LEFT JOIN Sensor S ON S.id_sensor = A.id_sensor
    WHERE A.id_alerta = @IdAlerta
END
GO

-- =============================================
-- Suscripción SaaS (CU-008-044 y CU-008-045)
-- =============================================

-- El token de pago llega encriptado con AES-256 desde la aplicación (R6).
CREATE PROCEDURE CrearSuscripcion
    @TipoPlan VARCHAR(50),
    @TokenPago VARCHAR(200),
    @Estado VARCHAR(50),
    @FechaInicio DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Suscripcion (tipoPlan, tokenPago, estado, fechaInicio)
    VALUES (@TipoPlan, @TokenPago, @Estado, @FechaInicio);
    SELECT SCOPE_IDENTITY();
END
GO

-- La baja es lógica: la suscripción pasa a Cancelada y se conserva el registro.
CREATE PROCEDURE CambiarEstadoSuscripcion
    @IdSuscripcion INT,
    @Estado VARCHAR(50)
AS
BEGIN
    UPDATE Suscripcion SET estado = @Estado WHERE id_suscripcion = @IdSuscripcion
END
GO

CREATE PROCEDURE TraerSuscripciones
AS
BEGIN
    SELECT id_suscripcion, tipoPlan, tokenPago, estado, fechaInicio
    FROM Suscripcion
    ORDER BY id_suscripcion DESC
END
GO

CREATE PROCEDURE TraerSuscripcionPorId
    @IdSuscripcion INT
AS
BEGIN
    SELECT id_suscripcion, tipoPlan, tokenPago, estado, fechaInicio
    FROM Suscripcion
    WHERE id_suscripcion = @IdSuscripcion
END
GO

-- =============================================
-- Operaciones y transacciones (CU-006-033 a CU-006-038)
-- =============================================

-- El estado de la orden empieza siempre con Pendiente, Recibida o Anulada.
-- Como indica la interfaz de los CU-006-035 y CU-006-036, en el mismo campo se
-- guarda el motivo de la anulación ("Anulada: ...") o el comprobante con el
-- que se recibió ("Recibida: ...").

-- Orden de compra con su detalle en un solo procedimiento (DALConexion lo
-- ejecuta dentro de una transacción): o se graba todo o no se graba nada.
-- El detalle llega como XML: <Detalle><Insumo idInsumo="1" cantidad="2.5" /></Detalle>
CREATE PROCEDURE CrearOrdenCompra
    @Cuit VARCHAR(20),
    @Fecha DATE,
    @Detalle XML
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdOrden INT;

    INSERT INTO OrdenCompra (cuitProveedor, fecha, estado) VALUES (@Cuit, @Fecha, 'Pendiente');
    SET @IdOrden = SCOPE_IDENTITY();

    INSERT INTO DetalleOrdenCompra (id_ordenCompra, id_insumo, cantidad)
    SELECT @IdOrden,
           X.I.value('@idInsumo', 'INT'),
           X.I.value('@cantidad', 'DECIMAL(8,2)')
    FROM @Detalle.nodes('/Detalle/Insumo') AS X(I);

    SELECT @IdOrden;
END
GO

CREATE PROCEDURE TraerOrdenesCompra
AS
BEGIN
    SELECT O.id_ordenCompra, O.cuitProveedor, P.nombre AS nombreProveedor, O.fecha, O.estado,
           (SELECT COUNT(1) FROM DetalleOrdenCompra D WHERE D.id_ordenCompra = O.id_ordenCompra) AS cantidadItems
    FROM OrdenCompra O
    LEFT JOIN Proveedor P ON P.cuit = O.cuitProveedor
    ORDER BY O.id_ordenCompra DESC
END
GO

CREATE PROCEDURE TraerOrdenCompraPorId
    @IdOrden INT
AS
BEGIN
    SELECT O.id_ordenCompra, O.cuitProveedor, P.nombre AS nombreProveedor, O.fecha, O.estado,
           (SELECT COUNT(1) FROM DetalleOrdenCompra D WHERE D.id_ordenCompra = O.id_ordenCompra) AS cantidadItems
    FROM OrdenCompra O
    LEFT JOIN Proveedor P ON P.cuit = O.cuitProveedor
    WHERE O.id_ordenCompra = @IdOrden
END
GO

-- Detalle de una orden (o de todas, si @IdOrden es NULL) con los datos del insumo.
CREATE PROCEDURE TraerDetalleOrdenCompra
    @IdOrden INT = NULL
AS
BEGIN
    SELECT D.id_ordenCompra, D.id_insumo, I.nombre, I.unidadDeMedida, D.cantidad, I.precioUnitario
    FROM DetalleOrdenCompra D
    INNER JOIN Insumo I ON I.id_insumo = D.id_insumo
    WHERE @IdOrden IS NULL OR D.id_ordenCompra = @IdOrden
    ORDER BY D.id_ordenCompra, I.nombre
END
GO

-- Cantidades ya pedidas en órdenes pendientes, por insumo: la predicción las
-- descuenta para no volver a pedir lo que ya está en camino.
CREATE PROCEDURE TraerCantidadesPendientesPorInsumo
AS
BEGIN
    SELECT D.id_insumo, SUM(D.cantidad) AS cantidad
    FROM DetalleOrdenCompra D
    INNER JOIN OrdenCompra O ON O.id_ordenCompra = D.id_ordenCompra
    WHERE O.estado = 'Pendiente'
    GROUP BY D.id_insumo
END
GO

-- CU-006-035: solo se anula una orden que sigue Pendiente. Devuelve 1 si la anuló.
CREATE PROCEDURE AnularOrdenCompra
    @IdOrden INT,
    @Estado VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenCompra SET estado = @Estado
    WHERE id_ordenCompra = @IdOrden AND estado = 'Pendiente';
    SELECT @@ROWCOUNT;
END
GO

-- CU-006-036: al registrar la factura se suma al stock lo pedido de cada insumo
-- (R8) y la orden pasa a Recibida. DALConexion lo ejecuta dentro de una
-- transacción: o se hace todo o nada. Si la orden ya no estaba Pendiente no
-- cambia nada y devuelve 0.
CREATE PROCEDURE RecibirOrdenCompra
    @IdOrden INT,
    @Estado VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE OrdenCompra SET estado = @Estado
    WHERE id_ordenCompra = @IdOrden AND estado = 'Pendiente';

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT 0;
        RETURN;
    END

    UPDATE I SET stock = ISNULL(I.stock, 0) + D.cantidad
    FROM Insumo I
    INNER JOIN DetalleOrdenCompra D ON D.id_insumo = I.id_insumo
    WHERE D.id_ordenCompra = @IdOrden;

    SELECT 1;
END
GO

-- CU-006-037: descuenta el consumo de cada insumo de la receta vendida (R8),
-- todo en una misma transacción. Si a algún insumo no le alcanza el stock (o
-- está dado de baja) no descuenta nada y devuelve esos insumos con resultado
-- 'S' (sin stock) o 'I' (inactivo). Si descontó, devuelve cada insumo con su
-- stock anterior y el nuevo, con resultado 'OK'.
CREATE PROCEDURE DescontarStockPorVenta
    @IdReceta INT,
    @Cantidad DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Consumo TABLE (id_insumo INT PRIMARY KEY, consumo DECIMAL(12,2));
    DECLARE @Antes TABLE (id_insumo INT PRIMARY KEY, stockAnterior DECIMAL(10,2));

    INSERT INTO @Consumo (id_insumo, consumo)
    SELECT D.id_insumo, CAST(ROUND(D.cantidad * @Cantidad, 2) AS DECIMAL(12,2))
    FROM DetalleReceta D
    WHERE D.id_receta = @IdReceta;

    BEGIN TRANSACTION;

    -- Bloquea los insumos de la receta: dos ventas al mismo tiempo no pueden
    -- descontar el mismo stock.
    IF EXISTS (SELECT 1
               FROM Insumo I WITH (UPDLOCK, HOLDLOCK)
               INNER JOIN @Consumo C ON C.id_insumo = I.id_insumo
               WHERE I.activo = 0 OR ISNULL(I.stock, 0) < C.consumo)
    BEGIN
        SELECT I.id_insumo, I.nombre, I.unidadDeMedida, ISNULL(I.stock, 0) AS stockAnterior,
               C.consumo, ISNULL(I.stock, 0) AS stockNuevo,
               CASE WHEN I.activo = 0 THEN 'I' ELSE 'S' END AS resultado
        FROM Insumo I
        INNER JOIN @Consumo C ON C.id_insumo = I.id_insumo
        WHERE I.activo = 0 OR ISNULL(I.stock, 0) < C.consumo
        ORDER BY I.nombre;

        COMMIT TRANSACTION;
        RETURN;
    END

    UPDATE I SET stock = ISNULL(I.stock, 0) - C.consumo
    OUTPUT inserted.id_insumo, deleted.stock INTO @Antes (id_insumo, stockAnterior)
    FROM Insumo I
    INNER JOIN @Consumo C ON C.id_insumo = I.id_insumo;

    COMMIT TRANSACTION;

    SELECT I.id_insumo, I.nombre, I.unidadDeMedida, ISNULL(A.stockAnterior, 0) AS stockAnterior,
           C.consumo, I.stock AS stockNuevo, 'OK' AS resultado
    FROM Insumo I
    INNER JOIN @Consumo C ON C.id_insumo = I.id_insumo
    INNER JOIN @Antes A ON A.id_insumo = I.id_insumo
    ORDER BY I.nombre;
END
GO

-- Eventos de una acción desde una fecha. Con 'Venta' es el historial de
-- consumo con el que se entrena la predicción de demanda (CU-006-033) y el
-- que usa el reporte gerencial.
CREATE PROCEDURE TraerEventosPorAccionDesde
    @Accion VARCHAR(100),
    @Desde DATETIME
AS
BEGIN
    SELECT E.id_evento, E.dni_usuario, U.nombreUsuario, E.fecha, E.accion, E.descripcion, E.criticidad
    FROM Evento E
    LEFT JOIN Usuario U ON E.dni_usuario = U.dni
    WHERE E.accion = @Accion AND E.fecha >= @Desde
    ORDER BY E.fecha, E.id_evento
END
GO

-- =============================================
-- DATOS INICIALES
-- =============================================

-- Roles: son los cinco perfiles que figuran en los casos de uso.
-- El WebMaster es el único que puede reparar el dígito verificador.
SET IDENTITY_INSERT Rol ON
INSERT INTO Rol (id_rol, nombre) VALUES (1, 'WebMaster')
INSERT INTO Rol (id_rol, nombre) VALUES (2, 'Encargado')
INSERT INTO Rol (id_rol, nombre) VALUES (3, 'Auditor')
INSERT INTO Rol (id_rol, nombre) VALUES (4, 'Tecnico')
INSERT INTO Rol (id_rol, nombre) VALUES (5, 'Empleado')
SET IDENTITY_INSERT Rol OFF
GO

-- Usuario Admin (contraseña: Admin123 → SHA256)
INSERT INTO Usuario (dni, id_rol, nombre, apellido, fechaNacimiento, correoElectronico, 
                     numeroTelefono, nombreUsuario, contraseña, activo, bloqueado, intentosFallidos)
VALUES (12345678, 1, 'Admin', 'Sistema', '1990-01-01', 'admin@intelligentstock.com',
        '1100000000', 'Admin', '3b612c75a7b5048a435fb6ec81e52ff92d6d795a8b5a9c17070f6a63c97a53b2', 1, 0, 0)
GO

-- Patentes (permisos individuales - hojas del Composite)
SET IDENTITY_INSERT Familia ON
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (1, 'AltaUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (2, 'BajaUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (3, 'ModificacionUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (4, 'ConsultaUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (5, 'BloquearUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (6, 'DesbloquearUsuario', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (7, 'CambiarContraseña', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (8, 'AltaFamilia', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (9, 'BajaFamilia', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (10, 'ModificacionFamilia', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (11, 'ConsultaFamilia', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (12, 'ConsultaPatente', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (13, 'BitacoraEventos', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (14, 'BackUp', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (15, 'Restore', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (16, 'ReparacionDV', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (17, 'AltaInsumo', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (18, 'BajaInsumo', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (19, 'ModificacionInsumo', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (20, 'ConsultaInsumo', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (21, 'AltaProveedor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (22, 'BajaProveedor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (23, 'ModificacionProveedor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (24, 'ConsultaProveedor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (25, 'AltaReceta', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (26, 'BajaReceta', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (27, 'ModificacionReceta', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (28, 'ConsultaReceta', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (29, 'AltaSensor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (30, 'BajaSensor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (31, 'ModificacionSensor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (32, 'ConsultaSensor', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (33, 'AltaSuscripcion', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (34, 'BajaSuscripcion', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (35, 'GenerarOrdenCompra', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (36, 'AnularOrdenCompra', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (37, 'ConsultarOrdenCompra', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (38, 'AjusteStock', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (39, 'DescontarStockVenta', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (40, 'ProcesarAlertaTermica', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (41, 'ExportarReporteGerencial', 'P')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (42, 'RegistrarFacturaProveedor', 'P')

-- Familias (agrupaciones de permisos - nodos compuestos del Composite)
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (100, 'Gestión de Usuarios', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (101, 'Gestión de Permisos', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (102, 'Administración del Sistema', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (103, 'Gestión de Insumos', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (104, 'Gestión de Proveedores', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (105, 'Gestión de Recetas', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (106, 'Gestión de Sensores', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (107, 'Gestión de Compras', 'F')
INSERT INTO Familia (id_familia, nombre, tipo) VALUES (108, 'Reportes', 'F')
SET IDENTITY_INSERT Familia OFF
GO

-- Relaciones Familia (jerarquía del Composite)
-- Gestión de Usuarios contiene patentes de usuario
INSERT INTO RelacionFamilia VALUES (100, 1)   -- AltaUsuario
INSERT INTO RelacionFamilia VALUES (100, 2)   -- BajaUsuario
INSERT INTO RelacionFamilia VALUES (100, 3)   -- ModificacionUsuario
INSERT INTO RelacionFamilia VALUES (100, 4)   -- ConsultaUsuario
INSERT INTO RelacionFamilia VALUES (100, 5)   -- BloquearUsuario
INSERT INTO RelacionFamilia VALUES (100, 6)   -- DesbloquearUsuario
INSERT INTO RelacionFamilia VALUES (100, 7)   -- CambiarContraseña

-- Gestión de Permisos
INSERT INTO RelacionFamilia VALUES (101, 8)   -- AltaFamilia
INSERT INTO RelacionFamilia VALUES (101, 9)   -- BajaFamilia
INSERT INTO RelacionFamilia VALUES (101, 10)  -- ModificacionFamilia
INSERT INTO RelacionFamilia VALUES (101, 11)  -- ConsultaFamilia
INSERT INTO RelacionFamilia VALUES (101, 12)  -- ConsultaPatente

-- Administración del Sistema
INSERT INTO RelacionFamilia VALUES (102, 13)  -- BitacoraEventos
INSERT INTO RelacionFamilia VALUES (102, 14)  -- BackUp
INSERT INTO RelacionFamilia VALUES (102, 15)  -- Restore
INSERT INTO RelacionFamilia VALUES (102, 16)  -- ReparacionDV

-- Gestión de Insumos
INSERT INTO RelacionFamilia VALUES (103, 17)  -- AltaInsumo
INSERT INTO RelacionFamilia VALUES (103, 18)  -- BajaInsumo
INSERT INTO RelacionFamilia VALUES (103, 19)  -- ModificacionInsumo
INSERT INTO RelacionFamilia VALUES (103, 20)  -- ConsultaInsumo

-- Gestión de Proveedores
INSERT INTO RelacionFamilia VALUES (104, 21)  -- AltaProveedor
INSERT INTO RelacionFamilia VALUES (104, 22)  -- BajaProveedor
INSERT INTO RelacionFamilia VALUES (104, 23)  -- ModificacionProveedor
INSERT INTO RelacionFamilia VALUES (104, 24)  -- ConsultaProveedor

-- Gestión de Recetas
INSERT INTO RelacionFamilia VALUES (105, 25)  -- AltaReceta
INSERT INTO RelacionFamilia VALUES (105, 26)  -- BajaReceta
INSERT INTO RelacionFamilia VALUES (105, 27)  -- ModificacionReceta
INSERT INTO RelacionFamilia VALUES (105, 28)  -- ConsultaReceta

-- Gestión de Sensores
INSERT INTO RelacionFamilia VALUES (106, 29)  -- AltaSensor
INSERT INTO RelacionFamilia VALUES (106, 30)  -- BajaSensor
INSERT INTO RelacionFamilia VALUES (106, 31)  -- ModificacionSensor
INSERT INTO RelacionFamilia VALUES (106, 32)  -- ConsultaSensor

-- Gestión de Compras
INSERT INTO RelacionFamilia VALUES (107, 35)  -- GenerarOrdenCompra
INSERT INTO RelacionFamilia VALUES (107, 36)  -- AnularOrdenCompra
INSERT INTO RelacionFamilia VALUES (107, 37)  -- ConsultarOrdenCompra
INSERT INTO RelacionFamilia VALUES (107, 42)  -- RegistrarFacturaProveedor

-- Reportes
INSERT INTO RelacionFamilia VALUES (108, 38)  -- AjusteStock
INSERT INTO RelacionFamilia VALUES (108, 39)  -- DescontarStockVenta
INSERT INTO RelacionFamilia VALUES (108, 40)  -- ProcesarAlertaTermica
INSERT INTO RelacionFamilia VALUES (108, 41)  -- ExportarReporteGerencial
GO

-- Acá se ve el Composite en acción: a un rol se le puede colgar una familia
-- entera (id >= 100) o una patente suelta (id <= 42), indistintamente.

-- WebMaster (1): todas las familias + las dos patentes que quedaron sueltas.
INSERT INTO RolFamilia VALUES (1, 100)  -- Gestión de Usuarios
INSERT INTO RolFamilia VALUES (1, 101)  -- Gestión de Permisos
INSERT INTO RolFamilia VALUES (1, 102)  -- Administración del Sistema
INSERT INTO RolFamilia VALUES (1, 103)  -- Gestión de Insumos
INSERT INTO RolFamilia VALUES (1, 104)  -- Gestión de Proveedores
INSERT INTO RolFamilia VALUES (1, 105)  -- Gestión de Recetas
INSERT INTO RolFamilia VALUES (1, 106)  -- Gestión de Sensores
INSERT INTO RolFamilia VALUES (1, 107)  -- Gestión de Compras
INSERT INTO RolFamilia VALUES (1, 108)  -- Reportes
INSERT INTO RolFamilia VALUES (1, 33)   -- AltaSuscripcion
INSERT INTO RolFamilia VALUES (1, 34)   -- BajaSuscripcion

-- Encargado (2): gestión de empleados, proveedores, insumos, recetas,
-- operaciones y consulta de sensores. No administra el sistema.
INSERT INTO RolFamilia VALUES (2, 100)  -- Gestión de Usuarios (009 a 015)
INSERT INTO RolFamilia VALUES (2, 103)  -- Gestión de Insumos (024 a 027)
INSERT INTO RolFamilia VALUES (2, 104)  -- Gestión de Proveedores (020 a 023)
INSERT INTO RolFamilia VALUES (2, 105)  -- Gestión de Recetas (028 a 031)
INSERT INTO RolFamilia VALUES (2, 107)  -- Gestión de Compras (033 a 036)
INSERT INTO RolFamilia VALUES (2, 108)  -- Reportes (032, 037, 038, 043)
INSERT INTO RolFamilia VALUES (2, 32)   -- ConsultaSensor (042)

-- Auditor (3): solo consultas, más la bitácora.
INSERT INTO RolFamilia VALUES (3, 4)    -- ConsultaUsuario (012)
INSERT INTO RolFamilia VALUES (3, 7)    -- CambiarContraseña (013)
INSERT INTO RolFamilia VALUES (3, 11)   -- ConsultaFamilia (007)
INSERT INTO RolFamilia VALUES (3, 12)   -- ConsultaPatente (008)
INSERT INTO RolFamilia VALUES (3, 13)   -- BitacoraEventos (016)
INSERT INTO RolFamilia VALUES (3, 20)   -- ConsultaInsumo (027)
INSERT INTO RolFamilia VALUES (3, 24)   -- ConsultaProveedor (023)
INSERT INTO RolFamilia VALUES (3, 28)   -- ConsultaReceta (031)
INSERT INTO RolFamilia VALUES (3, 32)   -- ConsultaSensor (042)
INSERT INTO RolFamilia VALUES (3, 37)   -- ConsultarOrdenCompra (034)
INSERT INTO RolFamilia VALUES (3, 41)   -- ExportarReporteGerencial (038)

-- Tecnico (4): mantenimiento del sistema y telemetría IoT.
-- Es el otro perfil que puede reparar el dígito verificador.
INSERT INTO RolFamilia VALUES (4, 102)  -- Administración del Sistema (016 a 019)
INSERT INTO RolFamilia VALUES (4, 106)  -- Gestión de Sensores (039 a 042)
INSERT INTO RolFamilia VALUES (4, 40)   -- ProcesarAlertaTermica (043)
INSERT INTO RolFamilia VALUES (4, 7)    -- CambiarContraseña (013)

-- Empleado (5): movimientos de stock del día a día y consultas.
INSERT INTO RolFamilia VALUES (5, 7)    -- CambiarContraseña (013)
INSERT INTO RolFamilia VALUES (5, 20)   -- ConsultaInsumo (027)
INSERT INTO RolFamilia VALUES (5, 24)   -- ConsultaProveedor (023)
INSERT INTO RolFamilia VALUES (5, 28)   -- ConsultaReceta (031)
INSERT INTO RolFamilia VALUES (5, 32)   -- ConsultaSensor (042)
INSERT INTO RolFamilia VALUES (5, 37)   -- ConsultarOrdenCompra (034)
INSERT INTO RolFamilia VALUES (5, 38)   -- AjusteStock (032)
INSERT INTO RolFamilia VALUES (5, 39)   -- DescontarStockVenta (037)
GO

-- Registros iniciales en tabla DV. Quedan vacíos: los dígitos se calculan
-- desde la aplicación. Al primer inicio de sesión del WebMaster el sistema
-- muestra estas tablas como "Sin dígitos verificadores" y se calculan con
-- Recalcular, en Reparar dígito verificador (queda registrado en la bitácora).
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Usuario', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Rol', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Familia', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('RolFamilia', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('RelacionFamilia', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Proveedor', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Insumo', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('OrdenCompra', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('DetalleOrdenCompra', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Receta', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('DetalleReceta', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Sensor', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Alerta', '', '')
INSERT INTO DV (tabla, DVH, DVV) VALUES ('Suscripcion', '', '')
GO

USE [master]
GO
