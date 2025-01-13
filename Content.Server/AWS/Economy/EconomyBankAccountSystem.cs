using Content.Shared.Interaction;
using Content.Shared.VendingMachines;
using Content.Server.VendingMachines;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.AWS.Economy;
using Content.Server.Popups;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
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
using Content.Server.Station.Systems;
using Robust.Server.GameStates;
using Content.Shared.Store;

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
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EconomyAccountHolderComponent, ComponentInit>(OnAccountComponentInit);

            SubscribeLocalEvent<EconomyBankTerminalComponent, InteractUsingEvent>(OnTerminalInteracted);

            SubscribeLocalEvent<EconomyBankATMComponent, GotEmaggedEvent>(OnATMEmagged);
            SubscribeLocalEvent<EconomyBankATMComponent, InteractUsingEvent>(OnATMInteracted);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
        }

        #region Account management
        /// <summary>
        /// Creates a new bank account entity.
        /// </summary>
        /// <returns>Whether the account was successfully created.</returns>
        [PublicAPI]
        public bool TryCreateAccount(string accountID,
                                     string accountName,
                                     ProtoId<CurrencyPrototype> allowedCurrency,
                                     ulong balance = 0,
                                     ulong penalty = 0,
                                     bool blocked = false,
                                     bool canReachPayDay = true,
                                     MapCoordinates? cords = null)
        {
            // Return if account with this id already exists.
            if (IsValidAccount(accountID))
                return false;

            var spawnCords = cords ?? MapCoordinates.Nullspace;
            var accountEntity = Spawn(null, spawnCords);
            _metaDataSystem.SetEntityName(accountEntity, accountID);
            var accountComp = EnsureComp<EconomyBankAccountComponent>(accountEntity);

            accountComp.AccountID = accountID;
            accountComp.AccountName = accountName;
            accountComp.AllowedCurrency = allowedCurrency;
            accountComp.Balance = balance;
            accountComp.Penalty = penalty;
            accountComp.Blocked = blocked;
            accountComp.CanReachPayDay = canReachPayDay;

            _pvsOverrideSystem.AddGlobalOverride(accountEntity);
            Dirty(accountEntity, accountComp);
            return true;
        }

        /// <summary>
        /// Enables a card or a bank account (described in setup) for usage.
        /// </summary>
        [PublicAPI]
        public bool TryActivate(Entity<EconomyAccountHolderComponent> entity)
        {
            if (!_prototypeManager.TryIndex(entity.Comp.AccountIdByProto, out EconomyAccountIdPrototype? proto))
                return false;

            var accountID = GenerateAccountId(proto.Prefix, proto.Streak, proto.NumbersPerStreak, proto.Descriptior);
            var accountName = entity.Comp.AccountName;
            var balance = (ulong)0;

            if (TryComp<IdCardComponent>(entity, out var idCardComponent))
                accountName = idCardComponent.FullName ?? entity.Comp.AccountName;
            if (TryComp<PresetIdCardComponent>(entity, out var presetIdCardComponent))
                if (_prototypeManager.TryIndex<EconomySallariesPrototype>("NanotrasenDefaultSallaries", out var sallariesPrototype)
                    && presetIdCardComponent.JobName is not null)
                    if (sallariesPrototype.Jobs.TryGetValue(presetIdCardComponent.JobName.Value, out var entry))
                        balance = (ulong)(entry.StartMoney * _robustRandom.NextDouble(0.5, 1.5));

            var station = _stationSystem.GetOwningStation(entity);
            var cords = station != null ? _transformSystem.GetMapCoordinates(station.Value) : MapCoordinates.Nullspace;
            if (entity.Comp.AccountSetup is { } setup && presetIdCardComponent is null && idCardComponent is null)
            {
                var setupID = setup.GenerateAccountID ? accountID :
                                                        setup.AccountID;

                if (setupID is null)
                    return false;

                if (!TryCreateAccount(setupID,
                                      setup.AccountName ?? "UNEXPECTED USER",
                                      setup.AllowedCurrency ?? "Thaler",
                                      setup.Balance ?? balance,
                                      setup.Penalty ?? 0,
                                      setup.Blocked ?? false,
                                      setup.CanReachPayDay ?? true,
                                      cords))
                    return false;

                entity.Comp.AccountID = setupID;
                entity.Comp.AccountName = setup.AccountName ?? "UNEXPECTED USER";
                Dirty(entity);
                return true;
            }

            if (presetIdCardComponent is not null && idCardComponent is not null)
            {
                var accountSetup = entity.Comp.AccountSetup;
                balance = accountSetup?.Balance ?? balance;

                if (!TryCreateAccount(accountID,
                                      accountName,
                                      accountSetup?.AllowedCurrency ?? "Thaler",
                                      balance,
                                      accountSetup?.Penalty ?? 0,
                                      accountSetup?.Blocked ?? false,
                                      accountSetup?.CanReachPayDay ?? true,
                                      cords))
                    return false;

                entity.Comp.AccountID = accountID;
                entity.Comp.AccountName = accountName;
                Dirty(entity);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to set the parameter chosen in arguments to a given value.
        /// </summary>
        /// <param name="param">Parameter to be changed.</param>
        /// <param name="value">New value of the parameter, must be of the same type as a changed value.</param>
        [PublicAPI]
        public bool TrySetAccountParameter(string accountID, EconomyBankAccountParam param, object value)
        {
            if (!TryGetAccount(accountID, out var entity))
                return false;

            var account = entity.Comp;
            switch (param)
            {
                case EconomyBankAccountParam.AccountName:
                    if (value is not string name)
                        return false;
                    account.AccountName = name;
                    break;
                case EconomyBankAccountParam.Blocked:
                    if (value is not bool blocked)
                        return false;
                    account.Blocked = blocked;
                    break;
                case EconomyBankAccountParam.CanReachPayDay:
                    if (value is not bool canReachPayDay)
                        return false;
                    account.CanReachPayDay = canReachPayDay;
                    break;
                default:
                    return false;
            }

            Dirty(entity);
            return true;
        }

        public enum EconomyBankAccountParam
        {
            AccountName,
            Blocked,
            CanReachPayDay,
        }

        [PublicAPI]
        public bool TryWithdraw(EconomyAccountHolderComponent component, EconomyBankATMComponent atm, ulong sum, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;
            if (!TryGetAccount(component.AccountID, out var account))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout");
                return false;
            }

            if (account.Comp.Blocked)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-account-blocked", ("accountId", account.Comp.AccountID));
                return false;
            }

            if (sum > 0 && account.Comp.Balance >= sum)
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

        [PublicAPI]
        public bool TrySendMoney(IEconomyMoneyHolder fromAccount, EconomyAccountHolderComponent? recipientAccount, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (fromAccount.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            if (recipientAccount is null || !TryGetAccount(recipientAccount.AccountID, out var account))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout");
                return false;
            }

            if (account.Comp.Blocked)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-account-blocked", ("accountId", account.Comp.AccountID));
                return false;
            }

            return TryChangeAccountBalance(recipientAccount.AccountID, amount);
        }

        [PublicAPI]
        public bool TrySendMoney(IEconomyMoneyHolder fromAccount, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (!TryGetAccount(recipientAccountId, out var account))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (account.Comp.Blocked)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-account-blocked", ("accountId", account.Comp.AccountID));
                return false;
            }

            if (fromAccount.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return TryChangeAccountBalance(recipientAccountId, amount);
        }

        [PublicAPI]
        public bool TrySendMoney(EconomyAccountHolderComponent fromAccount, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (!TryGetAccount(fromAccount.AccountID, out var fromBankAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", fromAccount.AccountID));
                return false;
            }

            if (!TryGetAccount(recipientAccountId, out var recipientAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (fromBankAccount.Comp.Blocked || recipientAccount.Comp.Blocked)
            {
                var blockedAccountID = fromBankAccount.Comp.Blocked ? fromBankAccount.Comp.AccountID : recipientAccount.Comp.AccountID;
                errorMessage = Loc.GetString("economybanksystem-transaction-error-account-blocked", ("accountId", blockedAccountID));
                return false;
            }

            if (fromBankAccount.Comp.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return TryTransferMoney(fromBankAccount.Comp.AccountID, recipientAccountId, amount);
        }

        [PublicAPI]
        public bool TrySendMoney(string fromAccountId, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            if (!TryGetAccount(fromAccountId, out var fromBankAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", fromAccountId));
                return false;
            }

            if (!TryGetAccount(recipientAccountId, out var recipientAccount))
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notfoundaccout", ("accountId", recipientAccountId));
                return false;
            }

            if (fromBankAccount.Comp.Blocked || recipientAccount.Comp.Blocked)
            {
                var blockedAccountID = fromBankAccount.Comp.Blocked ? fromAccountId : recipientAccountId;
                errorMessage = Loc.GetString("economybanksystem-transaction-error-account-blocked", ("accountId", blockedAccountID));
                return false;
            }

            if (fromBankAccount.Comp.Balance < amount)
            {
                errorMessage = Loc.GetString("economybanksystem-transaction-error-notenoughmoney");
                return false;
            }

            return TryTransferMoney(fromAccountId, recipientAccountId, amount);
        }

        /// <summary>
        /// Changes the balance of the account.
        /// </summary>
        /// <param name="addition">Whether to add or substract the given amount.</param>
        private bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true)
        {
            if (!TryGetAccount(accountID, out var entity))
                return false;

            var account = entity.Comp;
            if (!addition)
            {
                if (account.Balance - amount < 0)
                    return false;

                account.Balance -= amount;
                return true;
            }

            account.Balance += amount;

            Dirty(entity);
            return true;
        }

        /// <summary>
        /// Transfer money from one account to another (with logs).
        /// </summary>
        /// <returns>True if the transfer was successful, false otherwise.</returns>
        private bool TryTransferMoney(string senderID, string receiverID, ulong amount)
        {
            if (amount <= 0 ||
                !TryGetAccount(senderID, out var senderEntity) ||
                !TryGetAccount(receiverID, out var receiverEntity))
                return false;

            var sender = senderEntity.Comp;
            var receiver = receiverEntity.Comp;
            if (sender.Balance < amount)
                return false;

            sender.Balance -= amount;
            sender.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
                        ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", receiver.AccountID))));
            receiver.Balance += amount;
            receiver.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
                        ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", sender.AccountID))));

            Dirty(senderEntity);
            Dirty(receiverEntity);
            return true;
        }

        /// <summary>
        /// Adds a new log to the account.
        /// </summary>
        private void AddLog(string accountID, EconomyBankAccountLogField log)
        {
            if (!TryGetAccount(accountID, out var account))
                return;

            account.Comp.Logs.Add(log);
            Dirty(account);
        }

        private void Withdraw(EconomyAccountHolderComponent component, EconomyBankATMComponent atm, ulong sum)
        {
            if (!TryChangeAccountBalance(component.AccountID, sum, false))
                return;

            var pos = _transformSystem.GetMapCoordinates(atm.Owner);
            DropMoneyHandler(component.MoneyHolderEntId, sum, pos);

            if (TryGetAccount(component.AccountID, out var account))
            {
                var log = new EconomyBankAccountLogField(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-withdraw",
                ("amount", sum), ("currencyName", account.Comp.AllowedCurrency)));
                AddLog(component.AccountID, log);
            }

            _entManager.Dirty(component);
        }
        #endregion

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

        private void OnAccountComponentInit(Entity<EconomyAccountHolderComponent> entity, ref ComponentInit args)
        {
            // if has id card comp, then it will be initialized in the other place.
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
                if (insertedAccountComponent is not null && TryGetAccount(insertedAccountComponent.AccountID, out var account))
                    AddLog(account.Comp.AccountID,
                           new EconomyBankAccountLogField(_gameTiming.CurTime,
                           Loc.GetString("economybanksystem-log-insert",
                           ("amount", amount), ("currencyName", account.Comp.AllowedCurrency))));

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
                !TryComp<EconomyAccountHolderComponent>(usedEnt, out var economyBankAccountHolderComponent))
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

            if (!TryGetAccount(component.LinkedAccount, out var receiverAccount))
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
            else if (economyBankAccountHolderComponent is not null)
            {
                if (!TrySendMoney(economyBankAccountHolderComponent, component.LinkedAccount, amount, out var err))
                {
                    if (_netManager.IsServer)
                        _popupSystem.PopupEntity(err, uid, type: PopupType.MediumCaution);
                    return;
                }
            }

            UpdateTerminal((uid, component), 0, string.Empty);

            _popupSystem.PopupEntity(Loc.GetString("economybanksystem-transaction-success", ("amount", amount), ("currencyName", receiverAccount.Comp.AllowedCurrency)), uid, type: PopupType.Medium);
        }
    }
}
