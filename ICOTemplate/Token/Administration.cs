using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;
using System;
using System.ComponentModel;

namespace Neo.SmartContract
{
    public class Administration : Framework.SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> transfer;

        public static string[] GetAdministrationMethods() => new string[] {
            "AllocatePrivateSalePurchase",
            "LockPrivateSaleAllocation",
            "ContractMigrate",
            "EnableTransferFromWhitelisting",
            "UpdateAdminAddress",
            "WhitelistTransferFromAdd",
            "WhitelistTransferFromRemove",
            "ClaimUnsoldTokens"
        };

        public static object HandleAdministrationOperation(string operation, params object[] args)
        {
            if (operation == "WhitelistTransferFromRemove")
            {
                if (!Helpers.RequireArgumentLength(args, 2))
                {
                    return false;
                }

                return WhitelistTransferFromRemove((byte[])args[1]);
            }
            else if (operation == "WhitelistTransferFromAdd")
            {
                if (!Helpers.RequireArgumentLength(args, 2))
                {
                    return false;
                }
                return WhitelistTransferFromAdd((byte[])args[1]);
            } else if (operation == "EnableTransferFromWhitelisting")
            {
                if (!Helpers.RequireArgumentLength(args, 2))
                {
                    return false;
                }
                EnableTransferFromWhitelisting((bool)args[1]);
            } else if (operation == "ClaimUnsoldTokens")
            {
                if (!Helpers.RequireArgumentLength(args,1))
                {
                    return false;
                }
                TokenSale.ClaimUnsoldTokens();
            }


            switch (operation)
            {
                case "AllocatePrivateSalePurchase":
                    if (!Helpers.RequireArgumentLength(args, 4))
                    {
                        return false;
                    }
                    return AllocatePrivateSalePurchase((byte[])args[1], (string)args[2], (BigInteger)args[3]);
                case "ContractMigrate":
                    if (!Helpers.RequireArgumentLength(args, 10))
                    {
                        return false;
                    }
                    return ContractMigrate(args);
                case "LockPrivateSaleAllocation":
                    return LockPrivateSaleAllocation();
                case "UpdateAdminAddress":
                    if (!Helpers.RequireArgumentLength(args, 2))
                    {
                        return false;
                    }
                    return UpdateAdminAddress((byte[])args[1]);
            }

            return false;
        }

        /// <summary>
        /// allow allocation of presale purchases by contract administrator. this allows the nOS team to allocate the nOS tokens from the private sale, company reserve, and locked incentive reserve.
        /// This method will not allow the private allocations to exceed the defined amount
        /// the state of the `LockPrivateSaleAllocation` can be determined by the public using the method `IsPrivateSaleAllocationLocked` (returns timestamp that lock was put in place)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="amountPurchased"></param>
        /// <returns></returns>
        public static bool AllocatePrivateSalePurchase(byte[] address, string allocationType, BigInteger amountPurchased)
        {
            amountPurchased = amountPurchased * NEP5.factor;

            bool privateSaleLocked = Storage.Get(Storage.CurrentContext, StorageKeys.PrivateSaleAllocationLocked()).AsBigInteger() > 0;
            if (privateSaleLocked)
            {
                Runtime.Notify("AllocatePrivateSalePurchase() privateSaleLocked, can't allocate");
                return false;
            }

            if (allocationType != "incentive" && allocationType != "privateSale" && allocationType != "company")
            {
                return false;
            }

            BigInteger presaleAllocationMaxValue = ICOTemplate.LockedTokenAllocationAmount() * NEP5.factor;
            BigInteger presaleAllocatedValue = Storage.Get(Storage.CurrentContext, StorageKeys.PresaleAllocatedValue()).AsBigInteger();

            if ((presaleAllocatedValue + amountPurchased) > presaleAllocationMaxValue)
            {
                // this purchase will exceed the presale cap.. dont allow
                Runtime.Notify("AllocatePrivateSalePurchase() purchase will exceed max allocation");
                return false;
            }

            if(!TokenSale.SetVestingPeriodForAddress(address, allocationType, amountPurchased))
            {
                Runtime.Notify("SetVestingPeriodForAddress() failed.");
                return false;
            }

            Storage.Put(Storage.CurrentContext, StorageKeys.PresaleAllocatedValue(), presaleAllocatedValue + amountPurchased);
            transfer(null, address, amountPurchased);

            Runtime.Notify("AllocatePrivateSalePurchase() tokens allocated", address, amountPurchased, allocationType);

            return true;
        }

        /// <summary>
        /// once initial presale allocation completed perform lock that prevents allocation being used
        /// </summary>
        /// <returns></returns>
        public static bool LockPrivateSaleAllocation()
        {
            Runtime.Log("LockPrivateSaleAllocation() further presale allocations locked");
            Storage.Put(Storage.CurrentContext, StorageKeys.PrivateSaleAllocationLocked(), Helpers.GetBlockTimestamp());
            return true;
        }

        /// <summary>
        /// allow contract administrator to migrate the storage of this contract
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool ContractMigrate(object[] args)
        {
            // Contract Migrate(byte[] script, byte[] parameter_list, byte return_type, ContractPropertyState (using storage), string name, string version, string author, string email, string description)
            Contract.Migrate((byte[])args[1], (byte[])args[2], (byte)args[3], (ContractPropertyState)args[4], (string)args[5], (string)args[6], (string)args[7], (string)args[8], (string)args[9]);
            return true;
        }
        
        /// <summary>
        /// allow the contract administrator to update the admin address
        /// </summary>
        /// <param name="newAdminAddress"></param>
        /// <returns></returns>
        public static bool UpdateAdminAddress(byte[] newAdminAddress)
        {
            if(newAdminAddress.Length != 20)
            {
                return false;
            }

            Storage.Put(Storage.CurrentContext, StorageKeys.ContractAdmin(), newAdminAddress);
            return true;
        }

        /// <summary>
        /// allow admin to toggle TransferFrom whitelist on or off
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        public static bool EnableTransferFromWhitelisting(bool isEnabled)
        {
            Storage.Put(Storage.CurrentContext, StorageKeys.WhiteListTransferFromSettingChecked(), isEnabled ? "1" : "0");
            return true;
        }

        /// <summary>
        /// add a TransferFrom contract address to the whitelist
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool WhitelistTransferFromAdd(byte[] address)
        {
            if (address.Length != 20)
            {
                return false;
            }

            StorageMap TransferFromList = Storage.CurrentContext.CreateMap(StorageKeys.WhiteListedTransferFromList());
            TransferFromList.Put(address, "1");
            Runtime.Notify("WhitelistTransferFromAdd() added contract to whitelist", address);

            return true;
        }

        /// <summary>
        /// remove a TransferFrom contract address from the whitelist
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool WhitelistTransferFromRemove(byte[] address)
        {
            if (address.Length != 20)
            {
                return false;
            }

            StorageMap TransferFromList = Storage.CurrentContext.CreateMap(StorageKeys.WhiteListedTransferFromList());
            TransferFromList.Delete(address);
            Runtime.Notify("WhitelistTransferFromRemove() removed contract from whitelist", address);

            return true;
        }

    }
}
