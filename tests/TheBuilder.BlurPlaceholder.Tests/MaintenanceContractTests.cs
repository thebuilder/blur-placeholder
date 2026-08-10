using System.Text.Json;
using TheBuilder.BlurPlaceholder.Media;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class MaintenanceContractTests
{
    [Fact]
    public void Matching_cursor_resumes_from_the_next_page()
    {
        var stored = JsonSerializer.Serialize(new BlurPlaceholderMaintenanceCursor("fingerprint", true, 7));

        var restored = BlurPlaceholderMaintenanceCursor.TryRestore(stored, "fingerprint", true, out var cursor);

        Assert.True(restored);
        Assert.Equal(7, cursor.NextPageIndex);
    }

    [Theory]
    [InlineData("not-json", "fingerprint", true)]
    [InlineData("{\"Fingerprint\":\"old\",\"ForceRegeneration\":true,\"NextPageIndex\":7}", "fingerprint", true)]
    [InlineData("{\"Fingerprint\":\"fingerprint\",\"ForceRegeneration\":false,\"NextPageIndex\":7}", "fingerprint", true)]
    [InlineData("{\"Fingerprint\":\"fingerprint\",\"ForceRegeneration\":true,\"NextPageIndex\":-1}", "fingerprint", true)]
    public void Invalid_or_obsolete_cursor_starts_a_new_pass(
        string stored,
        string fingerprint,
        bool forceRegeneration)
    {
        var restored = BlurPlaceholderMaintenanceCursor.TryRestore(
            stored,
            fingerprint,
            forceRegeneration,
            out var cursor);

        Assert.False(restored);
        Assert.Equal(BlurPlaceholderMaintenanceCursor.Start(fingerprint, forceRegeneration), cursor);
    }
}
