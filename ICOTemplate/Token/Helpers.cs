using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;

namespace Neo.SmartContract
{
    public class Helpers : Framework.SmartContract
    {
        public static string[] GetHelperMethods() => new string[] {
            "BalanceOfVestedAddress",
            "IsPrivateSaleAllocationLocked",
            "supportedStandards",
            "BalanceOfSaleContribution"
        };

        public static object HandleHelperOperation(string operation, params object[] args)
        {
            switch (operation)
            {
                case "BalanceOfVestedAddress":
                    // retrieve the real balance of an address that has been subjected to whitepaper defined vesting period
                    if (!Helpers.RequireArgumentLength(args, 1))
                    {
                        return false;
                    }
                    return Helpers.BalanceOfVestedAddress((byte[])args[0]);
                case "IsPrivateSaleAllocationLocked":
                    // if the admin method `Administration.AllocatePresalePurchase` is permanently disabled, this method will return
                    // the timestamp the lock was put in place.
                    return Storage.Get(Storage.CurrentContext, StorageKeys.PrivateSaleAllocationLocked());
                case "supportedStandards":
                    // support NEP-10 by responding to supportedStandards
                    // https://github.com/neo-project/proposals/blob/master/nep-10.mediawiki
                    return ICOTemplate.SupportedStandards();
                case "BalanceOfSaleContribution":
                    if (!Helpers.RequireArgumentLength(args, 1))
                    {
                        return false;
                    }
                    return Helpers.BalanceOfSaleContribution((byte[])args[0]);
            }
            return false;
        }


        /// <summary>
        /// retreive the balance of an address showing contribution during token sale
        /// </summary>
        /// <param name="account">address to check balance of</param>
        /// <returns>number of tokens</returns>
        public static BigInteger BalanceOfSaleContribution(byte[] account)
        {
            if (account.Length != 20)
            {
                Runtime.Log("BalanceOfSaleContribution() invalid address supplied");
                return 0;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(StorageKeys.ContributionBalancePrefix());

            return balances.Get(account).AsBigInteger();
        }

        /// <summary>
        /// retreive the balance of an address and show any tokens that are locked due to vesting
        /// </summary>
        /// <param name="account">address to check balance of</param>
        /// <returns>number of tokens</returns>
        public static BigInteger BalanceOfVestedAddress(byte[] account)
        {
            if (account.Length != 20)
            {
                Runtime.Log("BalanceOfVestedAddress() invalid address supplied");
                return 0;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(StorageKeys.VestedBalancePrefix());

            return balances.Get(account).AsBigInteger();
        }

        /// <summary>
        /// get the latest blocks timestamp
        /// </summary>
        /// <returns></returns>
        public static uint GetBlockTimestamp()
        {
            Header header = Blockchain.GetHeader(Blockchain.GetHeight());
            return header.Timestamp;
        }

        /// <summary>
        /// get the time the contract was first initialised
        /// </summary>
        /// <returns></returns>
        public static uint GetContractInitTime()
        {
            return (uint)Storage.Get(Storage.CurrentContext, StorageKeys.ContractInitTime()).AsBigInteger();
        }
        
        /// <summary>
        /// test if a contract address is a whitelisted TransferFrom
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsContractWhitelistedTransferFrom(byte[] address)
        {
            if(IsTransferFromWhitelistingEnabled())
            {
                StorageMap TransferFromList = Storage.CurrentContext.CreateMap(StorageKeys.WhiteListedTransferFromList());

                bool isWhiteListed = TransferFromList.Get(address).AsString() == "1";
                Runtime.Notify("IsContractWhitelistedTransferFrom() address, isWhiteListed", address, isWhiteListed);
                return isWhiteListed;
            }

            return true;
        }

        /// <summary>
        /// determine if TransferFrom whitelisting is enabled for this contract or not
        /// </summary>
        /// <returns></returns>
        public static bool IsTransferFromWhitelistingEnabled()
        {
            bool isEnabled = Storage.Get(Storage.CurrentContext, StorageKeys.WhiteListTransferFromSettingChecked()).AsString() == "1";
            Runtime.Notify("IsTransferFromWhitelistingEnabled isEnabled", isEnabled);
            return isEnabled;
        }

        /// <summary>
        /// test that args contains the number of required args
        /// </summary>
        /// <param name="args">arguments provided to contract</param>
        /// <param name="numArgs">how many args we expect</param>
        /// <returns></returns>
        public static bool RequireArgumentLength(object[] args, int numArgs)
        {
            return args.Length == numArgs;
        }

        /// <summary>
        /// set the amount of tokens that can be transferred by a specific address
        /// </summary>
        /// <param name="transferApprovalKey"></param>
        /// <param name="amount"></param>
        public static void SetAllowanceAmount(byte[] transferApprovalKey, BigInteger amount)
        {
            if (transferApprovalKey.Length != 40)
            {
                Runtime.Log("SetAllowanceAmount() invalid transferApprovalKey.Length");
                return;
            }

            StorageMap allowances = Storage.CurrentContext.CreateMap(StorageKeys.TransferAllowancePrefix());

            if (amount == 0)
            {
                // originator is revoking approval for transfer - delete the authorisation
                allowances.Delete(transferApprovalKey);
            }
            else
            {
                // authorisations are not incrememntal
                allowances.Put(transferApprovalKey, amount);
            }
        }

        /// <summary>
        /// update the balance of an address - used to ensure maxContribution isn't exceeded
        /// </summary>
        /// <param name="address"></param>
        /// <param name="newBalance"></param>
        public static void SetBalanceOfSaleContribution(byte[] address, BigInteger newBalance)
        {
            if (address.Length != 20)
            {
                Runtime.Log("SetBalanceOfSaleContribution() address.length != 20");
                return;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(StorageKeys.ContributionBalancePrefix());

            if (newBalance <= 0)
            {
                balances.Delete(address);
            }
            else
            {
                Runtime.Notify("SetBalanceOfSaleContribution() setting balance", newBalance);
                balances.Put(address, newBalance);
            }
        }

        /// <summary>
        /// update the balance of an address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="newBalance"></param>
        public static void SetBalanceOf(byte[] address, BigInteger newBalance)
        {
            if (address.Length != 20)
            {
                Runtime.Log("SetBalanceOf() address.length != 20");
                return;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(StorageKeys.BalancePrefix());

            if (newBalance <= 0)
            {
                balances.Delete(address);
            }
            else
            {
                Runtime.Notify("SetBalanceOf() setting balance", newBalance);
                balances.Put(address, newBalance);
            }
        }

        /// <summary>
        /// update the balance of an address to include any tokens subject to a vesting period
        /// </summary>
        /// <param name="address"></param>
        /// <param name="newBalance"></param>
        public static void SetBalanceOfVestedAmount(byte[] address, BigInteger newBalance, string allocationType)
        {
            if (address.Length != 20)
            {
                Runtime.Log("SetBalanceOfVestedAmount() address.length != 20");
                return;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(StorageKeys.VestedBalancePrefix());

            if (newBalance <= 0)
            {
                balances.Delete(address);
            }
            else
            {
                Runtime.Notify("SetBalanceOfVestedAmount() setting balance", newBalance, allocationType);
                balances.Put(address, newBalance);
            }
        }

        /// <summary>
        /// a new token is minted, set the total supply value
        /// </summary>
        /// <param name="newlyMintedTokens">the number of tokens to add to the total supply</param>
        public static void SetTotalSupply(BigInteger newlyMintedTokens)
        {
            BigInteger currentTotalSupply = NEP5.TotalSupply();
            Runtime.Notify("SetTotalSupply() setting new totalSupply", newlyMintedTokens + currentTotalSupply);

            Storage.Put(Storage.CurrentContext, StorageKeys.TokenTotalSupply(), currentTotalSupply + newlyMintedTokens);
        }

        /// <summary>
        /// verify the contract invoker is the defined administrator
        /// </summary>
        /// <returns></returns>
        public static bool VerifyIsAdminAccount()
        {
            if (ContractInitialised())
            {
                return VerifyWitness(Storage.Get(Storage.CurrentContext, StorageKeys.ContractAdmin()));
            }
            else
            {
                return VerifyWitness(ICOTemplate.InitialAdminAccount);
            }

        }

        /// <summary>
        ///  verify that the witness (invocator) is valid
        /// </summary>
        /// <param name="verifiedAddress">known good address to compare with invocator</param>
        /// <returns>true if account was verified</returns>
        public static bool VerifyWitness(byte[] verifiedAddress)
        {
            return Runtime.CheckWitness(verifiedAddress);
        }

        /// <summary>
        /// determine if the contract has been previously initialised
        /// </summary>
        /// <returns></returns>
        public static bool ContractInitialised()
        {
            return Storage.Get(Storage.CurrentContext, StorageKeys.ContractInitTime()).AsBigInteger() > 0;
        }

    }

}
