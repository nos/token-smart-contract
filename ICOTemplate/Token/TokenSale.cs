using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;


namespace Neo.SmartContract
{
    public class TokenSale : Framework.SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> transfer;
        
     
        /// <summary>
        /// set a vesting schedule, as defined in the whitepaper, for tokens purchased during the presale
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tokenBalance"></param>
        /// <returns></returns>
        public static bool SetVestingPeriodForAddress(byte[] address, string allocationType, BigInteger tokensPurchased)
        {
            if (allocationType != "incentive" && allocationType != "privateSale" && allocationType != "company")
            {
                return false;
            }

            if (address.Length != 20)
            {
                return false;
            }

            BigInteger currentAvailableBalance = 0;        // how many tokens will be immediately available to the owner

            uint contractInitTime = Helpers.GetContractInitTime();
            uint currentTimestamp = Helpers.GetBlockTimestamp();
            StorageMap vestingData = Storage.CurrentContext.CreateMap(StorageKeys.VestedTokenPrefix());
            uint initialReleaseDate = 0;
            uint releaseFrequency = 0;
            object[] vestingObj = new object[0];

            if (allocationType == "incentive")
            {
                vestingObj = ICOTemplate.VestingIncentive();
                initialReleaseDate = (uint)vestingObj[0] + contractInitTime;
                releaseFrequency = (uint)vestingObj[1];
            }
            else if (allocationType == "privateSale")
            {
                vestingObj = ICOTemplate.VestingPrivateSale();
                initialReleaseDate = contractInitTime;
                releaseFrequency = (uint)vestingObj[0];
            }
            else if (allocationType == "company")
            {
                vestingObj = ICOTemplate.VestingCompany();
                initialReleaseDate = (uint)vestingObj[0] + contractInitTime;
                releaseFrequency = (uint)vestingObj[0];
            }

            object[] releasePeriod = new object[4];

            releasePeriod[0] = initialReleaseDate;
            releasePeriod[1] = initialReleaseDate + releaseFrequency;
            releasePeriod[2] = initialReleaseDate + (releaseFrequency * 2);
            releasePeriod[3] = initialReleaseDate + (releaseFrequency * 3);

            // calculate how much should be released
            BigInteger releaseAmount = tokensPurchased * ICOTemplate.DistributionPercentage() / 100;
            object[] lockoutTimes = new object[] { releasePeriod[0], releaseAmount, releasePeriod[1], releaseAmount, releasePeriod[2], releaseAmount, releasePeriod[3], releaseAmount };
            vestingData.Put(address, lockoutTimes.Serialize());

            // ensure the total amount purchased is saved
            Helpers.SetBalanceOf(address, tokensPurchased);
            Helpers.SetBalanceOfVestedAmount(address, tokensPurchased, allocationType);
            transfer(null, address, tokensPurchased);

            return true;
        }

        /// <summary>
        /// return the amount of tokens that are subject to vesting period
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static BigInteger SubjectToVestingPeriod(byte[] address)
        {
            BigInteger amountSubjectToVesting = 0;

            if (address.Length != 20)
            {
                return amountSubjectToVesting;
            }

            object[] tokensVesting = PublicTokensLocked(address);
            uint currentTimestamp = Helpers.GetBlockTimestamp();

            if (tokensVesting.Length > 0)
            {
                // this account has some kind of vesting period
                for (int i = 0; i < tokensVesting.Length; i++)
                {
                    int j = i + 1;
                    uint releaseDate = (uint)tokensVesting[i];
                    BigInteger releaseAmount = (BigInteger)tokensVesting[j];

                    if (currentTimestamp < releaseDate)
                    {
                        // the release date has not yet occurred. add the releaseAmount to the balance
                        amountSubjectToVesting += releaseAmount;
                    }
                    i++;
                }
            }

            return amountSubjectToVesting;
        }

        /// <summary>
        /// will return an array of token release dates if the user purchased in excess of the defined amounts
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static object[] PublicTokensLocked(byte[] address)
        {
            StorageMap vestingData = Storage.CurrentContext.CreateMap(StorageKeys.VestedTokenPrefix());
            byte[] storedData = vestingData.Get(address);

            if (storedData.Length > 0)
            {
                return (object[])storedData.Deserialize();
            }
            return new object[] { };
        }

        /// <summary>
        /// Claims unsold tokens
        /// </summary>
        /// <returns></returns>
        public static bool ClaimUnsoldTokens()
        {

            bool UnsoldTokensClaimed = Storage.Get(Storage.CurrentContext, StorageKeys.UnsoldTokensClaimed()).AsString() == "1";
            
            //This method can only be executed by the admin account, after the public sale, and can only be called once (use UnsoldTokensClaimed() storage item)
            if (Helpers.GetBlockTimestamp() >= ICOTemplate.PublicSaleEndTime() && UnsoldTokensClaimed == false && Helpers.VerifyIsAdminAccount())
            {
                byte[] address = ICOTemplate.AdditionalCompanyTokenFund;

                //Get amount remaining
                BigInteger amountRemaining = NEP5.CrowdsaleAvailableAmount();    
                
                //Add vested amount to account
                TokenSale.SetVestingPeriodForAddress(address, "company", amountRemaining);

                //Set total supply
                Helpers.SetTotalSupply(amountRemaining);

                //Set the UnsoldTokensClaimed() storage item so ClaimUnsoldTokens() cannot be called again
                Storage.Put(Storage.CurrentContext, StorageKeys.UnsoldTokensClaimed(), "1");

                transfer(null, address, amountRemaining);

                Runtime.Notify("ClaimUnsoldTokens() tokens allocated", address, amountRemaining);

                return true;
            }

            return false;

        }

    }
}
