// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

contract TestMultiDecl {
    function f(uint256 v) public pure returns (uint256) {
        // The exact construction from StringLib.sol line 27:
        uint256 t = v, l = 0;
        while (t != 0) { l++; t >>= 4; }
        return l;
    }
}