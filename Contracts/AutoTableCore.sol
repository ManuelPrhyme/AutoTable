// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

contract AutoTableCore {
    address public vendor;

    struct SchoolInfo {
        string schoolName;
        string adminEmail;
        address schoolAddress;
        uint256 registeredAt;
        bool licenseActive;
        uint256 expiresAt;
        uint256 gracePeriod;
        string activationCode;
        bool exists;
    }

    struct ActivationCode {
        address school;
        uint256 period;
        uint256 gracePeriod;
        uint256 expiresAt;
        uint256 activatedAt;
        bool isActive;
        bool exists;
    }

    mapping(address => SchoolInfo) public schools;
    mapping(string => ActivationCode) public codes;
    mapping(address => string) public schoolCurrentCode;
    address[] public registeredSchools;
    string[] public generatedCodes;

    event SchoolRegistered(address indexed schoolAddress, string schoolName, string adminEmail, uint256 timestamp);
    event CodeGenerated(string code, address indexed schoolAddress, uint256 period, uint256 gracePeriod, uint256 codeExpiresAt, uint256 timestamp);
    event LicenseActivated(string code, address indexed schoolAddress, uint256 period, uint256 gracePeriod, uint256 expiresAt, uint256 activatedAt);
    event CodeDeactivated(string code, address indexed schoolAddress, uint256 timestamp);

    modifier onlyVendor() { require(msg.sender == vendor, "Only vendor"); _; }
    modifier onlySchool() { require(schools[msg.sender].exists, "Not registered"); _; }

    constructor() { vendor = msg.sender; }

    function registerSchool(string calldata n, string calldata e) external {
        require(!schools[msg.sender].exists, "Exists");
        schools[msg.sender] = SchoolInfo(n, e, msg.sender, block.timestamp, false, 0, 0, "", true);
        registeredSchools.push(msg.sender);
        emit SchoolRegistered(msg.sender, n, e, block.timestamp);
    }

    function generateCode(address sa, uint256 p, uint256 g) external onlyVendor returns (string memory c) {
        require(schools[sa].exists, "Not registered");
        c = StringLib.genCode(sa, generatedCodes.length, block.timestamp, block.prevrandao, msg.sender);
        codes[c] = ActivationCode(sa, p, g, block.timestamp + 365 days, 0, true, true);
        schoolCurrentCode[sa] = c;
        generatedCodes.push(c);
        emit CodeGenerated(c, sa, p, g, block.timestamp + 365 days, block.timestamp);
    }

    function activate(string calldata c) external onlySchool {
        ActivationCode storage cc = codes[c];
        require(cc.exists && cc.isActive && block.timestamp < cc.expiresAt, "Invalid");
        require(cc.school == msg.sender, "Wrong school");
        cc.isActive = false;
        cc.activatedAt = block.timestamp;
        SchoolInfo storage s = schools[msg.sender];
        s.licenseActive = true;
        s.expiresAt = block.timestamp + cc.period;
        s.gracePeriod = cc.gracePeriod;
        s.activationCode = c;
        emit LicenseActivated(c, msg.sender, cc.period, cc.gracePeriod, s.expiresAt, block.timestamp);
    }

    function isLicensed(address sa) external view returns (bool) {
        SchoolInfo storage s = schools[sa];
        return s.exists && s.licenseActive && block.timestamp < s.expiresAt;
    }

    function getLicenseStatus(address sa) external view returns (uint8 st, uint256 exp, uint256 grace, uint256 rem) {
        SchoolInfo storage s = schools[sa];
        if (!s.exists || !s.licenseActive) return (0, 0, 0, 0);
        exp = s.expiresAt;
        grace = s.expiresAt + s.gracePeriod;
        if (block.timestamp < s.expiresAt) return (1, exp, grace, s.expiresAt - block.timestamp);
        if (block.timestamp < grace) return (2, exp, grace, grace - block.timestamp);
        return (3, exp, grace, 0);
    }

    function verifyCode(string calldata c, address sa) external view returns (bool v, uint256 p, uint256 g, uint256 exp) {
        ActivationCode storage cc = codes[c];
        if (!cc.exists || !cc.isActive || block.timestamp >= cc.expiresAt || cc.school != sa) return (false, 0, 0, 0);
        return (true, cc.period, cc.gracePeriod, cc.expiresAt);
    }

    function deactivateCode(string calldata c) external onlyVendor {
        require(codes[c].exists, "Not found");
        codes[c].isActive = false;
        emit CodeDeactivated(c, codes[c].school, block.timestamp);
    }

    function setVendor(address nv) external onlyVendor { require(nv != address(0), "Zero"); vendor = nv; }

    function getSchoolInfo(address sa) external view returns (string memory, string memory, address, uint256, bool, uint256, string memory) {
        SchoolInfo storage s = schools[sa];
        require(s.exists, "Not found");
        return (s.schoolName, s.adminEmail, s.schoolAddress, s.registeredAt, s.licenseActive, s.expiresAt, s.activationCode);
    }

    function getCodeInfo(string calldata c) external view returns (address, uint256, uint256, uint256, uint256, bool) {
        ActivationCode storage cc = codes[c];
        require(cc.exists, "Not found");
        return (cc.school, cc.period, cc.gracePeriod, cc.expiresAt, cc.activatedAt, cc.isActive);
    }

    function getRegisteredSchoolsCount() external view returns (uint256) { return registeredSchools.length; }
    function getGeneratedCodesCount() external view returns (uint256) { return generatedCodes.length; }
    function isRegistered(address sa) external view returns (bool) { return schools[sa].exists; }
}
