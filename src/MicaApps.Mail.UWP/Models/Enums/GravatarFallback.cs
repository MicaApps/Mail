#nullable enable
using Mail;

namespace Mail.Models.Enums;

/// <summary>
/// Email avatar fallback kind, used for <see cref="EmailToAvatarConverter"/>
/// </summary>
public enum GravatarFallback
{
    /// <summary>
    /// do not load any image
    /// </summary>
    None,

    /// <summary>
    /// a simple, cartoon-style silhouetted outline of a person (does not vary by email hash)
    /// </summary>
    MysteryPerson,

    /// <summary>
    /// a geometric pattern based on an email hash
    /// </summary>
    Identicon,

    /// <summary>
    /// a generated ‘monster’ with different colors, faces, etc
    /// </summary>
    Monsterid,

    /// <summary>
    /// generated faces with differing features and backgrounds
    /// </summary>
    Wavatar,

    /// <summary>
    /// awesome generated, 8-bit arcade-style pixelated faces
    /// </summary>
    Retro,

    /// <summary>
    /// a generated robot with different colors, faces, etc
    /// </summary>
    Robohash,

    /// <summary>
    /// a transparent PNG image (border added to HTML below for demonstration purposes)
    /// </summary>
    Blank
}