// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

library StringLib {
    function genCode(
        address sa,
        uint256 count,
        uint256 ts,
        uint256 rand,
        address sender
    ) internal pure returns (string memory) {
        uint256 nonce = uint256(keccak256(abi.encodePacked(ts, rand, sender, sa, count)));
        bytes memory pre = "ACT-";
        bytes memory sid = _hex(uint160(sa));
        bytes memory nh = _hex(nonce);
        bytes memory c = new bytes(pre.length + sid.length + 1 + nh.length);
        uint256 p = 0;
        for (uint256 i = 0; i < pre.length; i++) c[p++] = pre[i];
        for (uint256 i = 0; i < sid.length; i++) c[p++] = sid[i];
        c[p++] = "-";
        for (uint256 i = 0; i < nh.length; i++) c[p++] = nh[i];
        return string(c);
    }

    function _hex(uint256 v) internal pure returns (bytes memory) {
        if (v == 0) return "0";
        uint256 t = v, l = 0;
        while (t != 0) { l++; t >>= 4; }
        bytes memory r = new bytes(l);
        t = v;
        for (uint256 i = l; i > 0; i--) {
            uint256 n = t & 0xF;
            r[i - 1] = n < 10 ? bytes1(0x30 + n) : bytes1(0x61 + n - 10);
            t >>= 4;
        }
        return r;
    }

    function concat(string memory a, string memory b) internal pure returns (string memory) {
        bytes memory ab = bytes(a);
        bytes memory bb = bytes(b);
        bytes memory result = new bytes(ab.length + bb.length);
        uint256 p = 0;
        for (uint256 i = 0; i < ab.length; i++) result[p++] = ab[i];
        for (uint256 i = 0; i < bb.length; i++) result[p++] = bb[i];
        return string(result);
    }
}
