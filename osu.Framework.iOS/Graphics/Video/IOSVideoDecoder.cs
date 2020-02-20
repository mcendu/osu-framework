// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.iOS.Graphics.Video
{
    // ReSharper disable InconsistentNaming
    public unsafe class IOSVideoDecoder : VideoDecoder
    {
        private const string dll_name = "__Internal";

        [DllImport(dll_name)]
        private static extern AVFrame* av_frame_alloc();

        [DllImport(dll_name)]
        private static extern void av_frame_free(AVFrame** frame);

        [DllImport(dll_name)]
        private static extern void av_frame_unref(AVFrame* frame);

        [DllImport(dll_name)]
        private static extern byte* av_strdup(string s);

        [DllImport(dll_name)]
        private static extern void* av_malloc(ulong size);

        [DllImport(dll_name)]
        private static extern AVPacket* av_packet_alloc();

        [DllImport(dll_name)]
        private static extern void av_packet_unref(AVPacket* pkt);

        [DllImport(dll_name)]
        private static extern void av_packet_free(AVPacket** pkt);

        [DllImport(dll_name)]
        private static extern int av_read_frame(AVFormatContext* s, AVPacket* pkt);

        [DllImport(dll_name)]
        private static extern int av_seek_frame(AVFormatContext* s, int stream_index, long timestamp, int flags);

        [DllImport(dll_name)]
        private static extern AVCodec* avcodec_find_decoder(AVCodecID id);

        [DllImport(dll_name)]
        private static extern int avcodec_open2(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern int avcodec_receive_frame(AVCodecContext* avctx, AVFrame* frame);

        [DllImport(dll_name)]
        private static extern int avcodec_send_packet(AVCodecContext* avctx, AVPacket* avpkt);

        [DllImport(dll_name)]
        private static extern AVFormatContext* avformat_alloc_context();

        [DllImport(dll_name)]
        private static extern void avformat_close_input(AVFormatContext** s);

        [DllImport(dll_name)]
        private static extern int avformat_find_stream_info(AVFormatContext* ic, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern int avformat_open_input(AVFormatContext** ps, [MarshalAs((UnmanagedType)48)] string url, AVInputFormat* fmt, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern AVIOContext* avio_alloc_context(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        [DllImport(dll_name)]
        private static extern AVFilter* avfilter_get_by_name(string name);

        [DllImport(dll_name)]
        private static extern AVFilterInOut* avfilter_inout_alloc();

        [DllImport(dll_name)]
        private static extern void avfilter_graph_free(AVFilterGraph** graph);

        [DllImport(dll_name)]
        private static extern int avfilter_graph_create_filter(AVFilterContext** filt_ctx, AVFilter* filt, string name, string args, void* opaque, AVFilterGraph* graph_ctx);

        [DllImport(dll_name)]
        private static extern AVFilterGraph* avfilter_graph_alloc();

        [DllImport(dll_name)]
        private static extern int avfilter_graph_parse_ptr(AVFilterGraph* graph, string filters, AVFilterInOut** inputs, AVFilterInOut** outputs, void* log_ctx);

        [DllImport(dll_name)]
        private static extern int avfilter_graph_config(AVFilterGraph* graphctx, void* log_ctx);

        [DllImport(dll_name)]
        private static extern int av_buffersrc_add_frame_flags(AVFilterContext* buffer_src, AVFrame* frame, int flags);

        [DllImport(dll_name)]
        private static extern int av_buffersink_get_frame(AVFilterContext* ctx, AVFrame* frame);

        public IOSVideoDecoder(string filename, Scheduler scheduler)
            : base(filename, scheduler)
        {
        }

        public IOSVideoDecoder(Stream videoStream, Scheduler scheduler)
            : base(videoStream, scheduler)
        {
        }

        protected override FFmpegFuncs CreateFuncs() => new FFmpegFuncs
        {
            av_frame_alloc = av_frame_alloc,
            av_frame_free = av_frame_free,
            av_frame_unref = av_frame_unref,
            av_strdup = av_strdup,
            av_malloc = av_malloc,
            av_packet_alloc = av_packet_alloc,
            av_packet_unref = av_packet_unref,
            av_packet_free = av_packet_free,
            av_read_frame = av_read_frame,
            av_seek_frame = av_seek_frame,
            avcodec_find_decoder = avcodec_find_decoder,
            avcodec_open2 = avcodec_open2,
            avcodec_receive_frame = avcodec_receive_frame,
            avcodec_send_packet = avcodec_send_packet,
            avformat_alloc_context = avformat_alloc_context,
            avformat_close_input = avformat_close_input,
            avformat_find_stream_info = avformat_find_stream_info,
            avformat_open_input = avformat_open_input,
            avio_alloc_context = avio_alloc_context,
            avfilter_get_by_name = avfilter_get_by_name,
            avfilter_inout_alloc = avfilter_inout_alloc,
            avfilter_graph_free = avfilter_graph_free,
            avfilter_graph_create_filter = avfilter_graph_create_filter,
            avfilter_graph_alloc = avfilter_graph_alloc,
            avfilter_graph_parse_ptr = avfilter_graph_parse_ptr,
            avfilter_graph_config = avfilter_graph_config,
            av_buffersrc_add_frame_flags = av_buffersrc_add_frame_flags,
            av_buffersink_get_frame = av_buffersink_get_frame,
        };
    }
}
