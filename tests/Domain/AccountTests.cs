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
        Assert.NotEqual(Guid.Empty, account.Id.Value);
        Assert.Equal("Ana Lima", account.HolderName.Value);
        Assert.Equal("52998224725", account.Cpf.Value);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(AccountEventKind.AccountCreated, account.PendingEvent?.Type);
        Assert.Equal(1, account.Version.Value);
    }

    [Fact]
    public void NoOpDoesNotAdvanceVersionAndDeletionIsTerminal()
    {
        var account = Assert.IsType<Account>(Account.Open("Ana Lima", "52998224725", Now).Value);
        Assert.False(account.Revise("Ana Lima", AccountStatus.Active, AccountVersion.Initial, Now).Value);
        Assert.Equal(1, account.Version.Value);
        Assert.True(account.Revise("Ana Lima", AccountStatus.Inactive, AccountVersion.Initial, Now).Value);
        Assert.Equal(AccountEventKind.AccountUpdated, account.PendingEvent?.Type);
        Assert.True(account.Delete(account.Version, Now).Value);
        Assert.Equal(3, account.Version.Value);
        Assert.Equal(AccountEventKind.AccountDeleted, account.PendingEvent?.Type);
        Assert.Equal("account.deleted", account.Revise("Ana Lima", AccountStatus.Active, account.Version, Now).Error?.Code);
        Assert.Equal("account.deleted", account.Delete(account.Version, Now).Error?.Code);
    }
    [Fact]
    public void StaleWritesCannotChangeAccountOrCreateAnotherEvent()
    {
        var account = Account.Open("Ana Lima", "52998224725", Now).Value!;
        var originalVersion = account.Version;
        Assert.True(account.Revise("Ana Silva", AccountStatus.Inactive, originalVersion, Now).IsSuccess);
        var acceptedEvent = account.PendingEvent;

        var duplicate = account.Revise("Ana Silva", AccountStatus.Inactive, originalVersion, Now);
        var staleDelete = account.Delete(originalVersion, Now);

        Assert.Equal("account.stale_version", duplicate.Error?.Code);
        Assert.Equal("account.stale_version", staleDelete.Error?.Code);
        Assert.Same(acceptedEvent, account.PendingEvent);
        Assert.Equal(originalVersion.Value + 1, account.Version.Value);
        Assert.Equal("Ana Silva", account.HolderName.Value);
        Assert.False(account.Deleted);
    }

    [Fact]
    public void PortfolioAndOwnerChangesBelongToAccountAndSkipRepeats()
    {
        var manager = AccountSubject.Create(Guid.NewGuid()).Value!;
        var nextManager = AccountSubject.Create(Guid.NewGuid()).Value!;
        var owner = AccountSubject.Create(Guid.NewGuid()).Value!;
        var account = Account.Open("Ana Lima", "52998224725", Now, manager).Value!;

        Assert.Equal(manager, account.Manager);
        Assert.False(account.TransferPortfolio(manager).Value);
        Assert.True(account.TransferPortfolio(nextManager).Value);
        Assert.True(account.LinkOwner(owner).Value);
        Assert.False(account.LinkOwner(owner).Value);
        Assert.Equal(nextManager, account.Manager);
        Assert.Equal(owner, account.Owner);

        Assert.True(account.Delete(account.Version, Now).Value);
        Assert.Equal("account.deleted", account.TransferPortfolio(manager).Error?.Code);
        Assert.Equal("account.deleted", account.LinkOwner(manager).Error?.Code);
        Assert.Equal(nextManager, account.Manager);
        Assert.Equal(owner, account.Owner);
    }

    [Fact]
    public void DomainValuesRejectInvalidInput()
    {
        Assert.Equal("account.invalid_version", AccountVersion.Create(0).Error?.Code);
        Assert.Equal("account.invalid_version", AccountVersion.Create(-1).Error?.Code);
        Assert.Equal(AccountVersion.Initial, AccountVersion.Create(1).Value);
        Assert.Equal("account.invalid_subject", AccountSubject.Create(Guid.Empty).Error?.Code);
        Assert.Equal("account.invalid_name", AccountHolderName.Create(" ").Error?.Code);
        Assert.Equal(AccountHolderName.Create("Ana Lima").Value, AccountHolderName.Create(" Ana Lima ").Value);
    }

    [Fact]
    public void AccountCannotAdvanceBeyondTheVersionLimit()
    {
        var opened = Account.Open("Ana Lima", "52998224725", Now).Value!;
        var limit = AccountVersion.Create(long.MaxValue).Value!;
        var account = Account.Restore(opened.Id, opened.HolderName, opened.Cpf, opened.Status, limit,
            deleted: false, manager: null, owner: null);

        Assert.Equal("account.version_limit", account.Delete(limit, Now).Error?.Code);
        Assert.Equal("account.version_limit", account.Revise("Ana Silva", AccountStatus.Inactive, limit, Now).Error?.Code);
        Assert.Null(account.PendingEvent);
        Assert.False(account.Deleted);
        Assert.Equal(opened.HolderName, account.HolderName);
    }
}
