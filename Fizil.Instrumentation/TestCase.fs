﻿module TestCase

let utf8Encoding = System.Text.UTF8Encoding(false, true)


let removeUtf16ByteOrderMark(bytes: byte[]) : (string * byte[]) option =
    use stream = new System.IO.MemoryStream(bytes)
    use reader = new System.IO.StreamReader(stream, System.Text.UTF8Encoding.UTF8, true)
    let text = reader.ReadToEnd()
    try
        let encodedBytes = utf8Encoding.GetBytes(text)
        Some (text, encodedBytes)
    with 
    | :? System.Text.DecoderFallbackException -> None


/// How many test cases do we expect per example 
type MaximumTestCasesPerExample =

/// Most fuzz strategies produce one or more test cases per byte of example data
| TestCasesPerByte    of int

/// Some fuzz strategies (useOriginal) produce one or more test cases per example
| TestCasesPerExample of int


/// Fuzzing stage, such as calibration or byteflip 1/1
[<NoComparison>]
type Stage = {
    Name:                string
    TestCasesPerExample: MaximumTestCasesPerExample
    TestCases:           seq<byte[]>
}


[<NoComparison>]
type TestCase = 
    {
        Data:          byte[]

        /// Determines whether file contents should be read as text 
        /// (removing Unicode BOM at top of file if present)
        /// or binary (byte for byte data as found in file)
        FileExtension: string

        /// SourceFile will be Some filename on an example file,
        /// None on a generated test case
        SourceFile:    string option

        /// Fuzzing stage, such as calibration or byteflip 1/1
        Stage:         Stage
    }



