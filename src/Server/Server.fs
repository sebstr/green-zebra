open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Saturn

open Shared

open Giraffe.Serialization

let publicPath = Path.GetFullPath @"E:\Code\AutoShot\Pics"
let port = 8085us

let getInitCounter() : Task<Counter> = task { return 42 }

let getImgz next ctx = 
    Successful.OK "apa" next ctx

let getInts next ctx = 
    Successful.OK 2 next ctx

let imgs next ctx =     
    task {
        let d = DirectoryInfo(@"E:\Code\AutoShot\Pics\")
        let files = Array.map (fun (fi:FileInfo) -> {url = fi.Name; timestamp = fi.CreationTimeUtc}) (d.GetFiles "*.jpg")
        return! Successful.OK files next ctx
    }

let img name next ctx =         
    task {
        let d = DirectoryInfo(@"E:\Code\AutoShot\Pics\")
        let bytes = File.ReadAllBytes(@"E:\Code\AutoShot\Pics\" + name)
        return! Successful.OK bytes next ctx
    }
 
let i =
    (fun ctx -> "ii" |> Controller.text ctx)

let myShow ctx (id: string) =
    (@"E:\Code\AutoShot\Pics\" + id)
    //|> sprintf "Hello world, %s"
    |> Controller.file ctx 

let imgController = controller {
    //subController "/comments" commentController
 
    
    plug [All] (setHttpHeader "Content-Type" "image/jpeg")
    plug [All] (setHttpHeader "Content-Type2" "image/jpeg")
    plug [All] (setHttpHeader "user-controller-common" "12343")

    
    add (fun ctx -> "Add handler no version" |> Controller.text ctx)
    index (fun ct -> "Index handler no version" |> Controller.text ct)
    show myShow
    //show (fun ct -> "Index handler no version" |> Controller.text ct)
    //add2 (fun ctx -> "Add handler no version" |> Controller.text ctx)
    //view (img "chili-2018-09-14_13-50.jpg" )
    //show (fun (ctx, id) -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
    //edit (fun (ctx, id) -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
}


let webApp = router {    
    get "/imgz" getImgz
    get "/ints" getInts
    forward "/im" imgController
    get "/api/imgs" imgs
    getf "/img/%s" img
    get "/api/init" (fun next ctx ->
        task {
            let! counter = getInitCounter()
            return! Successful.OK counter next ctx
        })
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_gzip    
}

run app
