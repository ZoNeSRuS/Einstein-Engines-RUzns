using System.Collections.Frozen;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

public sealed class MsgEconomyAccountList : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public FrozenDictionary<string, EconomyBankAccount> Accounts = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var dictionaryCount = buffer.ReadByte();
        buffer.ReadPadBits();

        Dictionary<string, EconomyBankAccount> accounts = new();
        for (var i = 0; i < dictionaryCount; i++)
        {
            var key = buffer.ReadString();

            var id = buffer.ReadString();
            var name = buffer.ReadString();
            var currency = buffer.ReadString();
            var balance = buffer.ReadUInt64();
            var penalty = buffer.ReadUInt64();
            var blocked = buffer.ReadBoolean();
            var canReachPayday = buffer.ReadBoolean();

            // read logs
            var logsCount = buffer.ReadByte();
            buffer.ReadPadBits();
            List<EconomyBankAccountLogField> logs = new();
            for (var j = 0; j < logsCount; j++)
            {
                var date = TimeSpan.FromTicks(buffer.ReadInt64());
                var text = buffer.ReadString();
                logs.Add(new EconomyBankAccountLogField(date, text));
            }

            // add entry to the dictionary
            accounts.Add(key, new EconomyBankAccount(id, name, currency, balance, penalty, blocked, canReachPayday, logs));
        }

        Accounts = accounts.ToFrozenDictionary();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        // This shit is fucking nuts
        buffer.Write((byte)Accounts.Count);
        buffer.WritePadBits();

        foreach (var (key, value) in Accounts)
        {
            buffer.Write(key);

            buffer.Write(value.AccountID);
            buffer.Write(value.AccountName);
            buffer.Write(value.AllowedCurrency);
            buffer.Write(value.Balance);
            buffer.Write(value.Penalty);
            buffer.Write(value.Blocked);
            buffer.Write(value.CanReachPayDay);

            // write logs
            var logs = value.Logs;
            buffer.Write((byte)logs.Count);
            buffer.WritePadBits();
            foreach (var log in logs)
            {
                buffer.Write(log.Date.Ticks);
                buffer.Write(log.Text);
            }
        }
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
}