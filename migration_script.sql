BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE TABLE [Menus] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [StoreId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Menus] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Menus_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE TABLE [Groups] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [MenuId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Groups_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE TABLE [MenuProducts] (
        [Id] uniqueidentifier NOT NULL,
        [MenuId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MenuProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuProducts_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE TABLE [GroupProducts] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_GroupProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroupProducts_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE TABLE [MenuGroups] (
        [Id] uniqueidentifier NOT NULL,
        [MenuId] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MenuGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuGroups_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuGroups_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_CashDesks_MenuId] ON [CashDesks] ([MenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_GroupProducts_GroupId] ON [GroupProducts] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_GroupProducts_ProductId] ON [GroupProducts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_Groups_MenuId] ON [Groups] ([MenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_MenuGroups_GroupId] ON [MenuGroups] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_MenuGroups_MenuId] ON [MenuGroups] ([MenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_MenuProducts_MenuId] ON [MenuProducts] ([MenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_MenuProducts_ProductId] ON [MenuProducts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    CREATE INDEX [IX_Menus_StoreId] ON [Menus] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    ALTER TABLE [CashDesks] ADD CONSTRAINT [FK_CashDesks_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816092738_AddMenuEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260816092738_AddMenuEntities', N'10.0.9');
END;

COMMIT;
GO

