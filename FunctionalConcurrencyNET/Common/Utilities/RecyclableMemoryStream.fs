namespace RecyclableMemoryStream

// Install-Package Microsoft.IO.RecyclableMemoryStream
//open Microsoft.IO

//[<AutoOpen>]
//module RecyclableMemoryStreamManagerWrapper =
//    let private implementation = RecyclableMemoryStreamManager()
//    type MemoryStreamManager =
//        static member GetStream(tag) = implementation.GetStream(tag)
//        static member GetStream(tag, buffer, offset, count) = implementation.GetStream(tag = tag, buffer = buffer, offset = offset, count = count)
