using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Recording;

public interface ILookupRecorder
{
    void Record(LookupRecord record);
}
