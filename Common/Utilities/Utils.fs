namespace System

    [<AutoOpen>]
    module LazyEx =
        let force (x: Lazy<'T>) = x.Force()

namespace System.IO

    [<RequireQualifiedAccess>]
    module FileEx =
        open System.Threading.Tasks
        open System.Text

        let ReadAllBytesAsync (path:string) =
            async {
                use fs = new FileStream(path,
                                FileMode.Open, FileAccess.Read, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                let length = int fs.Length
                return! fs.AsyncRead(length)
            } |> Async.StartAsTask

        let WriteAllBytesAsync (path:string, bytes: byte[]) =
            async {
                use fs = new FileStream(path,
                                FileMode.Append, FileAccess.Write, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                do! fs.AsyncWrite(bytes, 0, bytes.Length)
            }

        let WriteAllBytesTask (path:string, bytes: byte[]) =
            WriteAllBytesAsync(path, bytes) |> Async.StartAsTask

        let ReadFromTextFileAsync(path:string) : Task<string> =
            async {
                use fs = new FileStream(path,
                                FileMode.Open, FileAccess.Read, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                let buffer = Array.zeroCreate<byte> 0x1000
                let rec readRec bytesRead (sb:StringBuilder) = async {
                    if bytesRead > 0 then
                        let content = Encoding.Unicode.GetString(buffer,0,bytesRead)
                        let! bytesRead = fs.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                        return! readRec bytesRead (sb.Append(content))
                    else return sb.ToString() }

                let! bytesRead = fs.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                return! readRec bytesRead (StringBuilder())
            }
            |> Async.StartAsTask

        let WriteTextToFileAsync(path:string) (content:string) : Task<unit> =
            async {
                let encodedContent = Encoding.Unicode.GetBytes(content)
                use fs = new FileStream(path,
                                FileMode.Append, FileAccess.Write, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                do! fs.WriteAsync(encodedContent,0,encodedContent.Length) |> Async.AwaitTask
            } |> Async.StartAsTask

namespace System.Drawing

open System
open System.Runtime.CompilerServices
open System.IO
open System.Drawing
open System.Drawing.Imaging

[<Sealed; Extension>]
type ImageExtensions =
    static member SaveImageAsync (path:string, format:ImageFormat) (image:Image) =
        async {
            use ms = new MemoryStream()
            image.Save(ms, format)
            do! FileEx.WriteAllBytesAsync(path, ms.ToArray())
        } |> Async.StartAsTask

[<AutoOpen>]
module ImageHelpers =
    type System.Drawing.Image with
        member this.SaveImageAsync (stream:Stream, format:ImageFormat) =
            async { this.Save(stream, format) }

namespace Utilities

[<AutoOpen>]
module Utils =
    open System

    let charDelimiters = [0..256] |> Seq.map(char)|> Seq.filter(fun c -> Char.IsWhiteSpace(c) || Char.IsPunctuation(c)) |> Seq.toArray

    /// Transforms a function by flipping the order of its arguments.
    let inline flip f a b = f b a

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    let inline tap fn x = fn x |> ignore; x

    /// Sequencing operator like Haskell's ($). Has better precedence than (<|) due to the
    /// first character used in the symbol.
    let (^) = (<|)

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    let inline tee fn x = fn x |> ignore; x

    /// Custom operator for `tee`: Given a value, apply a function to it, ignore the result, then return the original value.
    let inline (|>!) x fn = tee fn x

    let force (x: Lazy<'T>) = x.Force()

    /// Safely invokes `.Dispose()` on instances of `IDisposable`
    let inline dispose (d :#IDisposable) = match box d with null -> () | _ -> d.Dispose()

    let is<'T> (x: obj) = x :? 'T

    let delimiters =
            [0..256]
            |> List.map(char)
            |> List.filter(fun c -> Char.IsWhiteSpace(c) || Char.IsPunctuation(c))
            |> List.toArray