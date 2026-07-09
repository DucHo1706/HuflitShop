IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = '2' OR Name = 'Employee')
BEGIN
    INSERT INTO Roles (Id, Name, Description) VALUES ('2', 'Employee', N'Nhân viên');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'nhanvien@gmail.com')
BEGIN
    DECLARE @UserId NVARCHAR(50) = CAST(NEWID() AS NVARCHAR(50));
    INSERT INTO Users (Id, FullName, Email, UserName, PasswordHash, Role, Avatar, AvatarPublicId, AvatarVersion, IsActive, JoinedDate, HourlyRate, PhoneNumber) 
    VALUES (@UserId, N'Nhân Viên Test', 'nhanvien@gmail.com', 'nhanvien@gmail.com', '123456', 'Employee', '', '', '', 1, GETDATE(), 0, '0123456789');

    DECLARE @RoleId NVARCHAR(50) = (SELECT TOP 1 Id FROM Roles WHERE Name = 'Employee' OR Id = '2');
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
END
