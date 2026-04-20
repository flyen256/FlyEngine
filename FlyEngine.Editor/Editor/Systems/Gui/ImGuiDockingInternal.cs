using System.Runtime.InteropServices;

namespace FlyEngine.Editor.Systems.Gui;

public static class ImGuiDockingInternal
{
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igDockBuilderAddNode(uint node_id, int flags);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDockBuilderRemoveNode(uint node_id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe uint igDockBuilderSplitNode(uint node_id, int split_dir, float size_ratio_for_node_at_dir, uint* out_id_at_dir, uint* out_id_at_opposite_dir);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDockBuilderDockWindow(string window_name, uint node_id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDockBuilderFinish(uint node_id);
}