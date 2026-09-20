namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill required for `init` accessors and `record` types to
    /// compile under Unity's .NET Standard 2.1 API compatibility profile,
    /// whose base class library doesn't ship this marker type (it only
    /// exists in .NET 5+). The C# compiler only checks for the type's
    /// presence by name -- it never calls anything on it -- so an empty
    /// class satisfies every assembly that references WTRL.Core.
    ///
    /// This is the actual fix for CS0518 ("Predefined type
    /// 'System.Runtime.CompilerServices.IsExternalInit' is not defined or
    /// imported"), which surfaced across every WTRL.* assembly using
    /// `init`/`record` the first time this project was opened in a real,
    /// licensed Unity Editor.
    ///
    /// Must be `public`, not `internal`: the compiler resolves this
    /// well-known type via ordinary cross-assembly symbol visibility, so
    /// an `internal` declaration here would only fix WTRL.Core itself and
    /// leave every assembly that merely references Core still broken.
    ///
    /// NOTE: the C# 11 `required` keyword was deliberately NOT polyfilled
    /// here. It needs both a language-version bump (`-langversion:11`)
    /// *and* two additional attribute-type polyfills
    /// (`RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`).
    /// The language-version half was attempted via a per-assembly
    /// `<AssemblyName>.rsp` file next to each affected `.asmdef`
    /// (Unity's documented mechanism for per-assembly compiler args) but
    /// did not take effect in a real Editor compile for reasons not fully
    /// diagnosed (Unity's generated Bee-artifact `.rsp` for those
    /// assemblies showed no trace of the custom file's content even after
    /// a full `Library/Bee` cache wipe). Rather than ship two more
    /// speculative polyfills on top of an already-unverified compiler
    /// workaround, every `required` usage in this project was converted
    /// to the constructor-required-plus-init-optional pattern
    /// `WTRL.Vehicle/Definitions.cs` already established (see
    /// `WTRL.RPG/BuildRecipeProgress.cs`, `WTRL.Garage/
    /// VehicleConfigurationResolver.cs`, `WTRL.Career/CareerState.cs`,
    /// `WTRL.Lab/Definitions.cs`, `WTRL.Lab/RunEvidence.cs`). If a real
    /// need for `required` resurfaces, revisit the `.rsp` mechanism with
    /// a licensed Editor available to iterate against directly, rather
    /// than guessing blind.
    /// </summary>
    public static class IsExternalInit { }
}
