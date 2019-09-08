using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;

namespace Neo.SmartContract
{
    public class KYC : Framework.SmartContract
    {
        public static string[] GetKYCMethods() => new string[] {
            "AddAddress",
            "crowdsale_status",
            "GetBlockHeight",
            "GetGroupMaxContribution",
            "GetGroupNumber",
            "GetGroupUnlockTime",
            "GroupParticipationIsUnlocked",
            "RevokeAddress",
        };

        public static object HandleKYCOperation(string operation, params object[] args)
        {
            // neo-compiler doesn't support switch blocks with too many case statements due to c# compiler optimisations
            // * IL_0004 Call System.UInt32 <PrivateImplementationDetails>::ComputeStringHash(System.String) ---> System.Exception: not supported on neovm now.
            // therefore, extra if statements required for more than 6 operations
            if (operation == "crowdsale_status")
            {
                // test if an address is whitelisted
                if (!Helpers.RequireArgumentLength(args, 1))
                {
                    return false;
                }
                return AddressIsWhitelisted((byte[])args[0]);
            }
            else if (operation == "GetGroupNumber")
            {
                // allow people to check which group they have been assigned to during the whitelist process
                if (!Helpers.RequireArgumentLength(args, 1))
                {
                    return false;
                }
                return GetWhitelistGroupNumber((byte[])args[0]);
            }
            else if (operation == "GroupParticipationIsUnlocked")
            {
                // allow people to check if their group is unlocked (bool)
                if (!Helpers.RequireArgumentLength(args, 1))
                {
                    return false;
                }
                return GroupParticipationIsUnlocked((int)args[0]);
            } else if (operation == "GetBlockHeight")
            {
                // expose a method to retrieve current block height
                return Blockchain.GetHeight();
            }

            return false;
        }
       

        /// <summary>
        /// determine if the given address is whitelisted by testing if group number > 0
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool AddressIsWhitelisted(byte[] address)
        {
            if (address.Length != 20)
            {
                return false;
            }

            BigInteger whitelisted = GetWhitelistGroupNumber(address);
            return whitelisted > 0;
        }


        /// <summary>
        /// helper method to retrieve the stored group unlock block height
        /// </summary>
        /// <param name="groupNumber"></param>
        /// <returns></returns>
        public static uint GetGroupUnlockTime(BigInteger groupNumber)
        {
            BigInteger unlockTime = 0;

            if (groupNumber <= 0 || groupNumber > 4)
            {
                return 0;
            }
            else if (groupNumber > 0 && groupNumber <= 4)
            {
                unlockTime = (uint)ICOTemplate.PresaleStartTime();
            }
            return (uint)unlockTime;
        }

        /// <summary>
        /// retrieve the group number the whitelisted address is in
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static BigInteger GetWhitelistGroupNumber(byte[] address)
        {
            if (address.Length != 20)
            {
                return 0;
            }

            StorageMap kycWhitelist = Storage.CurrentContext.CreateMap(StorageKeys.KYCWhitelistPrefix());
            return kycWhitelist.Get(address).AsBigInteger();
        }

        /// <summary>
        /// determine if groupNumber is eligible to participate in public sale yet
        /// </summary>
        /// <param name="groupNumber"></param>
        /// <returns></returns>
        public static bool GroupParticipationIsUnlocked(int groupNumber)
        {
            if (groupNumber <= 0)
            {
                return false;
            }

            uint unlockBlockTime = GetGroupUnlockTime(groupNumber);
            return unlockBlockTime > 0 && unlockBlockTime <= Helpers.GetBlockTimestamp();
        }

        /// <summary>
        /// remove an address from the whitelist
        /// </summary>
        /// <param name="address"></param>
        public static bool RevokeAddress(byte[] address)
        {
            if (address.Length != 20)
            {
                return false;
            }

            if (Helpers.VerifyWitness(ICOTemplate.KycMiddlewareKey))
            {
                StorageMap kycWhitelist = Storage.CurrentContext.CreateMap(StorageKeys.KYCWhitelistPrefix());
                kycWhitelist.Delete(address);
                return true;
            }
            return false;
        }

      
    }
}
