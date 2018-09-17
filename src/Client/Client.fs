module Client

open System

open Elmish
open Elmish.React
open Fable.Import.Browser

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Shared

open Fulma
open Fable
open System.ComponentModel
open Fulma
open Fulma

type PlayState = Forward | Backward | Stopped

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { 
    Counter: Counter 
    Images: ImgUrl list option
    Message: string    
    PlayState: PlayState
    Speed: int
    }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>
| GetImages
| ImagesReceived of Result<List<ImgUrl>, exn>
| Tick of DateTime
| SetCounter of string
| SetPlayState of PlayState


let timer initial =
    let sub dispatch = 
        window.setInterval((fun _ -> 
            dispatch (Tick DateTime.Now)), initial.Speed, []) |> ignore
    Cmd.ofSub sub

let getImagesCmd =
    Cmd.ofPromise 
        (fetchAs<List<ImgUrl>> "/api/imgs")
        []
        (Ok >> ImagesReceived) 
        (Error >> ImagesReceived)

let loadCountCmd =
    Cmd.ofPromise
        (fetchAs<int> "/api/init")
        []
        (Ok >> InitialCountLoaded)
        (Error >> InitialCountLoaded)

let init () : Model * Cmd<Msg> =
    let initialModel = { Counter = 0; Images = None; Message = "Inget"; PlayState = Forward; Speed = 200}
    initialModel, getImagesCmd


let move currentModel = 
    match currentModel.Images with
    | None -> currentModel
    | Some imgs -> 
        match currentModel.PlayState with
        | Forward -> 
            if imgs.Length - 1 > currentModel.Counter 
            then { currentModel with Counter = currentModel.Counter + 1 } 
            else { currentModel with Counter = 0 } 
        | Backward -> 
            if currentModel.Counter > 0 
            then { currentModel with Counter = currentModel.Counter - 1 } 
            else { currentModel with Counter = imgs.Length - 1 } 
        | Stopped ->
            currentModel


let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | Increment ->
        { currentModel with Counter = currentModel.Counter + 1 }, Cmd.none
    | Decrement ->
        { currentModel with Counter = currentModel.Counter - 1 }, Cmd.none
    | GetImages ->        
        currentModel, getImagesCmd
    | ImagesReceived (Ok imgs) ->
        { currentModel with Images = Some imgs }, Cmd.none
    | ImagesReceived (Error exn) ->        
        { currentModel with Message = exn.Message }, Cmd.none
    | Tick dateTime ->
        move currentModel, Cmd.none 
        // match currentModel.PlayState with
        // | Forward -> { currentModel with Counter = currentModel.Counter + 1 }, Cmd.none
        // | Backward -> { currentModel with Counter = currentModel.Counter - 1 }, Cmd.none
        // | Stopped -> currentModel, Cmd.none
    | SetCounter c ->
        { currentModel with Counter = System.Int32.Parse(c) }, Cmd.none
    | SetPlayState ps ->
        { currentModel with PlayState = ps }, Cmd.none
    | _ -> currentModel, Cmd.none





let show = function
| { Counter = Counter } -> string Counter


let timeLine model dispatch =
    match model.Images with 
    | None -> str "No images"
    | Some imgs ->
        Content.content [ Content.Modifiers [ Modifier.TextAlignment (Fulma.Screen.All, TextAlignment.Centered) ] ] [
            input [ Type "range"; Class "slider is-large"; Min 0; Max (imgs.Length - 1); Value <| model.Counter.ToString(); OnChange (fun v -> dispatch (SetCounter v.Value))]; 
            div [] [
                str <| imgs.[model.Counter].timestamp.ToString("yyyy-MM-dd HH:mm")
            ]
        ]
        

let imageView = function
| { Images = Some imgs; Counter = counter } -> img [ Src <| "http://localhost:8085/" + imgs.[counter].url ]
| { Images = None } -> str "No images"

let button txt onClick =
    Button.button
        [ Button.Size IsSmall
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let view (model : Model) (dispatch : Msg -> unit) =
    Container.container [ Container.Modifiers [ Modifier.TextAlignment (Fulma.Screen.All, TextAlignment.Centered) ] ] [
        
        timeLine model dispatch

        Container.container [] [
            button "<" (fun _ -> dispatch <| SetPlayState Backward)
            button "||" (fun _ -> dispatch <| SetPlayState Stopped)
            button ">" (fun _ -> dispatch <| SetPlayState Forward)
        ]
        
        Container.container [] [
            imageView model
        ]
    ]
        





        


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif
 
Program.mkProgram init update view
|> Program.withSubscription timer
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
