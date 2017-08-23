namespace Pipeline

open System.Runtime.CompilerServices

[<Sealed; Extension>]
type public FSharpFuncUtils =

    [<Extension>]
    static member ToFSharpFunc<'a,'b> (func:System.Converter<'a,'b>) = fun x -> func.Invoke(x)

    [<Extension>]
    static member ToFSharpFunc<'a,'b> (func:System.Func<'a,'b>) = fun x -> func.Invoke(x)

    [<Extension>]
    static member ToFSharpFunc<'a,'b,'c> (func:System.Func<'a,'b,'c>) = fun x y -> func.Invoke(x,y)

    [<Extension>]
    static member ToFSharpFunc<'a,'b,'c,'d> (func:System.Func<'a,'b,'c,'d>) = fun x y z -> func.Invoke(x,y,z)

    static member Create<'a,'b> (func:System.Func<'a,'b>) = FSharpFuncUtils.ToFSharpFunc func

    static member Create<'a,'b,'c> (func:System.Func<'a,'b,'c>) = FSharpFuncUtils.ToFSharpFunc func

    static member Create<'a,'b,'c,'d> (func:System.Func<'a,'b,'c,'d>) = FSharpFuncUtils.ToFSharpFunc func