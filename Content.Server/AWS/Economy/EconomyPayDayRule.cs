using System.Linq;
using Content.Shared.Dataset;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.AWS.Economy;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Server.AWS.Economy;

namespace Content.Server.StationEvents.Events;

public sealed class EconomyPayDayRule : StationEventSystem<EconomyPayDayRuleComponent>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly EconomyBankAccountSystem _bankAccountSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    protected override void Started(EntityUid uid, EconomyPayDayRuleComponent ruleComponent, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var accounts = _bankAccountSystem.GetAccounts();
        if (!accounts.TryGetValue(ruleComponent.PayerAccountId, out var payerComp))
            return;

        if (!_prototype.TryIndex(ruleComponent.SallaryProto, out var sallariesProto))
            return;

        var enumerator = _entMan.AllEntityQueryEnumerator<MindContainerComponent>();
        Dictionary<string, ProtoId<JobPrototype>> manifest = new();
        while(enumerator.MoveNext(out var ent, out var mindContainerComponent))
        {
            if (TryComp<MindComponent>(mindContainerComponent.Mind, out var mindComponent)
                && TryComp<JobComponent>(mindContainerComponent.Mind, out var jobComponent)
                && mindComponent.CharacterName is not null
                && jobComponent.Prototype is not null)
            {
                manifest.Add(mindComponent.CharacterName, jobComponent.Prototype.Value);
            }
        }
        List<EconomyBankAccountComponent> blockedAccounts = new();

        foreach (var (id, comp) in accounts)
        {
            if (comp.Blocked || !comp.CanReachPayDay)
                continue;

            if (comp.Blocked)
                continue;

            if (!manifest.TryGetValue(comp.AccountName, out var job))
                continue;

            EconomySallariesJobEntry? entry = null;

            foreach (var item in sallariesProto.Jobs)
            {
                if (item.Key.Id == job.Id)
                    entry = item.Value;
            }

            if (entry is null)
                continue;

            ulong sallary = ((ulong)ruleComponent.Coef.Next(_random))/100* entry.Value.Sallary;
            string? err;

            switch (ruleComponent.PayType)
            {
                case EconomyPayDayRuleType.Adding:
                    _bankAccountSystem.TrySendMoney(payerComp, comp, sallary, out err);
                    break;
                case EconomyPayDayRuleType.Decrementing:
                    if (!_bankAccountSystem.TrySendMoney(comp, payerComp, sallary, out err))
                    {
                        comp.Blocked = true;
                        blockedAccounts.Add(comp);
                    }
                    break;
                default:
                    break;
            }
        }

        //notify that we blocked, or we cant proccess any payment
    }
}
