using HousePlanner.API.Controllers;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public sealed class AdminStaffControllerTests
{
    private static AdminStaffController Controller(string? role, Mock<IStaffAccountService>? staff = null)
    {
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(role is null
            ? null : new CurrentUserContext(Guid.NewGuid(), "user@example.com", role));
        return new AdminStaffController((staff ?? new Mock<IStaffAccountService>()).Object, current.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }

    [Fact]
    public void ControllerRequiresAdminAuthorization()
    {
        var attribute = Assert.Single(typeof(AdminStaffController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal("Admin", attribute.Roles);
    }

    [Theory]
    [InlineData(null, typeof(UnauthorizedResult))]
    [InlineData("Customer", typeof(ForbidResult))]
    [InlineData("Architect", typeof(ForbidResult))]
    [InlineData("Constructor", typeof(ForbidResult))]
    public async Task NonAdminCannotCreateStaff(string? role, Type expected)
    {
        var staff = new Mock<IStaffAccountService>();
        var result = await Controller(role, staff).Create(new CreateStaffRequestDto
        { FullName = "X", Email = "x@example.com", Password = "secret1", Role = "Architect" }, default);
        Assert.IsType(expected, result);
        staff.Verify(x => x.CreateAsync(It.IsAny<CreateStaffRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Architect")]
    [InlineData("Constructor")]
    public async Task AdminCanCreateAllowedStaff(string role)
    {
        var staff = new Mock<IStaffAccountService>();
        staff.Setup(x => x.CreateAsync(It.IsAny<CreateStaffRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StaffAccountDto(Guid.NewGuid(), "Staff", "staff@example.com", role, "Active"));
        var result = await Controller("Admin", staff).Create(new CreateStaffRequestDto
        { FullName = "Staff", Email = "staff@example.com", Password = "secret1", Role = role }, default);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, ((ObjectResult)result).StatusCode);
    }
}
