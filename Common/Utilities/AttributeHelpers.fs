namespace AttributeHelpers

open System.Runtime.CompilerServices

[<AutoOpen>]
module AttributeHelpers =
    /// This attribute is used to indicate that references to the elements of
    /// a module, record, or union type require explicit qualified access.
    type QualifiedAttribute = RequireQualifiedAccessAttribute

    /// This attribute is used to adjust the runtime representation for a module.
    /// Note: this may affect how a module is compiled.
    type ModuleAttribute = CompilationRepresentationAttribute

    /// This value may be combined with the `ModuleAttribute` to have the compiler append the
    /// word "Module" to the end of a type. Use with caution.
    [<Literal>]
    let Suffix = CompilationRepresentationFlags.ModuleSuffix

