using Orbit.Web.Authorization;
using Orbit.Web.Data;

namespace Orbit.UnitTests;

[TestClass]
public sealed class OrganizationPermissionsTests
{
    [TestMethod]
    [DataRow(OrganizationRole.Viewer)]
    [DataRow(OrganizationRole.Editor)]
    [DataRow(OrganizationRole.Admin)]
    public void CanRead_KnownRole_AllowsRead(OrganizationRole role) =>
        Assert.IsTrue(OrganizationPermissions.CanRead(role));

    [TestMethod]
    public void CanRead_UnknownRole_DeniesRead() =>
        Assert.IsFalse(OrganizationPermissions.CanRead((OrganizationRole)99));
}
