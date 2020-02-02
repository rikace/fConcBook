namespace GameOfLife

module Giraffe =

  open System.Globalization
  open Giraffe
  open Microsoft.Extensions.Primitives
  open Microsoft.AspNetCore.Http
  open Microsoft.Net.Http.Headers
  open System.Threading.Tasks
  open FSharp.Control.Tasks.V2.ContextInsensitive

  let private runTaskAndCatch (operation: unit -> Task<'t>) = task {
      try
        let! result = operation ()
        return Core.Ok result
      with e -> return Core.Error (sprintf "%s:\n:%s" e.Message e.StackTrace)
  }


  type HttpContext with
    /// Performs basic content negotiation on the incoming request to bind the type 'T based on content-type.
    /// Calls into underlying Giraffe HttpContext extension methods based on content types.
    ///
    /// For POST/PUT/PATCH/DELETE:
    /// * application/json -> BindJsonAsync
    /// * application/xml -> BindXmlAsync
    /// * application/x-www-form-urlencoded -> TryBindFormAsync
    ///
    /// For GET:
    /// * anything -> TryBindQueryString
    ///
    /// Since xml and json binding don't have a Try-variant, those are wrapped in a try/catch handler so they
    /// can return results as well.
    member this.TryBindModelAsync<'T> (?cultureInfo : CultureInfo) =
      task {
          let method = this.Request.Method
          let! result = task {
            if method.Equals "POST" || method.Equals "PUT" || method.Equals "PATCH" || method.Equals "DELETE" then
                let original = StringSegment(this.Request.ContentType)
                let parsed   = ref (MediaTypeHeaderValue(StringSegment("*/*")))
                match MediaTypeHeaderValue.TryParse(original, parsed) with
                | false -> return Core.Error (sprintf "Could not parse Content-Type HTTP header value '%s'" original.Value)
                | true  ->
                    match parsed.Value.MediaType.Value with
                    | "application/json"                  -> return! runTaskAndCatch this.BindJsonAsync<'T>
                    | "application/xml"                   -> return! runTaskAndCatch this.BindXmlAsync<'T>
                    | "application/x-www-form-urlencoded" -> return! this.TryBindFormAsync<'T>(?cultureInfo = cultureInfo)
                    | _ -> return Core.Error (sprintf "Cannot bind model from Content-Type '%s'" original.Value)
            else return this.TryBindQueryString<'T>(?cultureInfo = cultureInfo) }
          return result
    }

  /// An HttpHandler version of the `HttpContext.TryBindModelAsync<'t>` member, which allows for chaining via `>=>`.
  ///
  /// Performs basic content negotiation on the incoming request to bind the type 'T based on content-type.
  /// Calls into underlying Giraffe HttpContext extension methods based on content types.
  ///
  /// For POST/PUT/PATCH/DELETE:
  /// * application/json -> BindJsonAsync
  /// * application/xml -> BindXmlAsync
  /// * application/x-www-form-urlencoded -> TryBindFormAsync
  ///
  /// For GET:
  /// * anything -> TryBindQueryString
  ///
  /// Since xml and json binding don't have a Try-variant, those are wrapped in a try/catch handler so they
  /// can return results as well.
  let tryBindModelAsync<'T> (parsingErrorHandler : string -> HttpHandler)
                            (culture             : CultureInfo option)
                            (successhandler      : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let! result =
            match culture with
            | Some c -> ctx.TryBindModelAsync<'T> c
            | None   -> ctx.TryBindModelAsync<'T>()
        match result with
        | Core.Error msg -> return! parsingErrorHandler msg next ctx
        | Core.Ok model  -> return! successhandler model next ctx
    }

  module Auth =


    open Microsoft.AspNetCore.Authentication
    open Microsoft.Extensions.Logging

    /// Authenticates the request against the given `schemes`.
    /// If the request is authenticated, the authenticated identities are added to the current HTTP Context's User.
    let authenticateMany (schemes: string seq): HttpHandler =
      fun next ctx -> task {
        for scheme in schemes do
          let! authResult = ctx.AuthenticateAsync(scheme)
          if authResult.Succeeded
          then
            let logger = ctx.GetLogger(sprintf "Handlers.Authenticate.Scheme.%s" scheme)
            ctx.User.AddIdentities(authResult.Principal.Identities) // augment other logins with our own
            logger.LogInformation("Logged in user via scheme {0}", scheme)

        return! next ctx
      }

    /// Authenticates the request against the given `scheme`.
    /// If the request is authenticated, the authenticated identities are added to the current HTTP Context's User.
    let authenticate (scheme: string): HttpHandler = authenticateMany [scheme]

    /// Authenticates the request against the given scheme and returns None if the authentication request failed
    let tryAuthenticate (scheme: string): HttpHandler =
      fun next ctx -> task {
        let logger = ctx.GetLogger(sprintf "Handlers.Authenticate.Scheme.%s" scheme)
        try
          let! authResult = ctx.AuthenticateAsync(scheme)
          if authResult.Succeeded
          then
            ctx.User.AddIdentities(authResult.Principal.Identities) // augment other logins with our own
            logger.LogTrace("Logged in user via scheme {0}", scheme)
            return! next ctx
          else
            logger.LogTrace("Failed to log in user via scheme {0}", scheme)
            return None
        with
        | e ->
          logger.LogError(e, "Error while authenticating with auth scheme {name}", scheme)
          return None
      }

  /// **Description**
  ///
  /// Validates if a user has successfully authenticated. This function checks if the auth middleware was able to establish a user's identity by validating certain parts of the HTTP request (e.g. a cookie or a token) and set the `User` object of the `HttpContext`.
  ///
  /// This version is different from the built-in Giraffe version in that (and shadows it because) it checks all of the identities on the User, not just the first.
  /// **Parameters**
  ///
  /// `authFailedHandler`: A `HttpHandler` function which will be executed when authentication failed.
  ///
  /// **Output**
  ///
  /// A Giraffe `HttpHandler` function which can be composed into a bigger web application.
  ///
    let requiresAuthentication authFailedHandler = authorizeUser (fun user -> isNotNull user && user.Identities |> Seq.exists (fun identity -> identity.IsAuthenticated))  authFailedHandler


    /// Authenticates the request against the given `scheme`.
    /// If the request is authenticated, the authenticated identities are added to the current HTTP Context's User.
    /// If the reqeust is not authenticated, the request is terminated with a 401 status code.
    ///
    /// This extends the built-in `requiresAuthentication` handler with the ability to authenticate against a particular scheme before doing the 'must be logged-in' check
    let requiresAuthenticationScheme (scheme: string): HttpHandler = authenticate scheme >=> requiresAuthentication (setStatusCode 401)

  module GiraffeViewEngine =
    open GiraffeViewEngine

    let content = tag "content"
    let iframe = tag "iframe"

    let _dataDismiss = attr "data-dismiss"
    let _dataFaTransform = attr "data-fa-transform"
    let _dataForTemplate = attr "data-for-template"
    let _dataId = attr "data-id"
    let _dataInputFormat = attr "data-input-format"
    let _dataList   = attr "data-list"
    let _dataParent = attr "data-parent"
    let _dataTarget = attr "data-target"
    let _dataTemplate = attr "data-template"
    let _dataToggle = attr "data-toggle"
    let _dataTrimAtCount = attr "data-trim-at-count"
    let _dataValue = attr "data-value"

    /// helper for triple-quote strings so that they don't have their odd spacing in the raw text.
    let longstr (s: string) = System.Text.RegularExpressions.Regex.Replace(s, "\s+", " ") |> str

    /// helper for triple-quote format strings so that they don't have their odd spacing in the raw text.
    let longstrf fmt =
      Printf.kprintf longstr fmt

    /// Giraffe View Engine tag helpers for SVG generation and manipulation
    module SVG =

      let a                   = tag "a"
      let animate             = tag "animate"
      let animateMotion       = tag "animateMotion"
      let animateTransform    = tag "animateTransform"
      let circle              = tag "circle"
      let clipPath            = tag "clipPath"
      let colorProfile        = tag "color-profile"
      let defs                = tag "defs"
      let desc                = tag "desc"
      let discard             = tag "discard"
      let ellipse             = tag "ellipse"
      let feBlend             = tag "feBlend"

      let feColorMatrix       = tag "feColorMatrix"
      let feComponentTransfer = tag "feComponentTransfer"
      let feComposite         = tag "feComposite"
      let feConvolveMatrix    = tag "feConvolveMatrix"
      let feDiffuseLighting   = tag "feDiffuseLighting"
      let feDisplacementMap   = tag "feDisplacementMap"
      let feDistantLight      = tag "feDistantLight"
      let feDropShadow        = tag "feDropShadow"
      let feFlood             = tag "feFlood"
      let feFuncB             = tag "feFuncB"
      let feFuncG             = tag "feFuncG"
      let feFuncR             = tag "feFuncR"
      let feGaussianBlur      = tag "feGaussianBlur"
      let feImage             = tag "feImage"
      let feMerge             = tag "feMerge"
      let feMergeNode         = tag "feMergeNode"
      let feMorphology        = tag "feMorphology"
      let feOffset            = tag "feOffset"
      let fePointLight        = tag "fePointLight"
      let feSpecularLighting  = tag "feSpecularLighting"
      let feSpotLight         = tag "feSpotLight"
      let feTile              = tag "feTile"
      let feTurbulence        = tag "feTurbulence"
      let filter              = tag "filter"
      let foreignObject       = tag "foreignObject"
      let g                   = tag "g"
      let hatch               = tag "hatch"
      let hatchpath           = tag "hatchpath"
      let image               = tag "image"
      let line                = tag "line"
      let linearGradient      = tag "linearGradient"
      let marker              = tag "marker"
      let mask                = tag "mask"
      let mesh                = tag "mesh"
      let meshgradient        = tag "meshgradient"
      let meshpatch           = tag "meshpatch"
      let meshrow             = tag "meshrow"
      let metadata            = tag "metadata"
      let mpath               = tag "mpath"
      let path                = tag "path"
      let pattern             = tag "pattern"
      let polygon             = tag "polygon"
      let polyline            = tag "polyline"
      let radialGradient      = tag "radialGradient"
      let rect                = tag "rect"
      let script              = tag "script"
      let set                 = tag "set"
      let solidcolor          = tag "solidcolor"
      let stop                = tag "stop"
      let style               = tag "style"
      let svg                 = tag "svg"
      let switch              = tag "switch"
      let symbol              = tag "symbol"
      let text                = tag "text"
      let textPath            = tag "textPath"
      let title               = tag "title"
      let tspan               = tag "tspan"
      let unknown             = tag "unknown"
      let Use                 = tag "use"
      let view                = tag "view"

      [<AutoOpen>]
      module Attributes =
          let _accentHeight               = attr "accent-height"
          let _accumulate                 = attr "accumulate"
          let _additive                   = attr "additive"
          let _alignmentBaseline          = attr "alignment-baseline"
          let _allowReorder               = attr "allowReorder"
          let _alphabetic                 = attr "alphabetic"
          let _amplitude                  = attr "amplitude"
          let _arabicForm                 = attr "arabic-form"
          let _ascent                     = attr "ascent"
          let _attributeName              = attr "attributeName"
          let _attributeType              = attr "attributeType"
          let _autoReverse                = attr "autoReverse"
          let _azimuth                    = attr "azimuth"
          let _baseFrequency              = attr "baseFrequency"
          let _baselineShift              = attr "baseline-shift"
          let _baseProfile                = attr "baseProfile"
          let _bbox                       = attr "bbox"
          let _begin                      = attr "begin"
          let _bias                       = attr "bias"
          let _by                         = attr "by"
          let _calcMode                   = attr "calcMode"
          let _capHeight                  = attr "cap-height"
          let _class                      = attr "class"
          let _clip                       = attr "clip"
          let _clipPathUnits              = attr "clipPathUnits"
          let _clipPath                   = attr "clip-path"
          let _clipRule                   = attr "clip-rule"
          let _color                      = attr "color"
          let _colorInterpolation         = attr "color-interpolation"
          let _colorInterpolationFilters  = attr "color-interpolation-filters"
          let _colorProfile               = attr "color-profile"
          let _colorRendering             = attr "color-rendering"
          let _contentScriptType          = attr "contentScriptType"
          let _contentStyleType           = attr "contentStyleType"
          let _cursor                     = attr "cursor"
          let _cx                         = attr "cx"
          let _cy                         = attr "cy"
          let _d                          = attr "d"
          let _decelerate                 = attr "decelerate"
          let _diffuseConstant            = attr "diffuseConstant"
          let _direction                  = attr "direction"
          let _display                    = attr "display"
          let _divisor                    = attr "divisor"
          let _dominantBaseline           = attr "dominant-baseline"
          let _dur                        = attr "dur"
          let _dx                         = attr "dx"
          let _dy                         = attr "dy"
          let _edgeMode                   = attr "edgeMode"
          let _elevation                  = attr "elevation"
          let _enableBackground           = attr "enable-background"
          let _end                        = attr "end"
          let _exponent                   = attr "exponent"
          let _externalResourcesRequired  = attr "externalResourcesRequired"
          let _fill                       = attr "fill"
          let _fillOpacity                = attr "fill-opacity"
          let _fillRule                   = attr "fill-rule"
          let _filter                     = attr "filter"
          let _filterRes                  = attr "filterRes"
          let _filterUnits                = attr "filterUnits"
          let _floodColor                 = attr "flood-color"
          let _floodOpacity               = attr "flood-opacity"
          let _fontFamily                 = attr "font-family"
          let _fontSize                   = attr "font-size"
          let _fontSizeAdjust             = attr "font-size-adjust"
          let _fontStretch                = attr "font-stretch"
          let _fontStyle                  = attr "font-style"
          let _fontVariant                = attr "font-variant"
          let _fontWeight                 = attr "font-weight"
          let _format                     = attr "format"
          let _from                       = attr "from"
          let _fr                         = attr "fr"
          let _fx                         = attr "fx"
          let _fy                         = attr "fy"
          let _g1                         = attr "g1"
          let _g2                         = attr "g2"
          let _glyphName                  = attr "glyph-name"
          let _glyphOrientationHorizontal = attr "glyph-orientation-horizontal"
          let _glyphOrientationVertical   = attr "glyph-orientation-vertical"
          let _glyphRef                   = attr "glyphRef"
          let _gradientTransform          = attr "gradientTransform"
          let _gradientUnits              = attr "gradientUnits"
          let _hanging                    = attr "hanging"
          let _height                     = attr "height"
          let _href                       = attr "href"
          let _hreflang                   = attr "hreflang"
          let _horizAdvX                  = attr "horiz-adv-x"
          let _horizOriginX               = attr "horiz-origin-x"
          let _id                         = attr "id"
          let _ideographic                = attr "ideographic"
          let _imageRendering             = attr "image-rendering"
          let _in                         = attr "in"
          let _in2                        = attr "in2"
          let _intercept                  = attr "intercept"
          let _k                          = attr "k"
          let _k1                         = attr "k1"
          let _k2                         = attr "k2"
          let _k3                         = attr "k3"
          let _k4                         = attr "k4"
          let _kernelMatrix               = attr "kernelMatrix"
          let _kernelUnitLength           = attr "kernelUnitLength"
          let _kerning                    = attr "kerning"
          let _keyPoints                  = attr "keyPoints"
          let _keySplines                 = attr "keySplines"
          let _keyTimes                   = attr "keyTimes"
          let _lang                       = attr "lang"
          let _lengthAdjust               = attr "lengthAdjust"
          let _letterSpacing              = attr "letter-spacing"
          let _lightingColor              = attr "lighting-color"
          let _limitingConeAngle          = attr "limitingConeAngle"
          let _local                      = attr "local"
          let _markerEnd                  = attr "marker-end"
          let _markerMid                  = attr "marker-mid"
          let _markerStart                = attr "marker-start"
          let _markerHeight               = attr "markerHeight"
          let _markerUnits                = attr "markerUnits"
          let _markerWidth                = attr "markerWidth"
          let _mask                       = attr "mask"
          let _maskContentUnits           = attr "maskContentUnits"
          let _maskUnits                  = attr "maskUnits"
          let _mathematical               = attr "mathematical"
          let _max                        = attr "max"
          let _media                      = attr "media"
          let _method                     = attr "method"
          let _min                        = attr "min"
          let _mode                       = attr "mode"
          let _name                       = attr "name"
          let _offset                     = attr "offset"
          let _opacity                    = attr "opacity"
          let _operator                   = attr "operator"
          let _order                      = attr "order"
          let _orient                     = attr "orient"
          let _orientation                = attr "orientation"
          let _origin                     = attr "origin"
          let _overflow                   = attr "overflow"
          let _overlinePosition           = attr "overline-position"
          let _overlineThickness          = attr "overline-thickness"
          let _panose1                    = attr "panose-1"
          let _paintOrder                 = attr "paint-order"
          let _path                       = attr "path"
          let _pathLength                 = attr "pathLength"
          let _patternContentUnits        = attr "patternContentUnits"
          let _patternTransform           = attr "patternTransform"
          let _patternUnits               = attr "patternUnits"
          let _ping                       = attr "ping"
          let _pointerEvents              = attr "pointer-events"
          let _points                     = attr "points"
          let _pointsAtX                  = attr "pointsAtX"
          let _pointsAtY                  = attr "pointsAtY"
          let _pointsAtZ                  = attr "pointsAtZ"
          let _preserveAlpha              = attr "preserveAlpha"
          let _preserveAspectRatio        = attr "preserveAspectRatio"
          let _primitiveUnits             = attr "primitiveUnits"
          let _r                          = attr "r"
          let _radius                     = attr "radius"
          let _referrerPolicy             = attr "referrerPolicy"
          let _refX                       = attr "refX"
          let _refY                       = attr "refY"
          let _rel                        = attr "rel"
          let _renderingIntent            = attr "rendering-intent"
          let _repeatCount                = attr "repeatCount"
          let _repeatDur                  = attr "repeatDur"
          let _requiredExtensions         = attr "requiredExtensions"
          let _requiredFeatures           = attr "requiredFeatures"
          let _restart                    = attr "restart"
          let _result                     = attr "result"
          let _rotate                     = attr "rotate"
          let _rx                         = attr "rx"
          let _ry                         = attr "ry"
          let _scale                      = attr "scale"
          let _seed                       = attr "seed"
          let _shapeRendering             = attr "shape-rendering"
          let _slope                      = attr "slope"
          let _spacing                    = attr "spacing"
          let _specularConstant           = attr "specularConstant"
          let _specularExponent           = attr "specularExponent"
          let _speed                      = attr "speed"
          let _spreadMethod               = attr "spreadMethod"
          let _startOffset                = attr "startOffet"
          let _stdDeviation               = attr "stdDeviation"
          let _stemh                      = attr "stemh"
          let _stemv                      = attr "stemv"
          let _stitchTiles                = attr "stitchTiles"
          let _stopColor                  = attr "stop-color"
          let _stopOpacity                = attr "stop-opacity"
          let _strikethroughPosition      = attr "strikethrough-position"
          let _strikethroughThickness     = attr "strikethrough-thickness"
          let _string                     = attr "string"
          let _stroke                     = attr "stroke"
          let _strokeDasharray            = attr "stroke-dasharray"
          let _strokeDashoffset           = attr "stroke-dashoffset"
          let _strokeLinecap              = attr "stroke-linecap"
          let _strokeLinejoin             = attr "stroke-linejoin"
          let _strokeMiterlimit           = attr "stroke-miterlimit"
          let _strokeOpacity              = attr "stroke-opacity"
          let _strokeWidth                = attr "stroke-width"
          let _style                      = attr "style"
          let _surfaceScale               = attr "surfaceScale"
          let _systemLanguage             = attr "systemLanguage"
          let _tabindex                   = attr "tabindex"
          let _tableValues                = attr "tableValues"
          let _target                     = attr "target"
          let _targetX                    = attr "targetX"
          let _targetY                    = attr "targetY"
          let _textAnchor                 = attr "text-anchor"
          let _textDecoration             = attr "text-decoration"
          let _textRendering              = attr "text-rendering"
          let _textLength                 = attr "textLength"
          let _to                         = attr "to"
          let _type                       = attr "type"
          let _u1                         = attr "u1"
          let _u2                         = attr "u2"
          let _underlinePosition          = attr "underline-position"
          let _underlineThickness         = attr "underline-thickness"
          let _unicode                    = attr "unicode"
          let _unicodeBidi                = attr "unicode-bidi"
          let _unicodeRange               = attr "unicode-range"
          let _unitsPerEm                 = attr "units-per-em"
          let _vAlphabetic                = attr "v-alphabetic"
          let _vHanging                   = attr "v-hanging"
          let _vIdeographic               = attr "v-ideographic"
          let _vMathematical              = attr "v-mathematical"
          let _values                     = attr "values"
          let _vectorEffect               = attr "vector-effect"
          let _version                    = attr "version"
          let _vertAdvY                   = attr "vert-adv-y"
          let _vertOriginX                = attr "vert-origin-x"
          let _vertOriginY                = attr "vert-origin-y"
          let _viewBox                    = attr "viewBox"
          let _viewTarget                 = attr "viewTarget"
          let _visibility                 = attr "visibility"
          let _width                      = attr "width"
          let _widths                     = attr "widths"
          let _wordSpacing                = attr "word-spacing"
          let _writingMode                = attr "writing-mode"
          let _x                          = attr "x"
          let _xHeight                    = attr "x-height"
          let _x1                         = attr "x1"
          let _x2                         = attr "x2"
          let _xChannelSelector           = attr "xChannelSelector"
          let _xlinkActuate               = attr "xlink:actuate"
          let _xlinkArcrole               = attr "xlink:arcrole"
          let _xlinkHref                  = attr "xlink:href"
          let _xlinkRole                  = attr "xlink:role"
          let _xlinkShow                  = attr "xlink:show"
          let _xlinkTitle                 = attr "xlink:title"
          let _xlinkType                  = attr "xlink:type"
          let _xmlns                      = attr "xmlns"
          let _xmlBase                    = attr "xml:base"
          let _xmlLang                    = attr "xml:lang"
          let _xmlSpace                   = attr "xml:space"
          let _y                          = attr "y"
          let _y1                         = attr "y1"
          let _y2                         = attr "y2"
          let _yChannelSelector           = attr "yChannelSelector"
          let _z                          = attr "z"
          let _zoomAndPan                 = attr "zoomAndPan"
