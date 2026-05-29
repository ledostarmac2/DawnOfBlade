namespace DawnOfBlade.Learning;

public sealed record VocabularyEntry(
    string Term,
    string Translation,
    string Transliteration,
    string PartOfSpeech,
    string ExampleSentence,
    int Difficulty
);

