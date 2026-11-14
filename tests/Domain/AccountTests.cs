using Contas.Domain;
using Xunit;

public sealed class AccountTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("52998224724")]
    [InlineData("529@98224725")]
    public void OpeningRejectsInvalidCpf(string cpf)
    {
        var result = Account.Open("Ana Lima", cpf, Now);
        Assert.False(result.IsSuccess);
        Assert.Equal("account.invalid_cpf", result.Error?.Code);
    }

    [Fact]
    public void OpeningNormalizesInputAndRecordsCreation()
    {
        var account = Assert.IsType<Account>(Account.Open(" Ana Lima ", "529.982.247-25", Now).Value);
        Assert.Equal("Ana Lima", account.HolderName);
        Assert.Equal("52998224725", account.Cpf.Value);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal("AccountCreated", account.PendingEvent?.Type);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void NoOpDoesNotAdvanceVersionAndDeletionIsTerminal()
    {
        var account = Assert.IsType<Account>(Account.Open("Ana Lima", "52998224725", Now).Value);
        Assert.False(account.Revise("Ana Lima", AccountStatus.Active, Now).Value);
        Assert.Equal(1, account.Version);
        Assert.True(account.Revise("Ana Lima", AccountStatus.Inactive, Now).Value);
        Assert.Equal("AccountUpdated", account.PendingEvent?.Type);
        Assert.True(account.Delete(Now).Value);
        Assert.Equal(3, account.Version);
        Assert.Equal("AccountDeleted", account.PendingEvent?.Type);
        Assert.Equal("account.deleted", account.Revise("Ana Lima", AccountStatus.Active, Now).Error?.Code);
        Assert.Equal("account.deleted", account.Delete(Now).Error?.Code);
    }
}
