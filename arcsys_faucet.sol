// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/// @title AutoSchool360Faucet
/// @notice Standalone test-ETH faucet — self-contained, no StringLib, no hex.
/// @dev Clients request test ETH by address. The backend server wallet can also
///      call requestFunds(instance) on a client's behalf — the client never pays
///      gas; the backend pays and the drip goes straight to the client.
contract AutoSchool360Faucet {
    address public owner;
    uint256 public dripAmount;
    uint256 public cooldown;
    uint256 public totalDistributed;
    uint256 public requestCount;

    mapping(address => uint256) public lastRequest;
    mapping(address => uint256) public requestCounts;

    event FundsRequested(address indexed instance, uint256 amount, uint256 timestamp);
    event FundsWithdrawn(address indexed to, uint256 amount, uint256 timestamp);
    event DripAmountChanged(uint256 oldAmount, uint256 newAmount, uint256 timestamp);
    event CooldownChanged(uint256 oldCooldown, uint256 newCooldown, uint256 timestamp);
    event ContractFunded(address indexed funder, uint256 amount, uint256 timestamp);

    modifier onlyOwner() { require(msg.sender == owner, "Only owner"); _; }

    constructor() {
        owner = msg.sender;
        dripAmount = 0.05 ether;
        cooldown = 1 days;
    }

    function requestFunds(address instance) external {
        require(instance != address(0), "Zero address");
        require(block.timestamp > lastRequest[instance] + cooldown, "Cooldown active");
        require(address(this).balance >= dripAmount, "Insufficient balance");

        lastRequest[instance] = block.timestamp;
        requestCounts[instance]++;
        requestCount++;
        totalDistributed += dripAmount;

        payable(instance).transfer(dripAmount);

        emit FundsRequested(instance, dripAmount, block.timestamp);
    }

    /// @dev Backend-delivery uses this same public requestFunds: the backend
    ///      wallet calls it with the client's address as the argument, paying
    ///      the gas while the drip is credited to the client.
    function batchRequestFunds(address[] calldata instances) external {
        for (uint256 i = 0; i < instances.length; i++) {
            address instance = instances[i];
            if (instance == address(0)) continue;
            if (block.timestamp <= lastRequest[instance] + cooldown) continue;
            if (address(this).balance < dripAmount) break;

            lastRequest[instance] = block.timestamp;
            requestCounts[instance]++;
            requestCount++;
            totalDistributed += dripAmount;

            payable(instance).transfer(dripAmount);
            emit FundsRequested(instance, dripAmount, block.timestamp);
        }
    }

    function setDripAmount(uint256 newAmount) external onlyOwner {
        require(newAmount > 0, "Zero amount");
        uint256 oldAmount = dripAmount;
        dripAmount = newAmount;
        emit DripAmountChanged(oldAmount, newAmount, block.timestamp);
    }

    function setCooldown(uint256 newCooldown) external onlyOwner {
        uint256 oldCooldown = cooldown;
        cooldown = newCooldown;
        emit CooldownChanged(oldCooldown, newCooldown, block.timestamp);
    }

    function withdraw(uint256 amount) external onlyOwner {
        require(amount <= address(this).balance, "Insufficient balance");
        payable(owner).transfer(amount);
        emit FundsWithdrawn(owner, amount, block.timestamp);
    }

    function withdrawAll() external onlyOwner {
        uint256 balance = address(this).balance;
        require(balance > 0, "No balance");
        payable(owner).transfer(balance);
        emit FundsWithdrawn(owner, balance, block.timestamp);
    }

    function getRequestCount(address instance) external view returns (uint256) {
        return requestCounts[instance];
    }

    function timeUntilNextRequest(address instance) external view returns (uint256) {
        uint256 nextAllowed = lastRequest[instance] + cooldown;
        if (block.timestamp >= nextAllowed) return 0;
        return nextAllowed - block.timestamp;
    }

    function getContractBalance() external view returns (uint256) {
        return address(this).balance;
    }

    function transferOwnership(address newOwner) external onlyOwner {
        require(newOwner != address(0), "Zero address");
        owner = newOwner;
    }

    receive() external payable {
        emit ContractFunded(msg.sender, msg.value, block.timestamp);
    }
}