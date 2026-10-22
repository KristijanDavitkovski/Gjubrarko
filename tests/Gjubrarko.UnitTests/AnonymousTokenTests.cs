using Gjubrarko.Application.Common;

namespace Gjubrarko.UnitTests;

public class AnonymousTokenTests
{
    [Fact]
    public void Hash_matches_only_the_original_token()
    {
        var token = AnonymousToken.Create();
        var hash = AnonymousToken.Hash(token);

        Assert.True(AnonymousToken.Matches(token, hash));
        Assert.False(AnonymousToken.Matches(AnonymousToken.Create(), hash));
        Assert.False(AnonymousToken.Matches(token, null));
    }

    [Fact]
    public void Tokens_are_url_safe_and_unique()
    {
        var a = AnonymousToken.Create();
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.NotEqual(a, AnonymousToken.Create());
    }
}
