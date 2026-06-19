using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Recording;

public interface ITextExplanationRecorder
{
    void Record(TextExplanationRecord record);
}
