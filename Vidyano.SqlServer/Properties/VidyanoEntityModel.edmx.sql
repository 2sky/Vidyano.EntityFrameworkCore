SET QUOTED_IDENTIFIER OFF;
GO
IF SCHEMA_ID(N'Vidyano') IS NULL EXECUTE(N'CREATE SCHEMA [Vidyano]');
GO

-- Creating table 'Users'
CREATE TABLE [Vidyano].[Users] (
    [Id] uniqueidentifier  NOT NULL,
    [Name] nvarchar(255)  NOT NULL,
    [FriendlyName] nvarchar(max)  NULL,
    [PasswordHash] varchar(max)  NOT NULL,
    [Language] varchar(10)  NOT NULL,
    [CultureInfo] varchar(10) NOT NULL,
    [Version] varchar(100)  NOT NULL,
    [CreationDate] datetimeoffset(3)  NOT NULL,
    [LastLoginDate] datetimeoffset(3)  NULL
    ,[IsEnabled] bit not null
    ,[TwoFactorToken] varchar(100) null
    ,[ResetPasswordNextLogin] bit not null
);
GO

-- Creating table 'UserSettings'
CREATE TABLE [Vidyano].[UserSettings] (
    [Id] uniqueidentifier  NOT NULL,
    [Settings] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'UserProfiles'
CREATE TABLE [Vidyano].[UserProfiles] (
    [Id] uniqueidentifier  NOT NULL,
    [Profile] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'Settings'
CREATE TABLE [Vidyano].[Settings] (
    [Key] nvarchar(255)  NOT NULL,
    [Value] nvarchar(max)  NULL,
    [IsSystem] bit  NOT NULL,
	[Description] nvarchar(4000) NULL,
	[DataType] nvarchar(255) NULL
);
GO

-- Creating table 'Feedbacks'
CREATE TABLE [Vidyano].[Feedbacks] (
    [Id] uniqueidentifier  NOT NULL,
    [Type] nvarchar(100)  NOT NULL,
    [Comment] nvarchar(max)  NOT NULL,
    [Screenshot] varbinary(max)  NULL,
    [User] nvarchar(255)  NOT NULL,
    [CreatedOn] datetimeoffset(3)  NOT NULL
);
GO

-- Creating table 'CacheUpdates'
CREATE TABLE [Vidyano].[CacheUpdates] (
    [Id] uniqueidentifier  NOT NULL,
    [Timestamp] datetimeoffset  NOT NULL,
    [Data] varbinary(max)  NOT NULL,
	[Change] tinyint NULL,
    [Value] nvarchar(max) NULL
);
GO

-- Creating table 'RegisteredStreams'
CREATE TABLE [Vidyano].[RegisteredStreams] (
    [Id] uniqueidentifier  NOT NULL,
    [Key] nvarchar(max)  NOT NULL,
    [PersistentObjectId] uniqueidentifier  NOT NULL,
    [ValidUntil] datetimeoffset  NOT NULL
);
GO

-- Creating table 'Groups'
CREATE TABLE [Vidyano].[Groups] (
    [Id] uniqueidentifier  NOT NULL,
    [Name] nvarchar(255)  NOT NULL,
    [IsSystem] bit  NOT NULL,
    [CreationDate] datetimeoffset(3)  NOT NULL,
	[TwoFactorRequired] bit  NOT NULL
);
GO

-- Creating table 'UserGroup'
CREATE TABLE [Vidyano].[UserGroup] (
    [Users_Id] uniqueidentifier  NOT NULL,
    [Groups_Id] uniqueidentifier  NOT NULL
);
GO

-- Creating table 'Logs'
create table [Vidyano].[Logs] (
    [Id] bigint identity  NOT NULL,
    [ExternalId] uniqueidentifier  NOT NULL,
    [UserId] uniqueidentifier  NULL,
    [CreatedOn] datetimeoffset(3)  NOT NULL,
    [Type] tinyint  NOT NULL,
    [Message] nvarchar(max)  NOT NULL
);
GO

ALTER TABLE [Vidyano].[CacheUpdates] ADD CONSTRAINT CacheUpdates_DefaultTimestamp default (SYSDATETIMEOFFSET()) for [Timestamp];
GO

-- Creating primary key on [Id] in table 'Users'
ALTER TABLE [Vidyano].[Users]
ADD CONSTRAINT [PK_Users]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'UserSettings'
ALTER TABLE [Vidyano].[UserSettings]
ADD CONSTRAINT [PK_UserSettings]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'UserProfiles'
ALTER TABLE [Vidyano].[UserProfiles]
ADD CONSTRAINT [PK_UserProfiles]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Settings'
ALTER TABLE [Vidyano].[Settings]
ADD CONSTRAINT [PK_Settings]
    PRIMARY KEY CLUSTERED ([Key] ASC);
GO

-- Creating primary key on [Id] in table 'Feedbacks'
ALTER TABLE [Vidyano].[Feedbacks]
ADD CONSTRAINT [PK_Feedbacks]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'CacheUpdates'
ALTER TABLE [Vidyano].[CacheUpdates]
ADD CONSTRAINT [PK_CacheUpdates]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RegisteredStreams'
ALTER TABLE [Vidyano].[RegisteredStreams]
ADD CONSTRAINT [PK_RegisteredStreams]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Groups'
ALTER TABLE [Vidyano].[Groups]
ADD CONSTRAINT [PK_Groups]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Users_Id], [Groups_Id] in table 'UserGroup'
ALTER TABLE [Vidyano].[UserGroup]
ADD CONSTRAINT [PK_UserGroup]
    PRIMARY KEY CLUSTERED ([Users_Id], [Groups_Id] ASC);
GO

-- Creating primary key on [Id] in table 'Logs'
alter table [Vidyano].[Logs]
add constraint [PK_Vidyano_Logs]
	primary key clustered ([Id] ASC);
GO

-- Creating foreign key on [Groups_Id] in table 'UserGroup'
ALTER TABLE [Vidyano].[UserGroup]
ADD CONSTRAINT [FK_UserGroup_Group]
    FOREIGN KEY ([Groups_Id])
    REFERENCES [Vidyano].[Groups] ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UserGroup_Group'
CREATE INDEX [IX_FK_UserGroup_Group]
ON [Vidyano].[UserGroup]
    ([Groups_Id]);
GO

alter table [Vidyano].[Logs]
add constraint [FK_Logs_Users]
    foreign key ([UserId])
    references [Vidyano].[Users] ([Id])
    on delete cascade on update no action;
GO

alter table [Vidyano].[UserSettings]
add constraint [FK_UserSettings_Users]
    foreign key ([Id])
    references [Vidyano].[Users] ([Id])
    on delete cascade on update no action;
GO

alter table [Vidyano].[UserProfiles]
add constraint [FK_UserProfiles_Users]
    foreign key ([Id])
    references [Vidyano].[Users] ([Id])
    on delete cascade on update no action;
GO

-- --------------------------------------------------
-- Creating UserNotifications
-- --------------------------------------------------

CREATE TABLE [Vidyano].[UserNotifications](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[CreatedOn] [datetimeoffset](3) NOT NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[Message] [nvarchar](MAX) NOT NULL,
	[VisibleOn] [datetimeoffset](3) NOT NULL,
	[Read] BIT NOT NULL,
	[Archived] BIT NOT NULL,
	[PropagationChannels] INT NOT NULL,
    [IsPropagated] BIT NOT NULL,
	[Action] [nvarchar](2000) NULL,
	[IconUrl] [nvarchar](MAX) NULL
	CONSTRAINT [PK_Vidyano_UserNotifications] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_UserNotifications_User_UserId] FOREIGN KEY([UserId]) REFERENCES [Vidyano].[Users] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_UserNotifications_Users_CreatedBy] FOREIGN KEY([CreatedBy]) 	REFERENCES [Vidyano].[Users] ([Id])
)
GO

alter table [Vidyano].[Users] add constraint [UQ_Users_Name] unique ([Name])
GO

alter table[Vidyano].[Groups] add constraint[UQ_Groups_Name] unique([Name])
GO