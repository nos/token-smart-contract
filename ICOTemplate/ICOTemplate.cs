using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System.Numerics;

namespace Neo.SmartContract
{
    /// <summary>
    /// Updated ico template for the neo ecosystem
    /// </summary>
    public class ICOTemplate : Framework.SmartContract
    {
        ///<remarks>
        /// START user configurable fields
        ///</remarks>

        /// <summary>
        /// this is the initial admin account responsible for initialising the contract (reversed byte array of contract address)
        /// </summary>
        public static readonly byte[] InitialAdminAccount = { 172, 93, 207, 177, 41, 141, 8, 175, 19, 221, 90, 238, 233, 67, 54, 204, 47, 232, 62, 57 };

        /// <summary>
        /// Ethereum contributions middleware account
        /// </summary>
        public static readonly byte[] EthContributionListenerKey = { 216, 6, 188, 207, 10, 57, 209, 140, 176, 193, 128, 149, 72, 222, 4, 133, 135, 248, 79, 46 };
        // public static readonly byte[] EthContributionListenerKey = { 154, 25, 242, 137, 130, 96, 225, 205, 40, 171, 119, 160, 49, 166, 37, 244, 55, 76, 0, 62 };

        /// <summary>
        /// KYC middleware account
        /// </summary>
        public static readonly byte[] KycMiddlewareKey = { 149, 67, 119, 140, 241, 7, 126, 51, 16, 168, 205, 237, 225, 161, 64, 117, 68, 101, 182, 197 };

        /// <summary>
        ///  Company account to get 10,000,000 immediate tokens on InitSmartContract()
        /// </summary>
        public static readonly byte[] NosProjectKey = { 163, 78, 249, 186, 149, 73, 242, 165, 255, 174, 25, 102, 234, 143, 189, 222, 71, 131, 159, 32 };

        /// <summary>
        ///  Company account to get unsold tokens after the public sale
        /// </summary>
        public static readonly byte[] AdditionalCompanyTokenFund = { 249, 85, 33, 169, 71, 161, 147, 205, 102, 214, 123, 138, 241, 93, 53, 1, 184, 112, 172, 1 };

        /// <summary>
        /// NEP5.1 definition: the name we will give our token
        /// </summary>
        public static string TokenName() => "nOS";

        /// <summary>
        /// NEP5.1 definition: the trading symbol we will give our NEP5.1 token
        /// </summary>
        public static string TokenSymbol() => "NOS";

        /// <summary>
        /// NEP5.1 definition: the number of tokens that can be minted
        /// </summary>
        public const ulong TokenMaxSupply = 375000000;

        /// <summary>
        /// this is the default maximum amount of NOS that can be purchased during the public sale per user
        /// </summary>
        /// <returns></returns>
        public static ulong MaximumContributionAmount() => 92064;

        /// <summary>
        /// does your ICO accept NEO for payments?
        /// </summary>
        public static bool ICOAllowsNEO() => true;

        /// <summary>
        /// how many tokens will you get for a unit of neo
        /// </summary>
        /// <returns></returns>
        public static ulong ICONeoToTokenExchangeRate() => 168;

        /// <summary>
        /// does your ICO accept GAS for payments?
        /// </summary>
        public static bool ICOAllowsGAS() => false;

        /// <summary>
        /// how many tokens will you get for a unit of gas
        /// </summary>
        /// <returns></returns>
        public static ulong ICOGasToTokenExchangeRate() => 65;

        /// <summary>
        /// does your ICO accept GAS for payments?
        /// </summary>
        public static bool ICOAllowsETH() => true;

        /// <summary>
        /// ETH minimum contribution amount (0.1 ETH)
        /// </summary>
        public static ulong EthMinimumContribution() => 100000000000000000;

        /// <summary>
        /// how many tokens will you get for 1 ETH
        /// </summary>
        /// <returns></returns>
        public static ulong ICOEthToTokenExchangeRate() => 2066;

        /// <summary>
        /// tokens that fall under the 'incentive' bracket  will be subject to a vesting period of 2 years (25% unlocked after 1 year, then even distribution per 4 months)
        /// </summary>
         public static object[] VestingIncentive() => new object[] { 31536000, 10512000 };
        /// public static object[] VestingIncentive() => new object[] { 10800, 300 };

         /// <summary>
        /// tokens that fall under the 'privateSale' bracket  will be subject to a vesting period of 9 months (25% unlocked immediately, then even distribution per 3 months)
        /// </summary>
        public static object[] VestingPrivateSale() => new object[] { 7889400 };
        /// public static object[] VestingPrivateSale() => new object[] { 10800 };

        /// <summary>
        /// tokens that fall under the 'company' bracket  will be subject to a vesting period of 1 years (25% unlocked after 3 months, then even distribution per 3 months)
        /// </summary>
        public static object[] VestingCompany() => new object[] { 7889400 };
        /// public static object[] VestingCompany() => new object[] { 10800 };

        /// <summary>
        /// Define that 25% is released with each distribution period from the Vesting objects.
        /// </summary>
        /// <returns></returns>
        public static ulong DistributionPercentage() => 25;

        /// <summary>
        /// Tier 4 max allocation
        /// </summary>
        public static ulong PresaleTierFour() => 16968;

        /// <summary>
        /// Presale tier 3 max allocation
        /// </summary>
        public static ulong PresaleTierThree() => 27552;

        /// <summary>
        /// Presale tier 2 max allocation
        /// </summary>
        public static ulong PresaleTierTwo() => 46704;

        /// <summary>
        /// Presale tier 1 max allocation
        /// </summary>
        public static ulong PresaleTierOne() => (ulong)MaximumContributionAmount();

        /// <summary>
        /// Presale start time
        /// </summary>
        public static ulong PresaleStartTime() => 1540836000;

        /// <summary>
        /// Presale duration
        /// </summary>
        /// public static ulong PresaleDuration() => 900;

        /// <summary>
        /// Presale end time
        /// </summary>
        public static ulong PresaleEndTime() => 1541091600;

        /// <summary>
        /// Timestamp that public sale phase starts (1 hour after PresaleEndTime)
        /// </summary>
        public static ulong PublicSaleStartTime() => 1541095200;

        /// <summary>
        /// Public sale duration
        /// </summary>
        /// public static ulong PublicSaleDuration() => 900;

        /// <summary>
        /// Public sale end time
        /// </summary>
        public static ulong PublicSaleEndTime() => 1541700000;

        /// <summary>
        /// 22,500,000 allocated to angel phase + 67,500,000 of tokens allocated to private presale + 100,000,000 allocated to locked token incentive + 25,000,000 (Vested Company Reserve) + 50,000,000 (Ecosystem Adoption Reserve) = 265000000 total vested tokens
        /// </summary>
        /// <returns></returns>
        public static ulong LockedTokenAllocationAmount() => 265000000;

        /// <summary>
        /// 10,000,000 tokens are immediately available
        /// </summary>
        public static ulong ImmediateCompanyReserve() => 10000000;

        /// <summary>
        /// list NEPs supported by this contract
        /// </summary>
        /// <returns></returns>
        public static string SupportedStandards() => "{\"NEP-5\", \"NEP-10\"}";

        /// <summary>
        /// should whitelisting of TransferFrom transfer/transferFrom methods be checked
        /// </summary>
        /// <returns></returns>
        public static bool WhitelistTransferFromListings() => true;

        ///<remarks>
        /// END user configurable fields
        ///</remarks>

        /// <summary>
        /// the entry point for smart contract execution
        /// </summary>
        /// <param name="operation">string to determine execution operation performed</param>
        /// <param name="args">optional arguments, context specific depending on operation</param>
        /// <returns></returns>
        public static object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {

                // test if a nep5 method is being invoked
                foreach (string nepMethod in NEP5.GetNEP5Methods())
                {
                    if (nepMethod == operation)
                    {
                        return NEP5.HandleNEP5Operation(operation, args, ExecutionEngine.CallingScriptHash, ExecutionEngine.EntryScriptHash);
                    }
                }

                if (operation == "admin" && Helpers.VerifyIsAdminAccount())
                {
                    // allow access to administration methods
                    string adminOperation = (string)args[0];
                    foreach (string adminMethod in Administration.GetAdministrationMethods())
                    {
                        if (adminMethod == adminOperation)
                        {
                            return Administration.HandleAdministrationOperation(adminOperation, args);
                        }
                    }
                    return false;
                }

                // test if a kyc method is being invoked
                foreach (string kycMethod in KYC.GetKYCMethods())
                {
                    if (kycMethod == operation)
                    {
                        return KYC.HandleKYCOperation(operation, args);
                    }
                }

                // test if a helper/misc method is being invoked
                foreach (string helperMethod in Helpers.GetHelperMethods())
                {
                    if (helperMethod == operation)
                    {
                        return Helpers.HandleHelperOperation(operation, args);
                    }
                }

            }
            else if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Helpers.VerifyIsAdminAccount())
                {
                    return true;
                }
            }
        
            return false;
        }

    }
}
