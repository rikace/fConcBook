namespace GameOfLife

module Pages =
  open Giraffe.GiraffeViewEngine
  open ViewEngine
  open Microsoft.AspNetCore.Http
  
  type UserModel = { Name : string; Cash: decimal; UserId: string}    

  let indexView : Page<unit, HttpContext> =
    {
      Title = "Game of Life"
      Scripts = [
        yield "https://cdnjs.cloudflare.com/ajax/libs/jquery/3.3.1/jquery.min.js"
        yield "https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.bundle.min.js"
        yield "/js/site.js"
      ]
      
      Template = fun () ctx -> [
        
        
        div [ _class "container";] [
          main [ _role "main"; _class "pb-3" ] [
            div [ _class "text-center" ] [
              h2 [ _class "display-4" ] [
                input [ _id "btnStart"; _type "submit"; _value "Start" ]
              ]
              div [ _id "container"; _class "canvas-container"] []
            ]                  
          ]
        ]
      ] 
    }
