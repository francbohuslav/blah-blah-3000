using System.Runtime.InteropServices;
using System.Text;

namespace BlahBlah3000.Whisper;

/// <summary>A Vulkan device in the order the Vulkan loader lists it (the same order whisper.cpp numbers its GPUs).</summary>
internal sealed record GpuDevice(int Index, string Name, bool Discrete);

/// <summary>Lists the Vulkan devices and picks the one Whisper should use: the chosen one, otherwise a discrete GPU before an integrated one.</summary>
internal static class GpuDevices
{
    private const int VkStructureTypeInstanceCreateInfo = 1;
    private const int VkPhysicalDeviceTypeDiscrete = 2;

    // VkPhysicalDeviceProperties: apiVersion, driverVersion, vendorID, deviceID (4 x 4 B), deviceType (4 B), deviceName (256 B), ...
    private const int DeviceTypeOffset = 16;
    private const int DeviceNameOffset = 20;
    private const int DeviceNameLength = 256;
    private const int PropertiesSize = 1024;
    private const int InstanceCreateInfoSize = 64;

    [DllImport("vulkan-1.dll")]
    private static extern int vkCreateInstance(IntPtr createInfo, IntPtr allocator, out IntPtr instance);

    [DllImport("vulkan-1.dll")]
    private static extern int vkEnumeratePhysicalDevices(IntPtr instance, ref uint count, [In, Out] IntPtr[]? devices);

    [DllImport("vulkan-1.dll")]
    private static extern void vkGetPhysicalDeviceProperties(IntPtr device, IntPtr properties);

    [DllImport("vulkan-1.dll")]
    private static extern void vkDestroyInstance(IntPtr instance, IntPtr allocator);

    private static IReadOnlyList<GpuDevice>? _cache;

    /// <summary>All Vulkan devices; empty when Vulkan is not installed (then Whisper runs on the CPU).</summary>
    public static IReadOnlyList<GpuDevice> List()
    {
        return _cache ??= Enumerate();
    }

    /// <summary>The device index for <paramref name="chosenName"/> (empty = automatic) and a text for the log; -1 when there is no device.</summary>
    public static int Resolve(string chosenName, out string description)
    {
        var devices = List();
        if (devices.Count == 0)
        {
            description = "no Vulkan device found";
            return -1;
        }
        var chosen = chosenName.Length > 0 ? devices.FirstOrDefault(d => d.Name == chosenName) : null;
        if (chosen != null)
        {
            description = $"{chosen.Name} (chosen in the settings)";
            return chosen.Index;
        }
        var auto = devices.FirstOrDefault(d => d.Discrete) ?? devices[0];
        description = chosenName.Length > 0
            ? $"{auto.Name} (automatic; the chosen \"{chosenName}\" was not found)"
            : $"{auto.Name} (automatic)";
        return auto.Index;
    }

    private static List<GpuDevice> Enumerate()
    {
        var result = new List<GpuDevice>();
        var info = Marshal.AllocHGlobal(InstanceCreateInfoSize);
        var properties = Marshal.AllocHGlobal(PropertiesSize);
        var instance = IntPtr.Zero;
        try
        {
            for (var i = 0; i < InstanceCreateInfoSize; i += 4) Marshal.WriteInt32(info, i, 0);
            Marshal.WriteInt32(info, 0, VkStructureTypeInstanceCreateInfo);
            if (vkCreateInstance(info, IntPtr.Zero, out instance) != 0) return result;

            uint count = 0;
            if (vkEnumeratePhysicalDevices(instance, ref count, null) != 0 || count == 0) return result;
            var handles = new IntPtr[count];
            if (vkEnumeratePhysicalDevices(instance, ref count, handles) < 0) return result;

            for (var i = 0; i < count; i++)
            {
                vkGetPhysicalDeviceProperties(handles[i], properties);
                var type = Marshal.ReadInt32(properties, DeviceTypeOffset);
                var raw = new byte[DeviceNameLength];
                Marshal.Copy(properties + DeviceNameOffset, raw, 0, DeviceNameLength);
                var end = Array.IndexOf(raw, (byte)0);
                var name = Encoding.UTF8.GetString(raw, 0, end < 0 ? raw.Length : end);
                result.Add(new GpuDevice(i, name, type == VkPhysicalDeviceTypeDiscrete));
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Listing the Vulkan devices failed: {ex.Message}");
        }
        finally
        {
            if (instance != IntPtr.Zero) vkDestroyInstance(instance, IntPtr.Zero);
            Marshal.FreeHGlobal(info);
            Marshal.FreeHGlobal(properties);
        }
        return result;
    }
}
