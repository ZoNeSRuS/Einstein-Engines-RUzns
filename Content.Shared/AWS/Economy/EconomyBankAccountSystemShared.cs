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
        [Dependency] private readonly ISharedEconomyManager _economyManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EconomyBankTerminalComponent, ExaminedEvent>(OnBankTerminalExamine);
            SubscribeLocalEvent<EconomyBankTerminalComponent, EconomyTerminalMessage>(OnTerminalMessage);

            //SubscribeLocalEvent<EconomyBankAccountComponent, ComponentInit>(OnBankAccountComponentInit);
            SubscribeLocalEvent<EconomyBankAccountComponent, ExaminedEvent>(OnBankAccountExamine);
            SubscribeLocalEvent<EconomyMoneyHolderComponent, ExaminedEvent>(OnMoneyHolderExamine);

            SubscribeLocalEvent<EconomyBankATMComponent, ComponentInit>(OnATMComponentInit);
            SubscribeLocalEvent<EconomyBankATMComponent, ComponentRemove>(OnATMComponentRemove);
            SubscribeLocalEvent<EconomyBankATMComponent, EntInsertedIntoContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EntRemovedFromContainerMessage>(OnATMItemSlotChanged);
            //SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            //SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
        }

        // private void OnBankAccountComponentInit(EntityUid uid, EconomyBankAccountComponent comp, ComponentInit args)
        // {
        //     if (comp.ActivateOnSpawn)
        //     {
        //         comp.Activated = true;
        //         Dirty(uid, comp);
        //     }
        // }

        private void OnBankAccountExamine(Entity<EconomyBankAccountComponent> entity, ref ExaminedEvent args)
        {
            if (!_economyManager.TryGetAccount(entity.Comp.AccountID, out var account))
                return;

            args.PushMarkup(Loc.GetString("bankaccount-component-on-examine-detailed-message",
                ("id", account.AccountID)));
            args.PushMarkup(Loc.GetString("moneyholder-component-on-examine-detailed-message",
                ("moneyName", account.AllowedCurrency),
                ("balance", account.Balance)));
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

        [PublicAPI]
        public void UpdateATMUserInterface(Entity<EconomyBankATMComponent> entity, string? error = null)
        {
            var card = GetATMInsertedAccount(entity.Comp);
            EconomyBankAccount? bankAccount = null;
            if (card is not null && _economyManager.TryGetAccount(card.AccountID, out var account))
                bankAccount = account;

            _userInterfaceSystem.SetUiState(entity.Owner, EconomyBankATMUiKey.Key, new EconomyBankATMUserInterfaceState()
            {
                BankAccount = bankAccount is null ? null :
                new() {
                    Balance = bankAccount.Balance,
                    AccountId = bankAccount.AccountID,
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
    }
    public enum EconomyBankAccountMask
    {
        All,
        NotBlocked,
        Blocked,
    }
}
