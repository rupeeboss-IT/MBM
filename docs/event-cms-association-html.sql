/* Add optional rich HTML for "In Association With" (run on MBM database) */
IF COL_LENGTH('dbo.Events', 'AssociationHtml') IS NULL
BEGIN
    ALTER TABLE dbo.Events
    ADD AssociationHtml NVARCHAR(MAX) NOT NULL
        CONSTRAINT DF_Events_AssociationHtml DEFAULT (N'');
END
GO
