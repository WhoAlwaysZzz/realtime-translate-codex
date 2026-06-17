namespace RealtimeTranslate.Core.Stats;

public interface IWordQueryCounter
{
    int Increment(string word);
}
