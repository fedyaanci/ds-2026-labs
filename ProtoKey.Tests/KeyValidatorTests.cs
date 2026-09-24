using ProtoKey.Storage;

namespace ProtoKey.Tests;

public sealed class KeyValidatorTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("requests_total")]
    [InlineData("A-Z.09")]
    public void ValidKeysAreAccepted(string key)
    {
        Assert.True(KeyValidator.IsValidKey(key));
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains space")]
    [InlineData("кириллица")]
    [InlineData("slash/not-allowed")]
    public void InvalidKeysAreRejected(string key)
    {
        Assert.False(KeyValidator.IsValidKey(key));
    }

    [Fact]
    public void KeyLengthBoundaryIsEnforced()
    {
        Assert.True(KeyValidator.IsValidKey(new string('a', 1000)));
        Assert.False(KeyValidator.IsValidKey(new string('a', 1001)));
    }

    [Fact]
    public void EmptyPrefixIsAllowed()
    {
        Assert.True(KeyValidator.IsValidPrefix(string.Empty));
    }
}
