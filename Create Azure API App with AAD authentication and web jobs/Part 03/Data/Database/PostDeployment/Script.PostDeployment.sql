/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
IF NOT EXISTS(SELECT * FROM [Goods].[Categories])
BEGIN
	SET IDENTITY_INSERT [Goods].[Categories] ON
	INSERT INTO [Goods].[Categories] ([Id], [Name]) VALUES (1, N'Cars')
	INSERT INTO [Goods].[Categories] ([Id], [Name]) VALUES (2, N'Hardware')
	INSERT INTO [Goods].[Categories] ([Id], [Name]) VALUES (3, N'Furniture')
	INSERT INTO [Goods].[Categories] ([Id], [Name]) VALUES (4, N'Appliance')
	SET IDENTITY_INSERT [Goods].[Categories] OFF
END
