using Stateless;
using Stateless.Graph;

Console.WriteLine("Creating member from object");
var aMember = TicketSaga.From(TicketSaga.MembershipState.Actived, "Bejo");

Console.WriteLine($"Member {aMember.Name} created, membership state is {aMember.State}");

var triggers = aMember.GetPermittedTriggers();
Console.WriteLine($"Permitted triggers are: {string.Join(", ", triggers)}");

if (triggers.Contains(TicketSaga.MemberTriggers.Suspend))
{
    aMember.Suspend();
    Console.WriteLine($"state is {aMember.State}");
}

if (triggers.Contains(TicketSaga.MemberTriggers.Reactivate))
{
    await aMember.Reactivate(new Activate("asdasd", 500));
    Console.WriteLine($"state is {aMember.State}");
}

if (triggers.Contains(TicketSaga.MemberTriggers.Terminate))
{
    aMember.Terminate();
    Console.WriteLine($"state is {aMember.State}");
}

var anotherMember = TicketSaga.From(TicketSaga.MembershipState.Inactived, "Bejo");

if (aMember.Equals(anotherMember))
{
    Console.WriteLine("Members are equal");
}

Console.WriteLine("-----graph------");
var x = aMember.Graph();
Console.WriteLine(x);
Console.WriteLine("-----graph------");

Console.WriteLine("Press any key...");
Console.ReadKey();


record Activate(string Reason, long Delay);
class TicketSaga
{
    public enum MemberTriggers
    {
        Suspend,
        Terminate,
        Reactivate
    }

    public enum MembershipState
    {
        Inactived,
        Actived,
        Terminated
    }

    public string Name { get; }

    public MembershipState State => _machine.State;

    private readonly StateMachine<MembershipState, MemberTriggers> _machine;

    // [JsonConstructor]
    public TicketSaga(MembershipState state, string name)
    {
        _machine = new StateMachine<MembershipState, MemberTriggers>(state);
        Name = name;

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        var trigger = _machine.SetTriggerParameters<Activate>(MemberTriggers.Reactivate);

        _machine.Configure(MembershipState.Actived)
            .Permit(MemberTriggers.Suspend, MembershipState.Inactived)
            // .PermitDynamic(MemberTriggers.Suspend, () => { 
            //     Console.WriteLine("[SM] Suspending");
            //     return MembershipState.Inactived;
            // })
            .Permit(MemberTriggers.Terminate, MembershipState.Terminated)
            // .PermitDynamic(MemberTriggers.Terminate, () => { 
            //     Console.WriteLine("[SM] Terminating");
            //     return MembershipState.Terminated;
            // })
            .OnEntryFromAsync(trigger, async (d) => await Task.Run(() => Console.WriteLine($"[SM] Activating Reason: {d.Reason} Delay: {d.Delay}")));

        _machine.Configure(MembershipState.Inactived)
            .Permit(MemberTriggers.Reactivate, MembershipState.Actived)
            // .PermitDynamic(MemberTriggers.Reactivate, () => { 
            //     // Console.WriteLine("[SM] Activating");
            //     return MembershipState.Actived;
            // })
            .Permit(MemberTriggers.Terminate, MembershipState.Terminated);
        // .PermitDynamic(MemberTriggers.Terminate, () => { 
        //     Console.WriteLine("[SM] Terminating");
        //     return MembershipState.Terminated;
        // });

        _machine.Configure(MembershipState.Terminated)
            .Permit(MemberTriggers.Reactivate, MembershipState.Actived);
        // .PermitDynamic(MemberTriggers.Reactivate, () => { 
        //     Console.WriteLine("[SM] Suspending");
        //     return MembershipState.Actived;
        // });
    }

    public void Terminate()
    {
        _machine.Fire(MemberTriggers.Terminate);
    }

    public void Suspend()
    {
        _machine.Fire(MemberTriggers.Suspend);
    }

    public async Task Reactivate(Activate activate)
    {
        await _machine.FireAsync(MemberTriggers.Reactivate, activate);
    }

    public static TicketSaga From(MembershipState state, string name)
    {
        return new TicketSaga(state, name);
    }

    public IEnumerable<MemberTriggers> GetPermittedTriggers()
    {
        return _machine.GetPermittedTriggers();
    }

    public bool Equals(TicketSaga anotherMember)
    {
        return State == anotherMember.State && Name == anotherMember.Name;
    }

    public string Graph() => UmlDotGraph.Format(_machine.GetInfo());
}