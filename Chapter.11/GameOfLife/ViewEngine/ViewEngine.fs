namespace GameOfLife

open System.Collections.Generic
open Giraffe
open Microsoft.AspNetCore.Http
open Giraffe.GiraffeViewEngine

[<RequireQualifiedAccess>]
module ErrorBag =

  let [<Literal>] private ErrorsKey = "Errors"
  let [<Literal>] private PageErrorsKey = "PageErrors"

  type ValidationErrors = IDictionary<string, IList<string>>

  let private getDictValue<'a> key (ctx : HttpContext) =
      let dict = ctx.Items
      match dict.TryGetValue key with
      | true, (:? 'a as item) -> Some item
      | _, _ -> None

  let private setDictValue<'a> key (value:'a) (ctx : HttpContext) =
      ctx.Items.[key] <- value

module ViewEngine =

  type Page<'vm, 'context> = {
    Title: string
    Scripts: string list
    Template: 'vm -> 'context -> XmlNode list
  }
  
  
  let _role = attr "role"
  let _asp_area = attr "asp-area"
  let _asp_controller = attr "asp-controller"
  let _asp_action = attr "asp-action"
  let _dataTarget = attr "data-target"
  let _dataToggle = attr "data-toggle"
  
  let templateHead pageTitle =
      head [] [
        meta [_charset "utf-8"]
        meta [_name "viewport"
              _content "width=device-width, initial-scale=1, shrink-to-fit=no"]
        title [] [str pageTitle]
        link [_rel "icon"
              _type "image/x-icon"
              _href "/favicon.ico"]
        link [_rel "stylesheet"
              _type "text/css"
              _href  "https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css" ]
        link [_rel "stylesheet"
              _type "text/css"
              _href  "/css/site.css"]            
      ]
  
  let scripts pageScripts = [          
      for url in pageScripts do            
        yield script [ _src url ] []       
    ]
  
  
  let mainContent (page: Page<'a, HttpContext>) vm (ctx: HttpContext) =
      section [ _class "section" ] [
                     div [ _class "container" ] [
                           yield  h1 [ _class "title" ] [ str page.Title ]
                           yield! (page.Template vm ctx)
                     ]
                 ]
      
  let layout (ctx: HttpContext) (page: Page<'a, HttpContext>) vm =
      html [_lang "en"] [
            templateHead page.Title
            body [] [
              yield mainContent page vm ctx
              yield! scripts page.Scripts
            ]
      ]
      
  
  /// render a page with a viewmodel through the common application layout template
  let htmlLayout (page: Page<'a, HttpContext>) (vm: 'a): HttpHandler =
    fun next ctx -> htmlView (layout ctx page vm) next ctx


