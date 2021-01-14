
CREATE SCHEMA IF NOT EXISTS "Vidyano";
GO

CREATE TABLE "Vidyano"."Users" ("Id" uuid  NOT NULL,
    "Name" varchar(255)  NOT NULL,
    "FriendlyName" text  NULL,
    "PasswordHash" text  NOT NULL,
    "Language" varchar(10)  NOT NULL,
    "CultureInfo" varchar(10) NOT NULL,
    "Version" varchar(100)  NOT NULL,
    "CreationDate" timestamp(3) WITH TIME ZONE  NOT NULL,
    "LastLoginDate" timestamp(3) WITH TIME ZONE  NULL
    ,"IsEnabled" boolean not null
    ,"TwoFactorToken" varchar(100) null
    ,"ResetPasswordNextLogin"boolean not null
);
GO

CREATE TABLE "Vidyano"."UserSettings" (
    "Id" uuid  NOT NULL,
    "Settings" text  NOT NULL
);
GO

CREATE TABLE "Vidyano"."UserProfiles" (
    "Id" uuid  NOT NULL,
    "Profile" text  NOT NULL
);
GO

CREATE TABLE "Vidyano"."Settings" (
    "Key" varchar(255)  NOT NULL,
    "Value" text  NULL,
    "IsSystem" boolean  NOT NULL,
	"Description" varchar(4000) NULL,
	"DataType" varchar(255) NULL
);
GO

CREATE TABLE "Vidyano"."Feedbacks" (
    "Id" uuid  NOT NULL,
    "Type" varchar(100)  NOT NULL,
    "Comment" text  NOT NULL,
    "Screenshot" bytea  NULL,
    "User" varchar(255)  NOT NULL,
    "CreatedOn" timestamp(3) WITH TIME ZONE  NOT NULL
);
GO

CREATE TABLE "Vidyano"."CacheUpdates" (
    "Id" uuid  NOT NULL,
    "Timestamp" timestamp(6) with time zone  NOT NULL,
    "Data" bytea  NOT NULL,
	"Change" smallint NULL,
    "Value" text NULL
);
GO

CREATE TABLE "Vidyano"."RegisteredStreams" (
    "Id" uuid  NOT NULL,
    "Key" text  NOT NULL,
    "PersistentObjectId" uuid  NOT NULL,
    "ValidUntil" timestamp(6) with time zone  NOT NULL
);
GO

CREATE TABLE "Vidyano"."Groups" (
    "Id" uuid  NOT NULL,
    "Name" varchar(255)  NOT NULL,
    "IsSystem" boolean  NOT NULL,
    "CreationDate" timestamp(3) WITH TIME ZONE  NOT NULL,
	"TwoFactorRequired" boolean  NOT NULL
);
GO

CREATE TABLE "Vidyano"."UserGroup" (
    "Users_Id" uuid  NOT NULL,
    "Groups_Id" uuid  NOT NULL
);
GO

create sequence "Vidyano"."Logs_seq";
GO

create table "Vidyano"."Logs" (
    "Id" bigint default nextval ('"Vidyano"."Logs_seq"')  NOT NULL,
    "ExternalId" uuid  NOT NULL,
    "UserId" uuid  NULL,
    "CreatedOn" timestamp(3) WITH TIME ZONE  NOT NULL,
    "Type" smallint  NOT NULL,
    "Message" text  NOT NULL
);
GO

alter table "Vidyano"."CacheUpdates" alter column "Timestamp" set default CURRENT_TIMESTAMP;
GO

ALTER TABLE "Vidyano"."Users"
ADD CONSTRAINT "PK_Users"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."UserSettings"
ADD CONSTRAINT "PK_UserSettings"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."UserProfiles"
ADD CONSTRAINT "PK_UserProfiles"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."Settings"
ADD CONSTRAINT "PK_Settings"
    PRIMARY KEY ("Key");
GO

ALTER TABLE "Vidyano"."Feedbacks"
ADD CONSTRAINT "PK_Feedbacks"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."CacheUpdates"
ADD CONSTRAINT "PK_CacheUpdates"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."RegisteredStreams"
ADD CONSTRAINT "PK_RegisteredStreams"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."Groups"
ADD CONSTRAINT "PK_Groups"
    PRIMARY KEY ("Id");
GO

ALTER TABLE "Vidyano"."UserGroup"
ADD CONSTRAINT "PK_UserGroup"
    PRIMARY KEY ("Users_Id", "Groups_Id");
GO

alter table "Vidyano"."Logs"
add constraint "PK_Vidyano_Logs"
	primary key ("Id");
GO

ALTER TABLE "Vidyano"."UserGroup"
ADD CONSTRAINT "FK_UserGroup_Group"
    FOREIGN KEY ("Groups_Id")
    REFERENCES "Vidyano"."Groups" ("Id")
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

CREATE INDEX "IX_FK_UserGroup_Group"
ON "Vidyano"."UserGroup"
    ("Groups_Id");
GO


alter table "Vidyano"."Logs"
add constraint "FK_Logs_Users"
    foreign key ("UserId")
    references "Vidyano"."Users" ("Id")
    on delete cascade on update no action;
GO

alter table "Vidyano"."UserSettings"
add constraint "FK_UserSettings_Users"
    foreign key ("Id")
    references "Vidyano"."Users" ("Id")
    on delete cascade on update no action;
GO

alter table "Vidyano"."UserProfiles"
add constraint "FK_UserProfiles_Users"
    foreign key ("Id")
    references "Vidyano"."Users" ("Id")
    on delete cascade on update no action;
GO

create table "Vidyano"."UserNotifications"(
	"Id" uuid NOT NULL,
	"UserId" uuid NOT NULL,
	"CreatedOn" Timestamp(3) WITH TIME ZONE NOT NULL,
	"CreatedBy" uuid NULL,
	"Message" Text NOT NULL,
	"VisibleOn" Timestamp(3) WITH TIME ZONE NOT NULL,
	"Read" BOOLEAN NOT NULL,
	"Archived" BOOLEAN NOT NULL,
	"PropagationChannels" INT NOT NULL,
    "IsPropagated" BOOLEAN NOT NULL,
	"Action" Varchar(2000) NULL,
	"IconUrl" Text NULL
);
GO

alter table "Vidyano"."UserNotifications" add constraint "PK_Vidyano_UserNotifications" PRIMARY KEY ("Id");;
GO
alter table "Vidyano"."UserNotifications" add constraint "FK_UserNotifications_User_UserId" FOREIGN KEY("UserId") REFERENCES "Vidyano"."Users" ("Id") ON DELETE cascade;
GO
alter table "Vidyano"."UserNotifications" add constraint "FK_UserNotifications_Users_CreatedBy" FOREIGN KEY("CreatedBy") 	REFERENCES "Vidyano"."Users" ("Id");
GO

alter table "Vidyano"."Users" add constraint "UQ_Users_Name" unique ("Name");
GO

alter table "Vidyano"."Groups" add constraint "UQ_Groups_Name" unique("Name");
GO