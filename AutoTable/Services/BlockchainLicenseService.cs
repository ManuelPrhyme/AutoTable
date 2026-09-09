using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoTable.Models;

namespace AutoTable.Services
{
    public class BlockchainLicenseService
    {
        private static readonly Lazy<BlockchainLicenseService> _instance = new(() => new BlockchainLicenseService());
        public static BlockchainLicenseService Instance => _instance.Value;

        public const string SepoliaRpcUrl = "https://rpc.sepolia.org";
        public string? LicenseContractAddress { get; set; }
        public string? RegistrationContractAddress { get; set; }

        private const string LicenseABI = @"[{""inputs"":[{""name"":""code"",""type"":""}],""name"":""activate"",""outputs"":[],""type"":""function""},{""inputs"":[{""name"":""instance"",""type"":""address""}],""name"":""isLicensed"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""inputs"":[{""name"":""instance"",""type"":""address""}],""name"":""getExpiry"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""}]";

        private const string RegistrationABI = @"[{""inputs"":[{""name"":""schoolName"",""type"":""string""},{""name"":""adminEmail"",""type"":""string""}],""name"":""register"",""outputs"":[],""type"":""function""}]";

        private BlockchainLicenseService() { }

        public void Initialize(string licenseAddr, string regAddr)
        {
            LicenseContractAddress = licenseAddr;
            RegistrationContractAddress = regAddr;
        }

        public async Task<string> RegisterSchoolAsync(string schoolName, string adminEmail)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) throw new InvalidOperationException("No key pair available.");

            var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
            var contract = web3.Eth.GetContract(RegistrationABI, RegistrationContractAddress);
            var fn = contract.GetFunction("register");
            var gas = await fn.EstimateGasAsync(schoolName, adminEmail);
            var receipt = await fn.SendTransactionAndWaitForReceiptAsync(
                key.InstanceAddress,
                new Nethereum.Hex.HexTypes.HexBigInteger(gas.Value),
                null,
                null,
                schoolName,
                adminEmail);
            return receipt.TransactionHash;
        }

        public async Task<string> ActivateLicenseAsync(string activationCode)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) throw new InvalidOperationException("No key pair available.");

            var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
            var contract = web3.Eth.GetContract(LicenseABI, LicenseContractAddress);
            var fn = contract.GetFunction("activate");
            var gas = await fn.EstimateGasAsync(activationCode);
            var receipt = await fn.SendTransactionAndWaitForReceiptAsync(
                key.InstanceAddress,
                new Nethereum.Hex.HexTypes.HexBigInteger(gas.Value),
                null,
                null,
                activationCode);
            return receipt.TransactionHash;
        }

        public async Task<bool> IsLicensedAsync()
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return false;

            var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
            var contract = web3.Eth.GetContract(LicenseABI, LicenseContractAddress);
            var fn = contract.GetFunction("isLicensed");
            return await fn.CallAsync<bool>(key.InstanceAddress);
        }

        public async Task<DateTime?> GetExpiryAsync()
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return null;

            var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
            var contract = web3.Eth.GetContract(LicenseABI, LicenseContractAddress);
            var fn = contract.GetFunction("getExpiry");
            var result = await fn.CallAsync<Nethereum.Hex.HexTypes.HexBigInteger>(key.InstanceAddress);
            if (result.Value == 0) return null;
            return DateTimeOffset.FromUnixTimeSeconds((long)result.Value).DateTime;
        }

        public async Task<decimal> GetBalanceAsync()
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return 0;

            var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
            var balance = await web3.Eth.GetBalance.SendRequestAsync(key.InstanceAddress);
            return Nethereum.Web3.Web3.Convert.FromWei(balance.Value);
        }

        public async Task<bool> RequestGasTokensAsync(string faucetApiUrl)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return false;

            var message = $"Request gas for {key.InstanceAddress} at {DateTime.UtcNow:O}";
            var signature = key.SignMessage(message);

            using var client = new System.Net.Http.HttpClient();
            var request = new { InstanceAddress = key.InstanceAddress, Message = message, Signature = signature };
            var response = await client.PostAsJsonAsync($"{faucetApiUrl}/api/faucet/request", request);
            return response.IsSuccessStatusCode;
        }
    }
}
