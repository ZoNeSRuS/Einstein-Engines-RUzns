using Content.Shared.Humanoid;
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
}
