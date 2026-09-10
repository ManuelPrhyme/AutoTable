using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Nethereum.Contracts;
using AutoTable.Models;

namespace AutoTable.Services
{
    public class BlockchainLicenseService
    {
        private static readonly Lazy<BlockchainLicenseService> _instance = new(() => new BlockchainLicenseService());
        public static BlockchainLicenseService Instance => _instance.Value;

        // Primary Sepolia RPC (verified working). The old https://rpc.sepolia.org
        // endpoint returns 404 for JSON-RPC POSTs and must not be used.
        public const string SepoliaRpcUrl = "https://ethereum-sepolia-rpc.publicnode.com";
        /// <summary>Fallback RPC used when the primary endpoint is unreachable or rate-limited.</summary>
        public const string SepoliaRpcUrlFallback = "https://1rpc.io/sepolia";
        /// <summary>Sepolia chain id (11155111) — required for locally signed EIP-155 transactions.</summary>
        public const long SepoliaChainId = 11155111;
        public string? LicenseContractAddress { get; set; }
        public string? RegistrationContractAddress { get; set; }

        private const string LicenseABI = @"[{""inputs"":[{""name"":""code"",""type"":""string""}],""name"":""activate"",""outputs"":[],""type"":""function""},{""inputs"":[{""name"":""instanceAddress"",""type"":""address""}],""name"":""isLicensed"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""inputs"":[{""name"":""code"",""type"":""string""},{""name"":""schoolAddress"",""type"":""address""}],""name"":""verifyCode"",""outputs"":[{""name"":""isValid"",""type"":""bool""},{""name"":""period"",""type"":""uint256""},{""name"":""gracePeriod"",""type"":""uint256""},{""name"":""codeExpiresAt"",""type"":""uint256""}],""type"":""function""}]";

        private const string RegistrationABI = @"[{""inputs"":[{""name"":""schoolName"",""type"":""string""},{""name"":""adminEmail"",""type"":""string""}],""name"":""registerSchool"",""outputs"":[],""type"":""function""}]";

        // Matches the tuple returned by verifyCode (bool, uint256, uint256, uint256).
        public class VerifyCodeResult
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("bool", "isValid", 1)]
            public bool IsValid { get; set; }
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "period", 2)]
            public Nethereum.Hex.HexTypes.HexBigInteger Period { get; set; } = new(0);
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "gracePeriod", 3)]
            public Nethereum.Hex.HexTypes.HexBigInteger GracePeriod { get; set; } = new(0);
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "codeExpiresAt", 4)]
            public Nethereum.Hex.HexTypes.HexBigInteger CodeExpiresAt { get; set; } = new(0);
        }

        // Matches the tuple returned by getLicenseStatus
        // (uint8 status, uint256 expiresAt, uint256 gracePeriodEndsAt, uint256 remaining).
        public class LicenseStatusResult
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint8", "status", 1)]
            public byte Status { get; set; }                                          // 0=not licensed, 1=active, 2=grace, 3=expired
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "expiresAt", 2)]
            public Nethereum.Hex.HexTypes.HexBigInteger ExpiresAt { get; set; } = new(0);
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "gracePeriodEndsAt", 3)]
            public Nethereum.Hex.HexTypes.HexBigInteger GracePeriodEndsAt { get; set; } = new(0);
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "remaining", 4)]
            public Nethereum.Hex.HexTypes.HexBigInteger Remaining { get; set; } = new(0);
        }

        // DTO for decoding the LicenseActivated event emitted by AutoSchool360.activate().
        // The event ABI must be present in LicenseABI for Nethereum to decode it.
        public class LicenseActivatedEventDTO : Nethereum.ABI.FunctionEncoding.Attributes.EventDTO
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "code", 1)]
            public string Code { get; set; } = string.Empty;

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "schoolAddress", 2, true)]
            public string SchoolAddress { get; set; } = string.Empty;

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "period", 3)]
            public Nethereum.Hex.HexTypes.HexBigInteger Period { get; set; } = new(0);

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "gracePeriod", 4)]
            public Nethereum.Hex.HexTypes.HexBigInteger GracePeriod { get; set; } = new(0);

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "expiresAt", 5)]
            public Nethereum.Hex.HexTypes.HexBigInteger ExpiresAt { get; set; } = new(0);

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "activatedAt", 6)]
            public Nethereum.Hex.HexTypes.HexBigInteger ActivatedAt { get; set; } = new(0);
        }

        /// <summary>
        /// Result of ActivateLicenseAsync — includes the transaction hash and the
        /// on-chain parameters extracted from the LicenseActivated event.
        /// </summary>
        public class ActivateLicenseResult
        {
            public string TransactionHash { get; set; } = string.Empty;
            public long PeriodSeconds { get; set; }
            public long GraceSeconds { get; set; }
            public long ExpiresAt { get; set; }
            public long ActivatedAt { get; set; }
        }

        // ABI fragment for decoding the LicenseActivated event (standalone — no functions needed).
        private const string LicenseActivatedEventABI = @"[{""anonymous"":false,""name"":""LicenseActivated"",""type"":""event"",""inputs"":[{""indexed"":false,""name"":""code"",""type"":""string""},{""indexed"":true,""name"":""schoolAddress"",""type"":""address""},{""indexed"":false,""name"":""period"",""type"":""uint256""},{""indexed"":false,""name"":""gracePeriod"",""type"":""uint256""},{""indexed"":false,""name"":""expiresAt"",""type"":""uint256""},{""indexed"":false,""name"":""activatedAt"",""type"":""uint256""}]}]";

        // ABI fragment for the getLicenseStatus view (standalone).
        private const string LicenseStatusABI = @"[{""inputs"":[{""name"":""instanceAddress"",""type"":""address""}],""name"":""getLicenseStatus"",""outputs"":[{""name"":""status"",""type"":""uint8""},{""name"":""expiresAt"",""type"":""uint256""},{""name"":""gracePeriodEndsAt"",""type"":""uint256""},{""name"":""remaining"",""type"":""uint256""}],""type"":""function""}]";

        private BlockchainLicenseService() { }

        /// <summary>
        /// Builds a Web3 client with the instance's private key attached so that
        /// transactions are SIGNED LOCALLY (EIP-155, chain id 11155111) instead of
        /// relying on the public RPC node — public nodes never hold your private key
        /// and reject eth_sendTransaction.
        /// </summary>
        private Nethereum.Web3.Web3 CreateSendWeb3()
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) throw new InvalidOperationException("No key pair available.");
            var account = new Nethereum.Web3.Accounts.Account(key.PrivateKey, SepoliaChainId);
            return new Nethereum.Web3.Web3(account, SepoliaRpcUrl);
        }

        public void Initialize(string licenseAddr, string regAddr)
        {
            LicenseContractAddress = licenseAddr;
            RegistrationContractAddress = regAddr;
        }

        public async Task<string> RegisterSchoolAsync(string schoolName, string adminEmail)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) throw new InvalidOperationException("No key pair available.");

            var web3 = CreateSendWeb3();
            var contract = web3.Eth.GetContract(RegistrationABI, RegistrationContractAddress);
            var fn = contract.GetFunction("registerSchool");
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

        public async Task<ActivateLicenseResult> ActivateLicenseAsync(string activationCode)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) throw new InvalidOperationException("No key pair available.");

            var web3 = CreateSendWeb3();
            var contract = web3.Eth.GetContract(LicenseABI, LicenseContractAddress);
            var fn = contract.GetFunction("activate");
            var gas = await fn.EstimateGasAsync(activationCode);

            // 1. Send the transaction and WAIT for it to be mined.
            var receipt = await fn.SendTransactionAndWaitForReceiptAsync(
                key.InstanceAddress,
                new Nethereum.Hex.HexTypes.HexBigInteger(gas.Value),
                null,
                null,
                activationCode);

            // 2. Verify the LicenseActivated event was actually emitted in the receipt.
            //    SendTransactionAndWaitForReceiptAsync confirms the tx is mined, but we
            //    also assert the event log exists so the caller receives on-chain params.
            var eventContract = web3.Eth.GetContract(LicenseActivatedEventABI, LicenseContractAddress);
            var eventHandler = eventContract.GetEvent("LicenseActivated");
            var events = eventHandler.DecodeAllEventsForEvent<LicenseActivatedEventDTO>(receipt.Logs);

            if (events.Count == 0)
                throw new InvalidOperationException(
                    "LicenseActivated event not found in transaction receipt. " +
                    "The transaction may have reverted or the event was not emitted.");

            // 3. Extract on-chain parameters from the event (uint256 → HexBigInteger → long).
            var evt = events[0].Event;
            return new ActivateLicenseResult
            {
                TransactionHash = receipt.TransactionHash,
                PeriodSeconds = long.Parse(evt.Period.Value.ToString()),
                GraceSeconds = long.Parse(evt.GracePeriod.Value.ToString()),
                ExpiresAt = long.Parse(evt.ExpiresAt.Value.ToString()),
                ActivatedAt = long.Parse(evt.ActivatedAt.Value.ToString())
            };
        }

        /// <summary>
        /// Static on-chain verification of an activation code for a specific
        /// instance. Returns (isValid, periodSeconds, graceSeconds, codeExpiresAtSeconds).
        /// </summary>
        public async Task<(bool Valid, long PeriodSeconds, long GraceSeconds, long CodeExpiresAt)> VerifyCodeAsync(string code, string instanceAddress)
        {
            try
            {
                var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
                var contract = web3.Eth.GetContract(LicenseABI, LicenseContractAddress);
                var fn = contract.GetFunction("verifyCode");

                var result = await fn.CallAsync<VerifyCodeResult>(code, instanceAddress);
                // HexBigInteger.Value → BigInteger → string → long (compile-safe across JDK.NET versions).
                return (
                    result.IsValid,
                    long.Parse(result.Period.Value.ToString()),
                    long.Parse(result.GracePeriod.Value.ToString()),
                    long.Parse(result.CodeExpiresAt.Value.ToString()));
            }
            catch
            {
                // Offline / not configured / contract not deployed → treat as invalid.
                return (false, 0, 0, 0);
            }
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
            var contract = web3.Eth.GetContract(LicenseStatusABI, LicenseContractAddress);
            var fn = contract.GetFunction("getLicenseStatus");
            var result = await fn.CallAsync<LicenseStatusResult>(key.InstanceAddress);
            // status: 0 = not licensed, 1 = active, 2 = grace period, 3 = expired.
            // Only return an expiry date when the license is still within its term (active or grace).
            if (result.Status == 0 || result.Status == 3 || result.ExpiresAt.Value == 0) return null;
            return DateTimeOffset.FromUnixTimeSeconds((long)result.ExpiresAt.Value).DateTime;
        }

        public async Task<decimal> GetBalanceAsync()
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return 0;

            try
            {
                var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrl);
                var balance = await web3.Eth.GetBalance.SendRequestAsync(key.InstanceAddress);
                return Nethereum.Web3.Web3.Convert.FromWei(balance.Value);
            }
            catch
            {
                // Primary endpoint down / rate-limited — retry once against the fallback.
                var web3 = new Nethereum.Web3.Web3(SepoliaRpcUrlFallback);
                var balance = await web3.Eth.GetBalance.SendRequestAsync(key.InstanceAddress);
                return Nethereum.Web3.Web3.Convert.FromWei(balance.Value);
            }
        }

        /// <summary>
        /// Result of RequestGasTokensAsync. Ok=true only when the faucet server
        /// RESPONDED after its drip transaction was mined with receipt status
        /// 'success' — i.e. the funds were actually SENT, not merely accepted.
        /// </summary>
        public class FaucetRequestResult
        {
            public bool Ok { get; set; }
            public string? Detail { get; set; }
        }

        /// <summary>
        /// Requests test gas from the faucet server. The server verifies the
        /// signed request, then the server's operator wallet calls
        /// requestFunds(instance) on the client's behalf and AWAITS the block
        /// confirmation (waitForTransactionReceipt + status 'success') BEFORE
        /// responding — so a successful (Ok) return is the authoritative signal
        /// that the funds have been sent to the instance's wallet.
        /// </summary>
        public async Task<FaucetRequestResult> RequestGasTokensAsync(string faucetApiUrl)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return new FaucetRequestResult { Ok = false, Detail = "No key pair available." };

            try
            {
                var message = $"Request gas for {key.InstanceAddress} at {DateTime.UtcNow:O}";
                var signature = key.SignMessage(message);

                using var client = new System.Net.Http.HttpClient();
                var request = new { InstanceAddress = key.InstanceAddress, Message = message, Signature = signature };
                var response = await client.PostAsJsonAsync($"{faucetApiUrl}/api/faucet/request", request);

                // The faucet backend only returns 2xx AFTER the drip transaction
                // has been mined with status 'success', so this is the on-chain
                // "funds sent" confirmation rather than just an HTTP acknowledgement.
                if (response.IsSuccessStatusCode)
                {
                    return new FaucetRequestResult { Ok = true, Detail = "Funds sent (confirmed on-chain)." };
                }

                return new FaucetRequestResult
                {
                    Ok = false,
                    Detail = $"Faucet server rejected the request (HTTP {response.StatusCode})."
                };
            }
            catch (Exception ex)
            {
                return new FaucetRequestResult { Ok = false, Detail = ex.Message };
            }
        }

        /// <summary>
        /// Polls the instance's Sepolia balance until it is &gt; 0. Performs one
        /// initial check followed by up to `retryCount` further checks spaced
        /// `intervalSeconds` apart (defaults: 7 retries at 5 seconds). Used to
        /// gate the registration transaction on the faucet drip actually landing
        /// in the instance wallet, even though the server already confirmed sending.
        /// </summary>
        public async Task<bool> WaitForFundingAsync(int retryCount = 7, int intervalSeconds = 5)
        {
            var key = KeyManager.Instance;
            if (!key.HasKey) return false;

            for (var attempt = 0; attempt <= retryCount; attempt++)
            {
                var balance = await GetBalanceAsync();
                if (balance > 0) return true;
                if (attempt < retryCount) await Task.Delay(intervalSeconds * 1000);
            }
            return false;
        }
    }
}
