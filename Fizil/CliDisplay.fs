﻿module CliDisplay

open ExecutionResult
open Status
open System
open System.Globalization
open System.Linq
open TestCase


let backgroundColor = ConsoleColor.Black
let titleColor      = ConsoleColor.Gray
let valueColor      = ConsoleColor.White


let private consoleTitleRedrawInterval = TimeSpan(0, 0, 0, 15, 0)
let private consoleUpdateInterval = TimeSpan(0, 0, 0, 0, 100)


type private Status = 
    {
        Configuration:       Configuration
        ElapsedTime:         TimeSpan
        StageName:           string
        Executions:          uint64
        Crashes:             uint64
        NonZeroExitCodes:    uint64
        Paths:               uint64
        ExecutionsPerSecond: float
        LastError:           string option
        LastTitleRedraw:     DateTimeOffset
        LastUpdate:          DateTimeOffset
        ShouldRedrawTitles:  bool
        ShouldUpdate:        bool
    }
    with 
        member this.AddExecution(result: Result) =
            let now         = DateTimeOffset.UtcNow 
            let elapsedTime = now - this.Configuration.StartTime
            let stageMaxExecutions = 
                match result.TestCase.Stage.TestCasesPerExample with
                | TestCasesPerExample n -> n * this.Configuration.ExampleCount
                | TestCasesPerByte n    -> n * this.Configuration.ExampleBytes
            let stageName = sprintf "%s (%d)" result.TestCase.Stage.Name stageMaxExecutions
            let executions  = this.Executions + 1UL
            let executionsPerSecond = 
                if (elapsedTime.TotalMilliseconds > 0.0) 
                then Convert.ToDouble(executions) / Convert.ToDouble(elapsedTime.TotalMilliseconds) * 1000.0
                else 0.0
            let shouldRedrawTitles = this.Executions = 0UL || now - this.LastTitleRedraw > consoleTitleRedrawInterval
            let shouldUpdate = this.Executions = 0UL || now - this.LastUpdate > consoleUpdateInterval
            let newCrash = 
                match result.TestResult.Crashed, result.HasStdErrOutput with
                | true,  true  -> Some result.TestResult.StdErr
                | false, true  -> Some result.TestResult.StdErr
                | true,  false -> Some result.TestResult.StdOut
                | false, false -> None
            let lastTitleRedraw = if shouldRedrawTitles then now else this.LastTitleRedraw
            let lastUpdate = if shouldUpdate then now else this.LastUpdate
            {
                Configuration       = this.Configuration
                ElapsedTime         = elapsedTime
                StageName           = stageName
                Executions          = executions
                Crashes             = this.Crashes          + (if result.TestResult.Crashed       then 1UL else 0UL)
                NonZeroExitCodes    = this.NonZeroExitCodes + (if result.TestResult.ExitCode <> 0 then 1UL else 0UL)
                Paths               = this.Paths            + (if result.NewPathFound  then 1UL else 0UL)
                ExecutionsPerSecond = executionsPerSecond
                LastError           = 
                    match newCrash, this.LastError with
                    | None        , None     -> None
                    | Some message, None     -> 
                        System.Diagnostics.Debug.WriteLine message
                        newCrash
                    | None        , Some _   -> this.LastError
                    | Some message, Some old -> 
                        System.Diagnostics.Debug.WriteLine message
                        let paddedLength = old.TrimEnd().Length 
                        if paddedLength > message.Length 
                        then Some (message.PadRight(paddedLength))
                        else Some (message)
                LastTitleRedraw     = lastTitleRedraw
                LastUpdate          = lastUpdate
                ShouldRedrawTitles  = shouldRedrawTitles 
                ShouldUpdate        = shouldUpdate
            }


let private initialState() = 
    {
        Configuration       = { StartTime = DateTimeOffset.UtcNow; ExampleBytes = 0; ExampleCount = 0 }
        ElapsedTime         = TimeSpan.Zero
        StageName           = "initializing"
        Executions          = 0UL
        Crashes             = 0UL
        NonZeroExitCodes    = 0UL
        Paths               = 0UL
        ExecutionsPerSecond = 0.0
        LastError           = None
        LastTitleRedraw     = DateTimeOffset.UtcNow
        LastUpdate          = DateTimeOffset.UtcNow
        ShouldRedrawTitles  = true
        ShouldUpdate        = true
    }


let private writeValue (redrawTitle: bool) (title: string) (titleWidth: int) (formattedValue: string) =
    if redrawTitle
    then 
        Console.ForegroundColor <- titleColor
        Console.Write ((title.PadLeft titleWidth) + " : ")
    else
        Console.CursorLeft <- titleWidth + 3
    Console.ForegroundColor <- valueColor
    Console.WriteLine (formattedValue + "   ")


let private writeParagraph (redrawTitle: bool) (title: string) (leftColumnWidth: int) (formattedValue: string option) =
    Console.ForegroundColor <- titleColor
    match formattedValue with
    | Some value -> 
        if redrawTitle
        then
            Console.WriteLine ((title.PadLeft leftColumnWidth) + " :       ")
        else
            Console.CursorLeft <- (leftColumnWidth + 2)
            Console.WriteLine "        "
        Console.ForegroundColor <- valueColor
        Console.WriteLine value
    | None -> 
        if redrawTitle
        then 
            Console.Write ((title.PadLeft leftColumnWidth) + " : ")
        else
            Console.CursorLeft <- leftColumnWidth + 3
        Console.WriteLine "<none>"


let private formatTimeSpan(span: TimeSpan) : string =
    sprintf "%d days, %d hrs, %d minutes, %d seconds  " span.Days span.Hours span.Minutes span.Seconds


let private stripControlCharacters (str: string) : string =
    if str.Any(fun c -> Char.IsControl c)
    then new System.String(str.Where(fun c -> not <| Char.IsControl c).ToArray())
    else str


let private toConsole(status: Status) =
    Console.BackgroundColor <- backgroundColor
    Console.SetCursorPosition(0, 0)
    let titleWidth = 19
    let redrawTitle = status.ShouldRedrawTitles
    writeValue redrawTitle "Elapsed time"       titleWidth (status.ElapsedTime |> formatTimeSpan)
    writeValue redrawTitle "Stage"              titleWidth (status.StageName)
    writeValue redrawTitle "Executions"         titleWidth (status.Executions.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Crashes"            titleWidth (status.Crashes.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Nonzero exit codes" titleWidth (status.NonZeroExitCodes.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Paths"              titleWidth (status.Paths.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Executions/second"  titleWidth (status.ExecutionsPerSecond.ToString("G4", CultureInfo.CurrentUICulture))
    writeParagraph redrawTitle "Last error"     titleWidth (status.LastError |> Option.map stripControlCharacters)


let private agent: MailboxProcessor<Message> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop (state: Status) = async {
            let! message = inbox.Receive()
            match message with
            | Initialize configuration ->
                let state' = { state with Configuration = configuration }
                return! loop state'               
            | Update result -> 
                let state' = state.AddExecution result
                if (state'.ShouldUpdate)
                then state' |> toConsole
                return! loop state'
        }
        loop (initialState()))


let postResult(message: Message) =
    agent.Post message

