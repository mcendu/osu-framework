// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// Indicates that a glyph store caches data on disk.
    /// </summary>
    internal interface ICachingGlyphStore
    {
        /// <summary>
        /// A storage backing to be used for storing data.
        /// </summary>
        Storage? CacheStorage { get; set; }
    }
}
