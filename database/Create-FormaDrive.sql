-- Forma Drive: создание локальной базы для приложения.
-- В SSMS подключитесь к .\SQLEXPRESS с проверкой подлинности Windows и нажмите F5.
-- Повторное выполнение применяет только ещё не выполненные миграции.
USE [master];
GO
IF DB_ID(N'FormaDrive') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [FormaDrive]');
END;
GO
USE [FormaDrive];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE TABLE [dbo].[Cars] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Photo] nvarchar(80) NOT NULL,
        [Body] nvarchar(60) NOT NULL,
        [Transmission] nvarchar(60) NOT NULL,
        [Color] nvarchar(60) NOT NULL,
        [Seats] int NOT NULL,
        [Premium] bit NOT NULL,
        [Rate] int NOT NULL,
        [Deposit] int NOT NULL,
        [Mileage] int NOT NULL,
        CONSTRAINT [PK_Cars] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE TABLE [dbo].[Members] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Staff] bit NOT NULL,
        CONSTRAINT [PK_Members] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE TABLE [dbo].[ServicePeriods] (
        [Id] int NOT NULL IDENTITY,
        [CarId] int NOT NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NOT NULL,
        CONSTRAINT [PK_ServicePeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServicePeriods_Cars_CarId] FOREIGN KEY ([CarId]) REFERENCES [dbo].[Cars] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE TABLE [dbo].[Bookings] (
        [Id] int NOT NULL IDENTITY,
        [CarId] int NOT NULL,
        [MemberId] int NOT NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NOT NULL,
        [State] int NOT NULL,
        [Location] nvarchar(30) NOT NULL,
        [Seat] bit NOT NULL,
        [DailyRate] int NOT NULL,
        [Deposit] int NOT NULL,
        [Extras] int NOT NULL,
        [Total] int NOT NULL,
        [OutKm] int NULL,
        [InKm] int NULL,
        [OutNotes] nvarchar(500) NULL,
        [InNotes] nvarchar(500) NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_Cars_CarId] FOREIGN KEY ([CarId]) REFERENCES [dbo].[Cars] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE TABLE [dbo].[Favorites] (
        [MemberId] int NOT NULL,
        [CarId] int NOT NULL,
        CONSTRAINT [PK_Favorites] PRIMARY KEY ([MemberId], [CarId]),
        CONSTRAINT [FK_Favorites_Cars_CarId] FOREIGN KEY ([CarId]) REFERENCES [dbo].[Cars] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Favorites_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Body', N'Color', N'Deposit', N'Mileage', N'Name', N'Photo', N'Premium', N'Rate', N'Seats', N'Transmission') AND [object_id] = OBJECT_ID(N'[dbo].[Cars]'))
        SET IDENTITY_INSERT [dbo].[Cars] ON;
    EXEC(N'INSERT INTO [dbo].[Cars] ([Id], [Body], [Color], [Deposit], [Mileage], [Name], [Photo], [Premium], [Rate], [Seats], [Transmission])
    VALUES (1, N''Хэтчбек'', N''Белый'', 200, 18400, N''Volkswagen Golf'', N''golf'', CAST(0 AS bit), 95, 5, N''Автомат''),
    (2, N''Лифтбек'', N''Синий'', 200, 18400, N''Škoda Octavia'', N''octavia'', CAST(0 AS bit), 115, 5, N''Автомат''),
    (3, N''Хэтчбек'', N''Красный'', 200, 18400, N''Renault Clio'', N''clio'', CAST(0 AS bit), 75, 5, N''Механика''),
    (4, N''Седан'', N''Серебристый'', 200, 18400, N''Toyota Corolla'', N''corolla'', CAST(0 AS bit), 120, 5, N''Автомат''),
    (5, N''Кроссовер'', N''Оливковый'', 200, 18400, N''Hyundai Tucson'', N''tucson'', CAST(0 AS bit), 165, 5, N''Автомат''),
    (6, N''Кроссовер'', N''Тёмно-синий'', 200, 18400, N''Kia Sportage'', N''sportage'', CAST(0 AS bit), 160, 5, N''Автомат''),
    (7, N''Купе'', N''Серебристый'', 600, 18400, N''Porsche 911 Carrera'', N''carrera'', CAST(1 AS bit), 490, 4, N''Автомат''),
    (8, N''Седан'', N''Графитовый'', 600, 18400, N''BMW 530i'', N''530i'', CAST(1 AS bit), 285, 5, N''Автомат'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Body', N'Color', N'Deposit', N'Mileage', N'Name', N'Photo', N'Premium', N'Rate', N'Seats', N'Transmission') AND [object_id] = OBJECT_ID(N'[dbo].[Cars]'))
        SET IDENTITY_INSERT [dbo].[Cars] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Bookings_Car_Start_End] ON [dbo].[Bookings] ([CarId], [Start], [End]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE INDEX [MemberId] ON [dbo].[Bookings] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE INDEX [CarId] ON [dbo].[Favorites] ([CarId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [Email] ON [dbo].[Members] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Service_Car_Start_End] ON [dbo].[ServicePeriods] ([CarId], [Start], [End]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123085603_InitialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123085603_InitialSqlServer', N'9.0.17');
END;

COMMIT;
GO


-- Обновление ранее созданной базы FormaDrive. Выполните весь файл в SSMS.
USE [FormaDrive];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261123120000_TripFeatures')
BEGIN
ALTER TABLE dbo.Bookings ADD IssuedAt datetime2 NULL, ReturnedAt datetime2 NULL;
CREATE TABLE dbo.Inspections (
    Id int IDENTITY CONSTRAINT PK_Inspections PRIMARY KEY, BookingId int NOT NULL, StaffId int NOT NULL,
    IsReturn bit NOT NULL, CreatedAt datetime2 NOT NULL, Mileage int NOT NULL,
    Notes nvarchar(500) NOT NULL, DamageZones nvarchar(150) NOT NULL,
    CONSTRAINT FK_Inspections_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Inspections_Members_StaffId FOREIGN KEY (StaffId) REFERENCES dbo.Members(Id)
);
CREATE UNIQUE INDEX IX_Inspections_BookingId_IsReturn ON dbo.Inspections(BookingId,IsReturn);
CREATE INDEX IX_Inspections_StaffId ON dbo.Inspections(StaffId);
CREATE TABLE dbo.InspectionPhotos (
    Id int IDENTITY CONSTRAINT PK_InspectionPhotos PRIMARY KEY, InspectionId int NOT NULL,
    Content varbinary(max) NOT NULL, ContentType nvarchar(20) NOT NULL,
    CONSTRAINT FK_InspectionPhotos_Inspections_InspectionId FOREIGN KEY (InspectionId) REFERENCES dbo.Inspections(Id) ON DELETE CASCADE
);
CREATE INDEX IX_InspectionPhotos_InspectionId ON dbo.InspectionPhotos(InspectionId);
CREATE TABLE dbo.Payments (
    Id int IDENTITY CONSTRAINT PK_Payments PRIMARY KEY, BookingId int NOT NULL, StaffId int NOT NULL,
    Kind int NOT NULL, Amount int NOT NULL, CreatedAt datetime2 NOT NULL, Note nvarchar(500) NOT NULL, RequestId uniqueidentifier NOT NULL,
    CONSTRAINT FK_Payments_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Payments_Members_StaffId FOREIGN KEY (StaffId) REFERENCES dbo.Members(Id)
);
CREATE INDEX IX_Payments_BookingId ON dbo.Payments(BookingId);
CREATE UNIQUE INDEX IX_Payments_RequestId ON dbo.Payments(RequestId);
CREATE INDEX IX_Payments_StaffId ON dbo.Payments(StaffId);
CREATE TABLE dbo.Notifications (
    Id int IDENTITY CONSTRAINT PK_Notifications PRIMARY KEY, MemberId int NOT NULL, BookingId int NOT NULL,
    Text nvarchar(500) NOT NULL, CreatedAt datetime2 NOT NULL, ReadAt datetime2 NULL,
    CONSTRAINT FK_Notifications_Members_MemberId FOREIGN KEY (MemberId) REFERENCES dbo.Members(Id),
    CONSTRAINT FK_Notifications_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id)
);
CREATE INDEX IX_Notifications_MemberId_ReadAt ON dbo.Notifications(MemberId,ReadAt);
CREATE INDEX IX_Notifications_BookingId ON dbo.Notifications(BookingId);
INSERT dbo.__EFMigrationsHistory (MigrationId,ProductVersion) VALUES (N'20261123120000_TripFeatures',N'9.0.17');
END;
COMMIT;
GO

