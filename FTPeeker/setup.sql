USE [YOUR_DATABASE_NAME]
GO


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FTPK_Auth_Types](
	[TypeCode] [varchar](50) NOT NULL,
	[TypeDesc] [varchar](255) NOT NULL,
 CONSTRAINT [PK_FTPK_Auth_Types] PRIMARY KEY CLUSTERED 
(
	[TypeCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO FTPK_Auth_Types VALUES('PWD', 'Password');
INSERT INTO FTPK_Auth_Types VALUES('SSH', 'SSH Key');


CREATE TABLE [dbo].[FTPK_FTPs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[DisplayName] [varchar](100) NOT NULL,
	[Host] [varchar](100) NOT NULL,
	[UserName] [varchar](50) NOT NULL,
	[Password] [varchar](50) NOT NULL,
	[Port] [int] NOT NULL,
	[AuthTypeCode] [varchar](50) NOT NULL,
	[SSHKeyPath] [varchar](500) NULL,
	[SSHKeyPassword] [varchar](500) NULL,
 CONSTRAINT [PK_FTPK_FTPs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FTPK_FTPs]  WITH CHECK ADD  CONSTRAINT [FK_FTPK_FTPs_FTPK_Auth_Types] FOREIGN KEY([AuthTypeCode])
REFERENCES [dbo].[FTPK_Auth_Types] ([TypeCode])
GO

ALTER TABLE [dbo].[FTPK_FTPs] CHECK CONSTRAINT [FK_FTPK_FTPs_FTPK_Auth_Types]
GO



CREATE TABLE [dbo].[FTPK_Logs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[FileName] [varchar](500) NOT NULL,
	[Path] [varchar](500) NOT NULL,
	[UserID] [varchar](500) NOT NULL,
	[LogDate] [datetime] NOT NULL,
	[SiteID] [int] NOT NULL,
 CONSTRAINT [PK_FTPK_Logs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FTPK_Logs]  WITH CHECK ADD  CONSTRAINT [FK_FTPK_Logs_FTPK_FTPs] FOREIGN KEY([SiteID])
REFERENCES [dbo].[FTPK_FTPs] ([ID])
GO

ALTER TABLE [dbo].[FTPK_Logs] CHECK CONSTRAINT [FK_FTPK_Logs_FTPK_FTPs]
GO

CREATE TABLE [dbo].[FTPK_Permissions](
	[PermissionCode] [varchar](50) NOT NULL,
	[PermissionDesc] [varchar](50) NOT NULL,
 CONSTRAINT [PK_FTPK_Permissions] PRIMARY KEY CLUSTERED 
(
	[PermissionCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[FTPK_User_Permissions](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [varchar](50) NOT NULL,
	[PermissionCode] [varchar](50) NOT NULL,
 CONSTRAINT [PK_FTPK_User_Permissions_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FTPK_User_Permissions]  WITH CHECK ADD  CONSTRAINT [FK_FTPK_User_Permissions_FTPK_Permissions] FOREIGN KEY([PermissionCode])
REFERENCES [dbo].[FTPK_Permissions] ([PermissionCode])
GO

ALTER TABLE [dbo].[FTPK_User_Permissions] CHECK CONSTRAINT [FK_FTPK_User_Permissions_FTPK_Permissions]
GO

INSERT INTO [dbo].[FTPK_Permissions] ([PermissionCode],[PermissionDesc]) VALUES('ADMIN','Admin');
INSERT INTO [dbo].[FTPK_Permissions] ([PermissionCode],[PermissionDesc]) VALUES('DELETE','Delete');
INSERT INTO [dbo].[FTPK_Permissions] ([PermissionCode],[PermissionDesc]) VALUES('EDIT','Edit (Rename)');
INSERT INTO [dbo].[FTPK_Permissions] ([PermissionCode],[PermissionDesc]) VALUES('READ','Browse and Download');
INSERT INTO [dbo].[FTPK_Permissions] ([PermissionCode],[PermissionDesc]) VALUES('UPLOAD','Upload');

CREATE TABLE [dbo].[FTPK_User_FTP_Permissions](
	[PermissionID] [int] NOT NULL,
	[FTPID] [int] NOT NULL,
 CONSTRAINT [PK_FTPK_User_FTP_Permissions] PRIMARY KEY CLUSTERED 
(
	[PermissionID] ASC,
	[FTPID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FTPK_User_FTP_Permissions]  WITH CHECK ADD  CONSTRAINT [FK_FTPK_User_FTP_Permissions_FTPK_FTPs] FOREIGN KEY([FTPID])
REFERENCES [dbo].[FTPK_FTPs] ([ID])
GO

ALTER TABLE [dbo].[FTPK_User_FTP_Permissions] CHECK CONSTRAINT [FK_FTPK_User_FTP_Permissions_FTPK_FTPs]
GO

ALTER TABLE [dbo].[FTPK_User_FTP_Permissions]  WITH CHECK ADD  CONSTRAINT [FK_FTPK_User_FTP_Permissions_FTPK_User_Permissions] FOREIGN KEY([PermissionID])
REFERENCES [dbo].[FTPK_User_Permissions] ([ID])
GO

ALTER TABLE [dbo].[FTPK_User_FTP_Permissions] CHECK CONSTRAINT [FK_FTPK_User_FTP_Permissions_FTPK_User_Permissions]
GO





