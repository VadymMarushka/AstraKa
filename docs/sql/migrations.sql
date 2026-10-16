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
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [ExternalId] nvarchar(200) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [ZodiacSigns] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(20) NOT NULL,
        [SequenceNumber] int NOT NULL,
        CONSTRAINT [PK_ZodiacSigns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Starshepherds] (
        [UserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Starshepherds] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Starshepherds_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Wanderers] (
        [UserId] uniqueidentifier NOT NULL,
        [ZodiacSignId] int NOT NULL,
        CONSTRAINT [PK_Wanderers] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Wanderers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Wanderers_ZodiacSigns_ZodiacSignId] FOREIGN KEY ([ZodiacSignId]) REFERENCES [ZodiacSigns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Promise] nvarchar(500) NOT NULL,
        [Price] decimal(10,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Courses_Starshepherds_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Starshepherds] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Exercises] (
        [Id] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [VideoUrl] nvarchar(500) NULL,
        CONSTRAINT [PK_Exercises] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Exercises_Starshepherds_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Starshepherds] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Cohorts] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [StartDate] date NOT NULL,
        [SeatLimit] int NOT NULL,
        [SeatsTaken] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Cohorts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Cohort_SeatLimit] CHECK ([SeatLimit] > 0),
        CONSTRAINT [FK_Cohorts_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseDays] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [DayNumber] int NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [MeetLink] nvarchar(500) NULL,
        CONSTRAINT [PK_CourseDays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseDays_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseSigns] (
        [CourseId] uniqueidentifier NOT NULL,
        [ZodiacSignId] int NOT NULL,
        CONSTRAINT [PK_CourseSigns] PRIMARY KEY ([CourseId], [ZodiacSignId]),
        CONSTRAINT [FK_CourseSigns_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CourseSigns_ZodiacSigns_ZodiacSignId] FOREIGN KEY ([ZodiacSignId]) REFERENCES [ZodiacSigns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [WandererId] uniqueidentifier NOT NULL,
        [CohortId] uniqueidentifier NOT NULL,
        [EnrolledAt] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PaidPrice] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([WandererId], [CohortId]),
        CONSTRAINT [FK_Enrollments_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Wanderers_WandererId] FOREIGN KEY ([WandererId]) REFERENCES [Wanderers] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [DailyReports] (
        [Id] uniqueidentifier NOT NULL,
        [WandererId] uniqueidentifier NOT NULL,
        [CohortId] uniqueidentifier NOT NULL,
        [Date] date NOT NULL,
        [HarmonyLevel] tinyint NOT NULL,
        CONSTRAINT [PK_DailyReports] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DailyReport_HarmonyLevel] CHECK ([HarmonyLevel] BETWEEN 1 AND 10),
        CONSTRAINT [FK_DailyReports_Enrollments_WandererId_CohortId] FOREIGN KEY ([WandererId], [CohortId]) REFERENCES [Enrollments] ([WandererId], [CohortId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [Revelations] (
        [Id] uniqueidentifier NOT NULL,
        [WandererId] uniqueidentifier NOT NULL,
        [CohortId] uniqueidentifier NOT NULL,
        [CourseDayId] uniqueidentifier NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Revelations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Revelations_CourseDays_CourseDayId] FOREIGN KEY ([CourseDayId]) REFERENCES [CourseDays] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Revelations_Enrollments_WandererId_CohortId] FOREIGN KEY ([WandererId], [CohortId]) REFERENCES [Enrollments] ([WandererId], [CohortId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [DayExercises] (
        [Id] uniqueidentifier NOT NULL,
        [CourseDayId] uniqueidentifier NOT NULL,
        [ExerciseId] uniqueidentifier NOT NULL,
        [RevelationId] uniqueidentifier NULL,
        [OrderNumber] int NOT NULL,
        [Sets] int NOT NULL,
        [Amount] int NOT NULL,
        CONSTRAINT [PK_DayExercises] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DayExercise_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_DayExercise_Sets] CHECK ([Sets] > 0),
        CONSTRAINT [FK_DayExercises_CourseDays_CourseDayId] FOREIGN KEY ([CourseDayId]) REFERENCES [CourseDays] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DayExercises_Exercises_ExerciseId] FOREIGN KEY ([ExerciseId]) REFERENCES [Exercises] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DayExercises_Revelations_RevelationId] FOREIGN KEY ([RevelationId]) REFERENCES [Revelations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE TABLE [ExerciseCompletions] (
        [Id] uniqueidentifier NOT NULL,
        [DailyReportId] uniqueidentifier NOT NULL,
        [DayExerciseId] uniqueidentifier NOT NULL,
        [CompletedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ExerciseCompletions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExerciseCompletions_DailyReports_DailyReportId] FOREIGN KEY ([DailyReportId]) REFERENCES [DailyReports] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ExerciseCompletions_DayExercises_DayExerciseId] FOREIGN KEY ([DayExerciseId]) REFERENCES [DayExercises] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cohorts_CourseId] ON [Cohorts] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourseDays_CourseId_DayNumber] ON [CourseDays] ([CourseId], [DayNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_AuthorId] ON [Courses] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CourseSigns_ZodiacSignId] ON [CourseSigns] ([ZodiacSignId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyReports_WandererId_CohortId_Date] ON [DailyReports] ([WandererId], [CohortId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DayExercises_CourseDayId_OrderNumber] ON [DayExercises] ([CourseDayId], [OrderNumber]) WHERE [RevelationId] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DayExercises_ExerciseId] ON [DayExercises] ([ExerciseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DayExercises_RevelationId] ON [DayExercises] ([RevelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_CohortId] ON [Enrollments] ([CohortId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExerciseCompletions_DailyReportId_DayExerciseId] ON [ExerciseCompletions] ([DailyReportId], [DayExerciseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExerciseCompletions_DayExerciseId] ON [ExerciseCompletions] ([DayExerciseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Exercises_AuthorId] ON [Exercises] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Revelations_CourseDayId] ON [Revelations] ([CourseDayId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Revelations_WandererId_CohortId] ON [Revelations] ([WandererId], [CohortId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_ExternalId] ON [Users] ([ExternalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Wanderers_ZodiacSignId] ON [Wanderers] ([ZodiacSignId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ZodiacSigns_Name] ON [ZodiacSigns] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ZodiacSigns_SequenceNumber] ON [ZodiacSigns] ([SequenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016123549_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016123549_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    EXEC sp_rename N'[Users].[Role]', N'RoleId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    EXEC sp_rename N'[Exercises].[Unit]', N'UnitId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    EXEC sp_rename N'[Enrollments].[Status]', N'StatusId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    EXEC sp_rename N'[Courses].[Status]', N'StatusId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'RoleId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Users] ALTER COLUMN [RoleId] tinyint NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Exercises]') AND [c].[name] = N'UnitId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Exercises] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Exercises] ALTER COLUMN [UnitId] tinyint NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Enrollments]') AND [c].[name] = N'StatusId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Enrollments] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Enrollments] ALTER COLUMN [StatusId] tinyint NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Courses]') AND [c].[name] = N'StatusId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Courses] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Courses] ALTER COLUMN [StatusId] tinyint NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE TABLE [CourseStatuses] (
        [Id] tinyint NOT NULL,
        [Name] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_CourseStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE TABLE [EnrollmentStatuses] (
        [Id] tinyint NOT NULL,
        [Name] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_EnrollmentStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE TABLE [ExerciseUnits] (
        [Id] tinyint NOT NULL,
        [Name] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_ExerciseUnits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [Id] tinyint NOT NULL,
        [Name] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[CourseStatuses]'))
        SET IDENTITY_INSERT [CourseStatuses] ON;
    EXEC(N'INSERT INTO [CourseStatuses] ([Id], [Name])
    VALUES (CAST(1 AS tinyint), N''Draft''),
    (CAST(2 AS tinyint), N''Published'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[CourseStatuses]'))
        SET IDENTITY_INSERT [CourseStatuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[EnrollmentStatuses]'))
        SET IDENTITY_INSERT [EnrollmentStatuses] ON;
    EXEC(N'INSERT INTO [EnrollmentStatuses] ([Id], [Name])
    VALUES (CAST(1 AS tinyint), N''Active''),
    (CAST(2 AS tinyint), N''Left'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[EnrollmentStatuses]'))
        SET IDENTITY_INSERT [EnrollmentStatuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[ExerciseUnits]'))
        SET IDENTITY_INSERT [ExerciseUnits] ON;
    EXEC(N'INSERT INTO [ExerciseUnits] ([Id], [Name])
    VALUES (CAST(1 AS tinyint), N''Reps''),
    (CAST(2 AS tinyint), N''Seconds''),
    (CAST(3 AS tinyint), N''Breaths'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[ExerciseUnits]'))
        SET IDENTITY_INSERT [ExerciseUnits] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[UserRoles]'))
        SET IDENTITY_INSERT [UserRoles] ON;
    EXEC(N'INSERT INTO [UserRoles] ([Id], [Name])
    VALUES (CAST(1 AS tinyint), N''Wanderer''),
    (CAST(2 AS tinyint), N''Starshepherd''),
    (CAST(3 AS tinyint), N''Oracle'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[UserRoles]'))
        SET IDENTITY_INSERT [UserRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE INDEX [IX_Exercises_UnitId] ON [Exercises] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE INDEX [IX_Enrollments_StatusId] ON [Enrollments] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE INDEX [IX_Courses_StatusId] ON [Courses] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourseStatuses_Name] ON [CourseStatuses] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EnrollmentStatuses_Name] ON [EnrollmentStatuses] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExerciseUnits_Name] ON [ExerciseUnits] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserRoles_Name] ON [UserRoles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    ALTER TABLE [Courses] ADD CONSTRAINT [FK_Courses_CourseStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CourseStatuses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    ALTER TABLE [Enrollments] ADD CONSTRAINT [FK_Enrollments_EnrollmentStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [EnrollmentStatuses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    ALTER TABLE [Exercises] ADD CONSTRAINT [FK_Exercises_ExerciseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [ExerciseUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_UserRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [UserRoles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016130116_AddLookupTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016130116_AddLookupTables', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016152006_SeedReferenceData'
)
BEGIN
    ALTER TABLE [Exercises] ADD [Instructions] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016152006_SeedReferenceData'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AuthorId', N'Description', N'Instructions', N'Name', N'UnitId', N'VideoUrl') AND [object_id] = OBJECT_ID(N'[Exercises]'))
        SET IDENTITY_INSERT [Exercises] ON;
    EXEC(N'INSERT INTO [Exercises] ([Id], [AuthorId], [Description], [Instructions], [Name], [UnitId], [VideoUrl])
    VALUES (''169f5b0a-9451-4222-8115-b0d881cdeec6'', NULL, N''Give something to the sky. Do not ask what. It will know.'', N''Stand facing home with the palms joined at the chest. Slowly extend the arms forward with the palms turned up, as if giving something away, then draw the hands back to the chest. About 4 seconds out, 4 back. One rep = out and back.'', N''First Offering'', CAST(1 AS tinyint), NULL),
    (''32b2e728-63fb-4379-a642-308c06f925b0'', NULL, N''The stars hum at a frequency only the aligned can hear. Begin by humming at it.'', N''Sit cross-legged facing home with the hands on the knees. Inhale through the nose for 4 seconds. Exhale for 6 seconds while humming one low steady note. Keep the jaw loose and the volume quiet. Never hold the breath.'', N''Resonance'', CAST(3 AS tinyint), NULL),
    (''401175c3-8c76-4727-8b87-10c330c89e91'', NULL, N''Breathe on the flame, but do not extinguish it. Home is not that kind of place.'', N''Sit cross-legged facing home. Hold one index finger upright in front of the face, pointing toward home like a candle. Inhale through the nose for 4 seconds, then exhale slowly through pursed lips so the finger-flame would sway but not go out. Never hold the breath.'', N''Steady Flame'', CAST(3 AS tinyint), NULL),
    (''53035a10-7991-4649-90e9-4dba38cc3231'', NULL, N''Fall slowly, so the Earth has time to prepare. Lie as a star lies. Rise slowly, so it has time to notice.'', N''Stand facing home. Slowly lower yourself to kneeling, then sit, then lie down on your back with the arms and legs spread wide like a star, the gaze turned toward home. Pause for 2 seconds, then rise the same way. Use a hand on the floor for support if needed. One rep = down and up.'', N''Falling Star'', CAST(1 AS tinyint), NULL),
    (''8975cb3e-32d5-4e2d-9eaa-546a20a12b13'', NULL, N''Let the sky rise and fall inside you. It is only borrowing the space.'', N''Lie on your back with the head toward home and the knees bent, one hand on the chest and the other on the belly. Inhale through the nose for 4 seconds so the belly rises, then exhale for 6 seconds so it falls. Never hold the breath. One breath = one rise and fall.'', N''Quiet Tide'', CAST(3 AS tinyint), NULL),
    (''ada984f7-18c0-407d-85db-f2e4a345467a'', NULL, N''Be the line along which the light travels. Point at home, not near it.'', N''Lie on your side with your legs stacked, supported on the forearm beneath you. Lift the hips so the body forms one straight line. Extend the free arm straight forward and point it toward home. Hold, then switch sides at the halfway point. Lower the bottom knee to the floor if the line breaks.'', N''Beacon'', CAST(2 AS tinyint), NULL),
    (''b12345be-0505-40ff-acb3-a385ce5b53a0'', NULL, N''Become the bridge by which the soul crosses back. Open your palms; it may wish to carry something.'', N''Lie on your back, knees bent, feet flat and hip-width apart, head toward home. Rest the arms along the body with the palms open and facing up. Lift the hips until knees, hips and shoulders form one line. Hold while breathing evenly.'', N''Span'', CAST(2 AS tinyint), NULL),
    (''c7e7a3ad-2d18-4966-9bce-c0c69ca2d8c0'', NULL, N''Circle the center without touching it. Do not look away from home; it has noticed before.'', N''Sit cross-legged with the hands on the knees. Slowly circle the upper body around the center of the spine, about 6 seconds per circle, while the gaze stays fixed on home. Reverse the direction at the halfway point.'', N''Orbit'', CAST(2 AS tinyint), NULL),
    (''cb78039e-b011-4856-b6f1-1955674690ce'', NULL, N''Bow to the place you cannot see. It has seen you.'', N''Kneel facing home. Fold forward with the arms extended toward home and the forehead resting on the floor. Slowly return to kneeling with the hands on the knees. One rep = bow and return.'', N''Homeward Bow'', CAST(1 AS tinyint), NULL),
    (''df53500d-0502-494c-80f4-5074add1199f'', NULL, N''Stand on one leg, as the watchers do. The other leg is not needed tonight.'', N''Stand facing home next to a wall or chair. Place the sole of one foot against the inner calf or ankle of the standing leg (never against the knee), palms together at the chest, gaze on the horizon. Hold, then switch legs at the halfway point. Touch the support if you wobble.'', N''Lone Watcher'', CAST(2 AS tinyint), NULL),
    (''f621908e-c663-468f-872d-5de06632e0a4'', NULL, N''Point at the highest place in the sky above home. Return only when it has been located.'', N''Stand facing home with the feet hip-width apart. Raise both arms diagonally forward and up, reaching toward home, with the index fingers pointing and the gaze following them. Rise onto the toes at the same time. Lower slowly, about 4 seconds up and 4 down. One rep = up and down.'', N''Zenith Reach'', CAST(1 AS tinyint), NULL),
    (''fad9002c-ec63-4853-ae89-cd478db86811'', NULL, N''One finger points to the zenith, the other to the ground. Between them there is only you. Keep the line straight.'', N''Stand facing home with the feet wide. Raise one arm diagonally forward and up, and lower the other diagonally back and down, so that both arms form one straight line through the body. Point with the index fingers and keep the gaze on the upper finger. Switch the arms at the halfway point.'', N''Meridian'', CAST(2 AS tinyint), NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AuthorId', N'Description', N'Instructions', N'Name', N'UnitId', N'VideoUrl') AND [object_id] = OBJECT_ID(N'[Exercises]'))
        SET IDENTITY_INSERT [Exercises] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016152006_SeedReferenceData'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SequenceNumber') AND [object_id] = OBJECT_ID(N'[ZodiacSigns]'))
        SET IDENTITY_INSERT [ZodiacSigns] ON;
    EXEC(N'INSERT INTO [ZodiacSigns] ([Id], [Name], [SequenceNumber])
    VALUES (1, N''Aries'', 1),
    (2, N''Taurus'', 2),
    (3, N''Gemini'', 3),
    (4, N''Cancer'', 4),
    (5, N''Leo'', 5),
    (6, N''Virgo'', 6),
    (7, N''Libra'', 7),
    (8, N''Scorpio'', 8),
    (9, N''Sagittarius'', 9),
    (10, N''Capricorn'', 10),
    (11, N''Aquarius'', 11),
    (12, N''Pisces'', 12)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SequenceNumber') AND [object_id] = OBJECT_ID(N'[ZodiacSigns]'))
        SET IDENTITY_INSERT [ZodiacSigns] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016152006_SeedReferenceData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016152006_SeedReferenceData', N'10.0.13');
END;

COMMIT;
GO

