using HardwareEventDesigner.Runtime;

namespace HardwareEventDesigner.Runtime
{
    public interface IHardwareInputProvider
    {
        float GetValue(string channelId);
    }
}


