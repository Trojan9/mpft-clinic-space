using ClinicSpace.Domain.Notifications;
using ClinicSpace.UnitTests.Support;

namespace ClinicSpace.UnitTests.Domain;

public class EmailMessageTests
{
    [Fact]
    public void Failures_back_off_exponentially()
    {
        var message = new EmailMessage { NextAttemptAtUtc = TestData.Now };

        message.MarkFailed("smtp down", TestData.Now, maxAttempts: 8);
        message.NextAttemptAtUtc.Should().Be(TestData.Now.AddSeconds(30));

        message.MarkFailed("smtp down", TestData.Now, maxAttempts: 8);
        message.NextAttemptAtUtc.Should().Be(TestData.Now.AddSeconds(60));

        message.MarkFailed("smtp down", TestData.Now, maxAttempts: 8);
        message.NextAttemptAtUtc.Should().Be(TestData.Now.AddSeconds(120));
        message.IsDeadLettered.Should().BeFalse();
    }

    [Fact]
    public void Message_is_dead_lettered_when_attempts_run_out()
    {
        var message = new EmailMessage();

        message.MarkFailed("boom", TestData.Now, maxAttempts: 2);
        message.MarkFailed("boom", TestData.Now, maxAttempts: 2);

        message.IsDeadLettered.Should().BeTrue();
        message.Attempts.Should().Be(2);
    }

    [Fact]
    public void Long_errors_are_truncated_to_fit_the_column()
    {
        var message = new EmailMessage();

        message.MarkFailed(new string('x', 5000), TestData.Now, maxAttempts: 8);

        message.LastError!.Length.Should().Be(EmailMessage.LastErrorMaxLength);
    }

    [Fact]
    public void MarkSent_records_the_time_and_clears_the_error()
    {
        var message = new EmailMessage();
        message.MarkFailed("first try failed", TestData.Now, 8);

        message.MarkSent(TestData.Now.AddMinutes(1));

        message.SentAtUtc.Should().Be(TestData.Now.AddMinutes(1));
        message.LastError.Should().BeNull();
    }
}
