using Library.MVC.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Xunit;

namespace Library.Tests
{
    public class LibraryTests
    {
        [Fact]
        public void RolePageRequiresAdminPolicy()
        {
            // Test RolesController is decorated with Authorize(Roles="Admin")
            var type = typeof(RolesController);
            var attr = type.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("Admin", attr.Roles);
        }
    }
}