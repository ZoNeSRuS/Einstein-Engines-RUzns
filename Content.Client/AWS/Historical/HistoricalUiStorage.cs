

using Content.Shared.AWS.Historical;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.AWS.Historical;

public record HistoricalUiStorage(
    List<ProtoId<HistoryPrototype>> HistoryByButtonId,
    Dictionary<HistoryType, ProtoId<HistoryPrototype>> SelectedHistories,
    Dictionary<HistoryType, TextEdit> DescriptionFieldForTypes,
    Dictionary<HistoryType, OptionButton> OptionButtonsByType,
    Dictionary<HistoryType, List<ProtoId<HistoryPrototype>>> Histories);
