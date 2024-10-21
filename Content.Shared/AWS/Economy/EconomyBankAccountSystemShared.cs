using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Shared.Interaction;
using Content.Shared.VendingMachines;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Shared.AWS.Economy
{
    public class EconomyBankAccountSystemShared : EntitySystem
    {
        [Dependency] protected readonly EntityManager _entManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EconomyBankTerminalComponent, ExaminedEvent>(OnBankTerminalExamine);
            SubscribeLocalEvent<EconomyBankTerminalComponent, EconomyTerminalMessage>(OnTerminalMessage);

            SubscribeLocalEvent<EconomyBankAccountComponent, ComponentInit>(OnBankAccountComponentInit);
            SubscribeLocalEvent<EconomyBankAccountComponent, ExaminedEvent>(OnBankAccountExamine);
            SubscribeLocalEvent<EconomyMoneyHolderComponent, ExaminedEvent>(OnMoneyHolderExamine);

            SubscribeLocalEvent<EconomyBankATMComponent, ComponentInit>(OnATMComponentInit);
            SubscribeLocalEvent<EconomyBankATMComponent, ComponentRemove>(OnATMComponentRemove);
            SubscribeLocalEvent<EconomyBankATMComponent, EntInsertedIntoContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EntRemovedFromContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
        }

        private void OnBankAccountComponentInit(EntityUid uid, EconomyBankAccountComponent comp, ComponentInit args)
        {
            if (comp.ActivateOnSpawn)
            {
                comp.Activated = true;
                Dirty(uid, comp);
            }
        }

        private void OnBankAccountExamine(Entity<EconomyBankAccountComponent> entity, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("bankaccount-component-on-examine-detailed-message",
                ("id", entity.Comp.AccountId)));
            args.PushMarkup(Loc.GetString("moneyholder-component-on-examine-detailed-message",
                ("moneyName", entity.Comp.AllowCurrency),
                ("balance", entity.Comp.Balance)));
        }

        private void OnTerminalMessage(EntityUid uid, EconomyBankTerminalComponent comp, EconomyTerminalMessage args)
        {
            UpdateTerminal((uid, comp), args.Amount, args.Reason);
        }

        private void OnBankTerminalExamine(Entity<EconomyBankTerminalComponent> entity, ref ExaminedEvent args)
        {
            var comp = entity.Comp;
            args.PushMarkup(Loc.GetString("economyBankTerminal-component-on-examine-connected-to",
                ("accountId", comp.LinkedAccount)));

            if (comp.Amount > 0)
            {
                args.PushMarkup(Loc.GetString("economyBankTerminal-component-on-examine-pay-for-ifmorethanzero",
                ("amount", comp.Amount),
                ("currencyName", comp.AllowCurrency)));
            }
            else args.PushMarkup(Loc.GetString("economyBankTerminal-component-on-examine-pay-for-iflessthanzero"));

            if (comp.Reason != string.Empty)
                args.PushMarkup(Loc.GetString("economyBankTerminal-component-on-examine-reason", ("reason", comp.Reason)));
        }
        private void OnMoneyHolderExamine(Entity<EconomyMoneyHolderComponent> entity, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("moneyholder-component-on-examine-detailed-message",
                ("moneyName", entity.Comp.AllowCurrency),
                ("balance", entity.Comp.Balance)));
        }
        private void OnATMComponentInit(EntityUid uid, EconomyBankATMComponent atm, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, EconomyBankATMComponent.ATMCardId, atm.CardSlot);

            UpdateATMUserInterface((uid, atm));
        }
        private void OnATMComponentRemove(EntityUid uid, EconomyBankATMComponent atm, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, atm.CardSlot);
        }

        private void OnATMItemSlotChanged(EntityUid uid, EconomyBankATMComponent atm, ContainerModifiedMessage args)
        {
            if (args.Container.ID != atm.CardSlot.ID)
                return;

            UpdateATMUserInterface((uid, atm));
        }

        private void OnATMWithdrawMessage(EntityUid uid, EconomyBankATMComponent atm, EconomyBankATMWithdrawMessage args)
        {
            var bankAccount = GetATMInsertedAccount(atm);
            if (bankAccount is null)
                return;

            string? error;

            TryWithdraw(bankAccount, atm, args.Amount, out error);
            UpdateATMUserInterface((uid, atm), error);
        }

        private void OnATMTransferMessage(EntityUid uid, EconomyBankATMComponent atm, EconomyBankATMTransferMessage args)
        {
            var bankAccount = GetATMInsertedAccount(atm);
            if (bankAccount is null)
                return;

            string? error;

            TrySendMoney(bankAccount, args.RecipientAccountId, args.Amount, out error);
            UpdateATMUserInterface((uid, atm), error);
        }

        [PublicAPI]
        public EconomyBankAccountComponent? FindAccountById(string id)
        {
            var accounts = GetAccounts();
            if (accounts.TryGetValue(id, out var comp))
                return comp;

            return null;
        }

        private void Withdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum)
        {
            component.Balance -= sum;
            var pos = Comp<TransformComponent>(atm.Owner).MapPosition;
            DropMoneyHandler(component.MoneyHolderEntId, sum, pos);

            component.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-withdraw",
                ("amount", sum), ("currencyName", component.AllowCurrency))));

            _entManager.Dirty(component);
        }

        [PublicAPI]
        public bool TryWithdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = "";
            if (sum > 0 && component.Balance >= sum)
            {
                Withdraw(component, atm, sum);
                return true;
            }
            errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
            return false;
        }

        [PublicAPI]
        public Entity<EconomyMoneyHolderComponent> DropMoneyHandler(EntProtoId<EconomyMoneyHolderComponent> entId, ulong amount, MapCoordinates pos)
        {
            var ent = Spawn(entId, pos);

            var moneyHolderComp = Comp<EconomyMoneyHolderComponent>(ent);
            moneyHolderComp.Balance = amount;

            _entManager.Dirty(moneyHolderComp);

            return (ent, moneyHolderComp);
        }

        private void SendMoney(IEconomyMoneyHolder fromAccount, EconomyBankAccountComponent toSend, ulong amount)
        {
            fromAccount.Balance -= amount;
            toSend.Balance += amount;

            string senderAccoutId = "UNEXPECTED";
            if (fromAccount is EconomyBankAccountComponent)
            {
                var fromAccountComponent = (fromAccount as EconomyBankAccountComponent)!;
                fromAccountComponent.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
                    ("amount", amount), ("currencyName", toSend.AllowCurrency), ("accountId", toSend.AccountId))));

                senderAccoutId = fromAccountComponent.AccountId;
            }
            toSend.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
                    ("amount", amount), ("currencyName", toSend.AllowCurrency), ("accountId", senderAccoutId))));

            _entManager.Dirty((fromAccount as Component)!);
            _entManager.Dirty(toSend);
        }

/*      TODO:
        public void AddLog(EconomyBankAccountComponent comp, )*/

        [PublicAPI]
        public bool TrySendMoney(IEconomyMoneyHolder fromAccount, EconomyBankAccountComponent? recipientAccount, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (fromAccount.Balance >= amount)
            {
                if (recipientAccount is not null)
                {
                    if (fromAccount == recipientAccount)
                    {
                        errorMessage = "407";
                        return false;
                    }
                    SendMoney(fromAccount, recipientAccount, amount);
                    return true;
                }

                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout");
                return false;
            }

            errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
            return false;
        }

        [PublicAPI]
        public bool TrySendMoney(IEconomyMoneyHolder fromAccount, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            var recipientAccount = FindAccountById(recipientAccountId);
            if (recipientAccount is null)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            return TrySendMoney(fromAccount, recipientAccount, amount, out errorMessage);
        }

        [PublicAPI]
        public void UpdateATMUserInterface(Entity<EconomyBankATMComponent> entity, string? error = null)
        {
            var bankAccount = GetATMInsertedAccount(entity.Comp);
            _userInterfaceSystem.SetUiState(entity.Owner, EconomyBankATMUiKey.Key, new EconomyBankATMUserInterfaceState()
            {
                BankAccount = bankAccount is null ? null :
                new() {
                    Balance = bankAccount.Balance,
                    AccountId = bankAccount.AccountId,
                    AccountName = bankAccount.AccountName,
                    Blocked = bankAccount.Blocked,
                },
                Error = error,
            });
        }

        [PublicAPI]
        public EconomyBankAccountComponent? GetATMInsertedAccount(EconomyBankATMComponent atm)
        {
            TryComp(atm.CardSlot.Item, out EconomyBankAccountComponent? bankAccount);
            return bankAccount;
        }

        [PublicAPI]
        public void UpdateTerminal(Entity<EconomyBankTerminalComponent> entity, ulong amount, string? reason)
        {
            if (amount != 0)
                _popupSystem.PopupPredicted(Loc.GetString("economybanksystem-vending-insertcard"), entity, null);

            entity.Comp.Amount = amount;
            entity.Comp.Reason = reason is null ? string.Empty : reason;

            _entManager.Dirty(entity);
        }

        [PublicAPI]
        public Dictionary<string, EconomyBankAccountComponent> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.Activated)
        {
            Dictionary<string, EconomyBankAccountComponent> list = new();

            var accountsEnum = AllEntityQuery<EconomyBankAccountComponent>();
            while (accountsEnum.MoveNext(out var comp))
            {
                switch (flag)
                {
                    case EconomyBankAccountMask.Activated:
                        if (comp.Activated)
                            list.Add(comp.AccountId, comp);
                        break;
                    case EconomyBankAccountMask.ActivatedBlocked:
                        if (comp.Activated && comp.Blocked)
                            list.Add(comp.AccountId, comp);
                        break;
                    case EconomyBankAccountMask.ActivatedNotBlocked:
                        if (comp.Activated && !comp.Blocked)
                            list.Add(comp.AccountId, comp);
                        break;
                }
            }

            return list;
        }
    }
    public enum EconomyBankAccountMask
    {
        Activated,
        ActivatedBlocked,
        ActivatedNotBlocked,
    }
}
