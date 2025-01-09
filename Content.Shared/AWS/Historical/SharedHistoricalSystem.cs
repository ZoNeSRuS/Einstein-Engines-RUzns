using Content.Shared.Humanoid;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.AWS.Historical;

public abstract class SharedHistoricalSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        /*SubscribeLocalEvent<IsJobAllowedEvent>(OnIsJobAllowed);*/
    }

    [PublicAPI]
    public Dictionary<HistoryType, ProtoId<HistoryPrototype>> GetDefaultHistories()
    {
        var defaultHistories = new Dictionary<HistoryType, ProtoId<HistoryPrototype>>();
        var enumerator = _prototypeManager.EnumeratePrototypes<HistoryPrototype>().GetEnumerator();

        while (enumerator.MoveNext())
        {
            var proto = enumerator.Current;
            if (proto.IsDefault)
                defaultHistories[(HistoryType)proto.HistoryType] = proto.ID;
        }

        return defaultHistories;
    }

    [PublicAPI]
    public Dictionary<HistoryType, List<ProtoId<HistoryPrototype>>> GetHistories()
    {
        Dictionary<HistoryType, List<ProtoId<HistoryPrototype>>> histories = new();

        var enumerator = _prototypeManager.EnumeratePrototypes<HistoryPrototype>().GetEnumerator();

        while (enumerator.MoveNext())
        {
            var elem = enumerator.Current;

            if (!histories.TryGetValue((HistoryType)elem.HistoryType, out var protos))
            {
                histories[(HistoryType)elem.HistoryType] = protos = new([elem]);
                continue;
            }

            protos.Add(elem);
        }

        return histories;
    }

    [PublicAPI]
    public bool CanUseJob(ProtoId<JobPrototype> jobId, ProtoId<HistoryPrototype> historyId,
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        if (_prototypeManager.TryIndex(historyId, out var proto) && !proto.BlockingJobs.Contains(jobId))
            return true;

        error = "incorrect history";
        return false;
    }

/*    private void OnIsJobAllowed(ref IsJobAllowedEvent ev)
    {
        if (!_manager.IsAllowed(ev.Player, ev.JobId))
            ev.Cancelled = true;
    }*/
}
