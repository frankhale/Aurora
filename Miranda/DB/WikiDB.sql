USE [master]
GO
/****** Object:  Database [wiki]    Script Date: 02/17/2013 20:26:10 ******/
CREATE DATABASE [wiki] 
GO
ALTER DATABASE [wiki] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [wiki].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [wiki] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [wiki] SET ANSI_NULLS OFF
GO
ALTER DATABASE [wiki] SET ANSI_PADDING OFF
GO
ALTER DATABASE [wiki] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [wiki] SET ARITHABORT OFF
GO
ALTER DATABASE [wiki] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [wiki] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [wiki] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [wiki] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [wiki] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [wiki] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [wiki] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [wiki] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [wiki] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [wiki] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [wiki] SET  DISABLE_BROKER
GO
ALTER DATABASE [wiki] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [wiki] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [wiki] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [wiki] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [wiki] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [wiki] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [wiki] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [wiki] SET  READ_WRITE
GO
ALTER DATABASE [wiki] SET RECOVERY SIMPLE
GO
ALTER DATABASE [wiki] SET  MULTI_USER
GO
ALTER DATABASE [wiki] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [wiki] SET DB_CHAINING OFF
GO
USE [wiki]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 02/17/2013 20:26:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Users](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [varchar](50) NOT NULL,
	[FirstName] [varchar](25) NOT NULL,
	[LastName] [varchar](25) NOT NULL,
	[Identifier] [varchar](100) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Tags]    Script Date: 02/17/2013 20:26:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Tags](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[Name] [varchar](255) NOT NULL,
 CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Pages]    Script Date: 02/17/2013 20:26:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Pages](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[ModifiedOn] [datetime] NOT NULL,
	[Alias] [varchar](255) NOT NULL,
	[AuthorID] [int] NOT NULL,
	[Title] [varchar](255) NOT NULL,
	[Body] [varchar](max) NOT NULL,
	[Published] [bit] NOT NULL,
 CONSTRAINT [PK_Pages] PRIMARY KEY NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY],
 CONSTRAINT [IX_Title] UNIQUE NONCLUSTERED 
(
	[Title] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[PageTags]    Script Date: 02/17/2013 20:26:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PageTags](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[PageID] [int] NOT NULL,
	[TagID] [int] NOT NULL,
 CONSTRAINT [PK_PageTags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[v_RecentlyUpdatedPagesToday]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_RecentlyUpdatedPagesToday]
AS

select ID, Title, AuthorID, ModifiedOn from Pages
where DATEDIFF(day, ModifiedOn, CURRENT_TIMESTAMP) >= 0 AND
      DATEDIFF(day, ModifiedOn, CURRENT_TIMESTAMP) <= 0
GO
/****** Object:  Table [dbo].[Comments]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Comments](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[PageID] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[ModifiedOn] [datetime] NOT NULL,
	[AuthorID] [int] NOT NULL,
	[Body] [varchar](1000) NOT NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  View [dbo].[v_Pages]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_Pages]
AS


Select
	u.ID,
	u.UserName,
	u.FirstName,
	u.LastName,
	p.CreatedOn,
	p.ModifiedOn,
	p.Published,
	p.Title,
	p.Body,
	p.Alias
From
	Users as u
inner join Pages as p on u.ID = p.AuthorID
GO
/****** Object:  View [dbo].[v_AllPagesByTag]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_AllPagesByTag]
AS
SELECT     dbo.Tags.Name AS Tag, dbo.Pages.Title, dbo.Pages.Body, dbo.Pages.AuthorID, dbo.Pages.Alias, dbo.Pages.ModifiedOn, dbo.Pages.CreatedOn, 
                      dbo.Pages.ID, dbo.Pages.Published
FROM         dbo.Pages left outer JOIN
                      dbo.PageTags ON dbo.Pages.ID = dbo.PageTags.PageID 
                      left outer JOIN
                      dbo.Tags ON dbo.PageTags.TagID = dbo.Tags.ID
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[41] 4[20] 2[9] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Pages"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 220
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "PageTags"
            Begin Extent = 
               Top = 6
               Left = 236
               Bottom = 207
               Right = 396
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Tags"
            Begin Extent = 
               Top = 6
               Left = 434
               Bottom = 179
               Right = 594
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1785
         Width = 2280
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_AllPagesByTag'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_AllPagesByTag'
GO
/****** Object:  View [dbo].[v_Comments]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_Comments]
AS


Select
	u.ID,
	u.UserName,
	c.CreatedOn,
	c.ModifiedOn,
	c.PageID,
	c.Body
From
	Users as u
inner join Comments as c on u.ID = c.AuthorID
GO
/****** Object:  View [dbo].[v_PageTags]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_PageTags]
AS
SELECT     
		dbo.Tags.Name, 
		dbo.Pages.CreatedOn, 
		dbo.Pages.ModifiedOn, 
		dbo.Pages.Alias, 
		dbo.Pages.AuthorID,
		dbo.Pages.Title, 
        dbo.Pages.Body, 
        dbo.Pages.Published
FROM    dbo.Pages 
INNER JOIN dbo.PageTags ON 
        dbo.Pages.ID = dbo.PageTags.PageID 
INNER JOIN
        dbo.Tags ON dbo.PageTags.TagID = dbo.Tags.ID
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[31] 4[30] 2[21] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1[50] 4[25] 3) )"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 1
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Pages"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 207
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "PageTags"
            Begin Extent = 
               Top = 35
               Left = 260
               Bottom = 206
               Right = 420
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Tags"
            Begin Extent = 
               Top = 45
               Left = 485
               Bottom = 208
               Right = 645
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
      PaneHidden = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_PageTags'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_PageTags'
GO
/****** Object:  UserDefinedFunction [dbo].[GetIDListForTagID]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetIDListForTagID]
(
	@tag nvarchar(max)
)
RETURNS nvarchar(max)
AS
BEGIN
	DECLARE @result nvarchar(max)

	SELECT 
		@result = COALESCE(@result +  ',', '') + CONVERT(nvarchar(max), ID) 
	FROM 
		v_AllPagesByTag
	WHERE tag = @tag or
		 (@tag is null and Tag is null)

	RETURN @result
END
GO
/****** Object:  View [dbo].[v_TagPageCount]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_TagPageCount]
AS
SELECT     Tag, COUNT(Tag) AS Pages
FROM         dbo.v_AllPagesByTag
GROUP BY Tag
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[41] 4[20] 2[8] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "v_AllPagesByTag"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 125
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 2355
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 12
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_TagPageCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'v_TagPageCount'
GO
/****** Object:  View [dbo].[v_AllTagsWithPageList]    Script Date: 02/17/2013 20:26:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_AllTagsWithPageList]
AS

select distinct
	Tag,
	dbo.GetIDListForTagID(Tag) as IDList
From
	v_AllPagesByTag
GO
/****** Object:  ForeignKey [FK_Pages_User]    Script Date: 02/17/2013 20:26:10 ******/
ALTER TABLE [dbo].[Pages]  WITH CHECK ADD  CONSTRAINT [FK_Pages_User] FOREIGN KEY([AuthorID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Pages] CHECK CONSTRAINT [FK_Pages_User]
GO
/****** Object:  ForeignKey [FK_PageTags_Page]    Script Date: 02/17/2013 20:26:10 ******/
ALTER TABLE [dbo].[PageTags]  WITH CHECK ADD  CONSTRAINT [FK_PageTags_Page] FOREIGN KEY([PageID])
REFERENCES [dbo].[Pages] ([ID])
GO
ALTER TABLE [dbo].[PageTags] CHECK CONSTRAINT [FK_PageTags_Page]
GO
/****** Object:  ForeignKey [FK_PageTags_Tag]    Script Date: 02/17/2013 20:26:10 ******/
ALTER TABLE [dbo].[PageTags]  WITH CHECK ADD  CONSTRAINT [FK_PageTags_Tag] FOREIGN KEY([TagID])
REFERENCES [dbo].[Tags] ([ID])
GO
ALTER TABLE [dbo].[PageTags] CHECK CONSTRAINT [FK_PageTags_Tag]
GO
/****** Object:  ForeignKey [FK_Comments_Page]    Script Date: 02/17/2013 20:26:11 ******/
ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Page] FOREIGN KEY([PageID])
REFERENCES [dbo].[Pages] ([ID])
GO
ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_Page]
GO
/****** Object:  ForeignKey [FK_Comments_User]    Script Date: 02/17/2013 20:26:11 ******/
ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_User] FOREIGN KEY([AuthorID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_User]
GO
