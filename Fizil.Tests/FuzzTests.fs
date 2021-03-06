﻿namespace Fizil.Tests

open NUnit.Framework
open FsUnit

module FuzzTest = 

    [<Test>]
    let ``useOriginalExample returns originals``() = 
        let input = [| 0uy |]
        let fuzzed = Fuzz.useOriginalExample input
        
        fuzzed.TestCases |> Seq.length |> should equal 1
        fuzzed.TestCases |> Seq.head |> Seq.head |> should equal (input |> Array.head)


    [<Test>]
    let ``bitFlip1 flips l bit``() = 
        let input = [| 0b00000000uy; 0b11111111uy |]
        let expected = [ 
            [| 0b00000001uy; 0b11111111uy |]
            [| 0b00000010uy; 0b11111111uy |]
            [| 0b00000100uy; 0b11111111uy |]
            [| 0b00001000uy; 0b11111111uy |]
            [| 0b00010000uy; 0b11111111uy |]
            [| 0b00100000uy; 0b11111111uy |]
            [| 0b01000000uy; 0b11111111uy |]
            [| 0b10000000uy; 0b11111111uy |] 
            [| 0b00000000uy; 0b11111110uy |]
            [| 0b00000000uy; 0b11111101uy |]
            [| 0b00000000uy; 0b11111011uy |]
            [| 0b00000000uy; 0b11110111uy |]
            [| 0b00000000uy; 0b11101111uy |]
            [| 0b00000000uy; 0b11011111uy |]
            [| 0b00000000uy; 0b10111111uy |]
            [| 0b00000000uy; 0b01111111uy |] 
        ]
        let fuzzed = Fuzz.bitFlip 1 input
        let actual = List.ofSeq fuzzed.TestCases

        actual |> should equal expected


    [<Test>]
    let ``byteFlip1 flips l byte``() = 
        let input = [| 0b00000000uy; 0b11111111uy; 0b00000000uy |]
        let expected = [ 
            [| 0b11111111uy; 0b11111111uy; 0b00000000uy |]
            [| 0b00000000uy; 0b00000000uy; 0b00000000uy |]
            [| 0b00000000uy; 0b11111111uy; 0b11111111uy |]
        ]
        let fuzzed = Fuzz.byteFlip 1 input
        let actual = List.ofSeq fuzzed.TestCases
        actual |> should equal expected


    [<Test>]
    let ``couldBeBitflip 0 1 is true``() = 
        let actual = Fuzz.couldBeBitflip(0u, 1u)

        actual |> should be True


    [<Test>]
    let ``couldBeBitflip 0 0 is false``() = 
        let actual = Fuzz.couldBeBitflip(0u, 0u)

        actual |> should be False


    [<Test>]
    let ``couldBeBitflip with non-bitflipped values is false``() = 
        let oldValue = 2863311530u // 10101010101010101010101010101010
        let actual = Fuzz.couldBeBitflip(oldValue, 0u)

        actual |> should be False


    [<Test>]
    let ``arith8 returns expected values``() = 
        let input = [| 128uy |]
        let fuzzed = Fuzz.arith8 input
        let expected = 
            seq [ 
                [| 113uy |] 
                [| 114uy |] 
                [| 115uy |] 
                [| 116uy |] 
                [| 117uy |] 
                [| 118uy |] 
                [| 119uy |] 
                [| 120uy |] 
                [| 121uy |] 
                [| 122uy |] 
                [| 123uy |] 
                [| 124uy |] 
                [| 125uy |] 
                [| 126uy |] 
                [| 133uy |] 
                [| 135uy |] 
                [| 137uy |] 
                [| 138uy |] 
                [| 139uy |] 
                [| 141uy |] 
                [| 142uy |] 
            ]

        fuzzed.TestCases |> should equal expected


    [<Test>]
    let ``swap16 does what it says on the tin``() = 
        let input : uint16 = 0xFF00us
        let actual = Fuzz.swap16 input

        actual |> should equal 0x00FFus


    [<Test>]
    let ``swap32 does what it says on the tin``() = 
        let input : uint32 = 0xFF0FF000u
        let actual = Fuzz.swap32 input

        actual |> should equal 0x00F00FFFu


    [<Test>]
    let ``toBytes works``() =
        let actual = Fuzz.toBytes(0xAABBCCDDu)

        actual |> should equal (0xAAuy, 0xBBuy, 0xCCuy, 0xDDuy)


    [<TestCase(1u,     3u, 1uy, true)>]
    [<TestCase(1u,     3u, 4uy, true)>]
    [<TestCase(1u,   200u, 1uy, false)>]
    [<TestCase(1u,   200u, 4uy, false)>]
    [<TestCase(244u, 257u, 1uy, false)>]
    [<TestCase(244u, 257u, 2uy, true)>]
    let ``couldBeArith should return expected results``(oldValue: uint32, newValue: uint32, numBytes: uint8, expected: bool) =
        let actual = Fuzz.couldBeArith(oldValue, newValue, numBytes)

        actual |> should equal expected


    [<TestCase(1u, 256u, 2, false)>]
    [<TestCase(1u, 257u, 2, true)>]
    let ``couldBeInterest should return expected results``(oldValue: uint32, newValue: uint32, numBytes: int, expected: bool) =
        let actual = Fuzz.couldBeInterest(oldValue, newValue, numBytes, false)

        actual |> should equal expected