using Content.Shared.Interaction;
using Content.Shared.VendingMachines;
using Content.Server.VendingMachines;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.AWS.Economy;
using Content.Server.Popups;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Components;
using Content.Server.Access.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.AWS.Economy
{
    public sealed class EconomyBankAccountSystem : EconomyBankAccountSystemShared
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly VendingMachineSystem _vendingMachine = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEconomyManager _economyManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EconomyBankAccountComponent, ComponentInit>(OnAccountComponentInit);

            SubscribeLocalEvent<EconomyBankTerminalComponent, InteractUsingEvent>(OnTerminalInteracted);

            SubscribeLocalEvent<EconomyBankATMComponent, GotEmaggedEvent>(OnATMEmagged);
            SubscribeLocalEvent<EconomyBankATMComponent, InteractUsingEvent>(OnATMInteracted);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
            /*SubscribeLocalEvent<EconomyBankAccountComponent, ComponentStartup>(OnAccountComponentStartup);*/
        }

        private string GenerateAccountId(string prefix, uint strik, uint numbersPerStrik, string? descriptor)
        {
            var res = prefix;

            for (int i = 0; i < strik; i++)
            {
                string formedStrik = "";

                for (int num = 0; num < numbersPerStrik; num++)
                {
                    formedStrik += _robustRandom.Next(0, 10);
                }

                res = res.Length == 0 ? formedStrik : res + descriptor + formedStrik;
            }

            return res;
        }

        /// <summary>
        /// Enables a card or a bank account for usage.
        /// </summary>
        [PublicAPI]
        public bool TryActivate(Entity<EconomyBankAccountComponent> entity)
        {
            // if (entity.Comp.ActivateOnSpawn)
            // {
            //     entity.Comp.Activated = true;
            //     return true;
            // }

            if (!_prototypeManager.TryIndex(entity.Comp.AccountIdByProto, out EconomyAccountIdPrototype? proto))
                return false;

            var accountID = GenerateAccountId(proto.Prefix, proto.Streak, proto.NumbersPerStreak, proto.Descriptior);
            var accountName = entity.Comp.AccountName;
            var balance = (ulong)0;
            //entity.Comp.AccountId = GenerateAccountId(proto.Prefix, proto.Streak, proto.NumbersPerStreak, proto.Descriptior);

            if (TryComp<IdCardComponent>(entity, out var idCardComponent))
                accountName = idCardComponent.FullName ?? entity.Comp.AccountName;
            if (TryComp<PresetIdCardComponent>(entity, out var presetIdCardComponent))
                if (_prototypeManager.TryIndex<EconomySallariesPrototype>("NanotrasenDefaultSallaries", out var sallariesPrototype)
                    && presetIdCardComponent.JobName is not null)
                    if (sallariesPrototype.Jobs.TryGetValue(presetIdCardComponent.JobName.Value, out var entry))
                        balance = (ulong)(entry.StartMoney * _robustRandom.NextDouble(0.5, 1.5));

            if (entity.Comp.AccountSetup is { } setup && presetIdCardComponent is null && idCardComponent is null)
            {
                var setupID = setup.GenerateAccountID ? accountID :
                                                        setup.AccountID;

                if (setupID is null)
                    return false;

                var setupAccount = new EconomyBankAccount(setupID,
                                                   setup.AccountName,
                                                   setup.AllowedCurrency,
                                                   setup.Balance,
                                                   setup.Penalty,
                                                   setup.Blocked,
                                                   setup.CanReachPayDay);

                if (!_economyManager.TryAddAccount(setupAccount))
                    return false;

                entity.Comp.AccountID = setupAccount.AccountID;
                entity.Comp.AccountName = setupAccount.AccountName;
                Dirty(entity);
                return true;
            }

            if (presetIdCardComponent is not null && idCardComponent is not null)
            {
                var accountSetup = entity.Comp.AccountSetup;
                balance = accountSetup?.Balance ?? balance;
                var account = new EconomyBankAccount(accountID, accountName, accountSetup?.AllowedCurrency, balance,
                                                     accountSetup?.Penalty, accountSetup?.Blocked, accountSetup?.CanReachPayDay);

                if (!_economyManager.TryAddAccount(account))
                    return false;

                entity.Comp.AccountID = accountID;
                entity.Comp.AccountName = accountName;
                Dirty(entity);
                return true;
            }

            return false;
        }

        // [PublicAPI]
        // public EconomyBankAccountComponent? FindAccountById(string id)
        // {
        //     var accounts = GetAccounts();
        //     if (accounts.TryGetValue(id, out var comp))
        //         return comp;

        //     return null;
        // }

        private void Withdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum)
        {
            if (!_economyManager.TryChangeAccountBalance(component.AccountID, sum, false))
                return;

            var pos = _transformSystem.GetMapCoordinates(atm.Owner);
            DropMoneyHandler(component.MoneyHolderEntId, sum, pos);

            if (_economyManager.TryGetAccount(component.AccountID, out var account))
            {
                var log = new EconomyBankAccountLogField(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-withdraw",
                ("amount", sum), ("currencyName", account.AllowedCurrency)));
                _economyManager.AddLog(component.AccountID, log);
            }

            _entManager.Dirty(component);
        }

        [PublicAPI]
        public bool TryWithdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = "";
            if (!_economyManager.TryGetAccount(component.AccountID, out var account))
                return false;

            if (sum > 0 && account.Balance >= sum)
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

        // private void SendMoney(IEconomyMoneyHolder fromAccount, EconomyBankAccountComponent toSend, ulong amount)
        // {
        //     fromAccount.Balance -= amount;
        //     toSend.Balance += amount;

        //     string senderAccoutId = "UNEXPECTED";
        //     if (fromAccount is EconomyBankAccountComponent)
        //     {
        //         var fromAccountComponent = (fromAccount as EconomyBankAccountComponent)!;
        //         fromAccountComponent.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
        //             ("amount", amount), ("currencyName", toSend.AllowCurrency), ("accountId", toSend.AccountID))));

        //         senderAccoutId = fromAccountComponent.AccountID;
        //     }
        //     toSend.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
        //             ("amount", amount), ("currencyName", toSend.AllowCurrency), ("accountId", senderAccoutId))));

        //     _entManager.Dirty((fromAccount as Component)!);
        //     _entManager.Dirty(toSend);
        // }

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
                    // if (fromAccount == recipientAccount)
                    // {
                    //     errorMessage = "407";
                    //     return false;
                    // }
                    // SendMoney(fromAccount, recipientAccount, amount);
                    return _economyManager.TryChangeAccountBalance(recipientAccount.AccountID, amount);
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

            if (!_economyManager.IsValidAccount(recipientAccountId))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (fromAccount.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return _economyManager.TryChangeAccountBalance(recipientAccountId, amount);
        }

        [PublicAPI]
        public bool TrySendMoney(EconomyBankAccountComponent fromAccount, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (!_economyManager.TryGetAccount(fromAccount.AccountID, out var fromBankAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", fromAccount.AccountID));
                return false;
            }

            if (!_economyManager.IsValidAccount(recipientAccountId))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (fromBankAccount.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return _economyManager.TryTransferMoney(fromBankAccount.AccountID, recipientAccountId, amount);
        }

        [PublicAPI]
        public bool TrySendMoney(string fromAccountId, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (!_economyManager.TryGetAccount(fromAccountId, out var fromBankAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", fromAccountId));
                return false;
            }

            if (!_economyManager.IsValidAccount(recipientAccountId))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (fromBankAccount.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return _economyManager.TryTransferMoney(fromAccountId, recipientAccountId, amount);
        }

        // [PublicAPI]
        // public Dictionary<string, EconomyBankAccountComponent> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked)
        // {
        //     Dictionary<string, EconomyBankAccountComponent> list = new();

        //     var accountsEnum = AllEntityQuery<EconomyBankAccountComponent>();
        //     while (accountsEnum.MoveNext(out var comp))
        //     {
        //         switch (flag)
        //         {
        //             case EconomyBankAccountMask.Activated:
        //                 if (comp.Activated)
        //                     list.Add(comp.AccountId, comp);
        //                 break;
        //             case EconomyBankAccountMask.ActivatedBlocked:
        //                 if (comp.Activated && comp.Blocked)
        //                     list.Add(comp.AccountId, comp);
        //                 break;
        //             case EconomyBankAccountMask.ActivatedNotBlocked:
        //                 if (comp.Activated && !comp.Blocked)
        //                     list.Add(comp.AccountId, comp);
        //                 break;
        //         }
        //     }

        //     return list;
        // }

        private void OnAccountComponentInit(Entity<EconomyBankAccountComponent> entity, ref ComponentInit args)
        {
            // if has id card comp, then it will be initialized in other place
            if (entity.Comp.AccountSetup is null || HasComp<IdCardComponent>(entity))
                return;

            TryActivate(entity);
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

        private void OnATMEmagged(EntityUid uid, EconomyBankATMComponent component, ref GotEmaggedEvent args)
        {
            if (HasComp<EmaggedComponent>(uid) || args.Handled)
                return;

            var listMoney = component.EmagDropMoneyValues;
            var listMoneyCount = listMoney.Count;

            if (listMoneyCount == 0)
                return;

            if (component.EmagDropMoneyHolderRandomCount == 0)
                return;

            var moneyHolderCount = _robustRandom.Next(1, component.EmagDropMoneyHolderRandomCount + 1);
            var mapPos = _transformSystem.GetMapCoordinates(uid);

            for (int i = 0; i < moneyHolderCount; i++)
            {
                var droppedEnt = DropMoneyHandler(component.MoneyHolderEntId,
                    listMoney[_robustRandom.Next(0, listMoneyCount)], mapPos);
                droppedEnt.Comp.Emagged = true;
            }

            _audioSystem.PlayPvs(component.EmagSound, uid);
            args.Handled = true;
        }
        private void OnATMInteracted(EntityUid uid, EconomyBankATMComponent component, InteractUsingEvent args)
        {
            var usedEnt = args.Used;

            if (!TryComp<EconomyMoneyHolderComponent>(usedEnt, out var economyMoneyHolderComponent))
                return;

            var amount = economyMoneyHolderComponent.Balance;
            var insertedAccountComponent = GetATMInsertedAccount(component);

            if (TrySendMoney(economyMoneyHolderComponent, insertedAccountComponent, amount, out var error))
            {
                if (insertedAccountComponent is not null && _economyManager.TryGetAccount(insertedAccountComponent.AccountID, out var account))
                    _economyManager.AddLog(account.AccountID,
                                           new EconomyBankAccountLogField(_gameTiming.CurTime,
                                           Loc.GetString("economybanksystem-log-insert",
                                           ("amount", amount), ("currencyName", account.AllowedCurrency))));

                if (_netManager.IsServer)
                    _popupSystem.PopupEntity(Loc.GetString("economybanksystem-atm-moneyentering"), uid, type: PopupType.Medium);

                QueueDel(usedEnt);
            }
            if (_netManager.IsServer)
                _popupSystem.PopupEntity(error, uid, type: PopupType.Medium);

            UpdateATMUserInterface((uid, component), error);
        }
        private void OnTerminalInteracted(EntityUid uid, EconomyBankTerminalComponent component, InteractUsingEvent args)
        {
            var amount = component.Amount;
            var usedEnt = args.Used;
            string? selectedItemId = null;

            if (amount <= 0)
                return;

            if (!TryComp<EconomyMoneyHolderComponent>(usedEnt, out var economyMoneyHolderComponent) &
                !TryComp<EconomyBankAccountComponent>(usedEnt, out var economyBankAccountComponent))
                return;

            if (TryComp<VendingMachineComponent>(uid, out var vendingMachineComponent))
            {
                if (vendingMachineComponent.SelectedItemId is not null)
                {
                    selectedItemId = vendingMachineComponent.SelectedItemId;
                    if (vendingMachineComponent.Inventory.TryGetValue(vendingMachineComponent.SelectedItemId, out var entry))
                    {
                        if (entry.Price > 0)
                        {
                            vendingMachineComponent.SelectedItemId = null;
                            goto sendMoney;
                        }
                    }
                    return;
                }
            }

        sendMoney:
            if (vendingMachineComponent is not null && selectedItemId is not null)
            {
                _vendingMachine.AuthorizedVend(uid, args.User, vendingMachineComponent.SelectedItemInventoryType, selectedItemId, vendingMachineComponent);
                if (vendingMachineComponent.Denying)
                    return;

            }

            if (!_economyManager.TryGetAccount(component.LinkedAccount, out var receiverAccount))
                return;

            if (economyMoneyHolderComponent is not null)
            {
                if (!TrySendMoney(economyMoneyHolderComponent, component.LinkedAccount, amount, out var err))
                {
                    if (_netManager.IsServer)
                        _popupSystem.PopupEntity(err, uid, type: PopupType.MediumCaution);
                    return;
                }
            }
            else if (economyBankAccountComponent is not null)
            {
                if (!TrySendMoney(economyBankAccountComponent, component.LinkedAccount, amount, out var err))
                {
                    if (_netManager.IsServer)
                        _popupSystem.PopupEntity(err, uid, type: PopupType.MediumCaution);
                    return;
                }
            }

            UpdateTerminal((uid, component), 0, string.Empty);

            _popupSystem.PopupEntity(Loc.GetString("economybanksystem-transaction-success", ("amount", amount), ("currencyName", receiverAccount.AllowedCurrency)), uid, type: PopupType.Medium);
        }
    }
}
