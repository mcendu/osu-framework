// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Framework.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A glyph store for outline fonts which caches rasterized glyphs to the disk.
    /// </summary>
    public class CachingOutlineGlyphStore : OutlineGlyphStore, ICachingGlyphStore
    {
        /// <summary>
        /// A storage backing to be used for storing rasterized glyphs.
        /// </summary>
        internal Storage? CacheStorage { get; set; }

        Storage? ICachingGlyphStore.CacheStorage
        {
            get => CacheStorage;
            set => CacheStorage = value;
        }

        /// <summary>
        /// Create a glyph store for a font using the specified OpenType named instance.
        /// </summary>
        /// <param name="font">The underlying font.</param>
        /// <param name="namedInstance">The named instance to select.</param>
        /// <param name="nameOverride">
        /// The value of <see cref="OutlineGlyphStore.FontName"/>. If null, <paramref name="namedInstance"/> will be used.
        /// </param>
        public CachingOutlineGlyphStore(OutlineFont font, string namedInstance, string? nameOverride = null)
            : base(font, new FontVariation { NamedInstance = namedInstance }, nameOverride)
        {
        }

        /// <summary>
        /// Create a glyph store for a font using the specified OpenType variation parameters.
        /// </summary>
        /// <param name="font">The underlying font.</param>
        /// <param name="variation">The font variation parameters.</param>
        /// <param name="nameOverride">
        /// The value of <see cref="OutlineGlyphStore.FontName"/>. If null, it will be computed using a naming scheme based on
        /// <see href="https://download.macromedia.com/pub/developer/opentype/tech-notes/5902.AdobePSNameGeneration.html"/>.
        /// </param>
        public CachingOutlineGlyphStore(OutlineFont font, FontVariation? variation = null, string? nameOverride = null)
            : base(font, variation, nameOverride)
        {
        }

        /// <summary>
        /// Load a new font and create a glyph store for it.
        /// </summary>
        /// <param name="store">The font's resource store.</param>
        /// <param name="assetName">The asset name of the font.</param>
        public CachingOutlineGlyphStore(IResourceStore<byte[]> store, string assetName)
            : base(store, assetName)
        {
        }

        protected override TextureUpload? GetCachedGlyph(uint glyph)
        {
            if (CacheStorage == null)
                throw new InvalidOperationException($"{nameof(CacheStorage)} should be set before requesting characters.");

            string accessFilename = @$"{FontName.ComputeSHA2Hash()}#{glyph:x8}";

            // Finding an existing file validates that the file both exists on disk, and was generated for the correct font.
            // It doesn't guarantee that the generated cache file is in a good state.
            string? existing = CacheStorage.GetFiles(string.Empty, $"{accessFilename}*").FirstOrDefault();

            if (existing != null)
            {
                // Filename format is "filenameHashMD5#contentHashMD5#width#height"
                string[] split = existing.Split('#');

                if (split.Length == 4
                    && int.TryParse(split[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                    && int.TryParse(split[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                {
                    // Sanity check that the length of the file is expected, based on the width and height.
                    // If we ever see corrupt files in the wild, this should be changed to a full md5 check. Hopefully it will never happen.
                    using (var testStream = CacheStorage.GetStream(existing))
                    {
                        if (testStream.Length == width * height)
                        {
                            Memory<byte> data = new byte[width * height];
                            var image = new Image<Rgba32>(width, height);
                            testStream.ReadExactly(data.Span);

                            if (image.DangerousTryGetSinglePixelMemory(out var destination))
                            {
                                PixelOperations<A8>.Instance.To(SixLabors.ImageSharp.Configuration.Default,
                                    MemoryMarshal.Cast<byte, A8>(data.Span), destination.Span);
                                image.Mutate(ctx => ctx.Filter(new ColorMatrix(
                                    0, 0, 0, 0,
                                    0, 0, 0, 0,
                                    0, 0, 0, 0,
                                    0, 0, 0, 1,
                                    1, 1, 1, 0
                                )));
                                return new TextureUpload(image);
                            }
                        }
                    }
                }
            }

            return null;
        }

        protected override void CacheGlyph(uint glyph, TextureUpload rendered)
        {
            if (CacheStorage == null)
                throw new InvalidOperationException($"{nameof(CacheStorage)} should be set before requesting characters.");

            Span<A8> cached = new A8[rendered.Width * rendered.Height];
            PixelOperations<Rgba32>.Instance.To(SixLabors.ImageSharp.Configuration.Default, rendered.Data, cached);

            string filename = @$"{FontName.ComputeSHA2Hash()}#{glyph:x8}#{rendered.Width}#{rendered.Height}";

            using (var stream = CacheStorage.CreateFileSafely(filename))
                stream.Write(MemoryMarshal.AsBytes(cached));
        }
    }
}
