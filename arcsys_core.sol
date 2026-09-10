// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/// @title AutoSchool360
/// @notice Self-contained core licensing contract.
/// @dev No external libraries — inlines its own uint→string conversion
///      using decimal digits (no hex conversion, no StringLib dependency).
contract AutoSchool360 {
    address public vendor;
    address public delegate;      // administrative delegate that can generate & deactivate codes

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
    event DelegateAssigned(address indexed delegate, address indexed vendor);
    event DelegateRevoked(address indexed delegate, address indexed vendor);

    modifier onlyVendor() { require(msg.sender == vendor, "Only vendor"); _; }
    modifier onlyVendorOrDelegate() { require(msg.sender == vendor || msg.sender == delegate, "Only vendor or delegate"); _; }
    modifier onlySchool() { require(schools[msg.sender].exists, "Not registered"); _; }

    constructor() { vendor = msg.sender; }

    function registerSchool(string calldata n, string calldata e) external {
        require(!schools[msg.sender].exists, "Exists");
        schools[msg.sender] = SchoolInfo(n, e, msg.sender, block.timestamp, false, 0, 0, "", true);
        registeredSchools.push(msg.sender);
        emit SchoolRegistered(msg.sender, n, e, block.timestamp);
    }

    /// @dev Convert a uint256 to its **decimal** string representation.
    ///      Uses bytes1(uint8(...)) — never bytes(...) — so there is no
    ///      "Type bytes memory is not implicitly convertible to bytes1" error.
    function uint2str(uint256 _i) internal pure returns (string memory) {
        if (_i == 0) return "0";
        uint256 j = _i;
        uint256 len;
        while (j != 0) { len++; j /= 10; }
        bytes memory bstr = new bytes(len);
        uint256 k = len;
        while (_i != 0) {
            bstr[--k] = bytes1(uint8(48 + (_i % 10)));
            _i /= 10;
        }
        return string(bstr);
    }

    /// @dev Generate "ACT-<decimalAddress>-<decimalNonce>" directly —
    ///      no library call, no hex conversion, no manual byte-splicing.
    function genCode(
        address sa,
        uint256 count,
        uint256 ts,
        uint256 rand,
        address sender
    ) internal pure returns (string memory) {
        uint256 nonce = uint256(keccak256(abi.encodePacked(ts, rand, sender, sa, count)));
        return string(
            abi.encodePacked(
                "ACT-",
                uint2str(uint256(uint160(sa))),
                "-",
                uint2str(nonce)
            )
        );
    }

    function generateCode(address sa, uint256 p, uint256 g) external onlyVendorOrDelegate returns (string memory c) {
        require(schools[sa].exists, "Not registered");
        c = genCode(sa, generatedCodes.length, block.timestamp, block.prevrandao, msg.sender);
        codes[c] = ActivationCode(sa, p, g, block.timestamp + 1 hours, 0, true, true);
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

    function deactivateCode(string calldata c) external onlyVendorOrDelegate {
        require(codes[c].exists, "Not found");
        codes[c].isActive = false;
        emit CodeDeactivated(c, codes[c].school, block.timestamp);
    }

    function setDelegate(address newDelegate) external onlyVendor {
        require(newDelegate != address(0), "Zero address");
        delegate = newDelegate;
        emit DelegateAssigned(newDelegate, msg.sender);
    }

    function revokeDelegate() external onlyVendor {
        address old = delegate;
        delegate = address(0);
        emit DelegateRevoked(old, msg.sender);
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
