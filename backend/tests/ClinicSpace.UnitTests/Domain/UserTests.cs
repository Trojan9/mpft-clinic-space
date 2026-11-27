using ClinicSpace.Domain.Users;
using ClinicSpace.UnitTests.Support;

namespace ClinicSpace.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Account_locks_after_the_maximum_number_of_failures()
    {
        var user = TestData.Requester();
        var lockout = TimeSpan.FromMinutes(15);

        for (var i = 1; i < 5; i++)
        {
            user.RegisterFailedSignIn(TestData.Now, 5, lockout).Should().BeFalse();
        }

        user.RegisterFailedSignIn(TestData.Now, 5, lockout).Should().BeTrue();
        user.IsLockedOut(TestData.Now).Should().BeTrue();
        user.IsLockedOut(TestData.Now.AddMinutes(15).AddSeconds(1)).Should().BeFalse();
        user.AccessFailedCount.Should().Be(0, "the counter restarts after a lockout");
    }

    [Fact]
    public void Successful_sign_in_clears_failures_and_lockout()
    {
        var user = TestData.Requester();
        user.RegisterFailedSignIn(TestData.Now, 5, TimeSpan.FromMinutes(15));
        user.LockoutEndUtc = TestData.Now.AddMinutes(-1);

        user.RegisterSuccessfulSignIn(TestData.Now);

        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndUtc.Should().BeNull();
        user.LastSignInAtUtc.Should().Be(TestData.Now);
    }

    [Theory]
    [InlineData(" Sarah.Khan ", "SARAH.KHAN")]
    [InlineData("olivia", "OLIVIA")]
    public void Normalize_trims_and_upper_cases(string input, string expected) =>
        User.Normalize(input).Should().Be(expected);
}
