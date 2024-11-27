using Orleans.Streams;

namespace OrleansEventHubsSamples.ApiService.Grains;

public interface IAccountGrain : IGrainWithIntegerKey
{
    ValueTask<double> Deposit(double amount);

    ValueTask<double> Withdraw(double amount);

    ValueTask<double> GetBalance();
}

public sealed class AccountGrain : Grain<AccountState>, IAccountGrain
{
    private IAsyncStream<AccountState>? _stream;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Get the stream
        var streamId = StreamId.Create(Constants.StreamNamespace, this.GetPrimaryKeyLong());
        _stream = this.GetStreamProvider(Constants.StreamProvider)
            .GetStream<AccountState>(streamId);

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _stream = null;
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async ValueTask<double> Deposit(double amount)
    {
        State.Balance += amount;

        await WriteStateAsync();

        if (_stream is not null)
        {
            await _stream!.OnNextAsync(State);
        }
        
        return State.Balance;
    }

    public async ValueTask<double> Withdraw(double amount)
    {
        amount = Math.Min(amount, State.Balance);
        State.Balance -= amount;
        await WriteStateAsync();

        if (_stream is not null)
        {
            await _stream!.OnNextAsync(State);
        }

        return amount;
    }

    public ValueTask<double> GetBalance()
    {
        return ValueTask.FromResult(State.Balance);
    }
}

[GenerateSerializer]
public sealed class AccountState
{
    [Id(0)]
    public double Balance { get; set; }
}
