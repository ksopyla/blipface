IF exists (select * from sysobjects where id = OBJECT_ID('CountUse'))
		DROP TABLE dbo.CountUse
GO

CREATE TABLE CountUse(
	Id INTEGER PRIMARY KEY NOT NULL IDENTITY(1,1),
	UserGuid VARCHAR(255) NOT NULL,
	Version VARCHAR(15) NOT NULL,
	DateUse DATETIME NOT NULL
);